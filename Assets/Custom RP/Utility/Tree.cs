using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using KdTree;
using KdTree.Math;

namespace CustomURP
{
    public class VertexData
    {
        public Vector3 _position;
        public Vector3 _normal;
        public Vector2 _uv;

        public void Merge(VertexData other)
        {
            _position = 0.5f * (_position + other._position);
            _normal = 0.5f * (_normal + other._normal);
            _uv = 0.5f * (_uv + other._uv);
        }
    };

    public abstract class TreeBase
    {
        protected VertexData _vertex;
        public Dictionary<byte, VertexData> _boundaries = new Dictionary<byte, VertexData>();
        public abstract Vector3 _pos { get; }
        
        public abstract void GetData(List<VertexData> pos, Dictionary<byte, List<VertexData>> bd);
        public abstract void AddBoundary(int sub, int x, int z, byte bk, VertexData data);
        
        public virtual void Tick(ITerrainTreeScanner scanner)
        {
            scanner.Run(_vertex._position, out _vertex._position, out _vertex._normal);
        }
    };

    public class TreeLeaf : TreeBase
    {

        public TreeLeaf(VertexData data)
        {
            _vertex = data;
        }
        
        public TreeLeaf(Vector3 center, Vector2 uv)
        {
            _vertex = new VertexData();
            _vertex._position = center;
            _vertex._uv = uv;
        }

        public Vector3 _normal
        {
            get
            {
                return _vertex != null ? _vertex._normal : Vector3.up;
            }
        }

        public Vector2 _uv
        {
            get
            {
                return _vertex != null ? _vertex._uv : Vector2.zero;
            }
        }

        public override Vector3 _pos
        {
            get
            {
                return _vertex != null ? _vertex._position : Vector3.zero;
            }
        }
        public override void GetData(List<VertexData> pos, Dictionary<byte, List<VertexData>> bd)
        {
            pos.Add(_vertex);
            foreach (var k in _boundaries.Keys)
            {
                if(!bd.ContainsKey(k))
                    bd.Add(k, new List<VertexData>());
                bd[k].Add(_boundaries[k]);
            }
        }
        public override void AddBoundary(int sub, int x, int z, byte bk, VertexData data)
        {
            _boundaries.Add(bk, data);
        }
    }

    public class TreeNode : TreeBase
    {
        public TreeBase[] _children = new TreeBase[4];
        public TreeNode(int sub, Vector3 center, Vector2 size, Vector2 uv, Vector2 uvstep)
        {
            _vertex = new VertexData();
            _vertex._position = center;
            _vertex._uv = uv;
            Vector2 subSize = 0.5f * size;
            Vector2 subUVStep = 0.5f * uvstep;
            if (sub > 1)
            {
                _children[0] = new TreeNode(sub - 1, 
                    new Vector3(center.x - 0.5f * subSize.x, center.y, center.z - 0.5f * subSize.y), 
                        subSize, new Vector2(uv.x - 0.5f * subUVStep.x, uv.y - 0.5f * subUVStep.y), subUVStep);
                _children[1] = new TreeNode(sub - 1,
                    new Vector3(center.x + 0.5f * subSize.x, center.y, center.z - 0.5f * subSize.y), subSize,
                    new Vector2(uv.x + 0.5f * subUVStep.x, uv.y - 0.5f * subUVStep.y), subUVStep);
                
                _children[2] = new TreeNode(sub - 1,
                    new Vector3(center.x - 0.5f * subSize.x, center.y, center.z + 0.5f * subSize.y), subSize,
                    new Vector2(uv.x - 0.5f * subUVStep.x, uv.y + 0.5f * subUVStep.y), subUVStep);
                
                _children[3] = new TreeNode(sub - 1,
                    new Vector3(center.x + 0.5f * subSize.x, center.y, center.z + 0.5f * subSize.y), subSize,
                    new Vector2(uv.x + 0.5f * subUVStep.x, uv.y + 0.5f * subUVStep.y), subUVStep);  
            }
            else
            {
                _children[0] = new TreeLeaf(new Vector3(center.x - 0.5f * subSize.x, center.y, center.z - 0.5f * subSize.y),
                    new Vector2(uv.x - 0.5f * subUVStep.x, uv.y - 0.5f * subUVStep.y));
                
                _children[1] = new TreeLeaf(new Vector3(center.x + 0.5f * subSize.x, center.y, center.z - 0.5f * subSize.y),
                    new Vector2(uv.x + 0.5f * subUVStep.x, uv.y - 0.5f * subUVStep.y));
                
                _children[2] = new TreeLeaf(new Vector3(center.x - 0.5f * subSize.x, center.y, center.z + 0.5f * subSize.y),
                    new Vector2(uv.x - 0.5f * subUVStep.x, uv.y + 0.5f * subUVStep.y));
                
                _children[3] = new TreeLeaf(new Vector3(center.x + 0.5f * subSize.x, center.y, center.z + 0.5f * subSize.y),
                    new Vector2(uv.x + 0.5f * subUVStep.x, uv.y + 0.5f * subUVStep.y));
            }
                
        }

