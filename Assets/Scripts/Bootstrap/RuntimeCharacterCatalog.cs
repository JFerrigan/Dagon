using Dagon.Data;
using UnityEngine;

namespace Dagon.Bootstrap
{
    public static class RuntimeCharacterCatalog
    {
        private static WeaponDefinition harpoonWeapon;
        private static WeaponDefinition anchorChainWeapon;
        private static WeaponDefinition rotLanternWeapon;
        private static WeaponDefinition bilgeSprayWeapon;
        private static WeaponDefinition rotBeaconBombWeapon;
        private static WeaponDefinition floodlineWeapon;
        private static WeaponDefinition tideburstWeapon;
        private static ActiveAbilityDefinition brineSurgeAbility;
        private static ActiveAbilityDefinition dashAbility;
        private static ActiveAbilityDefinition frenzyAbility;
        private static WeaponDefinition[] weaponPool;
        private static CharacterProfileDefinition[] profiles;

        public static WeaponDefinition[] GetWeaponPool()
        {
            EnsureBuilt();
            return weaponPool;
        }

        public static CharacterProfileDefinition[] GetCharacterProfiles()
        {
            EnsureBuilt();
            return profiles;
        }

        public static CharacterProfileDefinition GetDefaultProfile()
        {
            EnsureBuilt();
            return profiles[0];
        }

        public static CharacterProfileDefinition GetProfileById(string characterId)
        {
            EnsureBuilt();
            if (!string.IsNullOrWhiteSpace(characterId))
            {
                for (var i = 0; i < profiles.Length; i++)
                {
                    if (profiles[i] != null && profiles[i].CharacterId == characterId)
                    {
                        return profiles[i];
                    }
                }
            }

            return GetDefaultProfile();
        }

        public static CharacterLoadoutDefinition CreateLoadout(CharacterProfileDefinition profile)
        {
            var resolved = profile != null ? profile : GetDefaultProfile();
            return CharacterLoadoutDefinition.CreateRuntime(resolved.StartingBaseWeapon, resolved.StartingActive);
        }

        private static void EnsureBuilt()
        {
            if (profiles != null && weaponPool != null)
            {
                return;
            }

            harpoonWeapon = WeaponDefinition.CreateRuntime(
                "weapon.harpoon_cast",
                "Harpoon Cast",
                "Launches barbed harpoons in short bursts.",
                WeaponRuntimeKind.ProjectileLauncher,
                WeaponProjectileVisualKind.Harpoon,
                1.5f,
                10f,
                1f,
                1,
                0f);
            anchorChainWeapon = WeaponDefinition.CreateRuntime(
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
                4.5f);
            rotLanternWeapon = WeaponDefinition.CreateRuntime(
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
                4.4f);
            bilgeSprayWeapon = WeaponDefinition.CreateRuntime(
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
                1.5f);
            rotBeaconBombWeapon = WeaponDefinition.CreateRuntime(
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
                1.5f);
            floodlineWeapon = WeaponDefinition.CreateRuntime(
                "weapon.floodline",
                "Floodline",
                "Unleash a slow tidal wall that travels forward and hammers enemies out of its lane.",
                WeaponRuntimeKind.Floodline,
                WeaponProjectileVisualKind.Orb,
                0.6f,
                4.6f,
                0.75f,
                1,
                0f,
                1.15f,
                6f,
                14f);
            tideburstWeapon = WeaponDefinition.CreateRuntime(
                "weapon.tideburst",
                "Tideburst",
                "Loose a weak ring of cursed brine orbs in every direction.",
                WeaponRuntimeKind.Tideburst,
                WeaponProjectileVisualKind.Orb,
                0.7f,
                5f,
                0.45f,
                8,
                0f,
                0f,
                0f,
                0.35f);

            brineSurgeAbility = ActiveAbilityDefinition.CreateRuntime(
                "ability.brine_surge",
                "Brine Surge",
                "A black-water burst that punishes crowd pressure.",
                ActiveAbilityRuntimeKind.BrineSurge,
                6f,
                4.2f,
                5f);
            dashAbility = ActiveAbilityDefinition.CreateRuntime(
                "ability.bilge_dash",
                "Bilge Dash",
                "Burst out of danger with a sudden seawater dash.",
                ActiveAbilityRuntimeKind.Dash,
                4.5f,
                5.4f,
                0f,
                durationSeconds: 0.18f,
                magnitude: 1f,
                charges: 1);
            frenzyAbility = ActiveAbilityDefinition.CreateRuntime(
                "ability.battle_frenzy",
                "Battle Frenzy",
                "Drive every weapon into a short all-out firing frenzy.",
                ActiveAbilityRuntimeKind.Frenzy,
                14f,
                0f,
                0f,
                durationSeconds: 4f,
                magnitude: 2f,
                charges: 1);

            weaponPool = new[]
            {
                anchorChainWeapon,
                rotLanternWeapon,
                bilgeSprayWeapon,
                rotBeaconBombWeapon,
                floodlineWeapon,
                tideburstWeapon
            };

            profiles = new[]
            {
                CharacterProfileDefinition.CreateRuntime(
                    "character.sailor",
                    "Sailor",
                    "A mire-tough sailor who breaks swarms with a brine burst.",
                    "Sprites/Characters/sailor_idle_front",
                    "Sprites/Characters/sailor_idle_front",
                    new Color(0.72f, 0.86f, 0.78f, 1f),
                    1f,
                    harpoonWeapon,
                    brineSurgeAbility),
                CharacterProfileDefinition.CreateRuntime(
                    "character.deckhand",
                    "Deckhand",
                    "A brutal close-range brawler who slips through danger on a charged dash.",
                    "Sprites/Characters/deckhand",
                    "Sprites/Characters/deckhand",
                    new Color(0.79f, 0.87f, 0.73f, 1f),
                    0.105f,
                    anchorChainWeapon,
                    dashAbility),
                CharacterProfileDefinition.CreateRuntime(
                    "character.captain",
                    "Captain",
                    "A decorated officer who turns every weapon into a storm of fire.",
                    "Sprites/Characters/captain",
                    "Sprites/Characters/captain",
                    new Color(0.92f, 0.83f, 0.67f, 1f),
                    0.11f,
                    rotLanternWeapon,
                    frenzyAbility)
            };
        }
    }
}
