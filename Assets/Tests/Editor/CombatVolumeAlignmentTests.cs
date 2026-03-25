using Dagon.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Dagon.Tests.Editor
{
    public sealed class CombatVolumeAlignmentTests
    {
        [Test]
        public void ApplySymmetricCapsuleLeniency_KeepsCenterUnchanged()
        {
            var root = new GameObject("Root");
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 1.5f, 0f);
            capsule.height = 2f;
            capsule.radius = 0.5f;

            CombatVolumeAlignment.ApplySymmetricCapsuleLeniency(capsule, 1.3f);

            Assert.AreEqual(1.5f, capsule.center.y, 0.0001f);
            Assert.AreEqual(2.6f, capsule.height, 0.0001f);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void TryAlignCapsuleToSpriteCenter_UsesRenderedSpriteCenter()
        {
            var root = new GameObject("Root");
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = Vector3.zero;
            capsule.height = 1f;
            capsule.radius = 0.5f;

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(root.transform, false);
            visuals.transform.localPosition = new Vector3(0f, 2f, 0f);
            visuals.transform.localScale = new Vector3(2f, 2f, 1f);

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateTestSprite();

            var aligned = CombatVolumeAlignment.TryAlignCapsuleToSpriteCenter(root.transform, capsule);

            Assert.IsTrue(aligned);
            Assert.Greater(capsule.center.y, 1.9f);

            Object.DestroyImmediate(root);
        }

        private static Sprite CreateTestSprite()
        {
            var texture = new Texture2D(4, 8, TextureFormat.RGBA32, false);
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}
