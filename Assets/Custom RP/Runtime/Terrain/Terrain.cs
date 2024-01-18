using System;
using UnityEngine;


namespace CustomURP
{
    public class Terrain : MonoBehaviour
    {
        public TerrainAsset _asset;
        public bool _isFrustumCullEnabled = true;
        public bool _isHizOcclusionCullingEnabled = true;

        [Range(0.01f, 1000f)]
        public float _hizDepthBias = 1;

        [Range(0, 100)]
        public int _boundsHeightRedundance = 5;

        [Range(0.1f, 1.9f)]
        public float _distanceEvaluation = 1.2f;

        public bool _seamLess = true;
        public bool _patchDebug = false;
        public bool _nodeDebug = false;
        public bool _mipDebug = false;
        public bool _patchBoundsDebug = false;

        TerrainBuilder _traverse;
        Material _terrainMaterial;
        bool _isTerrainMaterialDirty = false;
        Command _cmd = null;

        void Start()
        {
            if (_traverse == null)
            {
                if(_cmd == null) _cmd = new Command("Terrain");
                _traverse = new TerrainBuilder(_asset);
                _asset.BoundDebugMaterial.SetBuffer(ShaderParams._boundsListId, _traverse.PatchBoundsBuffer);
                this.Apply();
            }
            
        }

        void Apply()
        {
            if (_traverse == null) return;

            _traverse.IsFrustumCullEnabled = _isFrustumCullEnabled;
            _traverse.IsBoundsBufferOn = _patchBoundsDebug;
            _traverse.IsHizOcclusionCullingEnabled = _isHizOcclusionCullingEnabled;
            _traverse.BoundsHeightRedundance = _boundsHeightRedundance;
            _traverse.EnableSeamDebug = _patchDebug;
            _traverse.NodeEvalDistance = _distanceEvaluation;
            _traverse.HizDepthBias = _hizDepthBias;

            _isTerrainMaterialDirty = true;
        }

        // void Update()
        public void Tick(ref Command cmd, CustomRenderPipelineCamera camera)
        {
            // if(Input.GetKeyDown(KeyCode.Space))
            if(_traverse == null) return;
            // if(_traverse != null)
            _traverse.Tick(cmd.Context, camera);
            
            var material = EnsureMaterial();
            if (_isTerrainMaterialDirty)
            {
                UpdateTerrainMaterialProeprties();
            }
            
            // var patchMesh = _asset.PatchMesh;
            cmd.Cmd.DrawMeshInstancedIndirect(_asset.PatchMesh, 0, material, 0, _traverse.PatchIndirectArgs);
            if (_patchBoundsDebug)
            {
                cmd.Cmd.DrawMeshInstancedIndirect(_asset.CubeMesh, 0, _asset.BoundDebugMaterial, 0, _traverse.BoundsIndirectArgs);
            }
            
            // _cmd.Execute();
            cmd.Context.ExecuteCommandBuffer(cmd.Cmd);
            cmd.Cmd.Clear();
        }

        Material EnsureMaterial()
        {
            // if (_terrainMaterial)  return _terrainMaterial;
            //
            // _terrainMaterial = new Material(Shader.Find("Custom RP/GPUTerrain"));
            // _terrainMaterial.SetTexture(ShaderParams._heightTexId, _asset.HeightMap);
            // _terrainMaterial.SetTexture(ShaderParams._normalTexId, _asset.NormalMap);
            // _terrainMaterial.SetTexture(ShaderParams._MainTex, _asset.NormalMap);
            // _terrainMaterial.SetBuffer(ShaderParams._patchListId, _traverse.CulledPatchBuffer);
            //
            // UpdateTerrainMaterialProeprties();
            //
            // return _terrainMaterial;
            
            if(!_terrainMaterial){
                var material = new Material(Shader.Find("Custom RP/GPUTerrain"));
                material.SetTexture(ShaderParams._heightTexId,_asset.HeightMap);
                material.SetTexture(ShaderParams._normalTexId,_asset.NormalMap);
                material.SetTexture(ShaderParams._MainTex,_asset.VirtualMap);
                material.SetBuffer(ShaderParams._patchListId,_traverse.CulledPatchBuffer);
                _terrainMaterial = material;
                this.UpdateTerrainMaterialProeprties();
            }
            return _terrainMaterial;
        }

        void UpdateTerrainMaterialProeprties()
        {
            _isTerrainMaterialDirty = false;
            if (!_terrainMaterial) return;
            
            if(_seamLess)
            {
                _terrainMaterial.EnableKeyword("_ENABLE_LOD_SEAMLESS");
            }
            else
            {
                _terrainMaterial.DisableKeyword("_ENABLE_LOD_SEAMLESS");
            }
            
            if(_mipDebug)
            {
                _terrainMaterial.EnableKeyword("_ENABLE_MIP_DEBUG");
            }
            else
            {
                _terrainMaterial.DisableKeyword("_ENABLE_MIP_DEBUG");
            }
            
            if(_patchDebug)
            {
                _terrainMaterial.EnableKeyword("_ENABLE_PATCH_DEBUG");
            }
            else
            {
                _terrainMaterial.DisableKeyword("_ENABLE_PATCH_DEBUG");
            }
            
            if(_nodeDebug)
            {
                _terrainMaterial.EnableKeyword("_ENABLE_NODE_DEBUG");
            }
            else
            {
                _terrainMaterial.DisableKeyword("_ENABLE_NODE_DEBUG");
            }
            
            _terrainMaterial.SetVector(ShaderParams._worldSizeId, _asset.WorldSize);
            _terrainMaterial.SetMatrix(ShaderParams._worldToNormalMapMatrixId, Matrix4x4.Scale(_asset.WorldSize).inverse);
        }

        void OnDestroy()
        {
            if(_traverse != null) 
                _traverse.Dispose();
        }
    };
};
