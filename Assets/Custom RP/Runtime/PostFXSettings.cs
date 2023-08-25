using UnityEngine;

[CreateAssetMenu(fileName = "PostFXSettings", menuName = "Rendering/Custom Post FX Settings", order = 0)]
public class PostFXSettings : ScriptableObject 
{
    [SerializeField]
    Shader _shader = default;

    [System.NonSerialized]
    Material _material;


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