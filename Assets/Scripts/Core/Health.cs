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
        private float bonusMaxHealth;
        private float healingMultiplier = 1f;
        private float incomingDamageMultiplier = 1f;
        private float incomingContactDamageMultiplier = 1f;
        private bool isDead;

        public event Action<Health, GameObject> Died;
        public event Action<Health> Changed;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth + bonusMaxHealth;
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

            var scaledAmount = amount * incomingDamageMultiplier;
            if (source != null && source.GetComponent<Dagon.Gameplay.ContactDamage>() != null)
            {
                scaledAmount *= incomingContactDamageMultiplier;
            }

            var previousHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - scaledAmount);
            Dagon.Gameplay.CombatDebug.Log(
                "Health",
                $"target={name} amount={scaledAmount:0.##} source={(source != null ? source.name : "null")} hp={previousHealth:0.##}->{currentHealth:0.##}",
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
            currentHealth = MaxHealth;
            Changed?.Invoke(this);
        }

        public void Restore(float amount)
        {
            if (isDead || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Min(MaxHealth, currentHealth + (amount * healingMultiplier));
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
                currentHealth = Mathf.Min(currentHealth, MaxHealth);
                Changed?.Invoke(this);
            }
        }

        public void SetBonusMaxHealth(float value, bool fillBonusHealth)
        {
            var previousMaxHealth = MaxHealth;
            bonusMaxHealth = Mathf.Max(0f, value);
            var newMaxHealth = MaxHealth;
            if (fillBonusHealth && newMaxHealth > previousMaxHealth)
            {
                currentHealth = Mathf.Min(newMaxHealth, currentHealth + (newMaxHealth - previousMaxHealth));
            }
            else
            {
                currentHealth = Mathf.Min(currentHealth, newMaxHealth);
            }

            Changed?.Invoke(this);
        }

        public void SetHealingMultiplier(float multiplier)
        {
            healingMultiplier = Mathf.Max(0f, multiplier);
        }

        public void SetIncomingDamageMultiplier(float multiplier)
        {
            incomingDamageMultiplier = Mathf.Max(0f, multiplier);
        }

        public void SetIncomingContactDamageMultiplier(float multiplier)
        {
            incomingContactDamageMultiplier = Mathf.Max(0f, multiplier);
        }
    }
}
