using UnityEngine;

[DisallowMultipleComponent]
public class InstanceColor : MonoBehaviour
{

    static int BaseColorId = Shader.PropertyToID("_BaseColor");
    static int CutOffId = Shader.PropertyToID("_Cutoff");
    static int MetallicId = Shader.PropertyToID("_Metallic");
    static int SmoothnessId = Shader.PropertyToID("_Smoothness");
    static int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField]
    Color _BaseColor = Color.white;
    
    [SerializeField, Range(0f, 1f)]
    float _alphaCutOff = 0.5f;

    [SerializeField, Range(0f, 1f)]
    float _metallic = 0f;

    [SerializeField, Range(0f, 1f)]
    float _smoothness = 0.5f;

    [SerializeField, ColorUsage(false, true)]
    Color _emissionColor = Color.black;

    static MaterialPropertyBlock _propertyBlock;

    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (_propertyBlock == null) 
        {
            _propertyBlock = new MaterialPropertyBlock();
        }
        
        _propertyBlock.SetColor(BaseColorId, _BaseColor);
        _propertyBlock.SetFloat(CutOffId, _alphaCutOff);
        _propertyBlock.SetFloat(MetallicId, _metallic);
        _propertyBlock.SetFloat(SmoothnessId, _smoothness);
        _propertyBlock.SetColor(EmissionColorId, _emissionColor);
        GetComponent<MeshRenderer>().SetPropertyBlock(_propertyBlock);
    }
};