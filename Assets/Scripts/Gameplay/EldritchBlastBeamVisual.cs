using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EldritchBlastBeamVisual : MonoBehaviour
    {
        private static Texture2D fallbackTexture;

        private Material coreMaterial;
        private Material glowMaterial;
        private Mesh coreMesh;
        private Mesh glowMesh;
        private float duration;
        private float timer;
        private Vector3 startScale = Vector3.one;
        private Vector3 endScale = Vector3.one;
        private Color baseCoreTint;
        private Color baseGlowTint;

        internal static void Spawn(
            Vector3 origin,
            Vector3 direction,
            float length,
            float halfWidth,
            EldritchBlastWeapon.VisualResolved preset)
        {
            var effect = new GameObject("EldritchBlastBeamVisual");
            var normalizedDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
            var yaw = Mathf.Atan2(-normalizedDirection.z, normalizedDirection.x) * Mathf.Rad2Deg;
            effect.transform.position = origin + (normalizedDirection * (length * 0.5f)) + (Vector3.up * preset.HeightOffset);
            effect.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var component = effect.AddComponent<EldritchBlastBeamVisual>();
            component.Initialize(length, halfWidth, preset);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            var progress = duration > 0.0001f ? Mathf.Clamp01(timer / duration) : 1f;
            transform.localScale = Vector3.Lerp(startScale, endScale, progress);

            if (coreMaterial != null)
            {
                var color = baseCoreTint;
                color.a *= 1f - progress;
                coreMaterial.color = color;
            }

            if (glowMaterial != null)
            {
                var color = baseGlowTint;
                color.a *= 1f - (progress * 0.9f);
                glowMaterial.color = color;
            }

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (coreMaterial != null)
            {
                Destroy(coreMaterial);
            }

            if (glowMaterial != null)
            {
                Destroy(glowMaterial);
            }

            if (coreMesh != null)
            {
                Destroy(coreMesh);
            }

            if (glowMesh != null)
            {
                Destroy(glowMesh);
            }
        }

        private void Initialize(float length, float halfWidth, EldritchBlastWeapon.VisualResolved preset)
        {
            duration = Mathf.Max(0.05f, preset.Duration);
            baseCoreTint = preset.CoreTint;
            baseGlowTint = preset.GlowTint;

            var glowObject = new GameObject("Glow");
            glowObject.transform.SetParent(transform, false);
            var glowFilter = glowObject.AddComponent<MeshFilter>();
            var glowRenderer = glowObject.AddComponent<MeshRenderer>();
            glowMesh = BuildBeamMesh(length, halfWidth * Mathf.Max(1f, preset.WidthMultiplier));
            glowFilter.sharedMesh = glowMesh;
            glowMaterial = CreateMaterial(ResolveTexture(preset.SpriteResourcePath), baseGlowTint);
            glowRenderer.sharedMaterial = glowMaterial;
            glowRenderer.sortingOrder = preset.SortingOrder;
            ConfigureRenderer(glowRenderer);

            var coreObject = new GameObject("Core");
            coreObject.transform.SetParent(transform, false);
            var coreFilter = coreObject.AddComponent<MeshFilter>();
            var coreRenderer = coreObject.AddComponent<MeshRenderer>();
            coreMesh = BuildBeamMesh(length, halfWidth * 0.42f);
            coreFilter.sharedMesh = coreMesh;
            coreMaterial = CreateMaterial(ResolveTexture(preset.SpriteResourcePath), baseCoreTint);
            coreRenderer.sharedMaterial = coreMaterial;
            coreRenderer.sortingOrder = preset.SortingOrder + 1;
            ConfigureRenderer(coreRenderer);

            startScale = Vector3.one;
            endScale = new Vector3(preset.EndScaleMultiplier, 1f, Mathf.Lerp(1f, preset.EndScaleMultiplier, 0.35f));
            transform.localScale = startScale;
        }

        private static void ConfigureRenderer(Renderer renderer)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        private static Material CreateMaterial(Texture texture, Color tint)
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
            var material = new Material(shader);
            material.mainTexture = texture;
            material.color = tint;
            return material;
        }

        private static Mesh BuildBeamMesh(float length, float halfWidth)
        {
            var resolvedLength = Mathf.Max(0.5f, length);
            var resolvedHalfWidth = Mathf.Max(0.05f, halfWidth);
            var mesh = new Mesh
            {
                name = "EldritchBlastBeam"
            };
            mesh.vertices = new[]
            {
                new Vector3(-resolvedLength * 0.5f, 0f, -resolvedHalfWidth),
                new Vector3(-resolvedLength * 0.5f, 0f, resolvedHalfWidth),
                new Vector3(resolvedLength * 0.5f, 0f, resolvedHalfWidth),
                new Vector3(resolvedLength * 0.5f, 0f, -resolvedHalfWidth)
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
                    name = "EldritchBlastFallback"
                };
                fallbackTexture.SetPixel(0, 0, Color.white);
                fallbackTexture.Apply(false, true);
            }

            return fallbackTexture;
        }
    }
}
