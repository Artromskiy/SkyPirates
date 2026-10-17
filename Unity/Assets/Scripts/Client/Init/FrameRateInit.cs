using UnityEngine;

namespace SlyPirates.Client.Init
{
    public class FrameRateInit
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static void Init()
        {
            Application.targetFrameRate = 60;
        }
    }
}

