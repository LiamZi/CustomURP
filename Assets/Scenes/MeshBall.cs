using UnityEngine;
using UnityEngine.Rendering;

public class MeshBall : MonoBehaviour 
{
    static int BaseColorId = Shader.PropertyToID("_BaseColor");
    static int MetallicId = Shader.PropertyToID("_Metallic");
    static int SmoothnessId = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    Mesh _mesh = default;

    [SerializeField]
    Material _material = default;

    Matrix4x4[] _matrices = new Matrix4x4[1023];
    Vector4[] _baseColors = new Vector4[1023];
    float[] _metallic = new float[1023];
    float[] _smoothness = new float[1023];

    MaterialPropertyBlock _block;

    [SerializeField]
    LightProbeProxyVolume _lightProbeVolume = null;


    private void Awake() 
    {
        for(int i = 0; i < _matrices.Length; ++i)
        {
            _matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10f, Quaternion.Euler(Random.value * 360f, Random.value * 360f, Random.value * 360f), Vector3.one * Random.Range(0.5f, 1.5f));
            _baseColors[i] = new Vector4(Random.value, Random.value, Random.value, Random.value);
            _metallic[i] = Random.value < 0.25f ? 1f : 0f;
            _smoothness[i] = Random.Range(0.05f, 0.95f);
        }

    }

    private void Update() 
    {
        if(_block == null)
        {
            _block = new MaterialPropertyBlock();
            _block.SetVectorArray(BaseColorId, _baseColors);
            _block.SetFloatArray(MetallicId, _metallic);
            _block.SetFloatArray(SmoothnessId, _smoothness);

            //TODO: Calculate the SH 
            // var positions = new Vector3[1023];
            // for(int i = 0; i < _matrices.Length; i++)
            // {
            //     positions[i] = _matrices[i].GetColumn(3);
            // }

            // var lightProbes = new SphericalHarmonicsL2[1023];
            // LightProbes.CalculateInterpolatedLightAndOcclusionProbes(positions, lightProbes, null);
            // _block.CopySHCoefficientArraysFrom(lightProbes);

            //TODO: Use LLPV
            if(!_lightProbeVolume)
            {
                var positions = new Vector3[1023];
                for(int i = 0; i < _matrices.Length; i++)
                {
                    positions[i] = _matrices[i].GetColumn(3);
                }

                var lightProbes = new SphericalHarmonicsL2[1023];
                var occlusionProbes = new Vector4[1023];

                LightProbes.CalculateInterpolatedLightAndOcclusionProbes(positions, lightProbes, occlusionProbes);
                _block.CopySHCoefficientArraysFrom(lightProbes);
                _block.CopyProbeOcclusionArrayFrom(occlusionProbes);
            }
        }
        // Graphics.DrawMeshInstanced(_mesh, 0, _material, _matrices, 1023, _block, ShadowCastingMode.On, true, 0, null, LightProbeUsage.CustomProvided);
        // Graphics.DrawMeshInstanced(_mesh, 0, _material, _matrices, 1023, _block, ShadowCastingMode.On, true, 0, null, _lightProbeVolume ? LightProbeUsage.UseProxyVolume : LightProbeUsage.CustomProvided, _lightProbeVolume);
        Graphics.DrawMeshInstanced(_mesh, 0, _material, _matrices, 1023, _block, ShadowCastingMode.On, true, 0, null, _lightProbeVolume ? LightProbeUsage.UseProxyVolume : LightProbeUsage.CustomProvided, _lightProbeVolume);

    }

};