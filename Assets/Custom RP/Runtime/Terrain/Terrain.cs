﻿using System;
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
        Command _cmd;

        void Start()
        {
            if (_traverse == null)
            {
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

        void Update()
        {
            // if(Input.GetKeyDown(KeyCode.Space))
            if(_traverse == null) return;
            // if(_traverse != null)
            _traverse.Tick();

            var material = EnsureMaterial();
            if (_isTerrainMaterialDirty)
            {
                UpdateTerrainMaterialProeprties();
            }

            // var patchMesh = _asset.PatchMesh;
            // var bounds = new Bounds(Vector3.zero, Vector3.one * 10240);
            _cmd.Cmd.DrawMeshInstancedIndirect(_asset.PatchMesh, 0, _terrainMaterial, 0, _traverse.PatchBoundsBuffer);
            if (_patchBoundsDebug)
            {
                _cmd.Cmd.DrawMeshInstancedIndirect(_asset.CubeMesh, 0, _asset.BoundDebugMaterial, 0, _traverse.BoundsIndirectArgs);
            }
        }

        Material EnsureMaterial()
        {
            if (_terrainMaterial)  return _terrainMaterial;
            
            _terrainMaterial = new Material(Shader.Find("Custom RP/GPUTerrain"));
            _terrainMaterial.SetTexture(ShaderParams._heightTexId, _asset.HeightMap);
            _terrainMaterial.SetTexture(ShaderParams._normalTexId, _asset.NormalMap);
            _terrainMaterial.SetTexture(ShaderParams._MainTex, _asset.NormalMap);
            _terrainMaterial.SetBuffer(ShaderParams._patchListId, _traverse.CulledPatchBuffer);

            UpdateTerrainMaterialProeprties();
            
            return _terrainMaterial;
        }

        void UpdateTerrainMaterialProeprties()
        {
            _isTerrainMaterialDirty = false;
            if (!_terrainMaterial) return;
            
            if(_seamLess)
            {
                _terrainMaterial.EnableKeyword("ENABLE_LOD_SEAMLESS");
            }
            else
            {
                _terrainMaterial.DisableKeyword("ENABLE_LOD_SEAMLESS");
            }
            
            if(_mipDebug)
            {
                _terrainMaterial.EnableKeyword("ENABLE_MIP_DEBUG");
            }
            else
            {
                _terrainMaterial.DisableKeyword("ENABLE_MIP_DEBUG");
            }
            
            if(_patchDebug)
            {
                _terrainMaterial.EnableKeyword("ENABLE_PATCH_DEBUG");
            }
            else
            {
                _terrainMaterial.DisableKeyword("ENABLE_PATCH_DEBUG");
            }
            
            if(_nodeDebug)
            {
                _terrainMaterial.EnableKeyword("ENABLE_NODE_DEBUG");
            }
            else
            {
                _terrainMaterial.DisableKeyword("ENABLE_NODE_DEBUG");
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
