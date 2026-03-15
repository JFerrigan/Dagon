using Dagon.Core;
using Dagon.Gameplay;
using Dagon.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dagon.Bootstrap
{
    public sealed class PrototypeSceneBootstrap : MonoBehaviour
    {
        private enum StageKind
        {
            BlackMireRun,
            MireColossusBoss
        }

        private const string RuntimeRootName = "DagonStageRuntime";
        private const string BootstrapObjectName = "StageBootstrapRuntime";

        [SerializeField] private StageKind stageKind = StageKind.BlackMireRun;
        [SerializeField] private string runtimeRootName = RuntimeRootName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureStageBootstrapForScene()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (!TryGetStageKindForScene(sceneName, out var sceneStageKind))
            {
                return;
            }

            if (GameObject.FindGameObjectWithTag("Player") != null || GameObject.Find(RuntimeRootName) != null)
            {
                return;
            }

            var bootstrap = FindObjectOfType<PrototypeSceneBootstrap>();
            if (bootstrap == null)
            {
                var bootstrapObject = new GameObject(BootstrapObjectName);
                bootstrap = bootstrapObject.AddComponent<PrototypeSceneBootstrap>();
            }

            bootstrap.stageKind = sceneStageKind;
            bootstrap.runtimeRootName = RuntimeRootName;
            bootstrap.EnsureBuilt();
        }

        private void Awake()
        {
            EnsureBuilt();
        }

        private void EnsureBuilt()
        {
            if (GameObject.FindGameObjectWithTag("Player") != null || GameObject.Find(runtimeRootName) != null)
            {
                return;
            }

            var runtimeRoot = new GameObject(runtimeRootName);
            runtimeRoot.transform.SetParent(transform, false);
            CreateScene(runtimeRoot.transform);
        }

        private void CreateScene(Transform root)
        {
            var player = CreatePlayer(root);
            var cameraObject = ConfigureCamera(root, player.transform);
            CreateLight(root);
            CreateGround(root);
            CreateProps(root, cameraObject);
            CreateStageRuntimeSystems(root, player, cameraObject);
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

        private static void CreateCommonRuntimeSystems(Transform root, GameObject player, Camera camera)
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

        private void CreateStageRuntimeSystems(Transform root, GameObject player, Camera camera)
        {
            CreateCommonRuntimeSystems(root, player, camera);

            var spawnDirector = root.GetComponent<SpawnDirector>();
            var runState = root.GetComponent<RunStateManager>();

            if (stageKind != StageKind.MireColossusBoss)
            {
                return;
            }

            spawnDirector?.ConfigureStage(0, true);
            runState?.ConfigureStage(0.5f);
        }

        private static bool TryGetStageKindForScene(string sceneName, out StageKind sceneStageKind)
        {
            switch (sceneName)
            {
                case "BlackMire":
                case "SampleScene":
                    sceneStageKind = StageKind.BlackMireRun;
                    return true;
                case "MireColossusBoss":
                    sceneStageKind = StageKind.MireColossusBoss;
                    return true;
                default:
                    sceneStageKind = StageKind.BlackMireRun;
                    return false;
            }
        }
    }
}
