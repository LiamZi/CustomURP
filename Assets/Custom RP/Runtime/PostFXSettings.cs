using UnityEngine;

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
    }

    [SerializeField]
    BloomSettings _bloom = default;

    public BloomSettings Bloom => _bloom;


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