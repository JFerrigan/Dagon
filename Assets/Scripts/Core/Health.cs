using UnityEngine;
using UnityEngine.Events;
using System;

namespace Dagon.Core
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 10f;
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private UnityEvent onDamaged;
        [SerializeField] private UnityEvent onDeath;

        private float currentHealth;
        private bool isDead;

        public event Action<Health, GameObject> Died;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public void ApplyDamage(float amount, GameObject source)
        {
            if (isDead || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            onDamaged?.Invoke();

            if (currentHealth > 0f)
            {
                return;
            }

            isDead = true;
            onDeath?.Invoke();
            Died?.Invoke(this, source);

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }

        public void RestoreFull()
        {
            isDead = false;
            currentHealth = maxHealth;
        }

        public void SetMaxHealth(float value, bool refillHealth)
        {
            maxHealth = Mathf.Max(0.01f, value);
            if (refillHealth)
            {
                RestoreFull();
            }
            else
            {
                currentHealth = Mathf.Min(currentHealth, maxHealth);
            }
        }
    }
}
