using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MonolithAuraAreaVisual : MonoBehaviour
    {
        private static Texture2D fallbackTexture;
        private const float PulseSpeed = 2.1f;
        private const float PulseScaleAmplitude = 0.06f;
        private const float BaseAlpha = 0.5f;
        private const float PulseAlphaAmplitude = 0.28f;

        private MeshFilter ringFilter;
        private Material ringMaterial;
        private float radius;
        private float ringThickness;
        private Color baseTint = Color.white;

        public void Configure(float auraRadius, float thickness, Color tint, int sortingOrder)
        {
            radius = Mathf.Max(0.5f, auraRadius);
            ringThickness = Mathf.Clamp(thickness, 0.04f, radius - 0.05f);
            EnsureVisuals(sortingOrder);
            RebuildMeshes();
            SetTint(tint);
        }

        public void SetRadius(float auraRadius, float thickness)
        {
            var resolvedRadius = Mathf.Max(0.5f, auraRadius);
            var resolvedThickness = Mathf.Clamp(thickness, 0.04f, resolvedRadius - 0.05f);
            if (Mathf.Approximately(radius, resolvedRadius) && Mathf.Approximately(ringThickness, resolvedThickness))
            {
                return;
            }

            radius = resolvedRadius;
            ringThickness = resolvedThickness;
            RebuildMeshes();
        }

        public void SetTint(Color tint)
        {
            baseTint = tint;
            if (ringMaterial != null)
            {
                UpdatePulseVisual();
            }
        }

        private void Update()
        {
            UpdatePulseVisual();
        }

        private void OnDestroy()
        {
            if (ringMaterial != null)
            {
                Destroy(ringMaterial);
            }

            if (ringFilter != null && ringFilter.sharedMesh != null)
            {
                Destroy(ringFilter.sharedMesh);
            }
        }

        private void EnsureVisuals(int sortingOrder)
        {
            if (ringFilter == null)
            {
                var ringObject = new GameObject("AuraRing");
                ringObject.transform.SetParent(transform, false);
                ringFilter = ringObject.AddComponent<MeshFilter>();
                var renderer = ringObject.AddComponent<MeshRenderer>();
                ringMaterial = CreateMaterial();
                renderer.sharedMaterial = ringMaterial;
                renderer.sortingOrder = sortingOrder + 1;
                ConfigureRenderer(renderer);
            }
        }

        private void RebuildMeshes()
        {
            if (ringFilter == null)
            {
                return;
            }

            if (ringFilter.sharedMesh != null)
            {
                Destroy(ringFilter.sharedMesh);
            }

            ringFilter.sharedMesh = BuildRingMesh(radius, ringThickness);
        }

        private void UpdatePulseVisual()
        {
            if (ringMaterial == null || ringFilter == null)
            {
                return;
            }

            var pulse = (Mathf.Sin(Time.time * PulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            var tint = baseTint;
            tint.a *= BaseAlpha + (pulse * PulseAlphaAmplitude);
            ringMaterial.color = tint;

            var scale = 1f + ((pulse - 0.5f) * 2f * PulseScaleAmplitude);
            ringFilter.transform.localScale = new Vector3(scale, 1f, scale);
        }

        private static void ConfigureRenderer(Renderer renderer)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        private static Material CreateMaterial()
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
            var material = new Material(shader);
            material.mainTexture = ResolveTexture();
            return material;
        }

        private static Mesh BuildRingMesh(float radius, float thickness)
        {
            const int segmentCount = 40;
            var outerRadius = Mathf.Max(0.25f, radius);
            var innerRadius = Mathf.Max(0.05f, outerRadius - thickness);
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
                triangles[triIndex] = innerIndex;
                triangles[triIndex + 1] = outerIndex;
                triangles[triIndex + 2] = outerIndex + 2;
                triangles[triIndex + 3] = innerIndex;
                triangles[triIndex + 4] = outerIndex + 2;
                triangles[triIndex + 5] = innerIndex + 2;
            }

            var mesh = new Mesh { name = "MonolithAuraRing" };
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
                    name = "MonolithAuraTexture"
                };
                fallbackTexture.SetPixel(0, 0, Color.white);
                fallbackTexture.Apply(false, true);
            }

            return fallbackTexture;
        }
    }
}
