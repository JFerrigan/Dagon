using System.Collections.Generic;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class WatcherEyeMarkZone : MonoBehaviour
    {
        private readonly HashSet<GameObject> resolvedTargets = new();
        private float delay;
        private float lingerDuration;
        private float damage;
        private float radius;
        private Camera worldCamera;
        private GameObject sourceOwner;
        private bool detonated;

        public static void Spawn(
            Vector3 position,
            float radius,
            float delay,
            float damage,
            float lingerDuration,
            Camera camera,
            GameObject owner,
            string name = "WatcherEyeMarkZone")
        {
            var zone = new GameObject(name);
            zone.transform.position = position + Vector3.up * 0.05f;

            var component = zone.AddComponent<WatcherEyeMarkZone>();
            component.Initialize(radius, delay, damage, lingerDuration, camera, owner);
        }

        private void Update()
        {
            if (!detonated)
            {
                delay -= Time.deltaTime;
                if (delay <= 0f)
                {
                    Detonate();
                }

                return;
            }

            lingerDuration -= Time.deltaTime;
            if (lingerDuration <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void Initialize(float markRadius, float detonationDelay, float detonationDamage, float lingerTime, Camera camera, GameObject owner)
        {
            radius = Mathf.Max(0.1f, markRadius);
            delay = Mathf.Max(0.05f, detonationDelay);
            damage = Mathf.Max(0.1f, detonationDamage);
            lingerDuration = Mathf.Max(0.05f, lingerTime);
            worldCamera = camera;
            sourceOwner = owner;

            PlaceholderWeaponVisual.Spawn(
                "WatcherEyeMarkTelegraph",
                transform.position,
                new Vector3(radius * 2.2f, radius * 2.2f, 1f),
                worldCamera,
                new Color(0.82f, 0.94f, 0.76f, 0.34f),
                detonationDelay,
                1.04f,
                0f,
                spritePath: "Sprites/Effects/brine_surge",
                pixelsPerUnit: 256f,
                sortingOrder: 4,
                groundPlane: true);
        }

        private void Detonate()
        {
            if (detonated)
            {
                return;
            }

            detonated = true;
            resolvedTargets.Clear();

            var colliders = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (!CombatResolver.TryResolveUniqueHit(colliders[i], CombatTeam.Enemy, sourceOwner, resolvedTargets, out var resolvedHit))
                {
                    continue;
                }

                CombatResolver.TryApplyDamage(resolvedHit, sourceOwner, damage, CombatTeam.Enemy);
            }

            PlaceholderWeaponVisual.Spawn(
                "WatcherEyeMarkBlast",
                transform.position + Vector3.up * 0.12f,
                new Vector3(radius * 1.55f, radius * 1.55f, 1f),
                worldCamera,
                new Color(0.92f, 1f, 0.84f, 0.48f),
                lingerDuration,
                1.12f,
                0f,
                spritePath: "Sprites/Enemies/watcher_eye",
                pixelsPerUnit: 256f,
                sortingOrder: 10);
        }
    }
}
