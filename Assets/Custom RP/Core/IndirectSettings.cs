using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Core
{
    [Serializable]
    public class IndirectSettings
    {
        public bool _indirectRendering = true;
        public bool _runCompute = true;
        public bool _AsyncCompute = true;
        
    }
}