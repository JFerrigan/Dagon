using UnityEngine;

namespace Dagon.Gameplay
{
    public static class BodyBlockerResolver
    {
        public static Vector3 ResolvePlanarMovement(BodyBlocker self, Vector3 desiredDelta)
        {
            if (self == null || self.Suppressed)
            {
                desiredDelta.y = 0f;
                return desiredDelta;
            }

            desiredDelta.y = 0f;
            var resolvedDelta = desiredDelta;
            var proposedPosition = self.PlanarPosition + resolvedDelta;

            for (var pass = 0; pass < 2; pass++)
            {
                var correction = Vector3.zero;
                var blockers = BodyBlocker.Active;
                for (var i = 0; i < blockers.Count; i++)
                {
                    var other = blockers[i];
                    if (!ShouldInteract(self, other))
                    {
                        continue;
                    }

                    var combinedRadius = self.BodyRadius + other.BodyRadius;
                    var separation = proposedPosition - other.PlanarPosition;
                    separation.y = 0f;
                    var distance = separation.magnitude;
                    if (distance >= combinedRadius)
                    {
                        continue;
                    }

                    var normal = ResolveSeparationNormal(self, other, separation, desiredDelta);
                    var penetration = combinedRadius - distance;
                    correction += normal * (penetration * ResolveCorrectionFactor(self, other));
                }

                correction.y = 0f;
                if (correction.sqrMagnitude <= 0.000001f)
                {
                    break;
                }

                proposedPosition += correction;
                resolvedDelta += correction;
            }

            return resolvedDelta;
        }

        private static bool ShouldInteract(BodyBlocker self, BodyBlocker other)
        {
            if (self == null || other == null || self == other)
            {
                return false;
            }

            if (!self.isActiveAndEnabled || !other.isActiveAndEnabled || self.Suppressed || other.Suppressed)
            {
                return false;
            }

            if (self.Team == BodyBlocker.BodyTeam.Player && other.Team == BodyBlocker.BodyTeam.Enemy)
            {
                return other.BlocksPlayer;
            }

            if (self.Team == BodyBlocker.BodyTeam.Enemy && other.Team == BodyBlocker.BodyTeam.Player)
            {
                return self.BlocksPlayer && other.BlocksPlayer;
            }

            return self.Team == BodyBlocker.BodyTeam.Enemy &&
                   other.Team == BodyBlocker.BodyTeam.Enemy &&
                   self.SeparatesFromEnemies &&
                   other.SeparatesFromEnemies;
        }

        private static float ResolveCorrectionFactor(BodyBlocker self, BodyBlocker other)
        {
            if ((self.Team == BodyBlocker.BodyTeam.Player && other.Team == BodyBlocker.BodyTeam.Enemy) ||
                (self.Team == BodyBlocker.BodyTeam.Enemy && other.Team == BodyBlocker.BodyTeam.Player))
            {
                return 1f;
            }

            var totalWeight = Mathf.Max(0.1f, self.Weight + other.Weight);
            return Mathf.Clamp(other.Weight / totalWeight, 0.35f, 0.85f);
        }

        private static Vector3 ResolveSeparationNormal(BodyBlocker self, BodyBlocker other, Vector3 currentSeparation, Vector3 desiredDelta)
        {
            if (currentSeparation.sqrMagnitude > 0.000001f)
            {
                return currentSeparation.normalized;
            }

            if (desiredDelta.sqrMagnitude > 0.000001f)
            {
                return desiredDelta.normalized;
            }

            var fallback = self.GetInstanceID() > other.GetInstanceID() ? Vector3.right : Vector3.left;
            fallback.y = 0f;
            return fallback;
        }
    }
}
