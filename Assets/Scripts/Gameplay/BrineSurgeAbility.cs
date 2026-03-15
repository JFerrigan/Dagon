using Dagon.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BrineSurgeAbility : MonoBehaviour
    {
        [SerializeField] private float cooldown = 6f;
        [SerializeField] private float radius = 2.8f;
        [SerializeField] private float damage = 2f;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private Camera worldCamera;

        private float cooldownRemaining;

        public float CooldownRemaining => cooldownRemaining;
        public float CooldownDuration => cooldown;

        private void Update()
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.unscaledDeltaTime);

            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TryActivate();
            }
        }

        public void ModifyRadius(float amount)
        {
            radius = Mathf.Max(1f, radius + amount);
        }

        private void TryActivate()
        {
            if (cooldownRemaining > 0f)
            {
                return;
            }

            var colliders = Physics.OverlapSphere(transform.position, radius, enemyMask, QueryTriggerInteraction.Collide);
            foreach (var hit in colliders)
            {
                if (hit.transform == transform)
                {
                    continue;
                }

                var damageable = hit.GetComponentInParent<IDamageable>();
                damageable?.ApplyDamage(damage, gameObject);
            }

            BrineSurgeVisual.Spawn(transform.position, radius, ResolveCamera());
            cooldownRemaining = cooldown;
        }

        private Camera ResolveCamera()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            return worldCamera;
        }
    }
}
