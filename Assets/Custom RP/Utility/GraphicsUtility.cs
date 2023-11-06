using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomPipeline
{
    public static class GraphicsUtility
    {
        private static bool _isD3D = true;
        
        public static bool Platform
        {
            get { return _isD3D; }
        }
        
        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void SetPlatform()
        {
            _isD3D = SystemInfo.graphicsDeviceVersion.IndexOf("Direct3D", StringComparison.Ordinal) > -1;
        }
    };
}