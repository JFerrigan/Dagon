using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ContactDamage : MonoBehaviour
    {
        [SerializeField] private float damage = 1f;
        [SerializeField] private float cooldown = 0.75f;

        private float cooldownTimer;

        private void Update()
        {
            cooldownTimer -= Time.deltaTime;
        }

        private void OnTriggerStay(Collider other)
        {
            if (cooldownTimer > 0f)
            {
                return;
            }

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                return;
            }

            damageable.ApplyDamage(damage, gameObject);
            cooldownTimer = cooldown;
        }

        public void Configure(float newDamage)
        {
            damage = Mathf.Max(0.01f, newDamage);
        }
    }
}
