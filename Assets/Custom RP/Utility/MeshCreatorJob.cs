using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CustomURP
{
    public class LodDetail
    {
        public int _subdivision = 3;
        public float _slopeAngleError = 5f;
    }

    public interface ITerrainTreeScanner
    {
        void Run(Vector3 center, out Vector3 hitpos, out Vector3 hitnormal);
    }

    public class TerrainScanner : ITerrainTreeScanner
    {
        public Tree[] _trees { get; private set; }

        int _currXIndex = 0;
        int _currZIndex = 0;
        bool _stitchBorder = true;
        UnityEngine.Terrain _terrain;
        Bounds _bounds;
        Vector3 _checkStart;
        public int _detailedSize = 1;

        public int _maxX { get; private set; }
        public int _maxZ { get; private set; }
        public int _subdivision { get; private set; }
        public float _slopAngleError { get; private set; }
        public Vector2 _gridSize { get; private set; }

        public TerrainScanner(UnityEngine.Terrain terrain, Bounds bounds, int sub, float angleErr, int maxX, int maxZ, bool sbrd)
        {
            _terrain = terrain;
            _bounds = bounds;
            _maxX = maxX;
            _maxZ = maxZ;
            _subdivision = Mathf.Max(1, sub);
            _slopAngleError = angleErr;
            _stitchBorder = sbrd;
            _gridSize = new Vector2(bounds.size.x / maxX, bounds.size.z / maxZ);

            _checkStart = new Vector3(bounds.center.x - bounds.size.x / 2, 
                                    bounds.center.y + bounds.size.y / 2, 
                                    bounds.center.z - bounds.size.z / 2);

            _detailedSize = 1 << _subdivision;

            _trees = new Tree[maxX * maxZ];
        }

        public Tree GetSubTree(int x, int z)
        {
            if (x < 0 || x >= _maxX || z < 0 || z >= _maxX)
                return null;
            return _trees[x * _maxZ + z];
        }

        public void Tick()
        {
            if (Done)
                return;

            float fx = (_currXIndex + 0.5f) * _gridSize[0];
            float fz = (_currZIndex + 0.5f) * _gridSize[1];

            Vector3 center = _checkStart + fx * Vector3.right + fz * Vector3.forward;
            Vector2 uv = new Vector2((_currXIndex + 0.5f) / _maxX, (_currZIndex + 0.5f) / _maxZ);
            Vector2 uvstep = new Vector2(1f / _maxX, 1f / _maxZ);
            if (_trees[_currXIndex * _maxZ + _currZIndex] == null)
            {
                var t = new Tree(_subdivision, center, _gridSize, uv, uvstep);
                t._bounds = new Bounds(new Vector3(center.x, center.y, center.z), new Vector3(_gridSize.x, _bounds.size.y / 2, _gridSize.y));
                _trees[_currXIndex * _maxZ + _currZIndex] = t;
            }

            ScanTree(_trees[_currXIndex * _maxZ + _currZIndex]);
            ++_currXIndex;

            if (_currXIndex >= _maxX)
            {
                if (_currZIndex < _maxZ - 1)
                    _currXIndex = 0;
                ++_currZIndex;
            }
        }

        Vector3 AverageNormal(List<VertexData> data)
        {
            Vector3 normal = Vector3.up;
            for (int i = 0; i < data.Count; ++i)
            {
                normal += data[i]._normal;
            }

            return normal.normalized;
        }

        void MergeCorners(List<VertexData> l0, List<VertexData> l1, List<VertexData> l2, List<VertexData> l3)
        {
            List<VertexData> lvers = new List<VertexData>();
            lvers.Add(l0[0]);
            
            if (l1 != null)
                lvers.Add(l1[0]);
            if (l2 != null)
                lvers.Add(l2[0]);
            if (l3 != null)
                lvers.Add(l3[0]);
            
            Vector3 normal = AverageNormal(lvers);
            
            l0[0]._normal = normal;
            if (l1 != null)
                l1[0]._normal = normal;
            if (l2 != null)
                l2[0]._normal = normal;
            if (l3 != null)
                l3[0]._normal = normal;
        }

        void StitchCorner(int x, int z)
        {
            Tree center = GetSubTree(x, z);
            if (!center._boundaries.ContainsKey(Tree.LBCorner))
            {
                Debug.LogError("Boundary data missing.");
                return;
            }

            Tree right = GetSubTree(x + 1, z);
            Tree left = GetSubTree(x - 1, z);
            Tree rightTop = GetSubTree(x + 1, z + 1);
            Tree top = GetSubTree(x, z + 1);
            Tree leftTop = GetSubTree(x - 1, z + 1);
            Tree leftDown = GetSubTree(x - 1, z - 1);
            Tree down = GetSubTree(x, z - 1);
            Tree rightDown = GetSubTree(x + 1, z - 1);

            if (!center._stitchedBorders.Contains(Tree.LBCorner))
            {
                MergeCorners(center._boundaries[Tree.LBCorner],
                    left != null ? left._boundaries[Tree.RBCorner] : null,
                    leftDown != null ? leftDown._boundaries[Tree.RTCorner] : null,
                    down != null ? down._boundaries[Tree.LTCorner] : null);
                
                center._stitchedBorders.Add(Tree.LBCorner);
                
                if (left != null) left._stitchedBorders.Add(Tree.RBCorner);
                if (leftDown != null) leftDown._stitchedBorders.Add(Tree.RTCorner);
                if (down != null) left._stitchedBorders.Add(Tree.LTCorner);
            }
            
            if (!center._stitchedBorders.Contains(Tree.RBCorner))
            {
                MergeCorners(center._boundaries[Tree.RBCorner],
                    right != null ? right._boundaries[Tree.LBCorner] : null,
                    rightDown != null ? rightDown._boundaries[Tree.LTCorner] : null,
                    down != null ? down._boundaries[Tree.RTCorner] : null);
                
                center._stitchedBorders.Add(Tree.RBCorner);
                if (right != null) right._stitchedBorders.Add(Tree.LBCorner);
                if (rightDown != null) rightDown._stitchedBorders.Add(Tree.LTCorner);
                if (down != null) down._stitchedBorders.Add(Tree.RTCorner);
            }
            
            if (!center._stitchedBorders.Contains(Tree.LTCorner))
            {
                MergeCorners(center._boundaries[Tree.LTCorner],
                    left != null ? left._boundaries[Tree.RTCorner] : null,
                    leftTop != null ? leftTop._boundaries[Tree.RBCorner] : null,
                    top != null ? top._boundaries[Tree.LBCorner] : null);
                
                center._stitchedBorders.Add(Tree.LTCorner);
                
                if (left != null) left._stitchedBorders.Add(Tree.RTCorner);
                if (leftTop != null) leftTop._stitchedBorders.Add(Tree.RBCorner);
                if (top != null) top._stitchedBorders.Add(Tree.LBCorner);
            }
            
            if (!center._stitchedBorders.Contains(Tree.RTCorner))
            {
                MergeCorners(center._boundaries[Tree.RTCorner],
                    right != null ? right._boundaries[Tree.LTCorner] : null,
                    rightTop != null ? rightTop._boundaries[Tree.LBCorner] : null,
                    top != null ? top._boundaries[Tree.RBCorner] : null);
                
                center._stitchedBorders.Add(Tree.RTCorner);
                
                if (right != null) right._stitchedBorders.Add(Tree.LTCorner);
                
                if (rightTop != null) rightTop._stitchedBorders.Add(Tree.LBCorner);
                if (top != null) top._stitchedBorders.Add(Tree.RBCorner);
            }
        }
        

        public void FillData()
        {
            for (int i = 0; i < _trees.Length; ++i)
            {
                _trees[i].FillData(_slopAngleError);
            }

            float minDis = Mathf.Min(_gridSize.x, _gridSize.y) / _detailedSize / 2f;
            for (int x = 0; x < _maxX; ++x)
            {
                for (int z = 0; z < _maxZ; ++z)
                {
                    Tree center = GetSubTree(x, z);
                    StitchCorner(x, z);
                    center.StitchBorder(Tree.BBorder, Tree.TBorder, minDis, GetSubTree(x, z - 1));
                    center.StitchBorder(Tree.LBorder, Tree.RBorder, minDis, GetSubTree(x - 1, z));
                    center.StitchBorder(Tree.RBorder, Tree.LBorder, minDis, GetSubTree(x + 1, z));
                    center.StitchBorder(Tree.TBorder, Tree.BBorder, minDis, GetSubTree(x, z + 1));
                }
            }

            for (int i = 0; i < _trees.Length; ++i)
            {
                foreach (var node in _trees[i]._boundaries.Values)
                {
                    _trees[i]._vertices.AddRange(node);
                }
            }
        }

        void ScanTree(Tree sampler)
        {
            sampler.Tick(this);
            
            if (!_stitchBorder)
                return;

            int detailedX = _currXIndex * _detailedSize;
            int detailedZ = _currZIndex * _detailedSize;

            float bfx = _currXIndex * _gridSize[0];
            float bfz = _currZIndex * _gridSize[1];
            
            float borderOffset = 0;
            if(_currXIndex == 0 || _currZIndex == 0 || _currXIndex == _maxX - 1 || _currZIndex == _maxZ - 1)
                borderOffset = 0.000001f;

            RayCastBoundary(bfx + borderOffset, bfz + borderOffset, detailedX, detailedZ, Tree.LBCorner, sampler);
            RayCastBoundary(bfx + borderOffset, bfz + _gridSize[1] - borderOffset, detailedX, detailedZ + _detailedSize - 1, Tree.LTCorner, sampler);
            RayCastBoundary(bfx + _gridSize[0] - borderOffset, bfz + _gridSize[1] - borderOffset, detailedX + _detailedSize - 1, detailedZ + _detailedSize - 1, Tree.RTCorner, sampler);
            RayCastBoundary(bfx + _gridSize[0] - borderOffset, bfz + borderOffset,detailedX + _detailedSize - 1, detailedZ, Tree.RBCorner, sampler);

            for (int u = 1; u < _detailedSize; ++u)
            {
                float fx = (_currXIndex + (float)u / _detailedSize) * _gridSize[0];
                RayCastBoundary(fx, bfz + borderOffset, u + detailedX, detailedZ, Tree.BBorder, sampler);
                RayCastBoundary(fx, bfz + _gridSize[1] - borderOffset, u + detailedX, detailedZ + _detailedSize - 1, Tree.TBorder, sampler);
            }

            for (int v = 1; v < _detailedSize; ++v)
            {
                float fz = (_currZIndex + (float)v / _detailedSize) * _gridSize[1];
                RayCastBoundary(bfx + borderOffset, fz, detailedX, v + detailedZ, Tree.LBorder, sampler);
                RayCastBoundary(bfx + _gridSize[0] - borderOffset, fz,detailedX + _detailedSize - 1, v + detailedZ, Tree.RBorder, sampler);
            }
        }

        void RayCastBoundary(float fx, float fz, int x, int z, byte bk, Tree sampler)
        {
            Vector3 hitpos = _checkStart + fx * Vector3.right + fz * Vector3.forward;
            hitpos.x = Mathf.Clamp(hitpos.x, _bounds.min.x, _bounds.max.x);
            hitpos.z = Mathf.Clamp(hitpos.z, _bounds.min.z, _bounds.max.z);

            float localX = (hitpos.x - _bounds.min.x) / _bounds.size.x;
            float localY = (hitpos.z - _bounds.min.z) / _bounds.size.z;

            hitpos.y = _terrain.SampleHeight(hitpos) + _terrain.gameObject.transform.position.y;
            var hitNormal = _terrain.terrainData.GetInterpolatedNormal(localX, localY);

            VertexData vertex = new VertexData();
            vertex._position = hitpos;
            vertex._normal = hitNormal;
            vertex._uv = new Vector2(fx / _maxX / _gridSize[0], fz / _maxZ / _gridSize[1]);
            sampler.AddBoundary(_subdivision, x, z, bk, vertex);

        }

        public bool Done
        {
            get
            {
                return _currXIndex >= _maxX && _currZIndex >= _maxZ;
            }
        }
        public float Progress
        {
            get
            {
                return (float)(_currXIndex + _currZIndex * _maxX) / (float)(_maxX * _maxZ);
            }
        }
        public void Run(Vector3 center, out Vector3 hitpos, out Vector3 hitnormal)
        {
            hitpos = center;
            float fx = (center.x - _bounds.min.x) / _bounds.size.x;
            float fy = (center.z - _bounds.min.z) / _bounds.size.z;
            hitpos.y = _terrain.SampleHeight(center) + _terrain.gameObject.transform.position.y;
            hitnormal = _terrain.terrainData.GetInterpolatedNormal(fx, fy);
        }
        
    }

    public class MeshCreatorJob
    {
        public TerrainScanner[] _lods;
        int _currLodIndex = 0;


        public MeshCreatorJob(UnityEngine.Terrain terrain, Bounds bounds, int maxX, int maxZ, LodDetail[] settings)
        {
            _lods = new TerrainScanner[settings.Length];
            for (int i = 0; i < settings.Length; ++i)
            {
                LodDetail d = settings[i];
                _lods[i] = new TerrainScanner(terrain, bounds, d._subdivision, d._slopeAngleError, maxX, maxZ, i == 0);
            }
        }

        public void Tick()
        {
            if (_lods == null || Done)
                return;

            _lods[_currLodIndex].Tick();
            if (_lods[_currLodIndex].Done)
                ++_currLodIndex;

        }

        public float Progress
        {
            get
            {
                if (_currLodIndex < _lods.Length)
                {
                    return (_currLodIndex + _lods[_currLodIndex].Progress) / _lods.Length;
                }

                return 1.0f;
            }
        }

        public bool Done
        {
            get
            {
                return _currLodIndex >= _lods.Length;
            }
        }

        public void End()
        {
            TerrainScanner detail = _lods[0];
            detail.FillData();

            for (int i = 1; i < _lods.Length; ++i)
            {
                TerrainScanner scanner = _lods[i];
                for (int j = 0; j < detail._trees.Length; ++j)
                {
                    Tree dt = detail._trees[j];
                    Tree lt = scanner._trees[j];

                    foreach (var b in dt._boundaries)
                    {
                        lt._boundaries.Add(b.Key, b.Value);
                    }
                }
                scanner.FillData();
            }
        }
    };

    public class DataCreateJob
    {
        public TerrainScanner[] _lods;
        float _minEdgeLen;
        int _currLodIndex = 0;

        public DataCreateJob(UnityEngine.Terrain terrain, Bounds volume, int depth, LodDetail[] settings, float minEdge)
        {
            _lods = new TerrainScanner[settings.Length];
            _minEdgeLen = minEdge;
            int depthStride = Mathf.Max(1, depth / settings.Length);
            for (int i = 0; i < settings.Length; ++i)
            {
                var subdiv = settings[i]._subdivision;
                var angleError = settings[i]._slopeAngleError;
                var subDepth = Mathf.Max(1, depth - i * depthStride);
                int girdCount = 1 << subDepth;

                _lods[i] = new TerrainScanner(terrain, volume, subdiv, angleError, girdCount, girdCount, i == 0);
            }
        }

        public void Tick()
        {
            if (_lods == null || Done)
                return;
            
            _lods[_currLodIndex].Tick();
            if (_lods[_currLodIndex].Done)
                ++_currLodIndex;
        }

        public void End()
        {
            _lods[0].FillData();
            for (int i = 1; i < _lods.Length; ++i)
            {
                TerrainScanner detail = _lods[i - 1];
                TerrainScanner scaner = _lods[i];
                foreach (var tree in scaner._trees)
                {
                    tree.InitBoundary();

                    foreach (var dt in detail._trees)
                    {
                        if (tree._bounds.Contains(dt._bounds.center))
                        {
                            AddBoundaryFromDetail(tree, dt, _minEdgeLen / 2f);
                        }
                    }
                }
                
                scaner.FillData();
                
            }
        }

        byte GetBorderType(Bounds container, Bounds child)
        {
            byte type = Byte.MaxValue;
            float lBorder = container.center.x - container.extents.x;
            float rBorder = container.center.x + container.extents.x;
            float lChildBorder = child.center.x - child.extents.x;
            float rChildBorder = child.center.x + child.extents.x;

            if (Mathf.Abs(lBorder - lChildBorder) < 0.01f)
            {
                type = Tree.LBorder;
            }

            if (Mathf.Abs(rBorder - rChildBorder) < 0.01f)
            {
                type = Tree.RBorder;
            }

            float bBorder = container.center.z - container.extents.z;
            float tBorder = container.center.z + container.extents.z;
            float bChildBorder = child.center.z - child.extents.z;
            float tChildBorder = child.center.z + child.extents.z;

            if (Mathf.Abs(tBorder - tChildBorder) < 0.01f)
            {
                if (type == Tree.LBorder)
                {
                    type = Tree.LTCorner;
                }
                else if (type == Tree.RBorder)
                {
                    type = Tree.RTCorner;
                }
                else
                {
                    type = Tree.TBorder;
                }
            }

            if (Mathf.Abs(bBorder - bChildBorder) < 0.01f)
            {
                if (type == Tree.LBorder)
                {
                    type = Tree.LBCorner;
                }
                else if (type == Tree.RBorder)
                {
                    type = Tree.RBCorner;
                }
                else
                {
                    type = Tree.BBorder;
                }
            }
            
            return type;
        }

        void AddBoundaryFromDetail(Tree container, Tree detail, float minDis)
        {
            byte type = GetBorderType(container._bounds, detail._bounds);
            switch (type)
            {
                case Tree.LBorder:
                    container.MergeBoundary(Tree.LBorder, minDis, detail._boundaries[Tree.LTCorner]);
                    container.MergeBoundary(Tree.LBorder, minDis, detail._boundaries[Tree.LBorder]);
                    container.MergeBoundary(Tree.LBorder, minDis, detail._boundaries[Tree.LBCorner]);
                    break;
                case Tree.LTCorner:
                    container.MergeBoundary(Tree.TBorder, minDis, detail._boundaries[Tree.TBorder]);
                    container.MergeBoundary(Tree.TBorder, minDis, detail._boundaries[Tree.RTCorner]);
                    container.MergeBoundary(Tree.LTCorner, minDis, detail._boundaries[Tree.LTCorner]);
                    container.MergeBoundary(Tree.LBorder, minDis, detail._boundaries[Tree.LBorder]);
                    container.MergeBoundary(Tree.LBorder, minDis, detail._boundaries[Tree.LBCorner]);
                    break;
                case Tree.LBCorner:
                    container.MergeBoundary(Tree.BBorder, minDis, detail._boundaries[Tree.BBorder]);
                    container.MergeBoundary(Tree.BBorder, minDis, detail._boundaries[Tree.RBCorner]);
                    container.MergeBoundary(Tree.LBCorner, minDis, detail._boundaries[Tree.LBCorner]);
                    container.MergeBoundary(Tree.LBorder, minDis, detail._boundaries[Tree.LBorder]);
                    container.MergeBoundary(Tree.LBorder, minDis, detail._boundaries[Tree.LTCorner]);
                    break;
                case Tree.BBorder:
                    container.MergeBoundary(Tree.BBorder, minDis, detail._boundaries[Tree.BBorder]);
                    container.MergeBoundary(Tree.BBorder, minDis, detail._boundaries[Tree.LBCorner]);
                    container.MergeBoundary(Tree.BBorder, minDis, detail._boundaries[Tree.RBCorner]);
                    break;
                case Tree.RBCorner:
                    container.MergeBoundary(Tree.BBorder, minDis, detail._boundaries[Tree.BBorder]);
                    container.MergeBoundary(Tree.BBorder, minDis, detail._boundaries[Tree.LBCorner]);
                    container.MergeBoundary(Tree.RBCorner, minDis, detail._boundaries[Tree.RBCorner]);
                    container.MergeBoundary(Tree.RBorder, minDis, detail._boundaries[Tree.RBorder]);
                    container.MergeBoundary(Tree.RBorder, minDis, detail._boundaries[Tree.RTCorner]);
                    break;
                case Tree.RBorder:
                    container.MergeBoundary(Tree.RBorder, minDis, detail._boundaries[Tree.RTCorner]);
                    container.MergeBoundary(Tree.RBorder, minDis, detail._boundaries[Tree.RBorder]);
                    container.MergeBoundary(Tree.RBorder, minDis, detail._boundaries[Tree.RBCorner]);
                    break;
                case Tree.RTCorner:
                    container.MergeBoundary(Tree.TBorder, minDis, detail._boundaries[Tree.TBorder]);
                    container.MergeBoundary(Tree.TBorder, minDis, detail._boundaries[Tree.LTCorner]);
                    container.MergeBoundary(Tree.RTCorner, minDis, detail._boundaries[Tree.RTCorner]);
                    container.MergeBoundary(Tree.RBorder, minDis, detail._boundaries[Tree.RBorder]);
                    container.MergeBoundary(Tree.RBorder, minDis, detail._boundaries[Tree.RBCorner]);
                    break;
                case Tree.TBorder:
                    container.MergeBoundary(Tree.TBorder, minDis, detail._boundaries[Tree.RTCorner]);
                    container.MergeBoundary(Tree.TBorder, minDis, detail._boundaries[Tree.TBorder]);
                    container.MergeBoundary(Tree.TBorder, minDis, detail._boundaries[Tree.LTCorner]);
                    break;
                default:
                    break;
            }
        }

        public bool Done
        {
            get
            {
                return _currLodIndex >= _lods.Length;
            }
        }

        public float Progress
        {
            get
            {
                if (_currLodIndex < _lods.Length)
                {
                    return (_currLodIndex + _lods[_currLodIndex].Progress) / _lods.Length;
                }
                return 1.0f;
            }
        }
    };

};
