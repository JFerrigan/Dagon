using System.Collections.Generic;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BodyBlocker : MonoBehaviour
    {
        public enum BodyTeam
        {
            Player,
            Enemy
        }

        private static readonly List<BodyBlocker> ActiveBlockers = new();

        [SerializeField] private BodyTeam team = BodyTeam.Enemy;
        [SerializeField] private float bodyRadius = 0.5f;
        [SerializeField] private float bodyHeight = 1.5f;
        [SerializeField] private float weight = 1f;
        [SerializeField] private bool blocksPlayer = true;
        [SerializeField] private bool separatesFromEnemies = true;
        [SerializeField] private bool immovable;

        public static IReadOnlyList<BodyBlocker> Active => ActiveBlockers;

        public BodyTeam Team => team;
        public float BodyRadius => Mathf.Max(0.05f, bodyRadius);
        public float BodyHeight => Mathf.Max(0.1f, bodyHeight);
        public float Weight => Mathf.Max(0.1f, weight);
        public bool BlocksPlayer => blocksPlayer;
        public bool SeparatesFromEnemies => separatesFromEnemies;
        public bool Immovable => immovable;
        public bool Suppressed { get; private set; }

        public Vector3 PlanarPosition
        {
            get
            {
                var position = transform.position;
                position.y = 0f;
                return position;
            }
        }

        private void OnEnable()
        {
            if (!ActiveBlockers.Contains(this))
            {
                ActiveBlockers.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveBlockers.Remove(this);
        }

        private void LateUpdate()
        {
            if (Suppressed || immovable || team != BodyTeam.Enemy || !separatesFromEnemies)
            {
                return;
            }

            var passiveDelta = BodyBlockerResolver.ResolvePlanarMovement(this, Vector3.zero);
            passiveDelta.y = 0f;
            if (passiveDelta.sqrMagnitude > 0.000001f)
            {
                transform.position += passiveDelta;
            }
        }

        public void Configure(BodyTeam newTeam, float newRadius, float newHeight, float newWeight, bool newBlocksPlayer = true, bool newSeparatesFromEnemies = true, bool newImmovable = false)
        {
            team = newTeam;
            bodyRadius = Mathf.Max(0.05f, newRadius);
            bodyHeight = Mathf.Max(0.1f, newHeight);
            weight = Mathf.Max(0.1f, newWeight);
            blocksPlayer = newBlocksPlayer;
            separatesFromEnemies = newSeparatesFromEnemies;
            immovable = newImmovable;
        }

        public void SetSuppressed(bool suppressed)
        {
            Suppressed = suppressed;
        }
    }
}
