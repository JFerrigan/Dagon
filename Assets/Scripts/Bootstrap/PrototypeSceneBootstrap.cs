using Dagon.Core;
using Dagon.Data;
using Dagon.Gameplay;
using Dagon.Rendering;
using UnityEngine;

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

        [SerializeField] private StageKind stageKind = StageKind.BlackMireRun;
        [SerializeField] private string runtimeRootName = RuntimeRootName;

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
            ConfigurePlayerCombat(player, cameraObject);
            CreateLight(root);
            CreateGround(root, player.transform);
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

            player.AddComponent<PlayerMover>();
            player.AddComponent<PlayerCombatLoadout>();
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

        private static void ConfigurePlayerCombat(GameObject player, Camera camera)
        {
            var loadout = player.GetComponent<PlayerCombatLoadout>();
            if (loadout == null)
            {
                return;
            }

            loadout.SetWeaponPool(CreateWeaponPool());
            loadout.ConfigureRuntime(camera);
            loadout.Initialize(CreateStartingLoadout());
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

        private static void CreateGround(Transform root, Transform player)
        {
            var tiler = root.gameObject.AddComponent<MireGroundTiler>();
            tiler.Configure(player);
            tiler.Build();
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

            if (stageKind == StageKind.BlackMireRun)
            {
                spawnDirector?.ConfigureCampaign(30, 7, 20, 10, 0.35f, 0.7f);
                runState?.ConfigureLevelFlow("MireColossusBoss", "MainMenu", 1);
                return;
            }

            spawnDirector?.ConfigureCampaign(18, 5, 16, 8, 0.4f, 0.75f);
            runState?.ConfigureLevelFlow(string.Empty, "MainMenu", 2);
        }

        private static CharacterLoadoutDefinition CreateStartingLoadout()
        {
            return CharacterLoadoutDefinition.CreateRuntime(
                WeaponDefinition.CreateRuntime(
                    "weapon.harpoon_cast",
                    "Harpoon Cast",
                    "Launches barbed harpoons in short bursts.",
                    WeaponRuntimeKind.ProjectileLauncher,
                    WeaponProjectileVisualKind.Harpoon,
                    1.5f,
                    10f,
                    1f,
                    1,
                    0f),
                ActiveAbilityDefinition.CreateRuntime(
                    "ability.brine_surge",
                    "Brine Surge",
                    "A black-water burst that punishes crowd pressure.",
                    ActiveAbilityRuntimeKind.BrineSurge,
                    6f,
                    2.8f,
                    2f));
        }

        private static WeaponDefinition[] CreateWeaponPool()
        {
            return new[]
            {
                WeaponDefinition.CreateRuntime(
                    "weapon.abyss_orb",
                    "Abyss Orb",
                    "Loose a slow orb that adds steady ranged pressure.",
                    WeaponRuntimeKind.ProjectileLauncher,
                    WeaponProjectileVisualKind.Orb,
                    1.15f,
                    8f,
                    1.4f,
                    1,
                    0f),
                WeaponDefinition.CreateRuntime(
                    "weapon.riptide_fan",
                    "Riptide Fan",
                    "A broad fan of harpoons that thickens your front arc.",
                    WeaponRuntimeKind.ProjectileLauncher,
                    WeaponProjectileVisualKind.Harpoon,
                    0.9f,
                    11f,
                    0.8f,
                    3,
                    14f)
            };
        }
    }
}
