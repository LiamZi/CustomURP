using UnityEngine;

public class InstanceColor : MonoBehaviour
{
    [SerializeField]
    Color _color = Color.white;
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
        
        _propertyBlock.SetColor("_Color", _color);
        GetComponent<MeshRenderer>().SetPropertyBlock(_propertyBlock);
    }
};