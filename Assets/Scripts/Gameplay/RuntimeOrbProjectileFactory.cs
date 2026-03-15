using Dagon.Core;
using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Gameplay
{
    public static class RuntimeOrbProjectileFactory
    {
        public static HarpoonProjectile Create(Camera camera)
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
            renderer.sprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Enemies/mire_wretch");
            renderer.color = new Color(0.6f, 0.9f, 0.62f, 1f);
            renderer.sortingOrder = 11;
            visuals.transform.localScale = new Vector3(0.22f, 0.22f, 1f);

            var billboard = visuals.AddComponent<BillboardSprite>();
            billboard.Configure(camera, BillboardSprite.BillboardMode.YAxisOnly);

            return orb;
        }
    }
}
