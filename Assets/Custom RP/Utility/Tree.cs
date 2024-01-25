using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
        public TreeLeaf(Vector3 center, Vector2 uv)
        {
            
        }

        public override Vector3 _pos
        {
            get;
        }
        public override void GetData(List<VertexData> pos, Dictionary<byte, List<VertexData>> bd)
        {
            throw new System.NotImplementedException();
        }
        public override void AddBoundary(int sub, int x, int z, byte bk, VertexData data)
        {
            throw new System.NotImplementedException();
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
        
        public override void GetData(List<VertexData> pos, Dictionary<byte, List<VertexData>> bd)
        {
            throw new System.NotImplementedException();
        }
        
        public override void AddBoundary(int sub, int x, int z, byte bk, VertexData data)
        {
            throw new System.NotImplementedException();
        }

        public void CombineNode(float angleError)
        {
            
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
            
        }

        public void Tick(ITerrainTreeScanner scanner)
        {
            
        }

        public void AddBoundary(int subdivision, int x, int z, byte bk, VertexData vertex)
        {
            if (_node is TreeNode)
            {
                ((TreeNode)_node).AddBoundary(subdivision, x, z, bk, vertex);
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
            
        }
    }
}
