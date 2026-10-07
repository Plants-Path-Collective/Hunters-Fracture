using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Core.CombatSystem.Units;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Core.CombatSystem
{
    /// <summary>
    /// Purely cosmetic: snaps every Unit to its defense position when combat starts, and plays a
    /// self-contained move-out/pause/move-back sequence whenever a Unit deals damage. Never
    /// gates or delays actual combat resolution — reacts to CombatController's events, nothing
    /// more. Reads the four positioning lists already declared on CombatController (index must
    /// match the Unit's slot in Party/EnemyParty — see Unit.SlotIndex).
    /// </summary>
    [RequireComponent(typeof(CombatController))]
    public class CombatStagePositioner : MonoBehaviour
    {
        [Header("----- Gizmos (Scene View only) -----")]
        [SerializeField] private bool drawGizmos = true;
        [Tooltip("Also draws the attack positions and a line from each defense position to its attack position.")]
        [SerializeField] private bool drawAttackPositions = true;
        [SerializeField] private float gizmoRadius = 0.3f;
        [SerializeField] private Color allyColor = new(0.25f, 0.6f, 1f);
        [SerializeField] private Color enemyColor = new(1f, 0.35f, 0.3f);
        private static readonly COMBAT_SLOT[] AllSlots = { COMBAT_SLOT.Left, COMBAT_SLOT.Mid, COMBAT_SLOT.Right };

        [Header("----- Movement -----")]
        [Tooltip("Seconds to move to/from the attack position. 0 disables the attack hop entirely " +
                 "(units stay at their defense position the whole fight).")]
        [SerializeField] private float moveDuration = 0.25f;
        [Tooltip("Seconds to hold at the attack position before moving back.")]
        [SerializeField] private float pauseAtAttackPosition = 0.1f;

        private CombatController combat;

        /// <summary>Hop currently playing per Unit. Prevents overlapping tweens when one action
        /// deals damage several times (multi-target skills).</summary>
        private readonly Dictionary<Unit, Sequence> activeHops = new();

        private void Awake()
        {
            combat = GetComponent<CombatController>();
            combat.OnCombatStart += OnCombatStart;
            combat.OnBeforeDamage += OnBeforeDamage;
        }

        private void OnDestroy()
        {
            if (combat != null)
            {
                combat.OnCombatStart -= OnCombatStart;
                combat.OnBeforeDamage -= OnBeforeDamage;
            }

            // Kill() fires each sequence's OnKill, which removes it from activeHops while we iterate:
            // walk a copy instead.
            foreach (Sequence hop in new List<Sequence>(activeHops.Values))
                hop.Kill();
            activeHops.Clear();
        }

        private void OnCombatStart(IReadOnlyList<Unit> roster)
        {
            foreach (Unit unit in roster)
                SnapTo(unit, GetDefensePosition(unit));
        }

        private void OnBeforeDamage(DamageContext context)
        {
            if (moveDuration <= 0f) return;

            // Only the unit whose turn it is performs the hop. Reflected damage or on-hit item
            // effects also go through DealDamage, but their source is not the current actor.
            Unit attacker = context.source;
            if (attacker == null || attacker != combat.CurrentActor) return;
            if (activeHops.ContainsKey(attacker)) return;

            Transform defense = GetDefensePosition(attacker);
            Transform attack = ResolveAttackPosition(context);
            if (defense == null || attack == null) return;

            bool isRanged = context.damageType == UNITY_TYPE.Magical;
            PlayAttackHop(attacker, defense, attack, matchRotation: isRanged);
        }

        // ── Lookup ────────────────────────────────────────────────────────────

        private Transform GetDefensePosition(Unit unit) =>
            GetAnchor(unit, combat.alliesDefense, combat.enemiesDefense);

        private Transform GetAttackPosition(Unit unit) =>
            GetAnchor(unit, combat.alliesAttack, combat.enemiesAttack);

        private static Transform GetAnchor(Unit unit, SlotAnchors allies, SlotAnchors enemies)
        {
            if (unit.Slot is not COMBAT_SLOT slot) return null;
            return (unit.Team == UNIT_TEAM.Ally ? allies : enemies).Get(slot);
        }

        // ── Movement ──────────────────────────────────────────────────────────

        private static void SnapTo(Unit unit, Transform anchor)
        {
            if (anchor == null) return;
            unit.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        }

        /// <summary>
        /// Magical = ranged: the attacker steps forward on its own side (its own attack slot).
        /// Physical = melee: the attacker goes to the target's slot on the target's side.
        /// </summary>
        private Transform ResolveAttackPosition(DamageContext context)
        {
            Unit anchorOwner = context.damageType == UNITY_TYPE.Magical ? context.source : context.target;
            return GetAttackPosition(anchorOwner);
        }

        private void PlayAttackHop(Unit unit, Transform defense, Transform attack, bool matchRotation)
        {
            Sequence sequence = DOTween.Sequence();

            sequence.Append(unit.transform.DOMove(attack.position, moveDuration));
            if (matchRotation)
                sequence.Join(unit.transform.DORotateQuaternion(attack.rotation, moveDuration));

            sequence.AppendInterval(pauseAtAttackPosition);

            sequence.Append(unit.transform.DOMove(defense.position, moveDuration));
            if (matchRotation)
                sequence.Join(unit.transform.DORotateQuaternion(defense.rotation, moveDuration));

            // Kills the sequence if the Unit is destroyed mid-hop (CloseCombat), and cleans the
            // dictionary both on completion and on kill.
            sequence.SetLink(unit.gameObject);
            sequence.OnKill(() => activeHops.Remove(unit));

            activeHops[unit] = sequence;
        }

        // ── Gizmos ────────────────────────────────────────────────────────────

        /// <summary>Always visible in the Scene View (not only when selected). Labels show the slot name
        /// (MID / LEFT / RIGHT), which matches Unit.Slot.</summary>
        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            // 'combat' is only cached in Awake, so fetch it here to also work in Edit mode.
            CombatController controller = combat != null ? combat : GetComponent<CombatController>();
            if (controller == null) return;

            DrawPositions(controller.alliesDefense, "ALLY DEF", allyColor, solid: true);
            DrawPositions(controller.enemiesDefense, "ENEMY DEF", enemyColor, solid: true);

            if (!drawAttackPositions) return;

            DrawPositions(controller.alliesAttack, "ALLY ATK", allyColor, solid: false);
            DrawPositions(controller.enemiesAttack, "ENEMY ATK", enemyColor, solid: false);

            DrawHopLines(controller.alliesDefense, controller.alliesAttack, allyColor);
            DrawHopLines(controller.enemiesDefense, controller.enemiesAttack, enemyColor);
        }

        private void DrawPositions(SlotAnchors anchors, string label, Color color, bool solid)
        {
            Gizmos.color = color;

            foreach (COMBAT_SLOT slot in AllSlots)
            {
                Transform anchor = anchors.Get(slot);
                if (anchor == null) continue;

                if (solid) Gizmos.DrawSphere(anchor.position, gizmoRadius);
                else       Gizmos.DrawWireSphere(anchor.position, gizmoRadius);

                // Facing direction of the anchor.
                Gizmos.DrawRay(anchor.position, anchor.forward * gizmoRadius * 2f);

        #if UNITY_EDITOR
                var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
                style.normal.textColor = color;
                Handles.Label(anchor.position + Vector3.up * (gizmoRadius + 0.25f),
                    $"{label} {slot.ToString().ToUpperInvariant()}", style);
        #endif
            }
        }

        private void DrawHopLines(SlotAnchors defense, SlotAnchors attack, Color color)
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.4f);

            foreach (COMBAT_SLOT slot in AllSlots)
            {
                Transform from = defense.Get(slot);
                Transform to = attack.Get(slot);
                if (from == null || to == null) continue;

                Gizmos.DrawLine(from.position, to.position);
            }
        }
    }
}