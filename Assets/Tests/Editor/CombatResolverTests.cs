using Dagon.Core;
using Dagon.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Dagon.Tests.Editor
{
    public sealed class CombatResolverTests
    {
        [Test]
        public void TryApplyDamage_PlayerSourceDamagesEnemyTarget()
        {
            var player = CreateActor("Player", "Player", CombatTeam.Player, out _);
            var enemy = CreateActor("Enemy", null, CombatTeam.Enemy, out var enemyHealth);

            var applied = CombatResolver.TryApplyDamage(
                enemy.GetComponent<Collider>(),
                CombatResolver.GetTeam(player),
                player,
                2f);

            Assert.IsTrue(applied);
            Assert.AreEqual(3f, enemyHealth.CurrentHealth, 0.001f);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(enemy);
        }

        [Test]
        public void TryApplyDamage_EnemySourceCannotDamageEnemyTarget()
        {
            var enemyA = CreateActor("EnemyA", null, CombatTeam.Enemy, out _);
            var enemyB = CreateActor("EnemyB", null, CombatTeam.Enemy, out var enemyBHealth);

            var applied = CombatResolver.TryApplyDamage(
                enemyB.GetComponent<Collider>(),
                CombatResolver.GetTeam(enemyA),
                enemyA,
                2f);

            Assert.IsFalse(applied);
            Assert.AreEqual(5f, enemyBHealth.CurrentHealth, 0.001f);

            Object.DestroyImmediate(enemyA);
            Object.DestroyImmediate(enemyB);
        }

        [Test]
        public void GetTeam_PlayerTagAndHurtboxResolveToPlayerTeam()
        {
            var player = CreateActor("Player", "Player", CombatTeam.Player, out _);

            Assert.AreEqual(CombatTeam.Player, CombatResolver.GetTeam(player));

            Object.DestroyImmediate(player);
        }

        [Test]
        public void TryResolveTarget_PlayerSourceResolvesEnemyTarget()
        {
            var player = CreateActor("Player", "Player", CombatTeam.Player, out _);
            var enemy = CreateActor("Enemy", null, CombatTeam.Enemy, out _);

            var resolved = CombatResolver.TryResolveTarget(
                enemy.GetComponent<Collider>(),
                CombatResolver.GetTeam(player),
                player,
                out var hurtbox);

            Assert.IsTrue(resolved);
            Assert.NotNull(hurtbox);
            Assert.AreEqual(CombatTeam.Enemy, hurtbox.Team);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(enemy);
        }

        private static GameObject CreateActor(string name, string tag, CombatTeam team, out Health health)
        {
            var actor = new GameObject(name);
            if (!string.IsNullOrEmpty(tag))
            {
                actor.tag = tag;
            }

            var collider = actor.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;

            health = actor.AddComponent<Health>();
            health.SetMaxHealth(5f, true);
            actor.AddComponent<Hurtbox>().Configure(team, health);
            return actor;
        }
    }
}
