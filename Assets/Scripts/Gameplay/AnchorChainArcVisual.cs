using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class AnchorChainArcVisual : MonoBehaviour
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
            Vector3 aim,
            float radius,
            float arcAngle,
            float yawOffset,
            Camera worldCamera,
            AnchorChainWeapon.VisualResolved preset)
        {
            var effect = new GameObject("AnchorChainArcVisual");
            var yaw = Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg + yawOffset;
            effect.transform.position = origin + (Vector3.up * preset.HeightOffset);
            effect.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var component = effect.AddComponent<AnchorChainArcVisual>();
            component.Initialize(radius, arcAngle, preset);
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

        private void Initialize(float radius, float arcAngle, AnchorChainWeapon.VisualResolved preset)
        {
            duration = Mathf.Max(0.05f, preset.Duration);
            endScaleMultiplier = Mathf.Max(1f, preset.EndScaleMultiplier);
            baseTint = preset.Tint;

            meshFilter = gameObject.AddComponent<MeshFilter>();
            var renderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = BuildArcMesh(radius, arcAngle, preset);

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
            materialInstance = new Material(shader);
            materialInstance.mainTexture = ResolveTexture(preset.SpriteResourcePath);
            materialInstance.color = baseTint;
            renderer.sharedMaterial = materialInstance;
            renderer.sortingOrder = preset.SortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            startScale = Vector3.one;
            endScale = Vector3.one * endScaleMultiplier;
            transform.localScale = startScale;
        }

        private static Mesh BuildArcMesh(float radius, float arcAngle, AnchorChainWeapon.VisualResolved preset)
        {
            const int segmentCount = 16;

            var outerRadius = Mathf.Max(0.5f, radius * Mathf.Max(0.2f, preset.OuterRadiusMultiplier));
            var innerRadius = Mathf.Max(0.1f, outerRadius * Mathf.Clamp01(preset.InnerRadiusFactor));
            var clampedArc = Mathf.Clamp(arcAngle, 10f, 180f);
            var startAngle = -clampedArc * 0.5f;
            var step = clampedArc / segmentCount;

            var vertices = new Vector3[(segmentCount + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segmentCount * 6];

            for (var i = 0; i <= segmentCount; i++)
            {
                var angle = startAngle + (step * i);
                var radians = angle * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
                var innerIndex = i * 2;
                var outerIndex = innerIndex + 1;
                var t = i / (float)segmentCount;

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
                name = "AnchorChainArc"
            };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Texture2D ResolveTexture(string resourcePath)
        {
            if (!string.IsNullOrWhiteSpace(resourcePath))
            {
                var texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    return texture;
                }
            }

            if (fallbackTexture == null)
            {
                fallbackTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "AnchorChainFallback"
                };
                fallbackTexture.SetPixel(0, 0, Color.white);
                fallbackTexture.Apply(false, true);
            }

            return fallbackTexture;
        }
    }
}
