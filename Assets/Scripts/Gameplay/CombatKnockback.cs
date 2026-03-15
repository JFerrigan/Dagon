using UnityEngine;

namespace Dagon.Gameplay
{
    public static class CombatKnockback
    {
        public static bool TryApply(Collider hit, Vector3 direction, float strength)
        {
            if (hit == null || strength <= 0f)
            {
                return false;
            }

            var receiver = hit.GetComponentInParent<KnockbackReceiver>();
            if (receiver == null)
            {
                return false;
            }

            receiver.ApplyKnockback(direction, strength);
            return true;
        }
    }
}
