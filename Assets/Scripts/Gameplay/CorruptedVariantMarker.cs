using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CorruptedVariantMarker : MonoBehaviour
    {
        [SerializeField] private CorruptionAffixController.CorruptionAffixKind affixKind;

        public CorruptionAffixController.CorruptionAffixKind AffixKind => affixKind;

        public void SetAffixKind(CorruptionAffixController.CorruptionAffixKind kind)
        {
            affixKind = kind;
        }
    }
}
