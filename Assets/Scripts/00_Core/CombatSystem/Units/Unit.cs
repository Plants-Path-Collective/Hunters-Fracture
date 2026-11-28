using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Core.CombatSystem.SkillSystem;


namespace Core.CombatSystem.Units
{
    [RequireComponent(typeof(UnitInventory), typeof(UnitStatsController), typeof(UnitEffectController))]
    public class Unit : MonoBehaviour
    {
        private const float damageAnimationDuration = 0.22f;
        internal const float DeathAnimationDuration = 0.35f;
        internal const float PhysicalAttackAnimationDuration = 0.35f;

        [SerializeField] private UnitDefinitionSO definition;
        [SerializeField, Tooltip("Resonance charge indicators, ordered from the first charge to the last.")]
        private GameObject[] resonanceChargeIndicators = new GameObject[3];

        private Transform animationTarget;
        private Vector3 animationTargetBaseScale;
        private Quaternion animationTargetBaseRotation;
        private Tween animationTween;
        private Tween physicalAttackTween;
        private bool deathAnimationPlayed;

        public UnitDefinitionSO Definition => definition;

        public CombatController Combat { get; private set; }

        // ----- Identity -----
        public string Name { get; private set; }
        public UNITY_TYPE UnitType { get; private set; }
        public UNIT_TEAM Team { get; private set; }
        public Sprite Portrait { get; private set; }
        public string Description { get; private set; }

        // ----- Skills -----

        /// <summary>Skills of this Unit in combat. Cloned from the definition so effects can swap them
        /// temporarily without touching the shared asset.</summary>
        public List<ActionSO> Skills { get; private set; } = new();
        public ActionSO Ultimate { get; private set; }

        // ----- Item-driven modifiers (keyed by source so they can be replaced, never accumulated) -----
        private readonly Dictionary<object, float> spCostReductions = new();
        private readonly Dictionary<object, float> aggroBonuses = new();

        public void SetSpCostReduction(object source, float fraction) => spCostReductions[source] = fraction;
        public void ClearSpCostReduction(object source) => spCostReductions.Remove(source);
        public void SetAggroBonus(object source, float bonus) => aggroBonuses[source] = bonus;
        public void ClearAggroBonus(object source) => aggroBonuses.Remove(source);

        internal void SetResonanceCharges(int charges)
        {
            for (int i = 0; i < resonanceChargeIndicators.Length; i++)
            {
                GameObject indicator = resonanceChargeIndicators[i];
                if (indicator != null)
                    indicator.SetActive(i < charges);
            }
        }

        /// <summary>SP cost of an action for this unit, after item reductions.</summary>
        public int GetEffectiveSpCost(ActionSO action)
        {
            float reduction = 0f;
            foreach (float value in spCostReductions.Values) reduction += value;

            return Mathf.Max(0, Mathf.RoundToInt(action.spCost * (1f - Mathf.Clamp01(reduction))));
        }

        /// <summary>Relative chance of being picked as a target. 1 = default.</summary>
        public float AggroWeight
        {
            get
            {
                float weight = 1f;
                foreach (float bonus in aggroBonuses.Values) weight += bonus;
                return Mathf.Max(0f, weight);
            }
        }

        /// <summary>Current value of any of the 7 derived stats (items and statuses included). HP and SP
        /// return the maximums.</summary>
        public float GetStat(STAT_TYPE stat) => stat switch
        {
            STAT_TYPE.HP => MaxHP,
            STAT_TYPE.SP => MaxSP,
            STAT_TYPE.Speed => Speed,
            STAT_TYPE.Strength => Strength,
            STAT_TYPE.MagicPower => MagicPower,
            STAT_TYPE.PhysicalDefense => PhysicalDefense,
            STAT_TYPE.MagicalDefense => MagicalDefense,
            _ => 0f
        };

        // ----- Stats -----
        public bool IsAlive => HP > 0;

        // ----- Resource events -----

        /// <summary>Fired whenever HP or SP change, and whenever stats are recalculated (MaxHP/MaxSP may
        /// have changed). Subscribers read HP/MaxHP/SP/MaxSP from the Unit; bars bind to this per Unit.</summary>
        public event Action<Unit> OnResourcesChanged;

        private int hp;
        private int sp;

