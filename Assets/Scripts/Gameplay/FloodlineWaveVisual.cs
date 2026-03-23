using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class FloodlineWaveVisual : MonoBehaviour
    {
        private static Texture2D fallbackTexture;

        private MeshFilter meshFilter;
        private Material materialInstance;
        private float duration;
        private float timer;
        private Color baseTint;
        private Vector3 startScale = Vector3.one;
        private Vector3 endScale = Vector3.one;

        internal static void Attach(
            Transform parent,
            float gameplayLength,
            float gameplayHalfWidth,
            float visualDuration,
            FloodlineWeapon.VisualResolved preset)
        {
            var effect = new GameObject("FloodlineWaveVisual");
            effect.transform.SetParent(parent, false);
            effect.transform.localPosition = new Vector3(0f, preset.HeightOffset, 0f);
            effect.transform.localRotation = Quaternion.identity;

            var component = effect.AddComponent<FloodlineWaveVisual>();
            component.Initialize(gameplayLength, gameplayHalfWidth, visualDuration, preset);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            var progress = duration > 0.0001f ? Mathf.Clamp01(timer / duration) : 1f;
            transform.localScale = Vector3.Lerp(startScale, endScale, progress);

            if (materialInstance != null)
            {
                var color = baseTint;
                color.a *= 1f - (progress * 0.55f);
                materialInstance.color = color;
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

        private void Initialize(float gameplayLength, float gameplayHalfWidth, float visualDuration, FloodlineWeapon.VisualResolved preset)
        {
            duration = Mathf.Max(0.05f, visualDuration);
            baseTint = preset.Tint;

            meshFilter = gameObject.AddComponent<MeshFilter>();
            var renderer = gameObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = BuildWaveMesh(gameplayLength, gameplayHalfWidth, preset);

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
            endScale = new Vector3(preset.EndScaleMultiplier, 1f, preset.EndScaleMultiplier);
            transform.localScale = startScale;
        }

        private static Mesh BuildWaveMesh(float gameplayLength, float gameplayHalfWidth, FloodlineWeapon.VisualResolved preset)
        {
            var length = Mathf.Max(0.4f, gameplayLength * Mathf.Max(0.2f, preset.LengthMultiplier));
            var halfWidth = Mathf.Max(0.25f, gameplayHalfWidth * Mathf.Max(0.2f, preset.WidthMultiplier));
            var leadingWidth = halfWidth * 1.08f;
            var trailingWidth = halfWidth * 0.88f;
            var leadingEdge = length * 0.5f;
            var trailingEdge = -length * 0.5f;

            var mesh = new Mesh
            {
                name = "FloodlineWave"
            };
            mesh.vertices = new[]
            {
                new Vector3(trailingEdge, 0f, -trailingWidth),
                new Vector3(trailingEdge, 0f, trailingWidth),
                new Vector3(leadingEdge, 0f, leadingWidth),
                new Vector3(leadingEdge, 0f, -leadingWidth)
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
                    name = "FloodlineFallback"
                };
                fallbackTexture.SetPixel(0, 0, Color.white);
                fallbackTexture.Apply(false, true);
            }

            return fallbackTexture;
        }
    }
}
