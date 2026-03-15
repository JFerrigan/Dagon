using Dagon.Core;
using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class MirePropScatterer : MonoBehaviour
    {
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
            var sprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Props/harpoon_ground_prop");
            if (sprite == null || worldCamera == null)
            {
                return;
            }

            for (var i = 0; i < propCount; i++)
            {
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var radius = Random.Range(innerRadius, outerRadius);
                var position = new Vector3(Mathf.Cos(angle) * radius, 0.03f, Mathf.Sin(angle) * radius);

                var prop = new GameObject($"HarpoonProp_{i + 1}");
                prop.transform.SetParent(transform);
                prop.transform.position = position;

                var visuals = new GameObject("Visuals");
                visuals.transform.SetParent(prop.transform, false);
                visuals.transform.localPosition = Vector3.zero;
                visuals.transform.localScale = new Vector3(0.05f, 0.05f, 1f) * Random.Range(0.8f, 1.15f);

                var renderer = visuals.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 1;
                renderer.color = new Color(1f, 1f, 1f, 0.92f);

                var billboard = visuals.AddComponent<BillboardSprite>();
                billboard.Configure(worldCamera, BillboardSprite.BillboardMode.YAxisOnly);
            }
        }
    }
}