        // Current HP/SP are the only stats Unit actually owns — they're persisted state, not
        // derived. Only CombatController should write these (Kill/Revive/DealDamage/Heal/
        // SpendSP/RestoreSP).
        public int HP
        {
            get => hp;
            internal set
            {
                if (hp == value) return;
                hp = value;
                OnResourcesChanged?.Invoke(this);
            }
        }

        public int SP
        {
            get => sp;
            internal set
            {
                if (sp == value) return;
                sp = value;
                OnResourcesChanged?.Invoke(this);
            }
        }

        /// <summary>True if the Unit is currently guarding. Guarding is a temporary state that
        /// lasts for a number of turns, and is set by CombatController.Defend(). While
        /// defending, the Unit's effective defense stats are multiplied by UnitDefinitionSO.defendMultiplier
        /// (see EffectivePhysicalDefense / EffectiveMagicalDefense). Guarding is not a permanent
        /// state, and is reset to false at the start of any Unit's turn. Guarding is not a status effect, 
        /// and is not affected by buffs or debuffs.
        /// Multiplies the matching defense stat by UnitDefinitionSO.defendMultiplier
        /// (see EffectivePhysicalDefense / EffectiveMagicalDefense).</summary>
        public bool isDefending => defendingTurnsRemaining > 0;

        internal void SetDefending(int turns, IndicatorSO indicator = null)
        {
            defendingTurnsRemaining = turns;
            if (indicator != null) SetIndicator(indicator);
        }

        internal void TickDefending()
        {
            if (defendingTurnsRemaining > 0)
                defendingTurnsRemaining--;
        }

        // The 7 derived stats are owned by UnitStatsController, not copied here — this is a
        // pure pass-through, so RecalculateStats() (item pickups, buffs, etc. down the line)
        // is reflected everywhere automatically instead of going stale on a value copied once
        // at initialization.
        public int MaxHP => StatsController.MaxHP;
        public int MaxSP => StatsController.MaxSP;
        public float Speed => StatsController.Speed;
        public float Strength => StatsController.Strength;
        public float MagicPower => StatsController.MagicPower;
        public float PhysicalDefense => StatsController.PhysicalDefense;
        public float MagicalDefense => StatsController.MagicalDefense;

        /// <summary>Physical defense including the Defending multiplier. Use this (not PhysicalDefense)
        /// whenever damage is being mitigated.</summary>
        public float EffectivePhysicalDefense =>
            isDefending ? PhysicalDefense * Definition.defendMultiplier : PhysicalDefense;

        /// <summary>Magical defense including the Defending multiplier.</summary>
        public float EffectiveMagicalDefense =>
            isDefending ? MagicalDefense * Definition.defendMultiplier : MagicalDefense;

        public event Action<Unit> OnStatusesChanged;
        internal void NotifyStatusesChanged() => OnStatusesChanged?.Invoke(this);

        // ----- Indicators (icons that are not statuses) -----
        private readonly List<IndicatorEntry> indicators = new();
        public IReadOnlyList<IndicatorEntry> Indicators => indicators;
        public event Action<Unit> OnIndicatorsChanged;
        
        // ----- Fury (secondary resource) -----
        private int fury;

        /// <summary>Capacity of the Fury meter. 0 means this unit has none.</summary>
        public int MaxFury { get; internal set; }

        /// <summary>Current Fury. Only CombatController should write it (AddFury / SpendFury).</summary>
        public int Fury
        {
            get => fury;
            internal set
            {
                value = Mathf.Clamp(value, 0, MaxFury);
                if (fury == value) return;
                fury = value;
                OnResourcesChanged?.Invoke(this);
            }
        }

        /// <summary>Alternate form the unit is currently in, or null.</summary>
        public UnitFormSO ActiveForm { get; internal set; }

        /// <summary>HP the unit had when it entered its form.</summary>
        internal int PreFormHP { get; set; }

        /// <summary>Fired when the unit enters or leaves an alternate form.</summary>
        public event Action<Unit> OnFormChanged;

        internal void NotifyFormChanged() => OnFormChanged?.Invoke(this);

        internal void SetUnitType(UNITY_TYPE type) => UnitType = type;

