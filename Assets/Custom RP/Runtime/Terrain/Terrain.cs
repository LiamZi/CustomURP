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

        void Start()
        {
            if (_traverse == null)
            {
                _traverse = new TerrainBuilder(_asset);
                _asset.BoundDebugMaterial.SetBuffer(ShaderParams._boundsList, _traverse.PatchBoundsBuffer);
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

        void OnDestroy()
        {
            if(_traverse != null) 
                _traverse.Dispose();
        }
    };
};
