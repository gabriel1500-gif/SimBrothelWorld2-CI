using UnityEngine;

namespace SBW2.Core
{
    public class MobilePerformanceManager : MonoBehaviour
    {
        [SerializeField] int targetFrameRate = 60;

        void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = 0;

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        public static bool IsPortrait()
        {
            return Screen.height >= Screen.width;
        }

        public static string DeviceSummary()
        {
            return $"{SystemInfo.deviceModel} · {SystemInfo.operatingSystem} · " +
                   $"{Screen.width}x{Screen.height} · {SystemInfo.systemMemorySize} MB RAM";
        }
    }
}
