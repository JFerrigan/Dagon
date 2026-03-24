using Dagon.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerMover : MonoBehaviour
    {
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform movementReference;
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private PlayerSlowReceiver slowReceiver;
        [SerializeField] private BodyBlocker bodyBlocker;

        private Vector3 aimDirection = Vector3.forward;
        private float dashRemaining;
        private Vector3 dashVelocity;

        public Vector3 AimDirection => aimDirection;
        public Vector3 MoveDirection { get; private set; }
        public bool IsDashing => dashRemaining > 0f;
        public event System.Action<PlayerMover> DashStarted;

        private void OnEnable()
        {
            moveAction?.action.Enable();
            lookAction?.action.Enable();

            if (slowReceiver == null)
            {
                slowReceiver = GetComponent<PlayerSlowReceiver>();
            }

            if (bodyBlocker == null)
            {
                bodyBlocker = GetComponent<BodyBlocker>();
            }
        }

        private void OnDisable()
        {
            moveAction?.action.Disable();
            lookAction?.action.Disable();
        }

        private void Update()
        {
            if (dashRemaining > 0f)
            {
                bodyBlocker?.SetSuppressed(true);
                dashRemaining = Mathf.Max(0f, dashRemaining - Time.deltaTime);
                transform.position += dashVelocity * Time.deltaTime;
                aimDirection = dashVelocity.sqrMagnitude > 0.001f ? dashVelocity.normalized : aimDirection;
                return;
            }

            bodyBlocker?.SetSuppressed(false);

            MoveDirection = ResolveMoveDirection();

            if (MoveDirection.sqrMagnitude > 0f)
            {
                var speedMultiplier = slowReceiver != null ? slowReceiver.SpeedMultiplier : 1f;
                var desiredDelta = MoveDirection * (moveSpeed * speedMultiplier * Time.deltaTime);
                transform.position += bodyBlocker != null
                    ? BodyBlockerResolver.ResolvePlanarMovement(bodyBlocker, desiredDelta)
                    : desiredDelta;
            }

            UpdateAimDirection();
        }

        private Vector3 ResolveMoveDirection()
        {
            var moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : ReadKeyboardMovement();

            var forward = movementReference != null ? movementReference.forward : Vector3.forward;
            var right = movementReference != null ? movementReference.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            var moveDirection = (right * moveInput.x) + (forward * moveInput.y);
            return moveDirection.sqrMagnitude > 1f ? moveDirection.normalized : moveDirection;
        }

        private void UpdateAimDirection()
        {
            var lookInput = lookAction != null ? lookAction.action.ReadValue<Vector2>() : Vector2.zero;

            if (lookInput.sqrMagnitude > 0.01f)
            {
                aimDirection = ConvertInputToWorld(lookInput);
                return;
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera != null && Mouse.current != null)
            {
                var ray = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                var plane = new Plane(Vector3.up, transform.position);
                if (plane.Raycast(ray, out var distance))
                {
                    var hitPoint = ray.GetPoint(distance);
                    var toHit = hitPoint - transform.position;
                    toHit.y = 0f;
                    if (toHit.sqrMagnitude > 0.01f)
                    {
                        aimDirection = toHit.normalized;
                        return;
                    }
                }
            }

            if (MoveDirection.sqrMagnitude > 0.01f)
            {
                aimDirection = MoveDirection.normalized;
            }
        }

        private Vector3 ConvertInputToWorld(Vector2 input)
        {
            var forward = movementReference != null ? movementReference.forward : Vector3.forward;
            var right = movementReference != null ? movementReference.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;

            var worldDirection = (right.normalized * input.x) + (forward.normalized * input.y);
            return worldDirection.sqrMagnitude > 0.001f ? worldDirection.normalized : aimDirection;
        }

        private static Vector2 ReadKeyboardMovement()
        {
            if (Keyboard.current == null)
            {
                return Vector2.zero;
            }

            var move = Vector2.zero;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                move.x -= 1f;
            }

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                move.x += 1f;
            }

            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                move.y -= 1f;
            }

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                move.y += 1f;
            }

            return move.sqrMagnitude > 1f ? move.normalized : move;
        }

        public void ConfigureRuntime(Camera cameraReference, Transform movementFrame)
        {
            worldCamera = cameraReference;
            movementReference = movementFrame;
            bodyBlocker ??= GetComponent<BodyBlocker>();
        }

        public bool StartDash(Vector3 direction, float distance, float duration)
        {
            if (dashRemaining > 0f || duration <= 0f)
            {
                return false;
            }

            var resolvedDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : aimDirection;
            if (resolvedDirection.sqrMagnitude <= 0.001f)
            {
                resolvedDirection = Vector3.forward;
            }

            dashRemaining = Mathf.Max(0.05f, duration);
            dashVelocity = resolvedDirection * (distance / dashRemaining);
            MoveDirection = resolvedDirection;
            aimDirection = resolvedDirection;
            DashStarted?.Invoke(this);
            return true;
        }
    }
}
