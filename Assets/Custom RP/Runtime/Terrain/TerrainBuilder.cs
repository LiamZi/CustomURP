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
        Command _cmd = new Command("TerrainBuild");
        
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
        string _cmdOirginName = "";
        
        
        public TerrainBuilder(TerrainAsset asset)
        {
            _asset = asset;
            _shader = asset.TerrainShader;
            // _cmd = new Command("TerrainBuild");
            // _cmd = CustomRenderPipeline._cmd;
            // _cmdOirginName = _cmd.Name;
           
            _culledPatchBuffer = new ComputeBuffer(_maxNodeBufferSize * 64, PatchStripSize, ComputeBufferType.Append);
            
            _patchIndirectArgs = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
            _patchIndirectArgs.SetData(new uint[]{_asset.PatchMesh.GetIndexCount(0), 0, 0, 0, 0});

            _patchBoundsIndirectArgs = new ComputeBuffer(5, 4, ComputeBufferType.IndirectArguments);
            _patchBoundsIndirectArgs.SetData(new uint[]{_asset.CubeMesh.GetIndexCount(0), 0, 0, 0, 0});

            _maxLODNodeList = new ComputeBuffer(asset.MaxLodNodeCount * asset.MaxLodNodeCount, 8, ComputeBufferType.Append);
            InitializationLodNodeListData();

            // _nodeListA = new ComputeBuffer(_tempNodeBufferSize, 8, ComputeBufferType.Append);
            // _nodeListB = new ComputeBuffer(_tempNodeBufferSize, 8, ComputeBufferType.Append);
            _nodeListA = new ComputeBuffer(_tempNodeBufferSize,8,ComputeBufferType.Append);
            _nodeListB = new ComputeBuffer(_tempNodeBufferSize,8,ComputeBufferType.Append);
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
            // float size = _asset.WorldSize.x;
            // int maxCount = _asset.MaxLodNodeCount;
            // Vector4[] worldLodParams = new Vector4[_asset.MaxLod + 1];
            //
            // for (var lod = _asset.MaxLod; lod >= 0; lod--)
            // {
            //     var nodeSize = size / maxCount;
            //     var patchExtent = nodeSize / 16;
            //     var sectorCountPerNode = (int)Mathf.Pow(2, lod);
            //     worldLodParams[lod] = new Vector4(nodeSize, patchExtent, maxCount, sectorCountPerNode);
            //     maxCount *= 2;
            // }
            // _shader.SetVectorArray(ShaderParams._worldLodParamsId, worldLodParams);
            //
            // int[] nodeIdOffsetLod = new int[(_asset.MaxLod + 1) * 4];
            // int nodeIdOffset = 0;
            //
            // for (int lod = _asset.MaxLod; lod >= 0; lod--)
            // {
            //     nodeIdOffsetLod[lod * 4] = nodeIdOffset;
            //     nodeIdOffset += (int)(worldLodParams[lod].z * worldLodParams[lod].z);
            // }
            //
            // _shader.SetInts(ShaderParams._nodeIdOffsetOfLODId, nodeIdOffsetLod);
            
            float wSize = _asset.WorldSize.x;
            int nodeCount = _asset._maxLodNodeCount;
            Vector4[] worldLODParams = new Vector4[_asset.MaxLod + 1];
            for(var lod = _asset.MaxLod; lod >=0; lod --){
                var nodeSize = wSize / nodeCount;
                var patchExtent = nodeSize / 16;
                var sectorCountPerNode = (int)Mathf.Pow(2,lod);
                worldLODParams[lod] = new Vector4(nodeSize,patchExtent,nodeCount,sectorCountPerNode);
                nodeCount *= 2;
            }
            _shader.SetVectorArray(ShaderParams._worldLodParamsId,worldLODParams);

            int[] nodeIDOffsetLOD = new int[ (_asset.MaxLod + 1) * 4];
            int nodeIdOffset = 0;
            for(int lod = _asset.MaxLod; lod >=0; lod --){
                nodeIDOffsetLOD[lod * 4] = nodeIdOffset;
                nodeIdOffset += (int)(worldLODParams[lod].z * worldLODParams[lod].z);
            }
            _shader.SetInts("NodeIDOffsetOfLOD",nodeIDOffsetLOD);
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
            // _shader.SetTexture(kernelIndex, ShaderParams._quadTreeTextureId, _asset.GetQuadTreeMap(ref _cmd));
            // if (kernelIndex == _kernelOfTraverseQuadTree)
            // {
            //     _shader.SetBuffer(kernelIndex, ShaderParams._appendFinalNodeListId, _finalNodeListBuffer);
            //     _shader.SetTexture(kernelIndex, ShaderParams._minMaxHeightTextureId, _asset.GetMinMaxHeightMap(ref _cmd));
            //     _shader.SetBuffer(kernelIndex, ShaderParams._nodeDescriptorsId, _nodeDescriptors);
            // }
            // else if (kernelIndex == _kernelOfBuildLodMap)
            // {
            //     _shader.SetTexture(kernelIndex, ShaderParams._lodMapId, _lodMap);
            //     _shader.SetBuffer(kernelIndex, ShaderParams._nodeDescriptorsId, _nodeDescriptors);
            // }
            // else if (kernelIndex == _kernelOfBuildPatches)
            // {
            //     _shader.SetTexture(kernelIndex, ShaderParams._lodMapId, _lodMap);
            //     _shader.SetTexture(kernelIndex, ShaderParams._minMaxHeightTextureId, _asset.GetMinMaxHeightMap(ref _cmd));
            //     _shader.SetBuffer(kernelIndex, ShaderParams._finalNodeListId, _finalNodeListBuffer);
            //     _shader.SetBuffer(kernelIndex, ShaderParams._culledPatchListId, _culledPatchBuffer);
            //     _shader.SetBuffer(kernelIndex, ShaderParams._patchBoundsListId, _patchBoundsBuffer);
            // }
            
            _shader.SetTexture(kernelIndex,"_QuadTreeTexture",_asset.GetQuadTreeMap(ref _cmd));
            if(kernelIndex == _kernelOfTraverseQuadTree){
                _shader.SetBuffer(kernelIndex,ShaderParams._appendFinalNodeListId,_finalNodeListBuffer);
                _shader.SetTexture(kernelIndex,"_MinMaxHeightTexture",_asset.GetMinMaxHeightMap(ref _cmd));
                _shader.SetBuffer(kernelIndex,ShaderParams._nodeDescriptorsId,_nodeDescriptors);
            }else if(kernelIndex == _kernelOfBuildLodMap){
                _shader.SetTexture(kernelIndex,ShaderParams._lodMapId,_lodMap);
                _shader.SetBuffer(kernelIndex,ShaderParams._nodeDescriptorsId,_nodeDescriptors);
            }
            else if(kernelIndex == _kernelOfBuildPatches){
                _shader.SetTexture(kernelIndex,ShaderParams._lodMapId,_lodMap);
                _shader.SetTexture(kernelIndex,"_MinMaxHeightTexture",_asset.GetMinMaxHeightMap(ref _cmd));
                _shader.SetBuffer(kernelIndex,ShaderParams._finalNodeListId,_finalNodeListBuffer);
                _shader.SetBuffer(kernelIndex,"_CulledPatchList",_culledPatchBuffer);
                _shader.SetBuffer(kernelIndex,"_PatchBoundsList",_patchBoundsBuffer);
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

        // public void Tick(ScriptableRenderContext context, CustomRenderPipelineCamera camera)
        // {
        //     // var camera = Camera.main;
        //     // var context = CustomRenderPipeline._cmd.Context;
        //
        //     // _cmd.Context = context;
        //     
        //     _cmd.Clear();
        //     ClearBufferCounter();
        //     
        //     UpdateCameraFrustumPlanes(camera._camera);
        //     
        //
        //     if (_isNodeEvaluationCDirty)
        //     {
        //         _isNodeEvaluationCDirty = false;
        //         _cmd.SetComputeVectorParam(_shader, ShaderParams._nodeEvaluationCId, _nodeEvaluationC);
        //     }
        //     
        //     _cmd.SetComputeVectorParam(_shader, ShaderParams._cameraPositionWSId, camera._camera.transform.position);
        //     _cmd.SetComputeVectorParam(_shader, ShaderParams._worldSizeId, _asset.WorldSize);
        //     
        //     _cmd.CopyCounterValue(_maxLODNodeList, _indirectArgsBuffer, 0);
        //
        //     ComputeBuffer consumeNodeList = _nodeListA;
        //     ComputeBuffer appendNodeList = _nodeListB;
        //
        //     for (var lod = _asset._maxLOD; lod >= 0; lod--)
        //     {
        //         _cmd.SetComputeIntParam(_shader, ShaderParams._passLODId, lod);
        //         if (lod == _asset._maxLOD)
        //         {
        //             _cmd.SetComputeBufferParam(_shader, _kernelOfTraverseQuadTree, ShaderParams._consumeNodeListId, _maxLODNodeList);
        //         }
        //         else
        //         {
        //             _cmd.SetComputeBufferParam(_shader, _kernelOfTraverseQuadTree, ShaderParams._consumeNodeListId, consumeNodeList);
        //         }
        //         
        //         _cmd.SetComputeBufferParam(_shader, _kernelOfTraverseQuadTree, ShaderParams._appendNodeListId, appendNodeList);
        //         _cmd.DispatchCompute(_shader, _kernelOfTraverseQuadTree, _indirectArgsBuffer, 0);
        //         _cmd.CopyCounterValue(appendNodeList, _indirectArgsBuffer, 0);
        //         var temp = consumeNodeList;
        //         consumeNodeList = appendNodeList;
        //         appendNodeList = temp;
        //         
        //         // SwapNodeList(ref consumeNodeList, ref appendNodeList);
        //     }
        //     
        //     _cmd.DispatchCompute(_shader, _kernelOfBuildLodMap, 20, 20, 1);
        //
        //     var tmp = new uint3[_maxNodeBufferSize];
        //     _finalNodeListBuffer.GetData(tmp);
        //     
        //     
        //     _cmd.CopyCounterValue(_finalNodeListBuffer, _indirectArgsBuffer, 0);
        //     _cmd.DispatchCompute(_shader, _kernelOfBuildPatches, _indirectArgsBuffer, 0);
        //     _cmd.CopyCounterValue(_culledPatchBuffer, _patchIndirectArgs, 4);
        //     if (_isBoundsBufferOn)
        //     {
        //         _cmd.CopyCounterValue(_patchBoundsBuffer, _patchBoundsIndirectArgs, 4);
        //     }
        //     // _cmd.Execute();
        //     context.ExecuteCommandBuffer(_cmd.Cmd);
        //    
        //    
        //     // _cmd.Name = _cmdOirginName;
        //     this.LogPatchArgs();
        // }
        
        public void Tick(ScriptableRenderContext context, CustomRenderPipelineCamera ppcamera)
        {
            var camera = ppcamera._camera;
            
            //clear
            _cmd.Clear();
            this.ClearBufferCounter();

            this.UpdateCameraFrustumPlanes(camera);

            if(_isNodeEvaluationCDirty){
                _isNodeEvaluationCDirty = false;
                _cmd.SetComputeVectorParam(_shader, ShaderParams._nodeEvaluationCId,_nodeEvaluationC);
            }

            _cmd.SetComputeVectorParam(_shader,ShaderParams._cameraPositionWSId,camera.transform.position);
            _cmd.SetComputeVectorParam(_shader,ShaderParams._worldSizeId, _asset.WorldSize);

            //四叉树分割计算得到初步的Patch列表
            _cmd.CopyCounterValue(_maxLODNodeList,_indirectArgsBuffer,0);
            ComputeBuffer consumeNodeList = _nodeListA;
            ComputeBuffer appendNodeList = _nodeListB;
            for(var lod = _asset._maxLOD; lod >=0; lod --){
                _cmd.SetComputeIntParam(_shader,ShaderParams._passLODId,lod);
                if(lod == _asset._maxLOD){
                    _cmd.SetComputeBufferParam(_shader,_kernelOfTraverseQuadTree,ShaderParams._consumeNodeListId,_maxLODNodeList);
                }else{
                    _cmd.SetComputeBufferParam(_shader,_kernelOfTraverseQuadTree,ShaderParams._consumeNodeListId,consumeNodeList);
                }
                _cmd.SetComputeBufferParam(_shader,_kernelOfTraverseQuadTree,ShaderParams._appendNodeListId,appendNodeList);
                _cmd.DispatchCompute(_shader,_kernelOfTraverseQuadTree,_indirectArgsBuffer,0);
                _cmd.CopyCounterValue(appendNodeList,_indirectArgsBuffer,0);
                var temp = consumeNodeList;
                consumeNodeList = appendNodeList;
                appendNodeList = temp;
            }
            //生成LodMap
            _cmd.DispatchCompute(_shader,_kernelOfBuildLodMap,20,20,1);


            //生成Patch
            _cmd.CopyCounterValue(_finalNodeListBuffer,_indirectArgsBuffer,0);
            _cmd.DispatchCompute(_shader,_kernelOfBuildPatches,_indirectArgsBuffer,0);
            _cmd.CopyCounterValue(_culledPatchBuffer,_patchIndirectArgs,4);
            if(_isBoundsBufferOn){
                _cmd.CopyCounterValue(_patchBoundsBuffer,_patchBoundsIndirectArgs,4);
            }
            Graphics.ExecuteCommandBuffer(_cmd.Cmd);
            // context.ExecuteCommandBuffer(_cmd.Cmd);

            // this.LogPatchArgs();
        }

        void SwapNodeList(ref ComputeBuffer a, ref ComputeBuffer b)
        {
            (a, b) = (b, a);
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
            
            _shader.SetVectorArray(ShaderParams._cameraFrustumPlanesId, _cameraFrustumPlanesToVector4);
        }

        public ComputeBuffer PatchBoundsBuffer
        {
            get
            {
                return _patchBoundsBuffer;
            }
        }

        public ComputeBuffer CulledPatchBuffer
        {
            get
            {
                return _culledPatchBuffer;
            }
        }

        public ComputeBuffer PatchIndirectArgs
        {
            get
            {
                return _patchIndirectArgs;
            }
        }

        public ComputeBuffer BoundsIndirectArgs
        {
            get
            {
                return _patchBoundsIndirectArgs;
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
                _shader.SetInt(ShaderParams._boundsHeightRedundanceId, value);
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
                _shader.SetFloat(ShaderParams._hizDepthBiasId, Mathf.Clamp(value, 0.01f, 1000f));
            }

            get
            {
                return _hizDepthBias;
            }
        }

        void LogPatchArgs()
        {
            var data = new uint[5];
            _patchIndirectArgs.GetData(data);
            Debug.Log(data[1]);
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
