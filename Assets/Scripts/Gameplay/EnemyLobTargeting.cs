using UnityEngine;

namespace Dagon.Gameplay
{
    public static class EnemyLobTargeting
    {
        public static Vector3 ResolveLeadTargetPoint(
            Vector3 origin,
            Transform target,
            PlayerMover playerMover,
            float maxRange,
            float leadDistance,
            Vector3 fallbackForward)
        {
            var planarOrigin = new Vector3(origin.x, 0f, origin.z);
            var targetPosition = target != null ? target.position : origin + fallbackForward;
            var planarTarget = new Vector3(targetPosition.x, 0f, targetPosition.z);

            if (playerMover != null && playerMover.MoveDirection.sqrMagnitude > 0.01f)
            {
                planarTarget += playerMover.MoveDirection.normalized * Mathf.Max(0f, leadDistance);
            }

            var planarDelta = planarTarget - planarOrigin;
            if (planarDelta.sqrMagnitude <= 0.0001f)
            {
                var fallback = fallbackForward.sqrMagnitude > 0.001f ? fallbackForward.normalized : Vector3.forward;
                planarDelta = fallback * Mathf.Max(0.1f, maxRange * 0.6f);
            }

            var clamped = Vector3.ClampMagnitude(planarDelta, Mathf.Max(0.1f, maxRange));
            return new Vector3(origin.x + clamped.x, origin.y, origin.z + clamped.z);
        }
    }
}
