using System.Reflection;
using Dagon.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Dagon.Tests.Editor
{
    public sealed class RunStateManagerTests
    {
        private static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void TimedBossTransition_StopsSpawningWhenDelayIsReached()
        {
            var runStateObject = new GameObject("RunState");
            var runState = runStateObject.AddComponent<RunStateManager>();

            var spawnDirectorObject = new GameObject("SpawnDirector");
            var spawnDirector = spawnDirectorObject.AddComponent<SpawnDirector>();

            var player = new GameObject("Player");
            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();

            runState.Configure(player.transform, camera, spawnDirector, null, null);
            runState.ConfigureBossTransition(true, 10f, false);

            SetField(runState, "runTimer", 10f);
            InvokePrivateMethod(runState, "Update");

            Assert.IsTrue(GetField<bool>(runState, "bossTransitionArmed"));
            Assert.IsTrue(GetField<bool>(spawnDirector, "spawningStopped"));
            Assert.IsTrue(runState.BossWaveStarted);

            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(spawnDirectorObject);
            Object.DestroyImmediate(runStateObject);
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
