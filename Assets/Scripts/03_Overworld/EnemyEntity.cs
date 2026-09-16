using DG.Tweening;
using Player;
using UnityEngine;

namespace Overworld
{
    /// <summary>
    /// Overworld-only component. Unlike PlayerEntity, nothing here decides *when* to attack —
    /// Overworld enemy AI isn't designed yet (only the in-combat action-selection AI is
    /// contracted in combat.html). TryAttack() is inherited public so a placeholder/debug
    /// caller can trigger it in the meantime.
    /// </summary>
    public class EnemyEntity : Entity
    {
        protected override void PlayAttackPlaceholder()
        {
            transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 1, 0.5f)
                .OnComplete(() =>
                {
                    OnAttackHitFrame();
                    EndAttack();
                });
        }

        protected override void OnHitConnected(Entity target)
        {
            if (target is not PlayerEntity)
                return;

            base.OnHitConnected(target);
            // TODO: once CombatSetUp exists — CombatSetUp.Begin(playerAdvantage: false, this);
        }
    }
}