        /// <summary>Replaces skills and ultimate (entering a form).</summary>
        internal void SetKit(IReadOnlyList<ActionSO> skills, ActionSO ultimate)
        {
            Skills = skills != null ? new List<ActionSO>(skills) : new List<ActionSO>();
            Ultimate = ultimate;
        }

        /// <summary>Back to the definition's type, skills and ultimate (leaving a form).</summary>
        internal void RestoreBaseKit()
        {
            UnitType = definition.type;
            Skills = definition.skills != null ? new List<ActionSO>(definition.skills) : new List<ActionSO>();
            Ultimate = definition.ultimate;
        }

        // ----- References -----
        public UnitInventory Inventory { get; private set; }
        public UnitEffectController EffectController { get; private set; }
        public UnitStatsController StatsController { get; private set; }

        /// <summary>Position label of this Unit (Mid/Left/Right) within its team. Comes from its index in
        /// Party/EnemyParty (see CombatSlots.TryFromIndex) and picks its anchors in CombatController.
        /// Null until CombatController assigns it (e.g. a Unit created outside StartFromSetUp, like the
        /// old debug-only path).</summary>
        public COMBAT_SLOT? Slot { get; internal set; }

        /// <summary>
        /// turns that this unit can still defend for. 
        /// Decremented at the start of any unit's turn, 
        /// isDefending turns to false when it reaches 0.
        /// </summary>
        private int defendingTurnsRemaining;

        private void Awake()
        {
            Inventory = GetComponent<UnitInventory>();
            EffectController = GetComponent<UnitEffectController>();
            StatsController = GetComponent<UnitStatsController>();

            animationTarget = transform.Find("[Visuals]");
            if (animationTarget == null)
                animationTarget = transform;
            animationTargetBaseScale = animationTarget.localScale;
            animationTargetBaseRotation = animationTarget.localRotation;

            StatsController.OnStatsRecalculated.AddListener(() => OnResourcesChanged?.Invoke(this));
        }

