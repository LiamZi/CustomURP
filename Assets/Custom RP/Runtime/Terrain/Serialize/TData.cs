using System;
using UnityEngine;

namespace CustomURP
{
    [Serializable]
    public class DetailLayerData
    {
        public GameObject _prototype;
        public float _minWidth;
        public float _maxWidth;
        public float _minHeight;
        public float _maxHeight;
        public float _noiseSpread;
        public Color _healthyColor;
        public Color _dryColor;
        public int _maxDensity;
        public bool _waterFloating;
    }
    
    public class TData : ScriptableObject
    {
        public Material[] _detailMats;
        public Material[] _bakeDiffuseMats;
        public Material[] _bakeNormalMats;
        public Material _bakedMat;
        public TextAsset _treeData;
        public int _meshDataPack;
        public string _meshPrefix;
        public TextAsset _heightMap;
        public Vector3 _heightmapScale;
        public int _heightmapResolution;
        public DetailLayerData[] _detailPrototypes;
        public int _detailWidth;
        public int _detailHeight;
        public int _detailResolutionPerPatch;
        public TextAsset _detailLayers; 
    }
}
