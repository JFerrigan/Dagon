using UnityEngine;

namespace Dagon.Rendering
{
    [DisallowMultipleComponent]
    public sealed class ProjectileBillboardVisual : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform projectileRoot;

        private void LateUpdate()
        {
            if (projectileRoot == null)
            {
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            var cameraForward = targetCamera.transform.forward;
            transform.forward = cameraForward;

            var projected = Vector3.ProjectOnPlane(projectileRoot.forward, cameraForward);
            if (projected.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var screenUp = targetCamera.transform.up;
            var signedAngle = Vector3.SignedAngle(screenUp, projected.normalized, cameraForward);
            transform.rotation = Quaternion.AngleAxis(signedAngle - 90f, cameraForward) * transform.rotation;
        }

        public void Configure(Camera cameraReference, Transform root)
        {
            targetCamera = cameraReference;
            projectileRoot = root;
        }
    }
}
