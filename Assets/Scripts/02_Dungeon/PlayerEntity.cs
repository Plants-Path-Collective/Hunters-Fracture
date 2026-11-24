using Core;
using UnityEngine;
using UnityEngine.InputSystem;
using Core.CombatSystem;
using Core.CombatSystem.Units;

namespace Dungeon
{
    /// <summary>
    /// Overworld-only component. Lives alongside PlayerController on the Player GameObject,
    /// but only while the Player is in a dungeon/Overworld context — unlike PlayerController,
    /// which handles movement everywhere.
    /// </summary>
    public class PlayerEntity : Entity
    {
        public static PlayerEntity Instance;

        [Header("----- Combat -----")]
        [SerializeField] private AllyParty party = new();
        public AllyParty Party => party;

        private bool subscribed;

        protected override void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            base.Awake();
        }

        private void Start()
        {
            SubscribeAttack();

            if (!subscribed)
            {
                Debug.LogWarning($"[PlayerEntity] InputManager.Instance is null — " +
                "the persistent GameManager likely hasn't loaded yet (are you Play-ing this " +
                "scene directly instead of going through the boot scene?). Attack input won't work.");
            }
        }

        private void OnEnable() => SubscribeAttack();

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeAttack();
        }

        public void SetCurrentParty(AllyParty newParty)
        {
            party = newParty;

            Debug.Log("[PlayerEntity] New paty set");
        }

        protected override bool CanHit(Entity target) => target is EnemyEntity;

        private void SubscribeAttack()
        {
            if (subscribed || InputManager.Instance == null) return;

            InputManager.Instance.Dungeon.Attack.performed += OnAttackInput;
            subscribed = true;
            #if  UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[PlayerEntity] Attack subscribed (map: {InputManager.Instance.CurrentMap}).");
            #endif
        }

        private void UnsubscribeAttack()
        {
            // InputManager is DontDestroyOnLoad and can be gone first when the app closes.
            if (InputManager.Instance != null)
                InputManager.Instance.Dungeon.Attack.performed -= OnAttackInput;

            subscribed = false;
        }

        private void OnAttackInput(InputAction.CallbackContext ctx)
        {
            Debug.Log($"[PlayerEntity] Attack input received (map: {InputManager.Instance.CurrentMap}).");
            TryAttack();
        }
        
        protected override void PlayAttackPlaceholder()
        {
            PlayAttackAnimation();
        }

        protected override void OnHitConnected(Entity target)
        {
            if (target is not EnemyEntity enemy) return;

            base.OnHitConnected(target);
            CombatTransition.Begin(party, enemy.EnemyParty, UNIT_TEAM.Ally, gameObject, enemy.gameObject);
        }

        protected override void OnAttackMissed()
        {
            UIManager.Instance?.ShowDungeonLogTransient("Missed the attack.");
        }
    }
}