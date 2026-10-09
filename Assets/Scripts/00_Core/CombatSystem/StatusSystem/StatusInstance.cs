using Core.CombatSystem.Units;
using Core;

namespace Core.CombatSystem.StatusSystem
{
    /// <summary>Runtime data of one status on one unit. The StatusSO asset is shared; this is not.</summary>
    public class StatusInstance
    {
        public StatusSO definition;
        public Unit source;
        public Unit target;
        public float magnitude;
        public int remainingTurns;
    }

    /// <summary>Notification payload for OnStatusApplied / OnStatusExpired.</summary>
    public class StatusContext
    {
        public Unit source;
        public Unit target;
        public StatusSO status;
        public float magnitude;
        public int remainingTurns;
        public bool refreshed;
    }

    /// <summary>A relative change to one derived stat (0.8 = +80%, -0.3 = -30%), owned by a status.</summary>
    public struct TemporaryStatModifier
    {
        public STAT_TYPE stat;
        public float fraction;
    }
}