using UnityEngine;

namespace Dagon.Rendering
{
    [DisallowMultipleComponent]
    public sealed class BillboardSprite : MonoBehaviour
    {
        public enum BillboardMode
        {
            Full,
            YAxisOnly
        }

        [SerializeField] private BillboardMode mode = BillboardMode.Full;
        [SerializeField] private Camera targetCamera;

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            if (mode == BillboardMode.Full)
            {
                transform.forward = targetCamera.transform.forward;
                return;
            }

            var flattenedForward = targetCamera.transform.forward;
            flattenedForward.y = 0f;

            if (flattenedForward.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(flattenedForward.normalized, Vector3.up);
        }

        public void Configure(Camera cameraReference, BillboardMode billboardMode)
        {
            targetCamera = cameraReference;
            mode = billboardMode;
        }
    }
}
