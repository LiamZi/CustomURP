using Unity.Mathematics;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;


namespace CustomURP
{
    public class TerrainBuilder : System.IDisposable
    {
        TerrainAsset _asset;
        ComputeShader _shader;
        Command _cmd = null;
        
        ComputeBuffer _maxLODNodeList;
        ComputeBuffer _nodeListA;
        ComputeBuffer _nodeListB;
        ComputeBuffer _finalNodeListBuffer;
        ComputeBuffer _nodeDescriptors;
        ComputeBuffer _culledPatchBuffer;
        ComputeBuffer _patchIndirectArgs;
        ComputeBuffer _patchBoundsBuffer;
        ComputeBuffer _patchBoundsIndirectArgs;
        ComputeBuffer _indirectArgsBuffer;
        RenderTexture _lodMap;
        
        const int PatchStripSize = 9 * 4;
        int _kernelOfTraverseQuadTree;
        int _kernelOfBuildLodMap;
        int _kernelOfBuildPatches;
        int _maxNodeBufferSize = 200;
        int _tempNodeBufferSize = 50;
        bool _isBoundsBufferOn;
        bool _isNodeEvaluationCDirty = true;
        float _hizDepthBias = 1.0f;
        
        
        
        Vector4 _nodeEvaluationC = new Vector4(1, 0, 0, 0);
        Plane[] _cameraFrustumPlanes = new Plane[6];
        Vector4[] _cameraFrustumPlanesToVector4 = new Vector4[6]; 
        
        
        
        public TerrainBuilder(TerrainAsset asset)
        {
            _asset = asset;
            _shader = asset.TerrainShader;
            _cmd = new Command("TerrainBuild");
            _culledPatchBuffer = new ComputeBuffer(_maxNodeBufferSize * 64, PatchStripSize, ComputeBufferType.Append);
            
            _patchIndirectArgs = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
            _patchIndirectArgs.SetData(new uint[]{TerrainAsset.PatchMesh.GetIndexCount(0), 0, 0, 0, 0});

            _patchBoundsIndirectArgs = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
            _patchBoundsIndirectArgs.SetData(new uint[]{TerrainAsset.CubeMesh.GetIndexCount(0), 0, 0, 0, 0});

            _maxLODNodeList = new ComputeBuffer(asset.MaxLodNodeCount * asset.MaxLodNodeCount, 8, ComputeBufferType.Append);
            InitializationLodNodeListData();

            _nodeListA = new ComputeBuffer(_tempNodeBufferSize, 8, ComputeBufferType.Append);
            _nodeListB = new ComputeBuffer(_tempNodeBufferSize, 8, ComputeBufferType.Append);
            _indirectArgsBuffer = new ComputeBuffer(3, 4, ComputeBufferType.IndirectArguments);
            _indirectArgsBuffer.SetData(new uint[] { 1, 1, 1});
            
            _finalNodeListBuffer = new ComputeBuffer(_maxNodeBufferSize, 12, ComputeBufferType.Append);
            _nodeDescriptors = new ComputeBuffer((int)(_asset._maxNodeId + 1), 4);

            _patchBoundsBuffer = new ComputeBuffer(_maxNodeBufferSize * 64, 4 * 10, ComputeBufferType.Append);

            _lodMap = TextureUtility.CreateLodMap(160);

            if (SystemInfo.usesReversedZBuffer)
            {
                _shader.EnableKeyword("_REVERSE_Z");
            }
            else
            {
                _shader.DisableKeyword("_REVERSE_Z");
            }

            InitializationKernels();
            InitializationWorldParams();

            BoundsHeightRedundance = 5;
            HizDepthBias = 1.0f;
        }

        void InitializationKernels()
        {
            _kernelOfTraverseQuadTree = _shader.FindKernel("TraverseQuadTree");
            _kernelOfBuildLodMap = _shader.FindKernel("BuildLodMap");
            _kernelOfBuildPatches = _shader.FindKernel("BuildPatches");

            BindComputeShader(_kernelOfTraverseQuadTree);
            BindComputeShader(_kernelOfBuildLodMap);
            BindComputeShader(_kernelOfBuildPatches);
        }

        void InitializationWorldParams()
        {
            float size = _asset.WorldSize.x;
            int maxCount = _asset.MaxLodNodeCount;
            Vector4[] worldLodParams = new Vector4[_asset.MaxLod + 1];

            for (var lod = _asset.MaxLod; lod >= 0; lod--)
            {
                var nodeSize = size / maxCount;
                var patchExtent = nodeSize / 16;
                var sectorCountPerNode = (int)Mathf.Pow(2, lod);
                worldLodParams[lod] = new Vector4(nodeSize, patchExtent, maxCount, sectorCountPerNode);
                maxCount *= 2;
            }
            _shader.SetVectorArray(ShaderParams._worldLodParams, worldLodParams);

            int[] nodeIdOffsetLod = new int[(_asset.MaxLod + 1) * 4];
            int nodeIdOffset = 0;
            
            for (int lod = _asset.MaxLod; lod >= 0; lod--)
            {
                nodeIdOffsetLod[lod * 4] = nodeIdOffset;
                nodeIdOffset += (int)(worldLodParams[lod].z * worldLodParams[lod].z);
            }
            
            _shader.SetInts(ShaderParams._nodeIdOffsetOfLOD, nodeIdOffsetLod);
        }

        void InitializationLodNodeListData()
        {
            var maxNodeSize = _asset.MaxLodNodeCount;
            uint2[] data = new uint2[maxNodeSize * maxNodeSize];
            var index = 0;
            
            for (uint i = 0; i < maxNodeSize; i++)
            {
                for (uint j = 0; j < maxNodeSize; j++)
                {
                    data[index] = new uint2(i, j);
                    index++;
                }
            }
            
            _maxLODNodeList.SetData(data);
        }