        public override Vector3 _pos
        {
            get
            {
                return _vertex != null ? _vertex._position : Vector3.zero;
            }
        }

        public bool IsFullLeaf
        {
            get
            {
                for (int i = 0; i < _children.Length; ++i)
                {
                    if (_children[i] == null || !(_children[i] is TreeLeaf))
                        return false;
                }
                return true;
            }
        }
        
        public override void GetData(List<VertexData> pos, Dictionary<byte, List<VertexData>> bd)
        {
            for (int i = 0; i < 4; ++i)
            {
                _children[i].GetData(pos, bd);
            }

            foreach (var k in _boundaries.Keys)
            {
                if(!bd.ContainsKey(k))
                    bd.Add(k, new List<VertexData>());
                bd[k].Add(_boundaries[k]);
            }
        }
        
        public override void AddBoundary(int sub, int x, int z, byte bk, VertexData data)
        {
            int u = x >> sub;
            int v = z >> sub;
            int subx = x - u * (1 << sub);
            int subz = z - v * (1 << sub);
            --sub;
            int index = (subz >> sub) * 2 + (subx >> sub);
            _children[index].AddBoundary(sub, subx, subz, bk, data);
        }

        public void CombineNode(float angleError)
        {
            for (int i = 0; i < 4; ++i)
            {
                if (_children[i] is TreeNode)
                {
                    TreeNode subNode = (TreeNode)_children[i];
                    subNode.CombineNode(angleError);
                    if (subNode.IsFullLeaf)
                    {
                        TreeLeaf replaceLeaf = subNode.Combine(angleError);
                        if (replaceLeaf != null)
                            _children[i] = replaceLeaf;
                    }
                }
            }
        }

        public TreeLeaf Combine(float angleErr)
        {
            for (int i = 0; i < _children.Length; ++i)
            {
                if (_children[i] == null || !(_children[i] is TreeLeaf))
                    return null;
            }
            
            for (int i = 0; i < _children.Length; ++i)
            {
                TreeLeaf l = (TreeLeaf)_children[i];
                float dot = Vector3.Dot(l._normal.normalized, _vertex._normal.normalized);
                
                if (Mathf.Rad2Deg * Mathf.Acos(dot) >= angleErr)
                    return null;
            }
            TreeLeaf leaf = new TreeLeaf(_vertex);
            for (int i = 0; i < _children.Length; ++i)
            {
                TreeLeaf l = (TreeLeaf)_children[i];
                foreach (var k in l._boundaries.Keys)
                {
                    if (_boundaries.ContainsKey(k))
                        _boundaries[k].Merge(l._boundaries[k]);
                    else
                        _boundaries.Add(k, l._boundaries[k]);
                }
            }
            leaf._boundaries = _boundaries;
            return leaf;
        }

        public override void Tick(ITerrainTreeScanner scanner)
        {
            base.Tick(scanner);
            for (int i = 0; i < 4; ++i)
            {
                _children[i].Tick(scanner);
            }
        }
    };
    
