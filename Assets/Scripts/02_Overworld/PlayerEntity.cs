using Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using Core.CombatSystem;
using Core.CombatSystem.Units;

namespace Overworld
{
    /// <summary>
    /// Overworld-only component. Lives alongside PlayerController on the Player GameObject,
    /// but only while the Player is in a dungeon/Overworld context — unlike PlayerController,
    /// which handles movement everywhere.
    /// </summary>
    public class PlayerEntity : Entity
    {
        [Header("----- Combat -----")]
        [SerializeField] private AllyParty party = new();
        public AllyParty Party => party;

        private bool subscribed;

        private void Start()
        {
            SubscribeAttack();

            if (!subscribed)
            {
                Debug.LogWarning($"[{nameof(PlayerEntity)}] InputManager.Instance is null — " +
                "the persistent GameManager likely hasn't loaded yet (are you Play-ing this " +
                "scene directly instead of going through the boot scene?). Attack input won't work.");
            }
        }

        private void OnEnable() => SubscribeAttack();

        private void OnDisable() => UnsubscribeAttack();

        private void SubscribeAttack()
        {
            if (subscribed || InputManager.Instance == null) return;

            InputManager.Instance.Overworld.Attack.performed += OnAttackInput;
            subscribed = true;
            Debug.Log($"[{nameof(PlayerEntity)}] Attack subscribed (map: {InputManager.Instance.CurrentMap}).");
        }

        private void UnsubscribeAttack()
        {
            // InputManager is DontDestroyOnLoad and can be gone first when the app closes.
            if (InputManager.Instance != null)
                InputManager.Instance.Overworld.Attack.performed -= OnAttackInput;

            subscribed = false;
            Debug.Log($"[{nameof(PlayerEntity)}] Attack unsubscribed.");
        }

        private void OnAttackInput(InputAction.CallbackContext ctx)
        {
            Debug.Log($"[{nameof(PlayerEntity)}] Attack input received (map: {InputManager.Instance.CurrentMap}).");
            TryAttack();
        }
        
        protected override void PlayAttackPlaceholder()
        {
            // Placeholder lunge — swap for a real animation + Animation Event later,
            // calling OnAttackHitFrame() at the same relative point.
            transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 1, 0.5f)
                .OnComplete(() =>
                {
                    OnAttackHitFrame();
                    EndAttack();
                });
        }

        protected override void OnHitConnected(Entity target)
        {
            if (target is not EnemyEntity enemy) return;

            base.OnHitConnected(target);
            CombatTransition.Begin(party, enemy.EnemyParty, UNIT_TEAM.Ally, gameObject, enemy.gameObject);
        }
    }
}