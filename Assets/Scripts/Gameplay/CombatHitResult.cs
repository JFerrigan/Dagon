using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    public enum CombatHitType
    {
        None,
        Damageable,
        Blocked,
        Ignored
    }

    public readonly struct CombatHitResult
    {
        public CombatHitResult(
            CombatHitType type,
            Collider collider,
            IDamageable damageable = null,
            Hurtbox hurtbox = null,
            CombatTeam targetTeam = CombatTeam.Neutral,
            GameObject targetRoot = null,
            string reason = "")
        {
            Type = type;
            Collider = collider;
            Damageable = damageable;
            Hurtbox = hurtbox;
            TargetTeam = targetTeam;
            TargetRoot = targetRoot;
            Reason = reason ?? string.Empty;
        }

        public CombatHitType Type { get; }
        public Collider Collider { get; }
        public IDamageable Damageable { get; }
        public Hurtbox Hurtbox { get; }
        public CombatTeam TargetTeam { get; }
        public GameObject TargetRoot { get; }
        public string Reason { get; }

        public bool CanApplyDamage => Type == CombatHitType.Damageable && Damageable != null;
        public bool BlocksImpact => Type == CombatHitType.Damageable || Type == CombatHitType.Blocked;
    }
}
