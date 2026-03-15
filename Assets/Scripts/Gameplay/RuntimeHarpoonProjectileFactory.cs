using Dagon.Core;
using UnityEngine;
using Dagon.Rendering;

namespace Dagon.Gameplay
{
    public static class RuntimeHarpoonProjectileFactory
    {
        public static HarpoonProjectile Create(Camera camera)
        {
            var projectile = new GameObject("RuntimeHarpoonProjectile");
            projectile.SetActive(false);

            var collider = projectile.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.12f;

            var rigidbody = projectile.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            var harpoon = projectile.AddComponent<HarpoonProjectile>();

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(projectile.transform, false);

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Weapons/harpoon_projectile");
            renderer.color = Color.white;
            renderer.sortingOrder = 12;
            visuals.transform.localScale = new Vector3(0.035f, 0.035f, 1f);

            var orienter = visuals.AddComponent<ProjectileBillboardVisual>();
            orienter.Configure(camera, projectile.transform);

            return harpoon;
        }
    }
}
