using UnityEngine;

namespace Dagon.Core
{
    public interface IDamageable
    {
        void ApplyDamage(float amount, GameObject source);
    }
}
