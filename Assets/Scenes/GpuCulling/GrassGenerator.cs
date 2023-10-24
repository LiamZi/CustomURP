using System;
using System.Collections;
using System.Collections.Generic;
using CustomPipeline;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    public class GrassGenerator : MonoBehaviour
    {
        public Mesh _grassMesh;
        public int _subMeshIndex = 0;
        public Material _material;
        public int _grassCountPerRaw = 300;
        private HizDepthGenerator _Hiz;
        public ComputeShader _compute;

        private int _grassCount;
        private int _kernel;
        private Camera _camera;

        private ComputeBuffer _argsBuffer;
        private ComputeBuffer _grassMatrices;
        private ComputeBuffer _cullResult;

        private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };

        private int _cullResultID;
        private int _vpMatrixID;
        private int _posBufferID;
        private int _hizTextureId;

        void Start()
        {
            _grassCount = _grassCountPerRaw * _grassCountPerRaw;
            _camera = Camera.main;
            if(_camera) _Hiz = _camera.GetComponent<CustomRenderPipelineCamera>().HizDepth;

            if (_grassMesh != null)
            {
                _args[0] = _grassMesh.GetIndexCount(_subMeshIndex);
                _args[2] = _grassMesh.GetIndexStart(_subMeshIndex);
                _args[3] = _grassMesh.GetBaseVertex(_subMeshIndex);
            }

            InitComputeBuffer();
            InitGrassPos();
            InitComputeShader();
        }

        void InitComputeBuffer()
        {
            if (_grassMatrices != null) return;

            _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            _argsBuffer.SetData(_args);
            _grassMatrices = new ComputeBuffer(_grassCount, sizeof(float) * 16);
            _cullResult = new ComputeBuffer(_grassCount, sizeof(float) * 16, ComputeBufferType.Append);
        }

        void InitGrassPos()
        {
            const int padding = 2;
            int width = (100 - padding * 2);
            int widthStart = -width / 2;
            float step = (float)width / _grassCountPerRaw;
            Matrix4x4[] matrices = new Matrix4x4[_grassCount];

            for (int i = 0; i < _grassCountPerRaw; ++i)
            {
                for (int j = 0; j < _grassCountPerRaw; ++j)
                {
                    Vector2 xz = new Vector2(widthStart + step * i, widthStart + step * j);
                    Vector3 pos = new Vector3(xz.x, GetGroundHeight(xz), xz.y);
                    matrices[i * _grassCountPerRaw + j] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
                }
            } 
            
            _grassMatrices.SetData(matrices);
        }

        void InitComputeShader()
        {
            _kernel = _compute.FindKernel("CSMain");
            _compute.SetInt("_count", _grassCount);
            _compute.SetInt("_depthTextureSize", _Hiz.TextureSize);
            _compute.SetBool("_isOGL", ((Camera)_camera).projectionMatrix.Equals(GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false)));
            // _compute.SetBool("_isOGL", false);
            _compute.SetBuffer(_kernel, "_grassMatrixBuffer", _grassMatrices);

            _cullResultID = Shader.PropertyToID("_cullResultBuffer");
            _vpMatrixID = Shader.PropertyToID("_vpMatrix");
            _hizTextureId = Shader.PropertyToID("_hizTexture");
            _posBufferID = Shader.PropertyToID("positionBuffer");
        }

        float GetGroundHeight(Vector2 vec)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(vec.x, 10, vec.y), Vector3.down, out hit, 20))
            {
                return 10 - hit.distance;
            }

            return 0;
        }

        private void Update()
        {
            var texture = _Hiz.Texture;
            if (texture == null) return;
            // _Hiz.SaveToFile(ref texture, "GrassGenerator");
            _compute.SetTexture(_kernel, _hizTextureId, texture);
            _compute.SetMatrix(_vpMatrixID, GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false) * _camera.worldToCameraMatrix);
            _cullResult.SetCounterValue(0);
            _compute.SetBuffer(_kernel, _cullResultID, _cullResult);
            _compute.Dispatch(_kernel, 1 + _grassCount / 640, 1, 1);
            _material.SetBuffer(_posBufferID, _cullResult);
            
            ComputeBuffer.CopyCount(_cullResult, _argsBuffer, sizeof(uint));
            Graphics.DrawMeshInstancedIndirect(_grassMesh, _subMeshIndex, _material, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), _argsBuffer);
        }

        private void OnDisable()
        {
            _grassMatrices?.Release();
            _grassMatrices = null;
            
            _cullResult?.Release();
            _cullResult = null;
            
            _argsBuffer?.Release();
            _argsBuffer = null;
        }
    }
}