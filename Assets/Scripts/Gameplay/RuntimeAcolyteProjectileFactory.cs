using Dagon.Core;
using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Gameplay
{
    public static class RuntimeAcolyteProjectileFactory
    {
        public static DrownedAcolyteProjectile Create(Camera camera)
        {
            var projectile = new GameObject("RuntimeAcolyteProjectile");
            projectile.SetActive(false);

            var collider = projectile.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.16f;

            var body = projectile.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;

            var acolyteProjectile = projectile.AddComponent<DrownedAcolyteProjectile>();

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(projectile.transform, false);

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Effects/brine_surge", 256f);
            renderer.color = new Color(0.68f, 0.95f, 0.74f, 0.95f);
            renderer.sortingOrder = 13;
            visuals.transform.localScale = new Vector3(1.44f, 1.44f, 1f);

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(camera, BillboardSprite.BillboardMode.YAxisOnly);

            return acolyteProjectile;
        }
    }
}
