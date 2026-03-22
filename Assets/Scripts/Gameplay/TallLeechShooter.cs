using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class TallLeechShooter : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private HarpoonProjectile projectilePrefab;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private float preferredRange = 8.5f;
        [SerializeField] private float fireCooldown = 1.9f;
        [SerializeField] private float windupDuration = 0.32f;
        [SerializeField] private float projectileSpeed = 7.2f;
        [SerializeField] private float projectileDamage = 0.9f;

        private float fireTimer;
        private float windupTimer;
        private Vector3 queuedAimDirection = Vector3.forward;
        private bool windingUp;

        private void Update()
        {
            ResolveReferences();
            if (target == null || projectilePrefab == null)
            {
                return;
            }

            fireTimer -= Time.deltaTime;
            if (windingUp)
            {
                windupTimer -= Time.deltaTime;
                if (windupTimer <= 0f)
                {
                    FireShot();
                }

                return;
            }

            var toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (fireTimer > 0f || toTarget.sqrMagnitude <= 0.05f)
            {
                return;
            }

            if (toTarget.magnitude > preferredRange)
            {
                return;
            }

            queuedAimDirection = toTarget.normalized;
            windingUp = true;
            windupTimer = windupDuration;

            PlaceholderWeaponVisual.Spawn(
                "TallLeechShotWindup",
                transform.position + Vector3.up * 0.2f,
                new Vector3(0.95f, 0.95f, 1f),
                worldCamera,
                new Color(0.78f, 0.92f, 0.76f, 0.38f),
                windupDuration,
                1.04f,
                Mathf.Atan2(queuedAimDirection.x, queuedAimDirection.z) * Mathf.Rad2Deg,
                spritePath: "Sprites/Effects/brine_surge",
                pixelsPerUnit: 256f,
                sortingOrder: 11,
                groundPlane: false);
        }

        public void Configure(
            Transform newTarget,
            HarpoonProjectile newProjectilePrefab,
            Camera cameraReference,
            float newPreferredRange,
            float newCooldown,
            float newProjectileSpeed,
            float newProjectileDamage)
        {
            target = newTarget;
            projectilePrefab = newProjectilePrefab;
            worldCamera = cameraReference;
            preferredRange = Mathf.Max(1f, newPreferredRange);
            fireCooldown = Mathf.Max(0.2f, newCooldown);
            projectileSpeed = Mathf.Max(0.1f, newProjectileSpeed);
            projectileDamage = Mathf.Max(0.1f, newProjectileDamage);
            fireTimer = Random.Range(0.6f, 1.4f);
            windingUp = false;
            windupTimer = 0f;
        }

        private void ResolveReferences()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                target = player != null ? player.transform : null;
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        private void FireShot()
        {
            windingUp = false;
            fireTimer = fireCooldown;

            var projectile = Instantiate(
                projectilePrefab,
                transform.position + Vector3.up * 0.55f,
                Quaternion.LookRotation(queuedAimDirection, Vector3.up));
            projectile.gameObject.SetActive(true);
            projectile.Initialize(gameObject, queuedAimDirection, projectileSpeed, projectileDamage);
        }
    }
}
