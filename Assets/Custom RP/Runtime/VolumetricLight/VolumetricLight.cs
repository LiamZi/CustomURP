using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    [RequireComponent(typeof(Light))]
    public class VolumetricLight : MonoBehaviour
    {
        Light _light;
        Material _material;
        Command _cmd;
        Command _shadowCmd;
        Vector4[] _frustum = new Vector4[4];
        bool _reversedZ = false;

        [Range(1, 64)]
        public int _samplerCount = 8;
        [Range(0.0f, 1.0f)]
        public float _scatteringCoef = 0.5f;
        [Range(0.0f, 0.1f)]
        public float _exinctionCoef = 0.01f;
        [Range(0.0f, 1.0f)]
        public float _skyBoxExtinctionCoef = 0.9f;
        [Range(0.0f, 0.999f)]
        public float _mieG = 0.1f;
        public bool _fog = false;
        [Range(0.0f, 0.0f)]
        public float _heightScale = 0.1f;
        public float _groundLevel = 0;
        public bool _noise = false;
        public float _noiseScale = 0.015f;
        public float _noiseIntensity = 1.0f;
        public float _noiseIntensityOffset = 0.3f;
        public Vector2 _noiseVelocity = new Vector2(3.0f, 3.0f);
        public float _maxRayLength = 400.0f;

        public Light Light
        {
            get
            {
                return _light;
            }
        }

        public Material Material
        {
            get
            {
                return _material;
            }
        }

        void Start()
        {
#if UNITY_2020_3_OR_NEWER
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
            {
                _reversedZ = true;
            }
#endif

            _cmd = new Command("Volumetric Light");
            
        }

    }
}
