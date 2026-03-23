using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerDamageFeedback : MonoBehaviour
    {
        private static readonly Color HitFlashColor = new(1f, 0.42f, 0.42f, 1f);
        private static readonly Color ImmunityAuraColor = new(0.72f, 0.96f, 0.88f, 0.16f);

        [SerializeField] private Health playerHealth;
        [SerializeField] private TemporaryDamageImmunity damageImmunity;
        [SerializeField] private SpriteRenderer playerSpriteRenderer;
        [SerializeField] private float hitFlashDuration = 0.12f;
        [SerializeField] private float immunityAuraRadius = 2.2f;
        [SerializeField] private string immunityAuraSpritePath = "Sprites/Effects/brine_surge";

        private float hitFlashRemaining;
        private Color baseColor = Color.white;
        private GameObject immunityAura;
        private SpriteRenderer immunityAuraRenderer;

        private void Awake()
        {
            playerHealth ??= GetComponent<Health>();
            damageImmunity ??= GetComponent<TemporaryDamageImmunity>();
            if (playerSpriteRenderer == null)
            {
                playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (playerSpriteRenderer != null)
            {
                baseColor = playerSpriteRenderer.color;
            }

            EnsureImmunityAura();
            SetImmunityAuraActive(damageImmunity != null && damageImmunity.IsActive);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();

            if (playerSpriteRenderer != null)
            {
                playerSpriteRenderer.color = baseColor;
            }
        }

        private void Update()
        {
            UpdateHitFlash();
            UpdateImmunityAura();
        }

        public void Configure(SpriteRenderer renderer, Health health, TemporaryDamageImmunity immunity)
        {
            Unsubscribe();
            playerSpriteRenderer = renderer;
            playerHealth = health;
            damageImmunity = immunity;
            if (playerSpriteRenderer != null)
            {
                baseColor = playerSpriteRenderer.color;
            }

            Subscribe();
            SetImmunityAuraActive(damageImmunity != null && damageImmunity.IsActive);
        }

        private void HandleDamaged(Health health, float amountApplied, GameObject source)
        {
            if (amountApplied <= 0f)
            {
                return;
            }

            hitFlashRemaining = Mathf.Max(hitFlashRemaining, hitFlashDuration);
        }

        private void HandleImmunityActivated(TemporaryDamageImmunity immunity, float remainingDuration)
        {
            SetImmunityAuraActive(true);
        }

        private void HandleImmunityRefreshed(TemporaryDamageImmunity immunity, float remainingDuration)
        {
            SetImmunityAuraActive(true);
        }

        private void HandleImmunityEnded(TemporaryDamageImmunity immunity)
        {
            SetImmunityAuraActive(false);
        }

        private void UpdateHitFlash()
        {
            if (playerSpriteRenderer == null)
            {
                return;
            }

            if (hitFlashRemaining > 0f)
            {
                hitFlashRemaining = Mathf.Max(0f, hitFlashRemaining - Time.deltaTime);
                var progress = 1f - Mathf.Clamp01(hitFlashRemaining / Mathf.Max(0.01f, hitFlashDuration));
                playerSpriteRenderer.color = Color.Lerp(HitFlashColor, baseColor, progress);
                return;
            }

            playerSpriteRenderer.color = baseColor;
        }

        private void UpdateImmunityAura()
        {
            if (immunityAuraRenderer == null || damageImmunity == null || !damageImmunity.IsActive)
            {
                return;
            }

            var pulse = 0.88f + (Mathf.Sin(Time.time * 12f) * 0.08f);
            var alphaPulse = 0.72f + (Mathf.Sin(Time.time * 10f) * 0.18f);
            immunityAura.transform.localScale = new Vector3(immunityAuraRadius * pulse, immunityAuraRadius * pulse, 1f);
            immunityAuraRenderer.color = new Color(
                ImmunityAuraColor.r,
                ImmunityAuraColor.g,
                ImmunityAuraColor.b,
                ImmunityAuraColor.a * alphaPulse);
        }

        private void EnsureImmunityAura()
        {
            if (immunityAura != null)
            {
                return;
            }

            var sprite = RuntimeSpriteLibrary.LoadSprite(immunityAuraSpritePath, 256f);
            if (sprite == null)
            {
                return;
            }

            immunityAura = new GameObject("ImmunityAura");
            immunityAura.transform.SetParent(transform, false);
            immunityAura.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            immunityAura.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            immunityAura.transform.localScale = new Vector3(immunityAuraRadius, immunityAuraRadius, 1f);

            immunityAuraRenderer = immunityAura.AddComponent<SpriteRenderer>();
            immunityAuraRenderer.sprite = sprite;
            immunityAuraRenderer.sortingOrder = 3;
            immunityAuraRenderer.color = ImmunityAuraColor;
        }

        private void SetImmunityAuraActive(bool isActive)
        {
            if (immunityAura == null)
            {
                EnsureImmunityAura();
            }

            if (immunityAura != null)
            {
                immunityAura.SetActive(isActive);
            }
        }

        private void Subscribe()
        {
            if (playerHealth != null)
            {
                playerHealth.Damaged -= HandleDamaged;
                playerHealth.Damaged += HandleDamaged;
            }

            if (damageImmunity != null)
            {
                damageImmunity.Activated -= HandleImmunityActivated;
                damageImmunity.Activated += HandleImmunityActivated;
                damageImmunity.Refreshed -= HandleImmunityRefreshed;
                damageImmunity.Refreshed += HandleImmunityRefreshed;
                damageImmunity.Ended -= HandleImmunityEnded;
                damageImmunity.Ended += HandleImmunityEnded;
            }
        }

        private void Unsubscribe()
        {
            if (playerHealth != null)
            {
                playerHealth.Damaged -= HandleDamaged;
            }

            if (damageImmunity != null)
            {
                damageImmunity.Activated -= HandleImmunityActivated;
                damageImmunity.Refreshed -= HandleImmunityRefreshed;
                damageImmunity.Ended -= HandleImmunityEnded;
            }
        }
    }
}
