using UnityEditor;
using UnityEngine;

namespace CustomURP
{
    [CreateAssetMenu(menuName = "Custom URP/Terrain/TerrainAsset")]
    public class TerrainAsset : ScriptableObject
    {
        [SerializeField]
        public uint _maxNodeId = 34124;

        [SerializeField]
        public int _maxLOD = 5;

        [SerializeField]
        public int _maxLodNodeCount = 5;

        [SerializeField]
        Vector3 _worldSize = new Vector3(10240, 2048, 10240);

        [SerializeField]
        Texture2D _virtualMap = null;

        [SerializeField]
        Texture2D _heightMap = null;

        [SerializeField]
        Texture2D _normalMap = null;

        [SerializeField]
        Texture2D[] _minMaxHeightMaps;

        [SerializeField]
        Texture2D[] _quadTreeMaps;

        [SerializeField]
        ComputeShader _terrainShader;

        RenderTexture _quadTreeMap;
        RenderTexture _minMaxHeightMap;

        Material _boundsDebugMaterial;
        static Mesh _patchMesh;
        static Mesh _cubeMesh;

        public Vector3 WorldSize
        {
            get
            {
                return _worldSize;
            }
        }

        public Texture2D VirtualMap
        {
            get
            {
                return _virtualMap;
            }
        }

        public Texture2D HeightMap
        {
            get
            {
                return _heightMap;
            }
        }

        public Texture2D NormalMap
        {
            get
            {
                return _normalMap;
            }
        }

        public RenderTexture GetQuadTreeMap(ref Command cmd)
        {
            if (!_quadTreeMap)
            {
                _quadTreeMap = TextureUtility.CreateRenderTextureWithMipTextures(ref cmd, _quadTreeMaps, RenderTextureFormat.R16);
            }
            
            return _quadTreeMap;
        }


        public RenderTexture QuadTreeMap
        {
            get
            {
                if (!_quadTreeMap)
                {
                    _quadTreeMap = TextureUtility.CreateRenderTextureWithMipTextures(ref CustomRenderPipeline._cmd, _quadTreeMaps, RenderTextureFormat.R16);
                }
                return _quadTreeMap;
            }
        }

        public RenderTexture GetMinMaxHeightMap(ref Command cmd)
        {
            if (!_minMaxHeightMap)
            {
                _minMaxHeightMap = TextureUtility.CreateRenderTextureWithMipTextures(ref cmd, _minMaxHeightMaps, RenderTextureFormat.RG32);
            }

            return _minMaxHeightMap;
        }

        public RenderTexture MinMaxHeightMap
        {
            get
            {
                if (!_minMaxHeightMap)
                {
                    _minMaxHeightMap = TextureUtility.CreateRenderTextureWithMipTextures(ref CustomRenderPipeline._cmd, _minMaxHeightMaps, RenderTextureFormat.RG32);
                }
                return _minMaxHeightMap;
            }
        }

        public Material BoundDebugMaterial
        {
            get
            {
                if (!_boundsDebugMaterial)
                {
                    _boundsDebugMaterial = new Material(Shader.Find("Custom RP/Terrain/BoundsDebug"));
                }
                return _boundsDebugMaterial;
            }
        }

        public ComputeShader TerrainShader
        {
            get
            {
                return _terrainShader;
            }
        }

        public Mesh PatchMesh
        {
            get
            {
                if (!_patchMesh)
                {
                    _patchMesh = MeshUtility.CreatePlaneMesh(16);
                }
                return _patchMesh;
            }
        }

        public Mesh CubeMesh
        {
            get
            {
                if (!_cubeMesh)
                {
                    _cubeMesh = MeshUtility.CreateCube(1);
                }
                return _cubeMesh;
            }
        }

        public int MaxLodNodeCount
        {
            get
            {
                return _maxLodNodeCount;
            }
        }

        public int MaxLod
        {
            get
            {
                return _maxLOD;
            }
        }

        public uint MaxNodeId
        {
            get
            {
                return _maxNodeId;
            }
        }
    };
};
