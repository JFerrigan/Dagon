using Dagon.Core;
using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Gameplay
{
    public static class RuntimeOrbProjectileFactory
    {
        public static HarpoonProjectile Create(Camera camera)
        {
            return Create(
                camera,
                "Sprites/Enemies/mire_wretch",
                new Color(0.6f, 0.9f, 0.62f, 1f),
                new Vector3(0.88f, 0.88f, 1f),
                256f);
        }

        public static HarpoonProjectile Create(
            Camera camera,
            string spritePath,
            Color color,
            Vector3 visualScale,
            float pixelsPerUnit = 256f)
        {
            var projectile = new GameObject("RuntimeOrbProjectile");
            projectile.SetActive(false);

            var collider = projectile.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.28f;

            var rigidbody = projectile.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            var orb = projectile.AddComponent<HarpoonProjectile>();

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(projectile.transform, false);

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteLibrary.LoadSprite(spritePath, pixelsPerUnit);
            renderer.color = color;
            renderer.sortingOrder = 11;
            visuals.transform.localScale = visualScale;

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(camera, BillboardSprite.BillboardMode.YAxisOnly);

            return orb;
        }
    }
}
