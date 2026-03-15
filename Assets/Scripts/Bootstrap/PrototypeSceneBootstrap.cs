using Dagon.Core;
using Dagon.Gameplay;
using Dagon.Rendering;
using UnityEngine;

namespace Dagon.Bootstrap
{
    public sealed class PrototypeSceneBootstrap : MonoBehaviour
    {
        private const string BootstrapRootName = "DagonPrototype";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BuildPrototypeScene()
        {
            if (GameObject.Find(BootstrapRootName) != null || GameObject.FindGameObjectWithTag("Player") != null)
            {
                return;
            }

            var root = new GameObject(BootstrapRootName);
            var bootstrap = root.AddComponent<PrototypeSceneBootstrap>();
            bootstrap.CreateScene(root.transform);
        }

        private void CreateScene(Transform root)
        {
            var player = CreatePlayer(root);
            var cameraObject = ConfigureCamera(root, player.transform);
            CreateLight(root);
            CreateGround(root);
            CreateProps(root, cameraObject);
            CreateRuntimeSystems(root, player, cameraObject);
        }

        private static GameObject CreatePlayer(Transform root)
        {
            var player = new GameObject("Player");
            player.transform.SetParent(root);
            player.transform.position = new Vector3(0f, 0.5f, 0f);
            player.tag = "Player";

            var collider = player.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.height = 1.8f;
            collider.radius = 0.25f;
            collider.isTrigger = true;

            var rigidbody = player.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            player.AddComponent<Health>();
            player.AddComponent<CorruptionMeter>();

            var mover = player.AddComponent<PlayerMover>();
            var launcher = player.AddComponent<HarpoonLauncher>();
            launcher.SetProjectilePrefab(RuntimeHarpoonProjectileFactory.Create(null));
            launcher.Configure(1.5f, 10f, 1f, 1, 0f);
            player.AddComponent<BrineSurgeAbility>();
            player.AddComponent<ExperienceController>();
            player.AddComponent<CorruptionRuntimeEffects>();

            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(player.transform, false);
            visuals.transform.localPosition = Vector3.zero;

            var renderer = visuals.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Characters/sailor_idle_front");
            renderer.sortingOrder = 10;
            visuals.AddComponent<BillboardSprite>();

            return player;
        }

        private static Camera ConfigureCamera(Transform root, Transform player)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.SetParent(root);
            camera.transform.position = new Vector3(-8f, 9f, -8f);
            camera.transform.rotation = Quaternion.Euler(38f, 45f, 0f);
            camera.orthographic = false;
            camera.fieldOfView = 34f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.10f, 0.10f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;

            var follow = camera.GetComponent<FollowCameraRig>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<FollowCameraRig>();
            }

            follow.SetTarget(player);
            player.GetComponent<PlayerMover>().ConfigureRuntime(camera, camera.transform);
            camera.transform.LookAt(player.position + new Vector3(0f, 1.2f, 0f));

            var playerBillboard = player.GetComponentInChildren<BillboardSprite>();
            if (playerBillboard != null)
            {
                playerBillboard.Configure(camera, BillboardSprite.BillboardMode.YAxisOnly);
            }

            var launcher = player.GetComponent<HarpoonLauncher>();
            launcher.SetProjectilePrefab(RuntimeHarpoonProjectileFactory.Create(camera));

            return camera;
        }

        private static void CreateLight(Transform root)
        {
            var existingLight = FindObjectOfType<Light>();
            if (existingLight != null)
            {
                existingLight.transform.SetParent(root);
                existingLight.type = LightType.Directional;
                existingLight.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
                existingLight.intensity = 1.2f;
                existingLight.color = new Color(0.75f, 0.85f, 0.72f, 1f);
                return;
            }

            var lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(root);
            lightObject.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(0.75f, 0.85f, 0.72f, 1f);
        }

        private static void CreateGround(Transform root)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "BlackMireGround";
            ground.transform.SetParent(root);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(3.5f, 1f, 3.5f);

            var renderer = ground.GetComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(0.08f, 0.09f, 0.07f, 1f);
            material.SetFloat("_Smoothness", 0.15f);
            renderer.sharedMaterial = material;
        }

        private static void CreateProps(Transform root, Camera camera)
        {
            var scatterer = root.gameObject.AddComponent<MirePropScatterer>();
            scatterer.Configure(camera);
        }

        private static void CreateRuntimeSystems(Transform root, GameObject player, Camera camera)
        {
            var sprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Enemies/mire_wretch");
            var acolyteProjectile = RuntimeOrbProjectileFactory.Create(camera);
            var spawnDirector = root.gameObject.AddComponent<SpawnDirector>();
            spawnDirector.Configure(player.transform, camera, sprite, acolyteProjectile);

            root.gameObject.AddComponent<ExperienceHud>();
            player.GetComponent<CorruptionRuntimeEffects>().Configure(spawnDirector);

            var runState = root.gameObject.AddComponent<RunStateManager>();
            runState.Configure(player.transform, camera, spawnDirector, sprite, acolyteProjectile);
        }
    }
}
