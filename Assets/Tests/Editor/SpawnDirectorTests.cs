using System.Reflection;
using Dagon.Gameplay;
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

            InvokePrivateMethod(director, "Update");

            Assert.AreEqual(0, director.TotalSpawned);
            Assert.AreEqual(1f, GetField<float>(director, "spawnTimer"), 0.0001f);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(directorObject);
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, InstanceFlags);
            Assert.NotNull(method, $"Expected private method {methodName} to exist.");
            method.Invoke(target, null);
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
    }
}
