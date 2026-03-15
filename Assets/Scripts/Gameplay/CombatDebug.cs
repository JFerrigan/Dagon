using UnityEngine;

namespace Dagon.Gameplay
{
    public static class CombatDebug
    {
        public static bool Enabled = true;

        public static void Log(string category, string message, Object context = null)
        {
            if (!Enabled)
            {
                return;
            }

            if (context != null)
            {
                Debug.Log($"[CombatDebug][{category}] {message}", context);
                return;
            }

            Debug.Log($"[CombatDebug][{category}] {message}");
        }

        public static string NameOf(Object context)
        {
            return context != null ? context.name : "null";
        }
    }
}
