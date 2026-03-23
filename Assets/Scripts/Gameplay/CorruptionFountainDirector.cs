using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CorruptionFountainDirector : MonoBehaviour
    {
        private const float SpawnRadius = 10f;

        [SerializeField] private Transform player;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RunStateManager runStateManager;
        [SerializeField] private CorruptionMeter corruptionMeter;
        [SerializeField] private float initialDelay = 30f;
        [SerializeField] private float respawnDelay = 45f;
        [SerializeField] private float cleanseAmount = 25f;

        private float spawnTimer;
        private CorruptionFountain activeFountain;

        private void Awake()
        {
            spawnTimer = initialDelay;
        }

        private void Update()
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (activeFountain != null)
            {
                return;
            }

            if (runStateManager != null && runStateManager.BossWaveStarted)
            {
                return;
            }

            if (corruptionMeter == null || player == null || worldCamera == null)
            {
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
            {
                return;
            }

            SpawnFountain();
            spawnTimer = respawnDelay;
        }

        public void Configure(Transform playerTransform, Camera cameraReference, RunStateManager runState, CorruptionMeter meter)
        {
            player = playerTransform;
            worldCamera = cameraReference;
            runStateManager = runState;
            corruptionMeter = meter;
        }

        private void SpawnFountain()
        {
            var direction = Random.insideUnitCircle;
            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector2.right;
            }

            direction.Normalize();
            var position = player.position + new Vector3(direction.x, 0f, direction.y) * SpawnRadius;
            position.y = player.position.y;
            activeFountain = CorruptionFountain.Create(position, cleanseAmount, worldCamera, corruptionMeter);
        }
    }
}
