using System.Reflection;
using Dagon.Gameplay;
using Dagon.UI;
using NUnit.Framework;
using UnityEngine;

namespace Dagon.Tests.Editor
{
    public sealed class SpawnDirectorTests
    {
        private static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void Update_ResetsSpawnTimerWhenSpawnAttemptFails()
        {
            var directorObject = new GameObject("SpawnDirector");
            var director = directorObject.AddComponent<SpawnDirector>();

            var player = new GameObject("Player");
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();

            SetField(director, "player", player.transform);
            SetField(director, "worldCamera", camera);
            SetField(director, "regularSpawnQuota", 10);
            SetField(director, "minSpawnInterval", 0.25f);
            SetField(director, "maxSpawnInterval", 0.25f);
            SetField(director, "spawnTimer", -0.01f);
            SetField(director, "initialized", true);

            InvokePrivateMethod(director, "Update");

            Assert.AreEqual(0, director.TotalSpawned);
            Assert.AreEqual(0.25f, GetField<float>(director, "spawnTimer"), 0.0001f);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(directorObject);
        }

        [Test]
        public void Update_DoesNotAttemptSpawnWhenTimerHasNotElapsed()
        {
            var directorObject = new GameObject("SpawnDirector");
            var director = directorObject.AddComponent<SpawnDirector>();

            var player = new GameObject("Player");
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();

            SetField(director, "player", player.transform);
            SetField(director, "worldCamera", camera);
            SetField(director, "regularSpawnQuota", 10);
            SetField(director, "spawnTimer", 1f);
            SetField(director, "initialized", true);

            InvokePrivateMethod(director, "Update");

            Assert.AreEqual(0, director.TotalSpawned);
            Assert.AreEqual(1f, GetField<float>(director, "spawnTimer"), 0.0001f);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(directorObject);
        }

        [Test]
        public void InitializeRuntime_SpawnsOpeningWaveOnlyOnce()
        {
            var directorObject = new GameObject("SpawnDirector");
            var director = directorObject.AddComponent<SpawnDirector>();

            var player = new GameObject("Player");
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();

            SetField(director, "player", player.transform);
            SetField(director, "worldCamera", camera);
            SetField(director, "mireSprite", CreateTestSprite());
            SetField(director, "regularSpawnQuota", 10);
            SetField(director, "startingEnemies", 3);
            SetField(director, "maxAliveEnemies", 10);
            SetField(director, "minSpawnInterval", 0.25f);
            SetField(director, "maxSpawnInterval", 0.25f);

            Assert.IsTrue(director.InitializeRuntime("Test"));
            Assert.AreEqual(3, director.TotalSpawned);

            Assert.IsTrue(director.InitializeRuntime("TestAgain"));
            Assert.AreEqual(3, director.TotalSpawned);

            InvokePrivateMethod(director, "Start");
            Assert.AreEqual(3, director.TotalSpawned);
            Assert.IsTrue(director.Initialized);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(directorObject);
        }

        [Test]
        public void InitializeRuntime_AppliesConfiguredHealthBarPolicyToSpawnedEnemies()
        {
            var directorObject = new GameObject("SpawnDirector");
            var director = directorObject.AddComponent<SpawnDirector>();

            var player = new GameObject("Player");
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();

            SetField(director, "player", player.transform);
            SetField(director, "worldCamera", camera);
            SetField(director, "mireSprite", CreateTestSprite());
            SetField(director, "regularSpawnQuota", 4);
            SetField(director, "startingEnemies", 1);
            SetField(director, "maxAliveEnemies", 4);
            director.ConfigureHealthBars(false, 4.5f);

            Assert.IsTrue(director.InitializeRuntime("HealthBarTest"));

            var healthBar = directorObject.GetComponentInChildren<EnemyHealthBar>();
            Assert.NotNull(healthBar);
            Assert.IsTrue(healthBar.HideWhenFull);
            Assert.AreEqual(4.5f, healthBar.VisibleDurationAfterDamage, 0.0001f);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(directorObject);
        }

