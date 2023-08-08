using UnityEngine;

[DisallowMultipleComponent]
public class InstanceColor : MonoBehaviour
{

    static int BaseColorId = Shader.PropertyToID("_Color");
    static int CutOffId = Shader.PropertyToID("_CutOff");

    [SerializeField]
    Color _color = Color.white;
    
    [SerializeField, Range(0f, 1f)]
    float _alphaCutOff = 0.5f;

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
        GetComponent<MeshRenderer>().SetPropertyBlock(_propertyBlock);
    }
};