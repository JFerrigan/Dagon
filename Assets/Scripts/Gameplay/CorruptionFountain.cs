using Dagon.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CorruptionFountain : MonoBehaviour
    {
        private const string VisualSpriteResourcePath = "Sprites/Props/corruption_fountain";
        private static readonly Color AvailableTint = new(0.78f, 0.96f, 0.90f, 0.98f);
        private static readonly Color AvailableGlowTint = new(0.22f, 0.94f, 0.74f, 0.36f);
        private static readonly Color DepletedTint = new(0.44f, 0.52f, 0.50f, 0.96f);
        private static readonly Color DepletedGlowTint = new(0.12f, 0.22f, 0.20f, 0.12f);

        [SerializeField] private float cleanseAmount = 25f;
        [SerializeField] private float interactionRadius = 2.1f;

        private CorruptionMeter corruptionMeter;
        private Camera worldCamera;
        private SpriteRenderer mainRenderer;
        private SpriteRenderer glowRenderer;
        private Transform player;
        private Vector2Int fountainCell;
        private bool isDepleted;

        public static CorruptionFountain Create(Vector3 position, float cleanseValue, Camera camera, CorruptionMeter targetMeter, Vector2Int cell, bool depleted)
        {
            var fountainObject = new GameObject("CorruptionFountain");
            fountainObject.transform.position = position;

            var sphere = fountainObject.AddComponent<SphereCollider>();
            sphere.radius = 1.15f;
            sphere.isTrigger = true;

            var rigidbody = fountainObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            var fountain = fountainObject.AddComponent<CorruptionFountain>();
            fountain.cleanseAmount = Mathf.Max(1f, cleanseValue);
            fountain.corruptionMeter = targetMeter;
            fountain.worldCamera = camera;
            fountain.fountainCell = cell;
            fountain.BuildVisuals();
            fountain.SetDepleted(depleted);

            return fountain;
        }

        public Vector2Int FountainCell => fountainCell;
        public bool IsDepleted => isDepleted;

        private void Awake()
        {
            if (player == null)
            {
                var playerMover = FindFirstObjectByType<PlayerMover>();
                player = playerMover != null ? playerMover.transform : null;
            }
        }

        private void Update()
        {
            if (mainRenderer == null)
            {
                BuildVisuals();
            }

            if (player == null)
            {
                var playerMover = FindFirstObjectByType<PlayerMover>();
                player = playerMover != null ? playerMover.transform : null;
            }

            transform.Rotate(0f, 12f * Time.deltaTime, 0f, Space.World);

            if (glowRenderer != null)
            {
                var pulse = 0.55f + (Mathf.Sin(Time.time * 2.6f) * 0.18f);
                var tint = isDepleted ? DepletedGlowTint : AvailableGlowTint;
                glowRenderer.color = new Color(tint.r, tint.g, tint.b, tint.a * pulse);
            }

            if (isDepleted || player == null || Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
            {
                return;
            }

            var toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > interactionRadius * interactionRadius)
            {
                return;
            }

            corruptionMeter?.ReduceCorruption(cleanseAmount);
            SetDepleted(true);
        }

        private void OnGUI()
        {
            if (isDepleted || player == null)
            {
                return;
            }

            var toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > interactionRadius * interactionRadius)
            {
                return;
            }

            var rect = new Rect((Screen.width * 0.5f) - 90f, Screen.height - 88f, 180f, 24f);
            GUI.Label(rect, "Press E - Cleanse Corruption");
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

        private void BuildVisuals()
        {
            if (mainRenderer != null)
            {
                return;
            }

            var sprite = RuntimeSpriteLibrary.LoadSprite(VisualSpriteResourcePath, 256f) ??
                RuntimeSpriteLibrary.LoadSprite("Sprites/Pickups/barnacle_shard", 256f);

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(transform, false);
            visuals.transform.localPosition = new Vector3(0f, 0.34f, 0f);
            visuals.transform.localScale = new Vector3(3.4f, 3.4f, 1f);

            mainRenderer = visuals.AddComponent<SpriteRenderer>();
            mainRenderer.sprite = sprite;
            mainRenderer.sortingOrder = 3;
            mainRenderer.color = AvailableTint;

            var glow = new GameObject("Glow");
            glow.transform.SetParent(visuals.transform, false);
            glow.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            glow.transform.localScale = new Vector3(1.12f, 1.12f, 1f);

            glowRenderer = glow.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = sprite;
            glowRenderer.sortingOrder = 2;
            glowRenderer.color = AvailableGlowTint;

            var billboard = visuals.AddComponent<Dagon.Rendering.BillboardSprite>();
            billboard.Configure(worldCamera != null ? worldCamera : Camera.main, Dagon.Rendering.BillboardSprite.BillboardMode.YAxisOnly);
        }
    }
}
