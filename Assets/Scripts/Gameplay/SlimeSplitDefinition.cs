using UnityEngine;

namespace Dagon.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SlimeSplitDefinition : MonoBehaviour
    {
        public bool SpawnChildrenOnDeath { get; private set; }
        public bool CountsTowardDefeatProgress { get; private set; } = true;
        public bool ForceCorruptedChildren { get; private set; }
        public bool IsSmallVariant { get; private set; }

        public void Configure(bool spawnChildrenOnDeath, bool countsTowardDefeatProgress, bool forceCorruptedChildren, bool isSmallVariant)
        {
            SpawnChildrenOnDeath = spawnChildrenOnDeath;
            CountsTowardDefeatProgress = countsTowardDefeatProgress;
            ForceCorruptedChildren = forceCorruptedChildren;
            IsSmallVariant = isSmallVariant;
        }
    }
}
