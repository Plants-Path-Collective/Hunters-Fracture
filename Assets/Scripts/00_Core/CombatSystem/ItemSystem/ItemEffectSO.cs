using UnityEngine;

namespace CombatSystem.ItemSystem
{
    /// <summary>
    /// Abstract base for any item effect.
    /// </summary>
    /// <remarks>
    /// <para>This class intentionally has no members of its own. Its only job is to
    /// give <see cref="StatModifierEffectSO"/> and <see cref="TriggeredEffectSO"/> a
    /// common type, so that <c>ItemSO</c> can hold a single <c>List&lt;ItemEffectSO&gt;</c>
    /// containing a mix of both — even though the two have completely different APIs
    /// (one exposes <c>ApplyModifiers()</c>, the other exposes <c>Attach()</c>/<c>Detach()</c>).</para>
    /// <para>Being <c>abstract</c> means Unity will never let you create an asset of type
    /// <c>ItemEffectSO</c> itself — try adding <c>[CreateAssetMenu]</c> to this class and
    /// creating an asset from it; you can't. Only its concrete subclasses can become
    /// actual <c>.asset</c> files. That's the whole point: "ItemEffectSO" describes the
    /// shape every effect must fit, not a real effect you'd ever use directly.</para>
    /// </remarks>
    public abstract class ItemEffectSO : ScriptableObject
    {
    }
}