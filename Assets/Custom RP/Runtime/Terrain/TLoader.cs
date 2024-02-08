using System;
using System.Collections.Generic;
using System.IO;
using Core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


namespace CustomURP
{
    internal class RuntimeMeshPool
    {
        class MeshstreamCache
        {
            MemoryStream _memoryStream;
            int[] _offsets;
            int _usedCount = 0;
            
            
            public string Path { get; private set; }

            public bool Obselected
            {
                get
                {
                    return _offsets == null || _usedCount == _offsets.Length;
                }
            }

            public MeshstreamCache(string path, int pack, byte[] data)
            {
                this.Path = path;
                _memoryStream = new MemoryStream(data);
                _offsets = new int[pack];
                for (int i = 0; i < pack; ++i)
                {
                    _offsets[i] = FileUtility.ReadInt(_memoryStream);
                }
            }

            public RenderMesh GetMesh(int id)
            {
                int offsetStride = id % _offsets.Length;
                int offset = _offsets[offsetStride];
                var rm = new RenderMesh();
                _memoryStream.Position = offset;
                MeshUtils.Deserialize(_memoryStream, rm);
                ++_usedCount;
                return rm;
            }

            public void Clear()
            {
                _memoryStream.Close();
            }
        };

        TData _rawData;
        Dictionary<int, RenderMesh> _parsedMesh = new Dictionary<int, RenderMesh>();
        Dictionary<string, MeshstreamCache> _dataStreams = new Dictionary<string, MeshstreamCache>();
        IMeshDataLoader _loader;

        public RuntimeMeshPool(TData data, IMeshDataLoader loader)
        {
            _rawData = data;
            _loader = loader;
        }

        public RenderMesh PopMesh(int id)
        {
            if (!_parsedMesh.ContainsKey(id) && id >= 0)
            {
                int startMeshId = id / _rawData._meshDataPack * _rawData._meshDataPack;
                var path = string.Format("{0}_{1}", _rawData._meshPrefix, startMeshId);
                if (!_dataStreams.ContainsKey(path))
                {
                    var meshBytes = _loader.LoadMeshData(path);
                    var cache = new MeshstreamCache(path, _rawData._meshDataPack, meshBytes);
                    _dataStreams.Add(path, cache);
                }

                var streamCache = _dataStreams[path];
                var rm = streamCache.GetMesh(id);
                _parsedMesh.Add(id, rm);
                if (streamCache.Obselected)
                {
                    _dataStreams.Remove(streamCache.Path);
                    _loader.UnloadAsset(streamCache.Path);
                    streamCache.Clear();
                }
            }

            if (_parsedMesh.ContainsKey(id))
            {
                return _parsedMesh[id];
            }
            
            return null;
        }

        public void Clear()
        {
            foreach (var c in _dataStreams.Values)
            {
                _loader.UnloadAsset(c.Path);
                c.Clear();
            }
            
            _dataStreams.Clear();

            foreach (var m in _parsedMesh.Values)
            {
                m.Clear();
            }
            _parsedMesh.Clear();
        }
    };
    
    public class TLoader : MonoBehaviour
    {
        public TData _header;
        public LodPolicy _lodPolicy;
        public Camera _cullCamera;
        public GameObject _vtCreatorGO;
        public bool _receiveShadow = true;
        public float _detailDrawDistance = 80;
        public bool _showDebug;

        RuntimeMeshPool _meshPool;
        QuadTreeUtil _quadTree;
        HeightMap _heightMap;
        Array<QuadTreeNode> _activeCmd;
        Array<QuadTreeNode> _deactiveCmd;
        Dictionary<int, PooledRenderMesh> _activeMeshes = new Dictionary<int, PooledRenderMesh>();
        IVTCreator _vtCreator;
        DetailRenderer _detailRenderer;
        Matrix4x4 _projM;
        Matrix4x4 _detailProjM;
        Matrix4x4 _prevWorld2Camera;
        Plane[] _detailCullPlanes = new Plane[6];

        void ActiveMesh(QuadTreeNode node)
        {
            PooledRenderMesh patch = PooledRenderMesh.Pop();
            var m = _meshPool.PopMesh(node._meshIndex);
            patch.Reset(_header, _vtCreator, m, transform.position);
            _activeMeshes.Add(node._meshIndex, patch);
        }

