using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomPipeline
{
    public static class DeviceUtility
    {
        public static bool Platform { get; private set; } = true;
        public static bool CopyTextureSupported { get; private set; } = false;

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void SetPlatform()
        {
            Platform = SystemInfo.graphicsDeviceVersion.IndexOf("Direct3D", StringComparison.Ordinal) > -1;
            CopyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
        }
        
    };
}