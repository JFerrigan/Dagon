using UnityEngine;

namespace Dagon.Gameplay
{
    public static class CombatVolumeAlignment
    {
        public static bool TryAlignCapsuleToSpriteCenter(Transform root, CapsuleCollider capsule)
        {
            if (root == null || capsule == null)
            {
                return false;
            }

            var renderer = root.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null)
            {
                return false;
            }

            var localSpriteCenter = root.InverseTransformPoint(renderer.bounds.center);
            capsule.center = new Vector3(capsule.center.x, localSpriteCenter.y, capsule.center.z);
            return true;
        }

        public static void ApplySymmetricCapsuleLeniency(CapsuleCollider capsule, float heightMultiplier)
        {
            if (capsule == null)
            {
                return;
            }

            var originalHeight = Mathf.Max(capsule.radius * 2f, capsule.height);
            capsule.height = Mathf.Max(originalHeight, originalHeight * Mathf.Max(1f, heightMultiplier));
        }
    }
}
