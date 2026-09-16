using Core;
using DG.Tweening;
using Overworld;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    /// <summary>
    /// Overworld-only component. Lives alongside PlayerController on the Player GameObject,
    /// but only while the Player is in a dungeon/Overworld context — unlike PlayerController,
    /// which handles movement everywhere.
    /// </summary>
    public class PlayerEntity : Entity
    {
        private void OnEnable()
        {
            InputManager.Instance.Overworld.Attack.performed += OnAttackInput;
        }

        private void OnDisable()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.Overworld.Attack.performed -= OnAttackInput;
        }

        private void OnAttackInput(InputAction.CallbackContext ctx) => TryAttack();

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
            // TODO: once CombatSetUp exists — CombatSetUp.Begin(playerAdvantage: true, enemy);
        }
    }
}
