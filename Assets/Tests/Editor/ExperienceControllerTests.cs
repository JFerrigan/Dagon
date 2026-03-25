using Dagon.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Dagon.Tests.Editor
{
    public sealed class ExperienceControllerTests
    {
        [Test]
        public void GrantBonusChoice_QueuesUpgradeWithoutChangingLevelOrXp()
        {
            var player = new GameObject("Player");
            var controller = player.AddComponent<ExperienceController>();
            player.AddComponent<Health>();
            player.AddComponent<CorruptionMeter>();

            var startingLevel = controller.Level;
            var startingXp = controller.CurrentXp;

            controller.GrantBonusChoice();

            Assert.IsTrue(controller.HasPendingChoice);
            Assert.AreEqual(startingLevel, controller.Level);
            Assert.AreEqual(startingXp, controller.CurrentXp);

            Object.DestroyImmediate(player);
        }
    }
}
