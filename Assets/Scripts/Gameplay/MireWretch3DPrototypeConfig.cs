using UnityEngine;

namespace Dagon.Gameplay
{
    [CreateAssetMenu(fileName = "MireWretch3DPrototype", menuName = "Dagon/Prototype/Mire Wretch 3D Config")]
    public sealed class MireWretch3DPrototypeConfig : ScriptableObject
    {
        private const string ResourcePath = "Configs/MireWretch3DPrototype";

        private static MireWretch3DPrototypeConfig cachedInstance;

        [SerializeField] private Object skeletonPrefab;
        [SerializeField] private RuntimeAnimatorController idleController;
        [SerializeField] private RuntimeAnimatorController walkController;
        [SerializeField] private RuntimeAnimatorController damageController;
        [SerializeField] private RuntimeAnimatorController deathController;
        [SerializeField] private Vector3 localPosition = Vector3.zero;
        [SerializeField] private Vector3 localScale = Vector3.one;
        [SerializeField] private float yawOffset;
        [SerializeField] private float movementThreshold = 0.12f;

        public Object SkeletonPrefab => skeletonPrefab;
        public RuntimeAnimatorController IdleController => idleController;
        public RuntimeAnimatorController WalkController => walkController;
        public RuntimeAnimatorController DamageController => damageController;
        public RuntimeAnimatorController DeathController => deathController;
        public Vector3 LocalPosition => localPosition;
        public Vector3 LocalScale => localScale;
        public float YawOffset => yawOffset;
        public float MovementThreshold => movementThreshold;

        public static MireWretch3DPrototypeConfig Load()
        {
            if (cachedInstance == null)
            {
                cachedInstance = Resources.Load<MireWretch3DPrototypeConfig>(ResourcePath);
            }

            return cachedInstance;
        }

        public float GetPrimaryClipLength(RuntimeAnimatorController controller)
        {
            if (controller == null)
            {
                return 0f;
            }

            var clips = controller.animationClips;
            if (clips == null || clips.Length == 0 || clips[0] == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, clips[0].length);
        }
    }
}
