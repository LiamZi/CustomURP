using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using System.Threading;
using UnityEngine.Rendering;
using CustomURP;
using UnityEditor;

namespace Core
{
    public class QuadTree
    {
        public enum LocalPos
        {
            LeftDown,
            LeftUp,
            RightDown,
            RightUp
        };

        public QuadTree LeftDown { get; private set; }
        public QuadTree LeftUp { get; private set; }
        public QuadTree RightDown { get; private set; }
        public QuadTree RightUp { get; private set; }

        public int _lodLevel;
        public int2 _localPos;
        public int2 _renderingLocalPos;
        private double _distOffset;
    }

    public class QuadTreeBuildNode
    {
        public Bounds _bound;
        public int _meshId = -1;
        public int _lodLv = -1;
        public QuadTreeBuildNode[] _subNode;
        public Vector2 _uvMin;
        public Vector2 _uvMax;

        public QuadTreeBuildNode(int depth, Vector3 min, Vector3 max, Vector2 uvmin, Vector2 uvmax)
        {
            Vector3 center = 0.5f * (min + max);
            Vector3 size = max - min;
            Vector2 uvCenter = 0.5f * (uvmin + uvmax);
            Vector2 uvSize = uvmax - uvmin;
            _bound = new Bounds(center, size);
            _uvMin = uvmin;
            _uvMax = uvmax;

            if (depth > 0)
            {
                _subNode = new QuadTreeBuildNode[4];
                var subMin = new Vector3(center.x - 0.5f * size.x, min.y, center.z - 0.5f * size.z);
                var subMax = new Vector3(center.x, max.y, center.z);
                Vector2 uvSubMin = new Vector2(uvCenter.x - 0.5f * uvSize.x, uvCenter.y - 0.5f * uvSize.y);
                Vector2 uvSubMax = new Vector2(uvCenter.x, uvCenter.y);
                _subNode[0] = CreateSubNode(depth - 1, subMin, subMax, uvSubMin, uvSubMax);
                subMin = new Vector3(center.x, min.y, center.z - 0.5f * size.z);
                subMax = new Vector3(center.x + 0.5f * size.x, max.y, center.z);
                uvSubMin = new Vector2(uvCenter.x, uvCenter.y - 0.5f * uvSize.y);
                uvSubMax = new Vector2(uvCenter.x + 0.5f * uvSize.x, uvCenter.y);
                _subNode[1] = CreateSubNode(depth - 1, subMin, subMax, uvSubMin, uvSubMax);
                subMin = new Vector3(center.x - 0.5f * size.x, min.y, center.z);
                subMax = new Vector3(center.x, max.y, center.z + 0.5f * size.z);
                uvSubMin = new Vector2(uvCenter.x - 0.5f * uvSize.x, uvCenter.y);
                uvSubMax = new Vector2(uvCenter.x, uvCenter.y + 0.5f * uvSize.y);
                _subNode[2] = CreateSubNode(depth - 1, subMin, subMax, uvSubMin, uvSubMax);
                subMin = new Vector3(center.x, min.y, center.z);
                subMax = new Vector3(center.x + 0.5f * size.x, max.y, center.z + 0.5f * size.z);
                uvSubMin = new Vector2(uvCenter.x, uvCenter.y);
                uvSubMax = new Vector2(uvCenter.x + 0.5f * uvSize.x, uvCenter.y + 0.5f * uvSize.y);
                _subNode[3] = CreateSubNode(depth - 1, subMin, subMax, uvSubMin, uvSubMax);
                subMin = new Vector3(center.x, min.y, center.z - 0.5f * size.z);
                subMax = new Vector3(center.x + 0.5f * size.x, max.y, center.z);
                uvSubMin = new Vector2(uvCenter.x, uvCenter.y - 0.5f * uvSize.y);
                uvSubMax = new Vector2(uvCenter.x + 0.5f * uvSize.x, uvCenter.y);
                _subNode[1] = CreateSubNode(depth - 1, subMin, subMax, uvSubMin, uvSubMax);
                subMin = new Vector3(center.x - 0.5f * size.x, min.y, center.z);
                subMax = new Vector3(center.x, max.y, center.z + 0.5f * size.z);
                uvSubMin = new Vector2(uvCenter.x - 0.5f * uvSize.x, uvCenter.y);
                uvSubMax = new Vector2(uvCenter.x, uvCenter.y + 0.5f * uvSize.y);
                _subNode[2] = CreateSubNode(depth - 1, subMin, subMax, uvSubMin, uvSubMax);
                subMin = new Vector3(center.x, min.y, center.z);
                subMax = new Vector3(center.x + 0.5f * size.x, max.y, center.z + 0.5f * size.z);
                uvSubMin = new Vector2(uvCenter.x, uvCenter.y);
                uvSubMax = new Vector2(uvCenter.x + 0.5f * uvSize.x, uvCenter.y + 0.5f * uvSize.y);
                _subNode[3] = CreateSubNode(depth - 1, subMin, subMax, uvSubMin, uvSubMax);
            }
        }

