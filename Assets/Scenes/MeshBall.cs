using UnityEngine;

public class MeshBall : MonoBehaviour 
{
    static int BaseColorId = Shader.PropertyToID("_Color");

    [SerializeField]
    Mesh _mesh = default;

    [SerializeField]
    Material _material = default;

    Matrix4x4[] _matrices = new Matrix4x4[1023];
    Vector4[] _baseColors = new Vector4[1023];

    MaterialPropertyBlock _block;


    private void Awake() 
    {
        for(int i = 0; i < _matrices.Length; ++i)
        {
            _matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10f, Quaternion.Euler(Random.value * 360f, Random.value * 360f, Random.value * 360f), Vector3.one * Random.Range(0.5f, 1.5f));
            _baseColors[i] = new Vector4(Random.value, Random.value, Random.value, Random.value);
        }

    }

    private void Update() 
    {
        if(_block == null)
        {
            _block = new MaterialPropertyBlock();
            _block.SetVectorArray(BaseColorId, _baseColors);
        }
        Graphics.DrawMeshInstanced(_mesh, 0, _material, _matrices, 1023, _block);
    }

};