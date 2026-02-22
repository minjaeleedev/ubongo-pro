using UnityEngine;

namespace Ubongo
{
    public static class UnityObjectUtility
    {
        public static void SafeDestroy(Object target)
        {
            if (target == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                Object.DestroyImmediate(target);
                return;
            }
#endif

            Object.Destroy(target);
        }
    }
}