        protected virtual QuadTreeBuildNode CreateSubNode(int depth, Vector3 min, Vector3 max, Vector2 uvmin, Vector2 uvmax)
        {
            return new QuadTreeBuildNode(depth, min, max, uvmin, uvmax);
        }

        public bool AddMesh(MeshData data)
        {
            if (_bound.Contains(data._bounds.center) && data._bounds.size.x > 0.5f * _bound.size.x)
            {
                _meshId = data._meshId;
                _lodLv = data._lodLevel;
                data._lods[0]._uvMin = _uvMin;
                data._lods[0]._uvMax = _uvMax;
                return true;
            }
            else if (_subNode != null)
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (_subNode[i].AddMesh(data))
                        return true;
                }
                
            }
            return false;
        }

        public bool GetBounds(int meshId, ref Bounds bound)
        {
            if (_subNode == null && _meshId == meshId)
            {
                bound = _bound;
                return true;
            }
            else if (_subNode != null)
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (_subNode[i].GetBounds(meshId, ref bound))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    };

    public class QuadTreeNode
    {
        public Bounds _bound;
        public int _cellIndex = -1;
        public int _meshIndex = -1;
        public byte _lodLv = 0;
        public int[] _children = new int[0];
        float _diameter = 0;

        public QuadTreeNode(int cid)
        {
            _cellIndex = cid;
            Init();
        }

        void Init()
        {
            var horizonSize = _bound.size;
            horizonSize.y = 0;
            _diameter = horizonSize.magnitude;
        }

        public float PixelSize(Vector3 viewCenter, float fov, float screenH)
        {
            float distance = Vector3.Distance(viewCenter, _bound.center);
            return (_diameter * Mathf.Rad2Deg * screenH) / (distance * fov);
        }

        public void Serialize(Stream stream)
        {
            FileUtility.WriteVector3(stream, _bound.center);
            FileUtility.WriteVector3(stream, _bound.size);
            FileUtility.WriteInt(stream, _meshIndex);
            FileUtility.WriteInt(stream, _cellIndex);
            FileUtility.WriteByte(stream, _lodLv);
            FileUtility.WriteInt(stream, _children.Length);

            foreach (var c in _children)
            {
                FileUtility.WriteInt(stream, c);
            }
        }

        public void Deserialize(Stream stream, Vector3 offset)
        {
            Vector3 center = FileUtility.ReadVector3(stream);
            Vector3 size = FileUtility.ReadVector3(stream);
            _meshIndex = FileUtility.ReadInt(stream);
            _cellIndex = FileUtility.ReadInt(stream);
            _lodLv = FileUtility.ReadByte(stream);
            int len = FileUtility.ReadInt(stream);
            _bound = new Bounds(center + offset, size);
            _children = new int[len];
            for (int i = 0; i < len; ++i)
            {
                _children[i] = FileUtility.ReadInt(stream);
            }
            Init();
        }
    };

    public class QuadTreeUtil
    {
        protected QuadTreeNode[] _treeNodes;
        protected Array<QuadTreeNode> _candidates;
        protected Array<QuadTreeNode> _activeMeshes;
        protected Array<QuadTreeNode> _visibleMeshes;
        
        public int NodeCount
        {
            get
            {
                return _treeNodes.Length;
            }
        }

        public Bounds Bound
        {
            get
            {
                return _treeNodes[0]._bound;
            }
        }

        public Array<QuadTreeNode> ActiveNodes
        {
            get
            {
                return _activeMeshes;
            }
        }
        
        public float MinCellSize { get; private set; }

        public QuadTreeUtil(byte[] data, Vector3 offset)
        {
            MemoryStream stream = new MemoryStream(data);
            int treeLen = FileUtility.ReadInt(stream);
            Init(treeLen, stream, offset);
            stream.Close();
        }

        public QuadTreeUtil(int treelen, Stream stream, Vector3 offset)
        {
            Init(treelen, stream, offset);
        }

        public void Init(int treeLen, Stream stream, Vector3 offset)
        {
            _treeNodes = new QuadTreeNode[treeLen];
            MinCellSize = float.MaxValue;
            for (int i = 0; i < treeLen; ++i)
            {
                var node = new QuadTreeNode(-1);
                node.Deserialize(stream, offset);
                _treeNodes[i] = node;
                var size = Mathf.Min(node._bound.size.x, node._bound.size.z);
                if (size < MinCellSize)
                {
                    MinCellSize = size;
                }
            }
            
            _candidates = new Array<QuadTreeNode>(_treeNodes.Length);
            _activeMeshes = new Array<QuadTreeNode>(_treeNodes.Length);
            _visibleMeshes = new Array<QuadTreeNode>(_treeNodes.Length);
        }

        public void ResetRuntimeCache()
        {
            _candidates.Reset();
            _activeMeshes.Reset();
            _visibleMeshes.Reset();
        }

        public void CullQuadTree(Vector3 viewCenter, float fov, float screenH,
            float screenW, Matrix4x4 world2Cam, Matrix4x4 projmatrix,
            Array<QuadTreeNode> activeCmd, Array<QuadTreeNode> deactiveCmd, LodPolicy lodPolicy)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(projmatrix * world2Cam);
            _visibleMeshes.Reset();
            _candidates.Reset();
            _candidates.Add(_treeNodes[0]);

            int loop = 0;
            int nextStartIndex = 0;
            for (; loop < _treeNodes.Length; ++loop)
            {
                int cIdx = nextStartIndex;
                nextStartIndex = _candidates.Length;
                for (; cIdx < nextStartIndex; ++cIdx)
                {
                    var node = _candidates._data[cIdx];
                    var stopChild = false;
                    if (node._meshIndex >= 0)
                    {
                        float pixelSize = node.PixelSize(viewCenter, fov, screenH);
                        int lodLv = lodPolicy.GetLodLevel(pixelSize, screenW);
                        if (node._lodLv <= lodLv)
                        {
                            _visibleMeshes.Add(node);
                            stopChild = true;
                        }
                    }

                    if (!stopChild && node._children.Length > 0)
                    {
                        foreach (var c in node._children)
                        {
                            var childNode = _treeNodes[c];
                            if (GeometryUtility.TestPlanesAABB(planes, childNode._bound))
                            {
                                _candidates.Add(childNode);
                            }
                        }
                    }
                }
                if(_candidates.Length == nextStartIndex)
                    break;
            }

            for (int i = 0; i < _visibleMeshes.Length; ++i)
            {
                var meshId = _visibleMeshes._data[i];
                if (!_activeMeshes.Contains(meshId))
                {
                    activeCmd.Add(meshId);
                }
            }

            for (int i = 0; i < _activeMeshes.Length; ++i)
            {
                var meshId = _activeMeshes._data[i];
                if (!_visibleMeshes.Contains(meshId))
                {
                    deactiveCmd.Add(meshId);
                }
            }
            var temp = _activeMeshes;
            _activeMeshes = _visibleMeshes;
            _visibleMeshes = temp;
        }
        
    };
};