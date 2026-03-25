using Dagon.Core;
using Dagon.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DrownedReliquary : MonoBehaviour
    {
        public readonly struct InteractionView
        {
            public InteractionView(string title, string subtitle, WeaponDefinition[] removableWeapons)
            {
                Title = title;
                Subtitle = subtitle;
                RemovableWeapons = removableWeapons ?? System.Array.Empty<WeaponDefinition>();
            }

            public string Title { get; }
            public string Subtitle { get; }
            public WeaponDefinition[] RemovableWeapons { get; }
        }

        private const string VisualSpriteResourcePath = "Sprites/Props/drowned_reliquary";
        private static readonly Color AvailableTint = new(0.80f, 0.88f, 0.82f, 0.98f);
        private static readonly Color AvailableGlowTint = new(0.44f, 0.16f, 0.16f, 0.28f);
        private static readonly Color DepletedTint = new(0.42f, 0.44f, 0.46f, 0.96f);
        private static readonly Color DepletedGlowTint = new(0.10f, 0.10f, 0.12f, 0.10f);

        [SerializeField] private float corruptionCost = 25f;
        [SerializeField] private float maxHealthCost = 4f;
        [SerializeField] private float interactionRadius = 2.2f;
        [SerializeField] private float visualScaleMultiplier = 1f;

        private static DrownedReliquary activeInteraction;

        private PlayerCombatLoadout combatLoadout;
        private Health playerHealth;
        private CorruptionMeter corruptionMeter;
        private Camera worldCamera;
        private SpriteRenderer mainRenderer;
        private SpriteRenderer glowRenderer;
        private Transform player;
        private Vector2Int altarCell;
        private bool isDepleted;

        public static bool HasActiveInteraction => activeInteraction != null;

        public static DrownedReliquary Create(
            Vector3 position,
            Camera camera,
            PlayerCombatLoadout loadout,
            Health health,
            CorruptionMeter meter,
            Vector2Int cell,
            bool depleted,
            float scaleMultiplier = 1f)
        {
            var altarObject = new GameObject("DrownedReliquary");
            altarObject.transform.position = position;

            var sphere = altarObject.AddComponent<SphereCollider>();
            sphere.radius = 1.2f;
            sphere.isTrigger = true;

            var rigidbody = altarObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            var altar = altarObject.AddComponent<DrownedReliquary>();
            altar.combatLoadout = loadout;
            altar.playerHealth = health;
            altar.corruptionMeter = meter;
            altar.worldCamera = camera;
            altar.altarCell = cell;
            altar.visualScaleMultiplier = Mathf.Max(0.1f, scaleMultiplier);
            altar.BuildVisuals();
            altar.SetDepleted(depleted);
            return altar;
        }

        public Vector2Int AltarCell => altarCell;
        public bool IsDepleted => isDepleted;

        private void Awake()
        {
            RefreshPlayerReference();
        }

        private void Update()
        {
            if (mainRenderer == null)
            {
                BuildVisuals();
            }

            RefreshPlayerReference();
            transform.Rotate(0f, 7f * Time.deltaTime, 0f, Space.World);

            if (glowRenderer != null)
            {
                var pulse = 0.55f + (Mathf.Sin(Time.time * 2.1f) * 0.16f);
                var tint = isDepleted ? DepletedGlowTint : AvailableGlowTint;
                glowRenderer.color = new Color(tint.r, tint.g, tint.b, tint.a * pulse);
            }

            if (isDepleted || player == null || Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
            {
                return;
            }

            if (!IsPlayerInRange())
            {
                return;
            }

            activeInteraction = this;
        }

        private void OnDestroy()
        {
            if (activeInteraction == this)
            {
                activeInteraction = null;
            }
        }

        private void OnGUI()
        {
            if (player == null || activeInteraction == this)
            {
                return;
            }

            if (!IsPlayerInRange())
            {
                return;
            }

            var rect = new Rect((Screen.width * 0.5f) - 110f, Screen.height - 116f, 220f, 24f);
            if (isDepleted)
            {
                GUI.Label(rect, "Reliquary Emptied");
                return;
            }

            GUI.Label(rect, HasRemovableWeapons() ? "Press E - Offer A Weapon" : "No Weapon To Offer");
        }

        public static InteractionView GetInteractionView()
        {
            if (activeInteraction == null)
            {
                return default;
            }

            var removable = activeInteraction.GetRemovableWeapons();
            var subtitle = removable.Length > 0
                ? "Choose one non-base weapon to surrender, then choose the price."
                : "You have no removable weapon to offer.";
            return new InteractionView("Drowned Reliquary", subtitle, removable);
        }

        public static bool ConfirmCorruptionCost(string weaponId)
        {
            return activeInteraction != null && activeInteraction.TrySacrificeWeapon(weaponId, useCorruptionCost: true);
        }

        public static bool ConfirmMaxHealthCost(string weaponId)
        {
            return activeInteraction != null && activeInteraction.TrySacrificeWeapon(weaponId, useCorruptionCost: false);
        }

        public static void CancelInteraction()
        {
            activeInteraction = null;
        }

        public void SetDepleted(bool depleted)
        {
            isDepleted = depleted;
            if (mainRenderer == null)
            {
                return;
            }

            mainRenderer.color = depleted ? DepletedTint : AvailableTint;
            if (glowRenderer != null)
            {
                glowRenderer.enabled = true;
                glowRenderer.color = depleted ? DepletedGlowTint : AvailableGlowTint;
            }
        }

        private bool TrySacrificeWeapon(string weaponId, bool useCorruptionCost)
        {
            if (isDepleted || string.IsNullOrWhiteSpace(weaponId) || combatLoadout == null)
            {
                return false;
            }

            if (!combatLoadout.RemoveWeapon(weaponId))
            {
                return false;
            }

            if (useCorruptionCost)
            {
                corruptionMeter?.AddCorruption(corruptionCost);
            }
            else if (playerHealth != null)
            {
                var nextMax = Mathf.Max(1f, playerHealth.MaxHealth - maxHealthCost);
                playerHealth.SetMaxHealth(nextMax, refillHealth: false);
            }

            SetDepleted(true);
            activeInteraction = null;
            return true;
        }

        private bool HasRemovableWeapons()
        {
            return GetRemovableWeapons().Length > 0;
        }

        private WeaponDefinition[] GetRemovableWeapons()
        {
            if (combatLoadout == null)
            {
                return System.Array.Empty<WeaponDefinition>();
            }

            var removable = new System.Collections.Generic.List<WeaponDefinition>();
            foreach (var weapon in combatLoadout.Weapons)
            {
                if (weapon == null || weapon.IsBaseWeapon || weapon.Definition == null)
                {
                    continue;
                }

                removable.Add(weapon.Definition);
            }

            return removable.ToArray();
        }

        private void RefreshPlayerReference()
        {
            if (player == null)
            {
                var playerMover = FindFirstObjectByType<PlayerMover>();
                player = playerMover != null ? playerMover.transform : null;
            }

            if (combatLoadout == null && player != null)
            {
                combatLoadout = player.GetComponent<PlayerCombatLoadout>();
            }

            if (playerHealth == null && player != null)
            {
                playerHealth = player.GetComponent<Health>();
            }

            if (corruptionMeter == null && player != null)
            {
                corruptionMeter = player.GetComponent<CorruptionMeter>();
            }
        }

        private bool IsPlayerInRange()
        {
            if (player == null)
            {
                return false;
            }

            var toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            return toPlayer.sqrMagnitude <= interactionRadius * interactionRadius;
        }

        private void BuildVisuals()
        {
            if (mainRenderer != null)
            {
                return;
            }

            var sprite = RuntimeSpriteLibrary.LoadSprite(VisualSpriteResourcePath, 256f) ??
                RuntimeSpriteLibrary.LoadSprite("Sprites/Props/corruption_fountain", 256f);

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(transform, false);
            visuals.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            visuals.transform.localScale = new Vector3(3.3f, 3.3f, 1f) * visualScaleMultiplier;

            mainRenderer = visuals.AddComponent<SpriteRenderer>();
            mainRenderer.sprite = sprite;
            mainRenderer.sortingOrder = 3;
            mainRenderer.color = AvailableTint;

            var glow = new GameObject("Glow");
            glow.transform.SetParent(visuals.transform, false);
            glow.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            glow.transform.localScale = new Vector3(1.08f, 1.08f, 1f);

            glowRenderer = glow.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = sprite;
            glowRenderer.sortingOrder = 2;
            glowRenderer.color = AvailableGlowTint;

            var billboard = visuals.AddComponent<Dagon.Rendering.BillboardSprite>();
            billboard.Configure(worldCamera != null ? worldCamera : Camera.main, Dagon.Rendering.BillboardMode.YAxisOnly);
        }
    }
}
