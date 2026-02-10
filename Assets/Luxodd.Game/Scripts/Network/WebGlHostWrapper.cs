using System.Runtime.InteropServices;
using UnityEngine;

namespace Luxodd.Game.Scripts.Network
{
    public class WebGlHostWrapper : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern string Luxodd_GetParentHost();

    [DllImport("__Internal")]
    private static extern string Luxodd_GetWebSocketProtocol();
#endif
        
        public string GetParentHostSafe()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
        return Luxodd_GetParentHost();
#else
            
            return string.Empty;
#endif
        }

        public string GetWebSocketProtocolSafe()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
        return Luxodd_GetWebSocketProtocol();
#else
            return string.Empty;
#endif
        }
    }
}
