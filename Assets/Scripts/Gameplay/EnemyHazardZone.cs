using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EnemyHazardZone : MonoBehaviour
    {
        private float radius;
        private float duration;
        private float tickDamage;
        private float tickInterval;
        private Camera worldCamera;
        private float tickTimer;

        public static void Spawn(
            Vector3 position,
            float radius,
            float duration,
            float tickDamage,
            float tickInterval,
            Camera camera,
            Color tint,
            string name = "EnemyHazardZone")
        {
            var zone = new GameObject(name);
            zone.transform.position = position + Vector3.up * 0.08f;

            var component = zone.AddComponent<EnemyHazardZone>();
            component.Initialize(radius, duration, tickDamage, tickInterval, camera, tint);
        }

        private void Update()
        {
            duration -= Time.deltaTime;
            tickTimer -= Time.deltaTime;
            if (tickTimer <= 0f)
            {
                tickTimer = tickInterval;
                TickDamage();
            }

            if (duration <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void Initialize(float zoneRadius, float zoneDuration, float damage, float interval, Camera camera, Color tint)
        {
            radius = Mathf.Max(0.1f, zoneRadius);
            duration = Mathf.Max(0.1f, zoneDuration);
            tickDamage = Mathf.Max(0.01f, damage);
            tickInterval = Mathf.Max(0.1f, interval);
            tickTimer = tickInterval;
            worldCamera = camera;

            PlaceholderWeaponVisual.Spawn(
                "EnemyHazardVisual",
                transform.position,
                new Vector3(radius * 2f, radius * 2f, 1f),
                worldCamera,
                tint,
                zoneDuration,
                1f);
        }

        private void TickDamage()
        {
            var colliders = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < colliders.Length; i++)
            {
                var damageable = colliders[i].GetComponentInParent<IDamageable>();
                if (damageable == null)
                {
                    continue;
                }

                if (colliders[i].CompareTag("Player") || colliders[i].GetComponentInParent<PlayerMover>() != null)
                {
                    damageable.ApplyDamage(tickDamage, gameObject);
                }
            }
        }
    }
}
