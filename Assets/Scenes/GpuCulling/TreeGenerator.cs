using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace CustomURP
{
    public class TreeGenerator : MonoBehaviour
    {
        private int _kernel;
        public Mesh _mesh;
        public Material _meshMaterial;
        public ComputeShader _computeShader;
        public int _subMeshIndex = 0;
        
        [Range(0, 100000)] public int _count = 100000;
        
        private int _cachedInstanceCount = -1;
        private int _cachedSubMeshIndex = -1;
        ComputeBuffer _argsBuffer;
        uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
        
        private ComputeBuffer _localToWorldMatrixBuffer;
        private ComputeBuffer _cullResultBuffer;
        private Camera _camera;
        //
        private void Start()
        {
            _kernel = _computeShader.FindKernel("CSMain");
            _camera = Camera.main;
            _cullResultBuffer = new ComputeBuffer(_count, sizeof(float) * 16, ComputeBufferType.Append);
            _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            
            UpdateDataBuffer();
        }
        
        //
        // // Update is called once per frame
        void Update()
        {
            if (_cachedInstanceCount != _count || _cachedSubMeshIndex != _subMeshIndex)
            {
                UpdateDataBuffer();
            }
            
            Vector4[] planes = CameraTool.GetFrustumPlane(_camera);
            _computeShader.SetBuffer(_kernel, "_input", _localToWorldMatrixBuffer);
            _cullResultBuffer.SetCounterValue(0);
            _computeShader.SetBuffer(_kernel, "_cullResult", _cullResultBuffer);
            _computeShader.SetInt("_instanceCount", _count);
            _computeShader.SetVectorArray("_planes", planes);

            _computeShader.Dispatch(_kernel, 1 + (_count / 64), 1, 1);
            _meshMaterial.SetBuffer("positionBuffer", _cullResultBuffer);
            // _meshMaterial.SetColor("_Color", Color.white);
            
            ComputeBuffer.CopyCount(_cullResultBuffer, _argsBuffer, sizeof(uint));
            
            
            // Graphics.DrawMeshInstanced(_mesh, 0, _meshMaterial, _matrices, 1023, _block, ShadowCastingMode.On, true, 0, null, LightProbeUsage.CustomProvided);
             // Graphics.DrawMesh(_mesh, Vector3.zero, Quaternion.identity, _meshMaterial, 0);
            // Matrix4x4 m = Matrix4x4.TRS(Vector3.up, quaternion.identity, Vector3.one);
            //Graphics.DrawMeshInstanced(_mesh, 0, _meshMaterial, m, 1);
            // Graphics.DrawMeshInstanced(_mesh, 0, _meshMaterial, _matrices, _count);
            Graphics.DrawMeshInstancedIndirect(_mesh, _subMeshIndex, _meshMaterial, 
                            new Bounds(Vector3.zero, new Vector3(200.0f, 200.0f, 200.0f)), 
                            _argsBuffer);
        }
        
        void UpdateDataBuffer()
        {
            if (_mesh != null)
            {
                _subMeshIndex = Mathf.Clamp(_subMeshIndex, 0, _mesh.subMeshCount - 1);
            }
        
            if (_localToWorldMatrixBuffer != null)
            {
                _localToWorldMatrixBuffer.Release();
            }
        
            _localToWorldMatrixBuffer = new ComputeBuffer(_count, 16 * sizeof(float));
            
            List<Matrix4x4> localToWorldMatrices = new List<Matrix4x4>();
            for (int i = 0; i < _count; i++)
            {
                float angle = UnityEngine.Random.Range(0.0f, Mathf.PI * 2.0f);
                float distance = UnityEngine.Random.Range(20.0f, 100.0f);
                float height = UnityEngine.Random.Range(-2.0f, 2.0f);
                float size = UnityEngine.Random.Range(0.05f, 0.25f);
                Vector4 pos = new Vector4(Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance, size);
                var mat = Matrix4x4.TRS(pos, quaternion.identity, new Vector3(size, size, size));
                localToWorldMatrices.Add(mat);
            }
            
            _localToWorldMatrixBuffer.SetData(localToWorldMatrices);
        
            if (_mesh != null)
            {
                _args[0] = (uint)_mesh.GetIndexCount(_subMeshIndex);
                _args[2] = (uint)_mesh.GetIndexStart(_subMeshIndex);
                _args[3] = (uint)_mesh.GetBaseVertex(_subMeshIndex);
            }
            else
            {
                _args[0] = _args[1] = _args[2] = _args[3] = 0;
            }
            _argsBuffer.SetData(_args);
        
            _cachedInstanceCount = _count;
            _cachedSubMeshIndex = _subMeshIndex;
        }
        
        private void OnDisable()
        {
            _localToWorldMatrixBuffer?.Release();
            _localToWorldMatrixBuffer = null;
            
            _cullResultBuffer?.Release();
            _cullResultBuffer = null;
            
            _argsBuffer?.Release();
            _argsBuffer = null;
        }
    }
};