        void DeactiveMesh(QuadTreeNode node)
        {
            var p = _activeMeshes[node._meshIndex];
            _activeMeshes.Remove(node._meshIndex);
            PooledRenderMesh.Push(p);
        }

        void Awake()
        {
            IMeshDataLoader loader = new MeshDataResLoader();
            _quadTree = new QuadTreeUtil(_header._treeData.bytes, transform.position);
            _heightMap = new HeightMap(_quadTree.Bound, _header._heightmapResolution, 
                                    _header._heightmapScale, _header._heightMap.bytes);
            _activeCmd = new Array<QuadTreeNode>(_quadTree.NodeCount);
            _deactiveCmd = new Array<QuadTreeNode>(_quadTree.NodeCount);
            _meshPool = new RuntimeMeshPool(_header, loader);
            _vtCreator = _vtCreatorGO.GetComponent<IVTCreator>();
            _detailRenderer = new DetailRenderer(_header, _quadTree.Bound, _receiveShadow);
            _prevWorld2Camera = Matrix4x4.identity;
            _projM = Matrix4x4.Perspective(_cullCamera.fieldOfView, _cullCamera.aspect, 
                                    _cullCamera.nearClipPlane, _cullCamera.farClipPlane);
            _detailProjM = Matrix4x4.Perspective(_cullCamera.fieldOfView, _cullCamera.aspect, 
                                _cullCamera.nearClipPlane, _detailDrawDistance);
        }

        public void Tick(CustomRenderPipelineCamera camera, ref Command cmd)
        {
            if (_quadTree == null || _cullCamera == null) return;

            Matrix4x4 world2Camera = _cullCamera.worldToCameraMatrix;
            if (_prevWorld2Camera != world2Camera)
            {
                _prevWorld2Camera = world2Camera;
                _activeCmd.Reset();
                _deactiveCmd.Reset();
                _quadTree.CullQuadTree(_cullCamera.transform.position, _cullCamera.fieldOfView, 
                                    Screen.height, Screen.width, 
                                    world2Camera, _projM,
                                    _activeCmd, _deactiveCmd, _lodPolicy);
                
                for (int i = 0; i < _activeCmd.Length; ++i)
                // for(int i = 0; i < 1; ++i)
                {
                    // ActiveMesh(_activeCmd._data[0]);
                    ActiveMesh(_activeCmd._data[i]);
                }
                //
                for (int i = 0; i < _deactiveCmd.Length; ++i)
                // for(int i = 0; i < 1; ++i)
                {
                    // DeactiveMesh(_deactiveCmd._data[0]);
                    DeactiveMesh(_deactiveCmd._data[i]);
                }
                
                if (_quadTree.ActiveNodes.Length > 0)
                {
                    for (int i = 0; i < _quadTree.ActiveNodes.Length; ++i)
                    // for(int i = 0; i < 1; ++i)
                    {
                        var node = _quadTree.ActiveNodes._data[0];
                        var p = _activeMeshes[node._meshIndex];
                        p.UpdatePatch(_cullCamera.transform.position, _cullCamera.fieldOfView, Screen.height, Screen.width);
                    }
                }

                GeometryUtility.CalculateFrustumPlanes(_detailProjM * world2Camera, _detailCullPlanes);
                _detailRenderer.Cull(_detailCullPlanes);
            }
            
            _detailRenderer.Tick(_cullCamera, ref cmd);
            cmd.Execute();
            
            // if (_quadTree.ActiveNodes.Length > 0)
            // {
            //     // for (int i = 0; i < _quadTree.ActiveNodes.Length; ++i)
            //     for(int i = 0; i < 1; ++i)
            //     {
            //         var node = _quadTree.ActiveNodes._data[0];
            //         var p = _activeMeshes[node._meshIndex];
            //         p.UpdatePatch(_cullCamera.transform.position, _cullCamera.fieldOfView, Screen.height, Screen.width);
            //     }
            // }
        }

        public void OnDestroy()
        {
            _detailRenderer.Clear();
            _meshPool.Clear();
            PooledRenderMesh.Clear();
            HeightMap.UnregisterMap(_heightMap);
        }

        void OnDrawGizmos()
        {
            if (!_showDebug)
                return;

            if (_detailRenderer != null)
                _detailRenderer.DrawDebug();
        }
    };
};