        void BindComputeShader(int kernelIndex)
        {
            _shader.SetTexture(kernelIndex, ShaderParams._quadTreeTexture, _asset.QuadTreeMap);
            if (kernelIndex == _kernelOfTraverseQuadTree)
            {
                _shader.SetBuffer(kernelIndex, ShaderParams._appendFinalNodeList, _finalNodeListBuffer);
                _shader.SetTexture(kernelIndex, ShaderParams._minMaxHeightTexture, _asset.MinMaxHeightMap);
                _shader.SetBuffer(kernelIndex, ShaderParams._nodeDescriptors, _nodeDescriptors);
            }
            else if (kernelIndex == _kernelOfBuildLodMap)
            {
                _shader.SetTexture(kernelIndex, ShaderParams._lodMap, _lodMap);
                _shader.SetBuffer(kernelIndex, ShaderParams._nodeDescriptors, _nodeDescriptors);
            }
            else if (kernelIndex == _kernelOfBuildPatches)
            {
                _shader.SetTexture(kernelIndex, ShaderParams._lodMap, _lodMap);
                _shader.SetTexture(kernelIndex, ShaderParams._minMaxHeightTexture, _asset.MinMaxHeightMap);
                _shader.SetBuffer(kernelIndex, ShaderParams._finalNodeList, _finalNodeListBuffer);
                _shader.SetBuffer(kernelIndex, ShaderParams._culledPatchList, _culledPatchBuffer);
                _shader.SetBuffer(kernelIndex, ShaderParams._patchBoundsList, _patchBoundsBuffer);
            }
        }

        void ClearBufferCounter()
        {
            _cmd.SetBufferCounterValue(_maxLODNodeList, (uint)_maxLODNodeList.count);
            _cmd.SetBufferCounterValue(_nodeListA, 0);
            _cmd.SetBufferCounterValue(_nodeListB, 0);
            _cmd.SetBufferCounterValue(_finalNodeListBuffer, 0);
            _cmd.SetBufferCounterValue(_culledPatchBuffer, 0);
            _cmd.SetBufferCounterValue(_patchBoundsBuffer, 0);
        }

        public void Tick()
        {
            var camera = Camera.main;
            _cmd.Clear();
            ClearBufferCounter();
            
            UpdateCameraFrustumPlanes(camera);

            if (_isNodeEvaluationCDirty)
            {
                _isNodeEvaluationCDirty = false;
                _cmd.SetComputeVectorParam(_shader, ShaderParams._nodeEvaluationC, _nodeEvaluationC);
            }
            
            _cmd.SetComputeVectorParam(_shader, ShaderParams._cameraPositionWS, camera.transform.position);
            _cmd.SetComputeVectorParam(_shader, ShaderParams._worldSize, _asset.WorldSize);
            
            
        }

        void UpdateCameraFrustumPlanes(Camera camera)
        {
            GeometryUtility.CalculateFrustumPlanes(camera, _cameraFrustumPlanes);
            for (var i = 0; i < _cameraFrustumPlanes.Length; ++i)
            {
                Vector4 p = (Vector4)_cameraFrustumPlanes[i].normal;
                p.w = _cameraFrustumPlanes[i].distance;
                _cameraFrustumPlanesToVector4[i] = p;
            }
            
            _shader.SetVectorArray(ShaderParams._cameraFrustumPlanes, _cameraFrustumPlanesToVector4);
        }

        public ComputeBuffer PatchBoundsBuffer
        {
            get
            {
                return _patchBoundsBuffer;
            }
        }

        public bool IsFrustumCullEnabled
        {
            set
            {
                if (value)
                    _shader.EnableKeyword("ENALBE_FRUSTUM_CULL");
                else
                    _shader.DisableKeyword("ENALBE_FRUSTUM_CULL");
            }
        }

        public bool IsHizOcclusionCullingEnabled
        {
            set
            {
                if(value)
                    _shader.EnableKeyword("ENALBE_HIZ_CULL");
                else
                    _shader.DisableKeyword("ENALBE_HIZ_CULL");
            }
        }

        public bool IsBoundsBufferOn
        {
            set
            {
                if(value)
                    _shader.EnableKeyword("BOUNDS_DEBUG");
                else
                    _shader.DisableKeyword("BOUNDS_DEBUG");

                _isBoundsBufferOn = value;
            }

            get
            {
                return _isBoundsBufferOn;
            }
        }

        public int BoundsHeightRedundance
        {
            set
            {
                _shader.SetInt(ShaderParams._boundsHeightRedundance, value);
            }
        }

        public float NodeEvalDistance
        {
            set
            {
                _nodeEvaluationC.x = value;
                _isNodeEvaluationCDirty = true;
            }
        }

        public bool EnableSeamDebug
        {
            set
            {
                if(value)
                    _shader.EnableKeyword("ENABLE_SEAM");
                else
                    _shader.DisableKeyword("ENABLE_SEAM");
            }
        }

        public float HizDepthBias
        {
            set
            {
                _hizDepthBias = value;
                _shader.SetFloat(ShaderParams._hizDepthBias, Mathf.Clamp(value, 0.01f, 1000f));
            }

            get
            {
                return _hizDepthBias;
            }
        }
        
        
        public void Dispose()
        {
            _culledPatchBuffer.Dispose();
            _patchIndirectArgs.Dispose();
            _finalNodeListBuffer.Dispose();
            _maxLODNodeList.Dispose();
            _nodeListA.Dispose();
            _nodeListB.Dispose();
            _indirectArgsBuffer.Dispose();
            _patchBoundsBuffer.Dispose();
            _patchBoundsIndirectArgs.Dispose();
            _nodeDescriptors.Dispose();
        }
    }
}
