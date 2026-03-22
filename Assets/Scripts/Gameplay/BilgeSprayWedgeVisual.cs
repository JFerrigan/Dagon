using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BilgeSprayWedgeVisual : MonoBehaviour
    {
        private static Texture2D fallbackTexture;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Material materialInstance;
        private float duration;
        private float endScaleMultiplier;
        private float timer;
        private Color baseTint;
        private Vector3 startScale = Vector3.one;
        private Vector3 endScale = Vector3.one;

        internal static void Spawn(
            Vector3 origin,
            Vector3 aim,
            float range,
            float coneAngle,
            Camera worldCamera,
            BilgeSprayWeapon.VisualResolved preset)
        {
            var effect = new GameObject("BilgeSprayWedgeVisual");
            var yaw = Mathf.Atan2(-aim.z, aim.x) * Mathf.Rad2Deg;
            var forwardOffset = aim * (range * Mathf.Max(0f, preset.ForwardOffsetNormalized));
            effect.transform.position = origin + forwardOffset + (Vector3.up * preset.HeightOffset);
            effect.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var component = effect.AddComponent<BilgeSprayWedgeVisual>();
            component.Initialize(range, coneAngle, preset);
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

        private void Initialize(float range, float coneAngle, BilgeSprayWeapon.VisualResolved preset)
        {
            duration = Mathf.Max(0.05f, preset.Duration);
            endScaleMultiplier = Mathf.Max(1f, preset.EndScaleMultiplier);
            baseTint = preset.Tint;

            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = BuildWedgeMesh(range, coneAngle, preset);

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
            materialInstance = new Material(shader);
            materialInstance.mainTexture = ResolveTexture(preset.SpriteResourcePath);
            materialInstance.color = baseTint;
            meshRenderer.sharedMaterial = materialInstance;
            meshRenderer.sortingOrder = preset.SortingOrder;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            startScale = Vector3.one;
            endScale = Vector3.one * endScaleMultiplier;
            transform.localScale = startScale;
        }

        private static Mesh BuildWedgeMesh(float range, float coneAngle, BilgeSprayWeapon.VisualResolved preset)
        {
            var effectiveRange = Mathf.Max(0.5f, range * Mathf.Max(0.2f, preset.LengthMultiplier));
            var farHalfWidth = Mathf.Max(
                0.35f,
                Mathf.Sin(Mathf.Deg2Rad * Mathf.Clamp(coneAngle, 10f, 170f) * 0.5f) * range * Mathf.Max(0.2f, preset.WidthMultiplier));
            var nearHalfWidth = Mathf.Max(0.12f, farHalfWidth * Mathf.Clamp01(preset.NearWidthFactor));

            var mesh = new Mesh
            {
                name = "BilgeSprayWedge"
            };
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, -nearHalfWidth),
                new Vector3(0f, 0f, nearHalfWidth),
                new Vector3(effectiveRange, 0f, farHalfWidth),
                new Vector3(effectiveRange, 0f, -farHalfWidth)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
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
                    name = "BilgeSprayFallback"
                };
                fallbackTexture.SetPixel(0, 0, Color.white);
                fallbackTexture.Apply(false, true);
            }

            return fallbackTexture;
        }
    }
}
