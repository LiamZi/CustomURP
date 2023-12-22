using UnityEngine;

namespace CustomURP
{
    public enum FrameBlock
    {
        _Off = 1,
        _2x2 = 4,
        _4x4 = 16
    }
    
    [CreateAssetMenu(menuName = "Custom URP/Weathers/VolumeCloud")]
    public class VolumeCloudSettings : ScriptableObject
    {
        public Material _material;
        public Texture2D _blueNoise;
        public float _rtScale = 0.5f;
        public FrameBlock _frameBlock = FrameBlock._4x4;

        [Range(100, 600)]
        public int _shieldWith = 400;
        public bool _isFrameDebug = false;

        [Range(1, 16)]
        public int _frameDebug = 1;
    }
}
