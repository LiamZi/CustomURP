using UnityEngine;

[DisallowMultipleComponent]
public class InstanceColor : MonoBehaviour
{

    static int BaseColorId = Shader.PropertyToID("_Color");
    static int CutOffId = Shader.PropertyToID("_CutOff");
    static int MetallicId = Shader.PropertyToID("_Metallic");
    static int SmoothnessId = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    Color _color = Color.white;
    
    [SerializeField, Range(0f, 1f)]
    float _alphaCutOff = 0.5f;

    [SerializeField, Range(0f, 1f)]
    float _metallic = 0f;

    [SerializeField, Range(0f, 1f)]
    float _smoothness = 0.5f;

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
        
        _propertyBlock.SetColor(BaseColorId, _color);
        _propertyBlock.SetFloat(CutOffId, _alphaCutOff);
        _propertyBlock.SetFloat(MetallicId, _metallic);
        _propertyBlock.SetFloat(SmoothnessId, _smoothness);
        GetComponent<MeshRenderer>().SetPropertyBlock(_propertyBlock);
    }
};