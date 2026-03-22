using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RotLanternRadiusVisual : MonoBehaviour
    {
        private static Texture2D fallbackTexture;

        private MeshFilter meshFilter;
        private Material materialInstance;
        private float duration;
        private float timer;
        private float endScaleMultiplier;
        private Color baseTint;
        private Vector3 startScale = Vector3.one;
        private Vector3 endScale = Vector3.one;

        internal static void Spawn(
            Vector3 origin,
            float radius,
            float heightOffset,
            float ringThickness,
            Color tint,
            float duration,
            float endScaleMultiplier,
            int sortingOrder)
        {
            var effect = new GameObject("RotLanternRadiusVisual");
            effect.transform.position = origin + (Vector3.up * heightOffset);

            var component = effect.AddComponent<RotLanternRadiusVisual>();
            component.Initialize(radius, ringThickness, tint, duration, endScaleMultiplier, sortingOrder);
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

        private void Initialize(float radius, float ringThickness, Color tint, float visualDuration, float scaleMultiplier, int sortingOrder)
        {
            duration = Mathf.Max(0.05f, visualDuration);
            endScaleMultiplier = Mathf.Max(1f, scaleMultiplier);
            baseTint = tint;

            meshFilter = gameObject.AddComponent<MeshFilter>();
            var renderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = BuildRingMesh(radius, ringThickness);

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
            endScale = Vector3.one * endScaleMultiplier;
            transform.localScale = startScale;
        }

        private static Mesh BuildRingMesh(float radius, float ringThickness)
        {
            const int segmentCount = 40;
            var outerRadius = Mathf.Max(0.25f, radius);
            var innerRadius = Mathf.Max(0.05f, outerRadius - Mathf.Clamp(ringThickness, 0.02f, outerRadius - 0.02f));

            var vertices = new Vector3[(segmentCount + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segmentCount * 6];

            for (var i = 0; i <= segmentCount; i++)
            {
                var t = i / (float)segmentCount;
                var radians = t * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
                var innerIndex = i * 2;
                var outerIndex = innerIndex + 1;

                vertices[innerIndex] = direction * innerRadius;
                vertices[outerIndex] = direction * outerRadius;
                uvs[innerIndex] = new Vector2(t, 0f);
                uvs[outerIndex] = new Vector2(t, 1f);

                if (i == segmentCount)
                {
                    continue;
                }

                var triIndex = i * 6;
                triangles[triIndex + 0] = innerIndex;
                triangles[triIndex + 1] = outerIndex;
                triangles[triIndex + 2] = outerIndex + 2;
                triangles[triIndex + 3] = innerIndex;
                triangles[triIndex + 4] = outerIndex + 2;
                triangles[triIndex + 5] = innerIndex + 2;
            }

            var mesh = new Mesh
            {
                name = "RotLanternRadiusRing"
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
                    name = "RotLanternRadiusFallback"
                };
                fallbackTexture.SetPixel(0, 0, Color.white);
                fallbackTexture.Apply(false, true);
            }

            return fallbackTexture;
        }
    }
}
