using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CombatVolumeDebugOverlay : MonoBehaviour
    {
        private static readonly Color HurtColor = new(0.25f, 0.95f, 0.35f, 0.95f);
        private static readonly Color BodyColor = new(0.20f, 0.72f, 1f, 0.95f);
        private static readonly Color AttackColor = new(1f, 0.55f, 0.18f, 0.95f);

        private static Material lineMaterial;

        [SerializeField] private bool visible;

        public bool Visible => visible;

        public void SetVisible(bool shouldShow)
        {
            visible = shouldShow;
        }

        private void OnRenderObject()
        {
            if (!visible)
            {
                return;
            }

            EnsureMaterial();
            if (lineMaterial == null)
            {
                return;
            }

            lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.LINES);

            DrawCombatColliders();
            DrawBodyBlockers();

            GL.End();
            GL.PopMatrix();
        }

        private static void EnsureMaterial()
        {
            if (lineMaterial != null)
            {
                return;
            }

            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                return;
            }

            lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }

        private static void DrawCombatColliders()
        {
            var colliders = FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var index = 0; index < colliders.Length; index++)
            {
                var collider = colliders[index];
                if (!IsCombatRelevant(collider, out var color))
                {
                    continue;
                }

                DrawWireBounds(collider.bounds, color);
            }
        }

        private static void DrawBodyBlockers()
        {
            var blockers = BodyBlocker.Active;
            for (var index = 0; index < blockers.Count; index++)
            {
                var blocker = blockers[index];
                if (blocker == null || !blocker.isActiveAndEnabled)
                {
                    continue;
                }

                DrawCircle(blocker.PlanarPosition + Vector3.up * 0.05f, blocker.BodyRadius, BodyColor);
            }
        }

        private static bool IsCombatRelevant(Collider collider, out Color color)
        {
            color = default;
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (collider.GetComponentInParent<Hurtbox>() != null)
            {
                color = HurtColor;
                return true;
            }

            if (collider.GetComponentInParent<ContactDamage>() != null ||
                collider.GetComponentInParent<HarpoonProjectile>() != null ||
                collider.GetComponentInParent<DrownedAcolyteProjectile>() != null ||
                collider.GetComponentInParent<EnemyHazardZone>() != null ||
                collider.GetComponentInParent<MermaidBrinePool>() != null ||
                collider.GetComponentInParent<WatcherEyeMarkZone>() != null)
            {
                color = AttackColor;
                return true;
            }

            return false;
        }

        private static void DrawWireBounds(Bounds bounds, Color color)
        {
            var min = bounds.min;
            var max = bounds.max;

            var p0 = new Vector3(min.x, min.y, min.z);
            var p1 = new Vector3(max.x, min.y, min.z);
            var p2 = new Vector3(max.x, max.y, min.z);
            var p3 = new Vector3(min.x, max.y, min.z);
            var p4 = new Vector3(min.x, min.y, max.z);
            var p5 = new Vector3(max.x, min.y, max.z);
            var p6 = new Vector3(max.x, max.y, max.z);
            var p7 = new Vector3(min.x, max.y, max.z);

            DrawLine(p0, p1, color);
            DrawLine(p1, p2, color);
            DrawLine(p2, p3, color);
            DrawLine(p3, p0, color);

            DrawLine(p4, p5, color);
            DrawLine(p5, p6, color);
            DrawLine(p6, p7, color);
            DrawLine(p7, p4, color);

            DrawLine(p0, p4, color);
            DrawLine(p1, p5, color);
            DrawLine(p2, p6, color);
            DrawLine(p3, p7, color);
        }

        private static void DrawCircle(Vector3 center, float radius, Color color, int segments = 20)
        {
            var previous = center + new Vector3(radius, 0f, 0f);
            for (var segment = 1; segment <= segments; segment++)
            {
                var angle = (segment / (float)segments) * Mathf.PI * 2f;
                var next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                DrawLine(previous, next, color);
                previous = next;
            }
        }

        private static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            GL.Color(color);
            GL.Vertex(start);
            GL.Vertex(end);
        }
    }
}