    public class Tree
    {
        public const byte LBCorner = 0;
        public const byte LTCorner = 1;
        public const byte RTCorner = 2;
        public const byte RBCorner = 3;
        public const byte BBorder = 4;
        public const byte TBorder = 5;
        public const byte LBorder = 6;
        public const byte RBorder = 7;
        public Vector2 _uvMin = Vector2.zero;
        public Vector2 _uvMax = Vector2.one;
        
        public Dictionary<byte, List<VertexData>> _boundaries = new Dictionary<byte, List<VertexData>>();
        public Dictionary<byte, KdTree<float, int>> _boundaryKDTree = new Dictionary<byte, KdTree<float, int>>();
        public List<VertexData> _vertices = new List<VertexData>();
        public HashSet<byte> _stitchedBorders = new HashSet<byte>();
        public Vector3 _center { get { return _node._pos; } }
        public Bounds _bounds { get;  set; }
        
        TreeBase _node;
        

        public Tree(int sub, Vector3 center, Vector2 size, Vector2 uv, Vector2 uvstep)
        {
            _node = new TreeNode(sub, center, size, uv, uvstep);
            _uvMin = uv - 0.5f * uvstep;
            _uvMax = uv + 0.5f * uvstep;
        }

        public void InitBoundary()
        {
            for (byte flag = LBCorner; flag <= RBorder; ++flag)
            {
                _boundaries.Add(flag, new List<VertexData>());
                var tree = new KdTree<float, int>(2, new KdTree.Math.FloatMath());
                _boundaryKDTree.Add(flag, tree);
            }
        }

        public void Tick(ITerrainTreeScanner scanner)
        {
            _node.Tick(scanner);
        }

        public void AddBoundary(int subdivision, int x, int z, byte bk, VertexData vertex)
        {
            if (_node is TreeNode)
            {
                ((TreeNode)_node).AddBoundary(subdivision, x, z, bk, vertex);
            }
        }

        public void MergeBoundary(byte flag, float minDis, List<VertexData> src)
        {
            if (_boundaries.ContainsKey(flag) || !_boundaryKDTree.ContainsKey(flag))
            {
                Debug.LogError("The boundary need to merge not exists.");
            }

            var tree = _boundaryKDTree[flag];
            foreach (var vt in src)
            {
                var nodes = tree.GetNearestNeighbours(new float[]
                {
                    vt._position.x, vt._position.z
                }, 1);

                if (nodes != null && nodes.Length > 0)
                {
                    var dis = Vector2.Distance(new Vector2(vt._position.x, vt._position.z), new Vector2(nodes[0]._point[0], nodes[0]._point[1]));
                    if(dis <= minDis)
                        continue;
                }

                tree.Add(new float[]
                {
                    vt._position.x, vt._position.z
                }, 0);
                _boundaries[flag].Add(vt);
            }
        }

        public void FillData(float angleError)
        {
            if (angleError > 0)
            {
                CombineTree(angleError);
            }
            
            _node.GetData(_vertices, _boundaries);
        }

        void CombineTree(float angleError)
        {
            if (_node is TreeNode)
            {
                ((TreeNode)_node).CombineNode(angleError);
            }
        }

        public void StitchBorder(byte flag, byte nflag, float minDis, Tree neighbour)
        {
            if (neighbour == null)
                return;

            if (flag <= RBCorner || nflag <= RBCorner)
                return;

            if (!_boundaries.ContainsKey(flag))
            {
                Debug.LogError("Tree boundary doesn't contains corner : " + flag);
                return;
            }

            if (!neighbour._boundaries.ContainsKey(nflag))
            {
                Debug.LogError("Tree neighbour boundary doesn't contains corner : " + nflag);
                return;
            }
            
            if (_stitchedBorders.Contains(flag) && neighbour._stitchedBorders.Contains(nflag))
                return;
            if (_boundaries[flag].Count > neighbour._boundaries[nflag].Count)
            {
                neighbour._boundaries[nflag].Clear();
                neighbour._boundaries[nflag].AddRange(_boundaries[flag]);
            }
            else
            {
                _boundaries[flag].Clear();
                _boundaries[flag].AddRange(neighbour._boundaries[nflag]);
            }
            //
            _stitchedBorders.Add(flag);
            neighbour._stitchedBorders.Add(nflag);
        }
    }
}
