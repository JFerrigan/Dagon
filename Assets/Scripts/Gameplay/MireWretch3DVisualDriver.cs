using Dagon.Core;
using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class MireWretch3DVisualDriver : MonoBehaviour
    {
        private enum VisualState
        {
            None,
            Idle,
            Walk,
            Damage,
            Death
        }

        [SerializeField] private Transform gameplayRoot;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private Health health;
        [SerializeField] private MireWretch3DPrototypeConfig config;

        private VisualState currentState;
        private Vector3 lastGameplayPosition;
        private float transientStateRemaining;
        private bool dead;

        private void OnEnable()
        {
            SubscribeHealthEvents();
            if (gameplayRoot != null)
            {
                lastGameplayPosition = gameplayRoot.position;
            }
        }

        private void OnDisable()
        {
            UnsubscribeHealthEvents();
        }

        private void Update()
        {
            if (gameplayRoot == null || animator == null || config == null)
            {
                return;
            }

            var currentPosition = gameplayRoot.position;
            var planarDelta = currentPosition - lastGameplayPosition;
            planarDelta.y = 0f;
            lastGameplayPosition = currentPosition;

            if (planarDelta.sqrMagnitude > 0.0001f)
            {
                var facing = Quaternion.LookRotation(planarDelta.normalized, Vector3.up);
                transform.rotation = facing * Quaternion.Euler(0f, config.YawOffset, 0f);
            }

            if (dead)
            {
                return;
            }

            if (transientStateRemaining > 0f)
            {
                transientStateRemaining = Mathf.Max(0f, transientStateRemaining - Time.deltaTime);
                if (transientStateRemaining > 0f)
                {
                    return;
                }
            }

            var speed = planarDelta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            var locomotionState = speed >= Mathf.Max(0.01f, config.MovementThreshold)
                ? VisualState.Walk
                : VisualState.Idle;
            ApplyState(locomotionState, restart: false);
        }

        public void Configure(Transform gameplayTransform, Transform instantiatedModelRoot, MireWretch3DPrototypeConfig prototypeConfig)
        {
            gameplayRoot = gameplayTransform;
            modelRoot = instantiatedModelRoot;
            config = prototypeConfig;
            if (modelRoot != null)
            {
                modelRoot.SetParent(transform, false);
                modelRoot.localPosition = config != null ? config.LocalPosition : Vector3.zero;
                modelRoot.localRotation = Quaternion.identity;
                modelRoot.localScale = config != null ? config.LocalScale : Vector3.one;
            }

            animator = modelRoot != null ? modelRoot.GetComponentInChildren<Animator>() : null;
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }

            health = gameplayRoot != null ? gameplayRoot.GetComponent<Health>() : null;
            lastGameplayPosition = gameplayRoot != null ? gameplayRoot.position : Vector3.zero;
            currentState = VisualState.None;
            transientStateRemaining = 0f;
            dead = health != null && health.IsDead;
            SubscribeHealthEvents();

            if (!dead)
            {
                ApplyState(VisualState.Idle, restart: true);
            }
        }

        private void HandleDamaged(Health damagedHealth, float amount, GameObject source)
        {
            if (dead || amount <= 0f || damagedHealth == null || damagedHealth.IsDead)
            {
                return;
            }

            var duration = config != null ? config.GetPrimaryClipLength(config.DamageController) : 0f;
            ApplyTransientState(VisualState.Damage, duration);
        }

        private void HandleDied(Health deadHealth, GameObject source)
        {
            if (dead)
            {
                return;
            }

            dead = true;
            transientStateRemaining = 0f;
            ApplyState(VisualState.Death, restart: true);
        }

        private void ApplyTransientState(VisualState state, float duration)
        {
            ApplyState(state, restart: true);
            transientStateRemaining = Mathf.Max(0.1f, duration);
        }

        private void ApplyState(VisualState state, bool restart)
        {
            if (animator == null || config == null)
            {
                return;
            }

            if (!restart && currentState == state)
            {
                return;
            }

            var controller = ResolveController(state);
            if (controller == null)
            {
                return;
            }

            var controllerChanged = animator.runtimeAnimatorController != controller;
            currentState = state;
            if (controllerChanged)
            {
                animator.runtimeAnimatorController = controller;
                animator.Rebind();
                animator.Update(0f);
            }
            else if (restart)
            {
                animator.Play(0, 0, 0f);
                animator.Update(0f);
            }
        }

        private RuntimeAnimatorController ResolveController(VisualState state)
        {
            return state switch
            {
                VisualState.Walk => config.WalkController != null ? config.WalkController : config.IdleController,
                VisualState.Damage => config.DamageController != null ? config.DamageController : config.IdleController,
                VisualState.Death => config.DeathController != null ? config.DeathController : config.IdleController,
                _ => config.IdleController
            };
        }

        private void SubscribeHealthEvents()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= HandleDamaged;
            health.Died -= HandleDied;
            health.Damaged += HandleDamaged;
            health.Died += HandleDied;
        }

        private void UnsubscribeHealthEvents()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= HandleDamaged;
            health.Died -= HandleDied;
        }
    }
}
