using UnityEngine;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(fileName = "PostFXSettings", menuName = "Rendering/Custom Post FX Settings", order = 0)]
public class PostFXSettings : ScriptableObject 
{
    [SerializeField]
    Shader _shader = default;

    [System.NonSerialized]
    Material _material;

    [System.Serializable]
    public struct BloomSettings
    {
        [Range(0f, 16f)]
        public int _maxIterations;

        [Min(1f)]
        public int _downScaleLimit;
        public bool _bicubicUpSampling;

        [Min(0f)]
        public float _threshold;
        
        [Range(0f, 1f)]
        public float _thresholdKnee;
        
        [Min(0f)]
        public float _intensity;

        public bool _fadeFireFlies;

        public enum Mode 
        {
            Additive,
            Scattering
        };

        public Mode _mode;

        [Range(0.05f, 0.95f)]
        public float _scatter;
    }

    [SerializeField]
    BloomSettings _bloom = new BloomSettings 
    {
        _scatter = 0.7f
    };

    public BloomSettings Bloom => _bloom;

    [System.Serializable]
    public struct ToneMappingSettings
    {
        public enum Mode 
        {
            None = -1,
            ACES,
            Neutral,
            Reinhard
        };

        public Mode _mode;
    };

    [SerializeField]
    ToneMappingSettings _toneMapping = default;

    public ToneMappingSettings ToneMaping => _toneMapping;

    public Material Material
    {
        get 
        {
            if(_material == null && _shader != null)
            {
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.HideAndDontSave;
            }
            return _material;
        }
    }
}