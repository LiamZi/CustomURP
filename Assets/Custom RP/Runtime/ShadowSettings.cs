using UnityEngine;

[System.Serializable]
public class ShadowSettings
{
    [Min(0f)]
    public float _maxDistance = 100f;

    public enum TextureSize
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
        _8192 = 8192
    };

    [System.Serializable]
    public struct Directional
    {
        public TextureSize _atlasSize;

        [Range(1, 4)]
        public int _cascadeCount;

        [Range(0f, 1f)]
        public float _cascadeRatio1;
        [Range(0f, 1f)]
        public float _cascadeRatio2;
        [Range(0f, 1f)]
        public float _cascadeRatio3;

        public Vector3 GetCascadeRatios => new Vector3(_cascadeRatio1, _cascadeRatio2, _cascadeRatio3);
    };

    public Directional _directional = new Directional
    { 
        _atlasSize = TextureSize._1024, 
        _cascadeCount = 4,
        _cascadeRatio1 = 0.1f,
        _cascadeRatio2 = 0.25f,
        _cascadeRatio3 = 0.5f
    };



};