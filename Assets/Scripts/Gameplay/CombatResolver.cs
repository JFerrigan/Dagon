using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    public static class CombatResolver
    {
        public static CombatTeam GetTeam(GameObject source)
        {
            if (source == null)
            {
                return CombatTeam.Neutral;
            }

            var hurtbox = source.GetComponentInParent<Hurtbox>();
            if (hurtbox != null)
            {
                return hurtbox.Team;
            }

            return InferTeam(source);
        }

        public static bool TryResolveTarget(Collider hit, CombatTeam sourceTeam, GameObject sourceOwner, out Hurtbox hurtbox)
        {
            hurtbox = hit != null ? hit.GetComponentInParent<Hurtbox>() : null;
            if (hurtbox != null && hurtbox.Damageable != null)
            {
                var resolved = ValidateResolvedTarget(hurtbox.Damageable, hurtbox.Team, hurtbox.gameObject, sourceTeam, sourceOwner);
                CombatDebug.Log(
                    "ResolveTarget",
                    $"hit={CombatDebug.NameOf(hit)} source={CombatDebug.NameOf(sourceOwner)} sourceTeam={sourceTeam} target={CombatDebug.NameOf(hurtbox.gameObject)} targetTeam={hurtbox.Team} via=Hurtbox resolved={resolved}",
                    hit);
                return resolved;
            }

            var health = hit != null ? hit.GetComponentInParent<Health>() : null;
            if (health == null)
            {
                CombatDebug.Log(
                    "ResolveTarget",
                    $"hit={CombatDebug.NameOf(hit)} source={CombatDebug.NameOf(sourceOwner)} sourceTeam={sourceTeam} resolved=false reason=no_hurtbox_or_health",
                    hit);
                return false;
            }

            var inferredTeam = InferTeam(health.gameObject);
            var fallbackResolved = ValidateResolvedTarget(health, inferredTeam, health.gameObject, sourceTeam, sourceOwner);
            CombatDebug.Log(
                "ResolveTarget",
                $"hit={CombatDebug.NameOf(hit)} source={CombatDebug.NameOf(sourceOwner)} sourceTeam={sourceTeam} target={CombatDebug.NameOf(health.gameObject)} targetTeam={inferredTeam} via=HealthFallback resolved={fallbackResolved}",
                hit);
            return fallbackResolved;
        }

        public static bool TryApplyDamage(Collider hit, CombatTeam sourceTeam, GameObject sourceOwner, float damage)
        {
            if (!TryResolveDamageTarget(hit, sourceTeam, sourceOwner, out var damageable, out _, out _))
            {
                CombatDebug.Log(
                    "ApplyDamage",
                    $"hit={CombatDebug.NameOf(hit)} source={CombatDebug.NameOf(sourceOwner)} sourceTeam={sourceTeam} damage={damage:0.##} applied=false",
                    hit);
                return false;
            }

            CombatDebug.Log(
                "ApplyDamage",
                $"hit={CombatDebug.NameOf(hit)} source={CombatDebug.NameOf(sourceOwner)} sourceTeam={sourceTeam} damage={damage:0.##} applied=true",
                hit);
            damageable.ApplyDamage(damage, sourceOwner);
            return true;
        }

        public static bool CanDamage(Collider hit, CombatTeam sourceTeam, GameObject sourceOwner)
        {
            return TryResolveDamageTarget(hit, sourceTeam, sourceOwner, out _, out _, out _);
        }

        private static bool TryResolveDamageTarget(
            Collider hit,
            CombatTeam sourceTeam,
            GameObject sourceOwner,
            out IDamageable damageable,
            out CombatTeam targetTeam,
            out GameObject targetRoot)
        {
            damageable = null;
            targetTeam = CombatTeam.Neutral;
            targetRoot = null;

            if (hit == null)
            {
                CombatDebug.Log("ResolveDamageTarget", "resolved=false reason=null_hit");
                return false;
            }

            var hurtbox = hit.GetComponentInParent<Hurtbox>();
            if (hurtbox != null && hurtbox.Damageable != null)
            {
                damageable = hurtbox.Damageable;
                targetTeam = hurtbox.Team;
                targetRoot = hurtbox.gameObject;
            }
            else
            {
                var health = hit.GetComponentInParent<Health>();
                if (health == null)
                {
                    CombatDebug.Log(
                        "ResolveDamageTarget",
                        $"hit={CombatDebug.NameOf(hit)} source={CombatDebug.NameOf(sourceOwner)} resolved=false reason=no_health",
                        hit);
                    return false;
                }

                damageable = health;
                targetRoot = health.gameObject;
                targetTeam = InferTeam(targetRoot);
            }

            if (sourceOwner != null)
            {
                if (targetRoot == sourceOwner)
                {
                    CombatDebug.Log(
                        "ResolveDamageTarget",
                        $"hit={CombatDebug.NameOf(hit)} source={CombatDebug.NameOf(sourceOwner)} resolved=false reason=self_target",
                        hit);
                    return false;
                }

                var sourceHurtbox = sourceOwner.GetComponentInParent<Hurtbox>();
                var sourceRoot = sourceHurtbox != null
                    ? sourceHurtbox.gameObject
                    : sourceOwner.GetComponentInParent<Health>()?.gameObject ?? sourceOwner;
                if (targetRoot == sourceRoot)
                {
                    CombatDebug.Log(
                        "ResolveDamageTarget",
                        $"hit={CombatDebug.NameOf(hit)} source={CombatDebug.NameOf(sourceOwner)} target={CombatDebug.NameOf(targetRoot)} resolved=false reason=same_root",
                        hit);
                    return false;
                }
            }

            if (sourceTeam != CombatTeam.Neutral && targetTeam == sourceTeam)
            {
                CombatDebug.Log(
                    "ResolveDamageTarget",
                    $"hit={CombatDebug.NameOf(hit)} source={CombatDebug.NameOf(sourceOwner)} sourceTeam={sourceTeam} target={CombatDebug.NameOf(targetRoot)} targetTeam={targetTeam} resolved=false reason=same_team",
                    hit);
                return false;
            }

            return true;
        }

        private static bool ValidateResolvedTarget(
            IDamageable damageable,
            CombatTeam targetTeam,
            GameObject targetRoot,
            CombatTeam sourceTeam,
            GameObject sourceOwner)
        {
            if (damageable == null || targetRoot == null)
            {
                return false;
            }

            if (sourceOwner != null)
            {
                if (targetRoot == sourceOwner)
                {
                    CombatDebug.Log(
                        "ValidateTarget",
                        $"target={CombatDebug.NameOf(targetRoot)} source={CombatDebug.NameOf(sourceOwner)} resolved=false reason=self_target");
                    return false;
                }

                var sourceHurtbox = sourceOwner.GetComponentInParent<Hurtbox>();
                var sourceRoot = sourceHurtbox != null
                    ? sourceHurtbox.gameObject
                    : sourceOwner.GetComponentInParent<Health>()?.gameObject ?? sourceOwner;
                if (targetRoot == sourceRoot)
                {
                    CombatDebug.Log(
                        "ValidateTarget",
                        $"target={CombatDebug.NameOf(targetRoot)} source={CombatDebug.NameOf(sourceOwner)} resolved=false reason=same_root");
                    return false;
                }
            }

            if (sourceTeam != CombatTeam.Neutral && targetTeam == sourceTeam)
            {
                CombatDebug.Log(
                    "ValidateTarget",
                    $"target={CombatDebug.NameOf(targetRoot)} source={CombatDebug.NameOf(sourceOwner)} sourceTeam={sourceTeam} targetTeam={targetTeam} resolved=false reason=same_team");
                return false;
            }

            return true;
        }

        private static CombatTeam InferTeam(GameObject source)
        {
            if (source == null)
            {
                return CombatTeam.Neutral;
            }

            if (source.CompareTag("Player") || source.GetComponentInParent<PlayerMover>() != null)
            {
                return CombatTeam.Player;
            }

            if (source.GetComponentInParent<Health>() != null)
            {
                return CombatTeam.Enemy;
            }

            return CombatTeam.Neutral;
        }
    }
}
