using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CorruptionFountain : MonoBehaviour
    {
        private const string VisualSpriteResourcePath = "Sprites/Pickups/barnacle_shard";

        [SerializeField] private float cleanseAmount = 25f;

        private CorruptionMeter corruptionMeter;

        public static CorruptionFountain Create(Vector3 position, float cleanseValue, Camera camera, CorruptionMeter targetMeter)
        {
            var fountainObject = new GameObject("CorruptionFountain");
            fountainObject.transform.position = position + Vector3.up * 0.2f;

            var sphere = fountainObject.AddComponent<SphereCollider>();
            sphere.radius = 0.75f;
            sphere.isTrigger = true;

            var rigidbody = fountainObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            var fountain = fountainObject.AddComponent<CorruptionFountain>();
            fountain.cleanseAmount = Mathf.Max(1f, cleanseValue);
            fountain.corruptionMeter = targetMeter;

            WorldPickupVisualFactory.Create(
                fountainObject.transform,
                camera,
                VisualSpriteResourcePath,
                new Color(0.70f, 0.92f, 0.88f, 0.95f),
                new Vector3(0.24f, 0.24f, 1f),
                new Vector3(0f, 0.05f, 0f));

            return fountain;
        }

        private void Update()
        {
            transform.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            corruptionMeter?.ReduceCorruption(cleanseAmount);
            Destroy(gameObject);
        }
    }
}