        internal void PlayPhysicalAttackAnimation()
        {
            if (deathAnimationPlayed || animationTarget == null) return;

            physicalAttackTween?.Kill();
            animationTarget.localRotation = animationTargetBaseRotation;
            physicalAttackTween = animationTarget
                .DOLocalRotate(Vector3.up * 360f, PhysicalAttackAnimationDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }

        internal void PlayDamageAnimation()
        {
            if (deathAnimationPlayed || animationTarget == null) return;

            animationTween?.Kill();
            animationTarget.localScale = animationTargetBaseScale;
            animationTween = animationTarget
                .DOPunchScale(animationTargetBaseScale * 0.12f, damageAnimationDuration, 5, 0.65f)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }

        internal float PlayDeathAnimation()
        {
            if (deathAnimationPlayed) return 0f;
            deathAnimationPlayed = true;

            animationTween?.Kill();
            physicalAttackTween?.Kill();
            if (animationTarget == null) return 0f;

            animationTarget.localScale = animationTargetBaseScale;
            animationTarget.localRotation = animationTargetBaseRotation;
            animationTween = animationTarget
                .DOScale(Vector3.zero, DeathAnimationDuration)
                .SetEase(Ease.InCubic)
                .SetLink(gameObject);
            return DeathAnimationDuration;
        }

        internal void ResetAnimation()
        {
            animationTween?.Kill();
            physicalAttackTween?.Kill();
            if (animationTarget != null)
            {
                animationTarget.localScale = animationTargetBaseScale;
                animationTarget.localRotation = animationTargetBaseRotation;
            }
            deathAnimationPlayed = false;
        }

        /// <summary>
        /// team is an explicit parameter and always wins over definition.team — the definition's
        /// own team field is just a reference/default for a future character-sheet inspector,
        /// not something that should lock a UnitDefinitionSO to one side forever.
        /// </summary>
        public void InitializeFromDefinition(UnitDefinitionSO definition, UNIT_TEAM team)
        {
            SetIdentity(definition, team);

            StatsController.SetBase(definition);
            StatsController.RecalculateStats(Inventory);

            HP = MaxHP;
            SP = MaxSP;
        }

        /// <summary>
        /// Called by CombatController.StartCombat() once this Unit is part of the roster —
        /// separate from InitializeFromDefinition() because identity/stats are set once, but
        /// which CombatController a Unit is fighting under is fresh each encounter. Triggers
        /// UnitEffectController's first reconcile pass, now that TriggeredEffectSO.Attach() can
        /// reach unit.Combat to subscribe to its events.
        /// </summary>
        public void EnterCombat(CombatController combat)
        {
            Combat = combat;
            EffectController.Initialize(this);
        }

        /// <summary>Releases the combat link: detaches all triggered effects and drops the controller reference.</summary>
        public void ExitCombat()
        {
            if (EffectController != null)
                EffectController.DetachAll();

            Combat = null;
        }

        private void SetIdentity(UnitDefinitionSO definition, UNIT_TEAM team)
        {
            this.definition = definition;

            Name = definition.unitName;
            UnitType = definition.type;
            Team = team;
            Portrait = definition.portrait;
            Description = definition.description;

            Skills = definition.skills != null ? new List<ActionSO>(definition.skills) : new List<ActionSO>();
            Ultimate = definition.ultimate;
        }

        /// <summary>
        /// Builds a combat Unit from persistent party data. Inventory goes in first so the stat
        /// recompute inside InitializeFromDefinition already includes item modifiers. HP/SP are
        /// clamped to the recalculated max; -1 means "full".
        /// </summary>
        public void InitializeFromParty(PartyMemberData member)
        {
            if (member == null || member.definition == null)
            {
                Debug.LogError($"[Unit] {gameObject.name}: PartyMemberData or its definition is null.");
                return;
            }

            StatsController.SetBase(member.definition);
            Inventory.Populate(member.inventorySnapshot);
            SetIdentity(member.definition, UNIT_TEAM.Ally);

            HP = member.currentHP < 0 ? MaxHP : Mathf.Min(member.currentHP, MaxHP);
            SP = member.currentSP < 0 ? MaxSP : Mathf.Min(member.currentSP, MaxSP);
        }



        /// <summary>
        /// Builds a Unit at full HP/SP, with an optional starting inventory (elite enemies).
        /// </summary>
        public void InitializeFresh(UnitDefinitionSO definition, IReadOnlyDictionary<int, int> snapshot = null, 
        UNIT_TEAM team = UNIT_TEAM.Enemy)
        {
            if (definition == null)
            {
                Debug.LogError($"[Unit] {gameObject.name}: definition is null.");
                return;
            }

            StatsController.SetBase(definition);
            Inventory.Populate(snapshot);
            SetIdentity(definition, team);

            HP = MaxHP;
            SP = MaxSP;
        }

         public void SetIndicator(IndicatorSO definition, int count = 0)
        {
            if (definition == null) return;
            int i = IndexOfIndicator(definition);
            if (i >= 0)
            {
                if (indicators[i].count == count) return;
                indicators[i] = new IndicatorEntry(definition, count);
            }
            else indicators.Add(new IndicatorEntry(definition, count));
            OnIndicatorsChanged?.Invoke(this);
        }

        public void AddToIndicator(IndicatorSO definition, int amount)
        {
            if (definition == null) return;
            int i = IndexOfIndicator(definition);
            SetIndicator(definition, (i >= 0 ? indicators[i].count : 0) + amount);
        }

        public void ClearIndicator(IndicatorSO definition)
        {
            int i = IndexOfIndicator(definition);
            if (i < 0) return;
            indicators.RemoveAt(i);
            OnIndicatorsChanged?.Invoke(this);
        }

        internal void ClearIndicators()
        {
            if (indicators.Count == 0) return;
            indicators.Clear();
            OnIndicatorsChanged?.Invoke(this);
        }

        /// <summary>Drops the turn-scoped markers: the ones that end when the turn starts (turnEnded = false)
        /// or the ones that last through the turn and end when it closes (turnEnded = true).</summary>
        internal void ExpireTurnIndicators(bool turnEnded)
        {
            bool changed = indicators.RemoveAll(e =>
                e.definition.expiresOnOwnTurn && e.definition.lastsThroughOwnTurn == turnEnded) > 0;
            if (changed) OnIndicatorsChanged?.Invoke(this);
        }

        private int IndexOfIndicator(IndicatorSO definition)
        {
            for (int i = 0; i < indicators.Count; i++)
                if (indicators[i].definition == definition) return i;
            return -1;
        }
    }
}