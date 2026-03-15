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
        public event Action<Health> Changed;

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
                Dagon.Gameplay.CombatDebug.Log(
                    "Health",
                    $"target={name} amount={amount:0.##} source={(source != null ? source.name : "null")} applied=false isDead={isDead}",
                    this);
                return;
            }

            var previousHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            Dagon.Gameplay.CombatDebug.Log(
                "Health",
                $"target={name} amount={amount:0.##} source={(source != null ? source.name : "null")} hp={previousHealth:0.##}->{currentHealth:0.##}",
                this);
            onDamaged?.Invoke();
            Changed?.Invoke(this);

            if (currentHealth > 0f)
            {
                return;
            }

            isDead = true;
            Dagon.Gameplay.CombatDebug.Log(
                "Health",
                $"target={name} died source={(source != null ? source.name : "null")}",
                this);
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
            Changed?.Invoke(this);
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
                Changed?.Invoke(this);
            }
        }
    }
}
