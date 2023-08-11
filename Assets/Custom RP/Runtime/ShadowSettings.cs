using UnityEngine;

[System.Serializable]
public class ShadowSettings
{


    [Min(0.001f)]
    public float _maxDistance = 100f;

    [Range(0.001f, 1f)]
    public float _distanceFade = 0.1f;

    public enum TextureSize
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
        _8192 = 8192
    };

    public enum FilterMode
    {
        PCF2x2,
        PCF3x3,
        PCF5x5,
        PCF7x7,
    };

    public enum CascadeBlendMode
    {
        Hard,
        Soft,
        Dither
    };

    [System.Serializable]


    public struct Directional
    {
        public TextureSize _atlasSize;

        public FilterMode _filter;

        [Range(1, 4)]
        public int _cascadeCount;

        [Range(0f, 1f)]
        public float _cascadeRatio1;
        [Range(0f, 1f)]
        public float _cascadeRatio2;
        [Range(0f, 1f)]
        public float _cascadeRatio3;
        [Range(0.001f, 1f)]
        public float _cascadeFade;

        public CascadeBlendMode _cascadblendMode;

        public Vector3 GetCascadeRatios => new Vector3(_cascadeRatio1, _cascadeRatio2, _cascadeRatio3);
    };

    public Directional _directional = new Directional
    { 
        _atlasSize = TextureSize._1024, 
        _filter = FilterMode.PCF2x2,
        _cascadeCount = 4,
        _cascadeRatio1 = 0.1f,
        _cascadeRatio2 = 0.25f,
        _cascadeRatio3 = 0.5f,
        _cascadeFade = 0.1f,
        _cascadblendMode = CascadeBlendMode.Hard
    };



};