using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RectOutlineVisual : MonoBehaviour
    {
        private static Texture2D fallbackTexture;

        private MeshFilter meshFilter;
        private Material materialInstance;
        private float duration;
        private float timer;
        private Color baseTint;
        private Vector3 startScale = Vector3.one;
        private Vector3 endScale = Vector3.one;

        internal static void Spawn(
            Vector3 origin,
            float width,
            float length,
            float heightOffset,
            float outlineThickness,
            float yaw,
            Color tint,
            float duration,
            float endScaleMultiplier,
            int sortingOrder,
            string name = "RectOutlineVisual")
        {
            var effect = new GameObject(name);
            effect.transform.position = origin + (Vector3.up * heightOffset);
            effect.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var component = effect.AddComponent<RectOutlineVisual>();
            component.Initialize(width, length, outlineThickness, tint, duration, endScaleMultiplier, sortingOrder);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            var progress = duration > 0.0001f ? Mathf.Clamp01(timer / duration) : 1f;
            transform.localScale = Vector3.Lerp(startScale, endScale, progress);

            if (materialInstance != null)
            {
                var color = baseTint;
                color.a *= 1f - progress;
                materialInstance.color = color;
            }

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (materialInstance != null)
            {
                Destroy(materialInstance);
            }

            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Destroy(meshFilter.sharedMesh);
            }
        }

        private void Initialize(
            float width,
            float length,
            float outlineThickness,
            Color tint,
            float visualDuration,
            float scaleMultiplier,
            int sortingOrder)
        {
            duration = Mathf.Max(0.05f, visualDuration);
            baseTint = tint;

            meshFilter = gameObject.AddComponent<MeshFilter>();
            var renderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = BuildRectOutlineMesh(width, length, outlineThickness);

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
            materialInstance = new Material(shader);
            materialInstance.mainTexture = ResolveTexture();
            materialInstance.color = baseTint;
            renderer.sharedMaterial = materialInstance;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            startScale = Vector3.one;
            endScale = Vector3.one * Mathf.Max(1f, scaleMultiplier);
            transform.localScale = startScale;
        }

        private static Mesh BuildRectOutlineMesh(float width, float length, float outlineThickness)
        {
            var safeWidth = Mathf.Max(0.15f, width);
            var safeLength = Mathf.Max(0.2f, length);
            var maxThickness = Mathf.Min(safeWidth, safeLength) * 0.5f - 0.01f;
            var safeThickness = Mathf.Clamp(outlineThickness, 0.02f, Mathf.Max(0.02f, maxThickness));

            var outerHalfWidth = safeWidth * 0.5f;
            var outerHalfLength = safeLength * 0.5f;
            var innerHalfWidth = Mathf.Max(0.01f, outerHalfWidth - safeThickness);
            var innerHalfLength = Mathf.Max(0.01f, outerHalfLength - safeThickness);

            var vertices = new[]
            {
                new Vector3(-innerHalfWidth, 0f, -innerHalfLength),
                new Vector3(-outerHalfWidth, 0f, -outerHalfLength),
                new Vector3(innerHalfWidth, 0f, -innerHalfLength),
                new Vector3(outerHalfWidth, 0f, -outerHalfLength),
                new Vector3(innerHalfWidth, 0f, innerHalfLength),
                new Vector3(outerHalfWidth, 0f, outerHalfLength),
                new Vector3(-innerHalfWidth, 0f, innerHalfLength),
                new Vector3(-outerHalfWidth, 0f, outerHalfLength)
            };

            var uvs = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0.25f, 0f),
                new Vector2(0.25f, 1f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 1f),
                new Vector2(0.75f, 0f),
                new Vector2(0.75f, 1f)
            };

            var triangles = new[]
            {
                0, 1, 3,
                0, 3, 2,
                2, 3, 5,
                2, 5, 4,
                4, 5, 7,
                4, 7, 6,
                6, 7, 1,
                6, 1, 0
            };

            var mesh = new Mesh
            {
                name = "RectOutlineMesh"
            };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Texture2D ResolveTexture()
        {
            if (fallbackTexture == null)
            {
                fallbackTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "RectOutlineFallback"
                };
                fallbackTexture.SetPixel(0, 0, Color.white);
                fallbackTexture.Apply(false, true);
            }

            return fallbackTexture;
        }
    }
}
