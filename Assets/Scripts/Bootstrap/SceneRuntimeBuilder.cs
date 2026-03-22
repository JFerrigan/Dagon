using Dagon.Core;
using Dagon.Data;
using Dagon.Gameplay;
using Dagon.Rendering;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dagon.Bootstrap
{
    public sealed class SceneRuntimeBuilder : MonoBehaviour
    {
        private const string RuntimeRootName = "DagonStageRuntime";

        [SerializeField] private string runtimeRootName = RuntimeRootName;
        [SerializeField] private RuntimeStageConfig stageConfig;

        private void Awake()
        {
            Time.timeScale = 1f;
            WarnAboutDuplicateBuilders();
            EnsureBuilt();
        }

        private void EnsureBuilt()
        {
            var existingRuntimeRoot = GameObject.Find(runtimeRootName);
            if (existingRuntimeRoot != null)
            {
                WarnAboutDuplicateRuntimeSystems();
                return;
            }

            var runtimeRoot = new GameObject(runtimeRootName);
            runtimeRoot.transform.SetParent(transform, false);
            CreateScene(runtimeRoot.transform);
            WarnAboutDuplicateRuntimeSystems();
        }

        private void CreateScene(Transform root)
        {
            var player = CreatePlayer(root);
            var cameraObject = ConfigureCamera(root, player.transform);
            ConfigurePlayerCombat(player, cameraObject);
            CreateLight(root);
            var groundTiler = CreateGround(root, player.transform);
            var propScatterer = CreateProps(root, cameraObject, player.transform);
            CreateStageRuntimeSystems(root, player, cameraObject, groundTiler, propScatterer);
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
            player.AddComponent<Hurtbox>().Configure(CombatTeam.Player, player.GetComponent<Health>());
            player.AddComponent<KnockbackReceiver>().Configure(1f, 18f, 6f);

            player.AddComponent<PlayerMover>();
            player.AddComponent<PlayerSlowReceiver>();
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

        private static MireGroundTiler CreateGround(Transform root, Transform player)
        {
            var tiler = root.gameObject.AddComponent<MireGroundTiler>();
            tiler.Configure(player);
            tiler.Build();
            return tiler;
        }

        private static MirePropScatterer CreateProps(Transform root, Camera camera, Transform player)
        {
            var scatterer = root.gameObject.AddComponent<MirePropScatterer>();
            scatterer.Configure(camera, player);
            return scatterer;
        }

        private static SpawnDirector CreateCommonRuntimeSystems(Transform root, GameObject player, Camera camera, bool includeRunStateManager)
        {
            var sprite = RuntimeSpriteLibrary.LoadSprite("Sprites/Enemies/mire_wretch");
            var acolyteProjectile = RuntimeOrbProjectileFactory.Create(camera);
            var spawnDirector = root.gameObject.AddComponent<SpawnDirector>();
            spawnDirector.Configure(player.transform, camera, sprite, acolyteProjectile);

            root.gameObject.AddComponent<ExperienceHud>();
            player.GetComponent<CorruptionRuntimeEffects>().Configure(spawnDirector);

            if (includeRunStateManager)
            {
                var runState = root.gameObject.AddComponent<RunStateManager>();
                runState.Configure(player.transform, camera, spawnDirector, sprite, acolyteProjectile);
            }

            return spawnDirector;
        }

        private void CreateStageRuntimeSystems(Transform root, GameObject player, Camera camera, MireGroundTiler groundTiler, MirePropScatterer propScatterer)
        {
            var spawnDirector = CreateCommonRuntimeSystems(root, player, camera, includeRunStateManager: true);
            var runState = root.GetComponent<RunStateManager>();
            var resolvedStage = ResolveStageConfig();

            if (resolvedStage.StageKind == RuntimeStageConfig.StageKind.MireColossusBoss)
            {
                ConfigureBossScene(spawnDirector, runState, resolvedStage.Settings);
                spawnDirector?.InitializeRuntime(resolvedStage.StageKind.ToString());
                return;
            }

            ApplyStageRuntimeConfig(spawnDirector, resolvedStage.Settings);
            ConfigureBlackMireRun(runState, resolvedStage.Settings);

            var progressionDirector = root.gameObject.AddComponent<WorldProgressionDirector>();
            progressionDirector.Configure(
                player.transform,
                runState,
                spawnDirector,
                groundTiler,
                propScatterer,
                RuntimeBiomeProfile.CreateDefaultSequence());

            spawnDirector?.InitializeRuntime(resolvedStage.StageKind.ToString());
            Debug.Log(
                $"SceneRuntimeBuilder resolved {resolvedStage.StageKind} for scene '{SceneManager.GetActiveScene().name}' with " +
                $"Quota={resolvedStage.Settings.spawnQuota}, Starting={resolvedStage.Settings.startingEnemies}, MaxAlive={resolvedStage.Settings.maxAliveEnemies}, " +
                $"EliteEvery={resolvedStage.Settings.eliteSpawnEvery}, Interval={resolvedStage.Settings.minSpawnInterval:0.00}-{resolvedStage.Settings.maxSpawnInterval:0.00}, " +
                $"OpeningWaveEnabled={resolvedStage.Settings.openingWaveEnabled}, BarsAlwaysVisible={resolvedStage.Settings.enemyHealthBarsAlwaysVisible}, " +
                $"BarVisibleDuration={resolvedStage.Settings.enemyHealthBarVisibleDuration:0.00}, BossDelay={resolvedStage.Settings.bossTransitionDelaySeconds:0.0}, " +
                $"ShowSpawnProgress={resolvedStage.Settings.showSpawnProgressUi}, SpawnRamp={resolvedStage.Settings.useSpawnRamp}, " +
                $"RampDelay={resolvedStage.Settings.spawnRampDelaySeconds:0.0}, RampDuration={resolvedStage.Settings.spawnRampDurationSeconds:0.0}, " +
                $"RampMaxReduction={resolvedStage.Settings.spawnRampMaxIntervalReduction:0.00}, RampAliveCap={resolvedStage.Settings.spawnRampAdditionalAliveCap}.",
                this);

            if (resolvedStage.Settings.enableSandboxUi)
            {
                var sandboxController = root.gameObject.AddComponent<DeveloperSandboxController>();
                sandboxController.Configure(player.GetComponent<PlayerCombatLoadout>());
            }
        }

        private static void ApplyStageRuntimeConfig(SpawnDirector spawnDirector, RuntimeStageConfig.StageRuntimeSettings config)
        {
            spawnDirector?.ConfigureCampaign(
                config.spawnQuota,
                config.startingEnemies,
                config.maxAliveEnemies,
                config.eliteSpawnEvery,
                config.minSpawnInterval,
                config.maxSpawnInterval);
            spawnDirector?.ConfigureSpawnFlow(config.openingWaveEnabled);
            spawnDirector?.ConfigureSpawnRamp(
                config.useSpawnRamp,
                config.spawnRampDelaySeconds,
                config.spawnRampDurationSeconds,
                config.spawnRampMaxIntervalReduction,
                config.spawnRampAdditionalAliveCap);
            spawnDirector?.ConfigureHealthBars(config.enemyHealthBarsAlwaysVisible, config.enemyHealthBarVisibleDuration);
        }

        private static void ConfigureBlackMireRun(RunStateManager runState, RuntimeStageConfig.StageRuntimeSettings config)
        {
            runState?.ConfigureLevelFlow(string.Empty, "MainMenu", 1);
            runState?.ConfigureBossTransition(true, config.bossTransitionDelaySeconds, config.showSpawnProgressUi);
            runState?.ConfigureBossResolution(false);
            runState?.ConfigureBossAmbientSpawning(true, 1.8f, 2);
        }

        private static void ConfigureBossScene(SpawnDirector spawnDirector, RunStateManager runState, RuntimeStageConfig.StageRuntimeSettings config)
        {
            spawnDirector?.ConfigureCampaign(
                config.spawnQuota,
                config.startingEnemies,
                config.maxAliveEnemies,
                config.eliteSpawnEvery,
                config.minSpawnInterval,
                config.maxSpawnInterval);
            spawnDirector?.ConfigureSpawnFlow(config.openingWaveEnabled);
            spawnDirector?.ConfigureHealthBars(config.enemyHealthBarsAlwaysVisible, config.enemyHealthBarVisibleDuration);
            runState?.ConfigureLevelFlow(string.Empty, "MainMenu", 1);
            runState?.ConfigureBossTransition(false, config.bossTransitionDelaySeconds, config.showSpawnProgressUi);
            runState?.ConfigureBossResolution(true);
            runState?.ConfigureBossAmbientSpawning(false, 1f, 0);
        }

        private RuntimeStageConfig.ResolvedStageConfig ResolveStageConfig()
        {
            if (stageConfig != null)
            {
                return stageConfig.Resolve();
            }

            var inferred = RuntimeStageConfig.ResolveForScene(SceneManager.GetActiveScene().name);
            Debug.LogWarning(
                $"Scene '{SceneManager.GetActiveScene().name}' is missing RuntimeStageConfig. Falling back to inferred stage {inferred.StageKind}.",
                this);
            return inferred;
        }

        private void WarnAboutDuplicateBuilders()
        {
            var builders = FindObjectsByType<SceneRuntimeBuilder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (builders.Length > 1)
            {
                Debug.LogWarning(
                    $"Scene '{SceneManager.GetActiveScene().name}' has {builders.Length} SceneRuntimeBuilder instances. " +
                    "This can duplicate runtime setup and spawning.",
                    this);
            }
        }

        private void WarnAboutDuplicateRuntimeSystems()
        {
            var spawnDirectors = FindObjectsByType<SpawnDirector>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (spawnDirectors.Length > 1)
            {
                Debug.LogWarning(
                    $"Scene '{SceneManager.GetActiveScene().name}' has {spawnDirectors.Length} SpawnDirector instances. " +
                    "This can cause spawn-count divergence between editor play and builds.",
                    this);
            }
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
                    "weapon.anchor_chain",
                    "Anchor Chain",
                    "Sweep a brutal chain arc that punishes enemies pressing too close.",
                    WeaponRuntimeKind.AnchorChain,
                    WeaponProjectileVisualKind.Harpoon,
                    0.85f,
                    0f,
                    1.8f,
                    1,
                    0f,
                    4.8f,
                    105f,
                    4.5f),
                WeaponDefinition.CreateRuntime(
                    "weapon.rot_lantern",
                    "Rot Lantern",
                    "A cursed lantern emits baleful pulses around the sailor.",
                    WeaponRuntimeKind.RotLantern,
                    WeaponProjectileVisualKind.Harpoon,
                    0.75f,
                    0f,
                    0.8f,
                    1,
                    0f,
                    4.4f),
                WeaponDefinition.CreateRuntime(
                    "weapon.bilge_spray",
                    "Bilge Spray",
                    "Blast foul brine in a short cone that slows and softens the swarm.",
                    WeaponRuntimeKind.BilgeSpray,
                    WeaponProjectileVisualKind.Harpoon,
                    0.65f,
                    0f,
                    0.7f,
                    1,
                    0f,
                    4.8f,
                    120f,
                    0f,
                    0.25f,
                    1.5f),
                WeaponDefinition.CreateRuntime(
                    "weapon.rot_beacon_bomb",
                    "Rot Beacon Bomb",
                    "Throw a cursed beacon bomb that lands, pulses a slowing field, then erupts in a final blast.",
                    WeaponRuntimeKind.RotBeaconBomb,
                    WeaponProjectileVisualKind.Orb,
                    0.55f,
                    8f,
                    0.45f,
                    2,
                    0f,
                    2.6f,
                    3.6f,
                    1.8f,
                    0.25f,
                    1.5f)
            };
        }
    }
}
