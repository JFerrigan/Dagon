using UnityEngine;

namespace Dagon.Core
{
    [DisallowMultipleComponent]
    public sealed class FollowCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(-12f, 14f, -12f);
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private bool preserveInitialRotation = true;

        private Vector3 velocity;
        private Quaternion initialRotation;

        private void Awake()
        {
            initialRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

            if (preserveInitialRotation)
            {
                transform.rotation = initialRotation;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
