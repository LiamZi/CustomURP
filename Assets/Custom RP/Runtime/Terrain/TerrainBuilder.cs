using UnityEngine;
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
            
        }
    }
}
