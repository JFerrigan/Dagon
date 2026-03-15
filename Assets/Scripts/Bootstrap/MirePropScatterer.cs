using Dagon.Core;
using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class MirePropScatterer : MonoBehaviour
    {
        private readonly struct PropVisual
        {
            public PropVisual(string name, Sprite sprite, Vector3 baseScale, int sortingOrder)
            {
                Name = name;
                Sprite = sprite;
                BaseScale = baseScale;
                SortingOrder = sortingOrder;
            }

            public string Name { get; }
            public Sprite Sprite { get; }
            public Vector3 BaseScale { get; }
            public int SortingOrder { get; }
        }

        [SerializeField] private Camera worldCamera;
        [SerializeField] private int propCount = 6;
        [SerializeField] private float innerRadius = 4f;
        [SerializeField] private float outerRadius = 14f;

        public void Configure(Camera cameraReference)
        {
            worldCamera = cameraReference;
        }

        private void Start()
        {
            var propVisuals = new[]
            {
                new PropVisual("HarpoonProp", RuntimeSpriteLibrary.LoadSprite("Sprites/Props/harpoon_ground_prop"), new Vector3(0.05f, 0.05f, 1f), 1),
                new PropVisual("BarrelProp", RuntimeSpriteLibrary.LoadSprite("Sprites/Props/barrel_ground_prop"), new Vector3(0.11f, 0.11f, 1f), 2)
            };

            if (worldCamera == null)
            {
                return;
            }

            for (var i = 0; i < propCount; i++)
            {
                var selectedVisual = propVisuals[Random.Range(0, propVisuals.Length)];
                if (selectedVisual.Sprite == null)
                {
                    continue;
                }

                var angle = Random.Range(0f, Mathf.PI * 2f);
                var radius = Random.Range(innerRadius, outerRadius);
                var position = new Vector3(Mathf.Cos(angle) * radius, 0.03f, Mathf.Sin(angle) * radius);

                var prop = new GameObject($"{selectedVisual.Name}_{i + 1}");
                prop.transform.SetParent(transform);
                prop.transform.position = position;

                var visuals = new GameObject("Visuals");
                visuals.transform.SetParent(prop.transform, false);
                visuals.transform.localPosition = Vector3.zero;
                visuals.transform.localScale = selectedVisual.BaseScale * Random.Range(0.8f, 1.15f);

                var renderer = visuals.AddComponent<SpriteRenderer>();
                renderer.sprite = selectedVisual.Sprite;
                renderer.sortingOrder = selectedVisual.SortingOrder;
                renderer.color = new Color(1f, 1f, 1f, 0.92f);

                var billboard = visuals.AddComponent<BillboardSprite>();
                billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);
            }
        }
    }
}
