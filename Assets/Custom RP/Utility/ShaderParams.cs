using System.Collections;
using UnityEngine;

namespace CustomPipeline
{
    public static partial class ShaderParams
    {
        public static readonly int _MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int _Count = Shader.PropertyToID("_Count");

        public static readonly int _SourceTex = Shader.PropertyToID("_SourceTex");
        public static readonly int _DestTex = Shader.PropertyToID("_DestTex");
    };
    

};