        [Test]
        public void InitializeRuntime_UsesLatestCampaignConfiguration()
        {
            var directorObject = new GameObject("SpawnDirector");
            var director = directorObject.AddComponent<SpawnDirector>();

            var player = new GameObject("Player");
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();

            SetField(director, "player", player.transform);
            SetField(director, "worldCamera", camera);
            SetField(director, "mireSprite", CreateTestSprite());
            director.ConfigureCampaign(12, 2, 5, 6, 0.4f, 0.9f);
            director.ConfigureCampaign(18, 4, 7, 9, 0.5f, 1.1f);

            Assert.IsTrue(director.InitializeRuntime("ConfigTest"));
            Assert.AreEqual(4, director.TotalSpawned);
            Assert.AreEqual(18, director.RemainingSpawns + director.TotalSpawned);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(directorObject);
        }

        [Test]
        public void InitializeRuntime_SkipsOpeningWaveWhenDisabled()
        {
            var directorObject = new GameObject("SpawnDirector");
            var director = directorObject.AddComponent<SpawnDirector>();

            var player = new GameObject("Player");
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();

            SetField(director, "player", player.transform);
            SetField(director, "worldCamera", camera);
            SetField(director, "mireSprite", CreateTestSprite());
            SetField(director, "regularSpawnQuota", 20);
            SetField(director, "startingEnemies", 4);
            SetField(director, "maxAliveEnemies", 8);
            director.ConfigureSpawnFlow(false);

            Assert.IsTrue(director.InitializeRuntime("AmbientTest"));
            Assert.AreEqual(0, director.TotalSpawned);
            Assert.IsFalse(director.OpeningWaveEnabled);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(directorObject);
        }

        [Test]
        public void BuildSpawnPosition_StaysOnPlayerHeightPlane()
        {
            var directorObject = new GameObject("SpawnDirector");
            var director = directorObject.AddComponent<SpawnDirector>();
            var player = new GameObject("Player");
            player.transform.position = new Vector3(3f, 0.5f, -2f);

            SetField(director, "player", player.transform);
            SetField(director, "spawnRadius", 10f);
            SetField(director, "spawnHeightOffset", 0f);

            var position = (Vector3)InvokePrivateMethodWithResult(director, "BuildSpawnPosition");

            Assert.AreEqual(player.transform.position.y, position.y, 0.0001f);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(directorObject);
        }

        [Test]
        public void ResetTimer_UsesFasterIntervalsAfterSpawnRampProgresses()
        {
            var directorObject = new GameObject("SpawnDirector");
            var director = directorObject.AddComponent<SpawnDirector>();

            SetField(director, "configuredMinSpawnInterval", 3f);
            SetField(director, "configuredMaxSpawnInterval", 4f);
            SetField(director, "useSpawnRamp", true);
            SetField(director, "spawnRampDelaySeconds", 0f);
            SetField(director, "spawnRampDurationSeconds", 10f);
            SetField(director, "spawnRampMaxIntervalReduction", 1f);
            SetField(director, "initialized", true);
            SetField(director, "runtimeStartedAt", Time.time - 10f);

            InvokePrivateMethod(director, "ResetTimer");

            var timer = GetField<float>(director, "spawnTimer");
            Assert.LessOrEqual(timer, 3f);
            Assert.GreaterOrEqual(timer, 2f);

            Object.DestroyImmediate(directorObject);
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, InstanceFlags);
            Assert.NotNull(method, $"Expected private method {methodName} to exist.");
            method.Invoke(target, null);
        }

        private static object InvokePrivateMethodWithResult(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, InstanceFlags);
            Assert.NotNull(method, $"Expected private method {methodName} to exist.");
            return method.Invoke(target, null);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, InstanceFlags);
            Assert.NotNull(field, $"Expected private field {fieldName} to exist.");
            field.SetValue(target, value);
        }

        private static T GetField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, InstanceFlags);
            Assert.NotNull(field, $"Expected private field {fieldName} to exist.");
            return (T)field.GetValue(target);
        }

        private static Sprite CreateTestSprite()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.SetPixel(1, 0, Color.white);
            texture.SetPixel(0, 1, Color.white);
            texture.SetPixel(1, 1, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
        }
    }
}
