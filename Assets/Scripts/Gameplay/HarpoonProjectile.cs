using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class HarpoonProjectile : MonoBehaviour
    {
        [SerializeField] private float lifetime = 2f;

        private GameObject owner;
        private Vector3 direction = Vector3.forward;
        private float speed = 10f;
        private float damage = 1f;

        public void Initialize(GameObject projectileOwner, Vector3 moveDirection, float moveSpeed, float projectileDamage)
        {
            owner = projectileOwner;
            direction = moveDirection.normalized;
            speed = moveSpeed;
            damage = projectileDamage;
        }

        private void Update()
        {
            transform.position += direction * (speed * Time.deltaTime);
            lifetime -= Time.deltaTime;

            if (lifetime <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == owner)
            {
                return;
            }

            if (other.gameObject == owner)
            {
                return;
            }

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                return;
            }

            damageable.ApplyDamage(damage, owner);
            Destroy(gameObject);
        }
    }
}
