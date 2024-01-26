using System.Collections.Generic;
using TriangleNet.Geometry;
using UnityEngine;

namespace CustomURP
{
    public class TessellationJob
    {
        public MeshData[] _mesh;
        public TerrainScanner[] _scanners;
        public float MinTriArea { get; private set; }
        protected int _currentIndex = 0;

        public TessellationJob(TerrainScanner[] scanner, float minTriangleArea)
        {
            _scanners = scanner;
            MinTriArea = minTriangleArea;
            _mesh = new MeshData[scanner[0]._trees.Length];
        }

        // public TessellationJob(Terrain terrain, Bounds volume, int sub, float angleError, int maxX, int maxZ, bool sbrd)
        // {
        // }


        public virtual void Tick()
        {
            if (Done)
                return;

            _mesh[_currentIndex] = new MeshData(_currentIndex, _scanners[0]._trees[_currentIndex]._bounds);
            _mesh[_currentIndex]._lods = new MeshData.Lod[_scanners.Length];

            for (int lod = 0; lod < _scanners.Length; ++lod)
            {
                var lodData = new MeshData.Lod();
                var tree = _scanners[lod]._trees[_currentIndex];
                RunTessellation(tree._vertices, lodData, MinTriArea);
                lodData._uvMin = tree._uvMin;
                lodData._uvMax = tree._uvMax;
                _mesh[_currentIndex]._lods[lod] = lodData;
            }

            ++_currentIndex;
        }
        protected void RunTessellation(List<VertexData> lVerts, MeshData.Lod lod, float minTriArea)
        {
            if (lVerts.Count < 3)
            {
                ++_currentIndex;
                return;
            }

            InputGeometry geometry = new InputGeometry();
            for (int i = 0; i < lVerts.Count; i++)
            {
                var vert = lVerts[i];
                geometry.AddPoint(vert._position.x, lVerts[i]._position.z, 0);
            }

            TriangleNet.Mesh meshRepresentation = new TriangleNet.Mesh();
            meshRepresentation.Triangulate(geometry);

            if (meshRepresentation.Vertices.Count != lVerts.Count)
            {
                Debug.LogError("Trianglate seems failed.");
            }
            int vIdx = 0;
            lod._vertices = new Vector3[meshRepresentation.Vertices.Count];
            lod._normals = new Vector3[meshRepresentation.Vertices.Count];
            lod._uvs = new Vector2[meshRepresentation.Vertices.Count];
            lod._faces = new int[meshRepresentation.triangles.Count * 3];

            foreach (var v in meshRepresentation.Vertices)
            {
                lod._vertices[vIdx] = new Vector3(v.x, lVerts[vIdx]._position.y, v.y);
                lod._normals[vIdx] = lVerts[vIdx]._normal;
                var uv = lVerts[vIdx]._uv;
                lod._uvs[vIdx] = uv;
                ++vIdx;
            }

            vIdx = 0;
            foreach (var t in meshRepresentation.triangles.Values)
            {
                var p = new Vector2[]
                {
                    new Vector2(lod._vertices[t.P0].x, lod._vertices[t.P0].z), new Vector2(lod._vertices[t.P1].x, lod._vertices[t.P1].z),
                    new Vector2(lod._vertices[t.P2].x, lod._vertices[t.P2].z)

                };

                var triarea = UnityEngine.Mathf.Abs((p[2].x - p[0].x) * (p[1].y - p[0].y) - (p[1].x - p[0].x) * (p[2].y - p[0].y)) / 2.0f;

                if (triarea < minTriArea)
                    continue;

                lod._faces[vIdx] = t.P2;
                lod._faces[vIdx + 1] = t.P1;
                lod._faces[vIdx + 2] = t.P0;
                vIdx += 3;
            }
        }
        public float Progress
        {
            get
            {
                return (float)(_currentIndex / (float)(_mesh.Length));
            }
        }

        public bool Done
        {
            get
            {
                return _currentIndex >= _mesh.Length;
            }
        }
    };

    public class TessellationDataJob : TessellationJob
    {
        List<Tree> _subTree = new List<Tree>();
        List<int> _lodLvArr = new List<int>();
        
        public TessellationDataJob(TerrainScanner[] scanner, float minTriangleArea) : base(scanner, minTriangleArea)
        {
            int total = 0;
            foreach (var s in _scanners)
            {
                total += s._trees.Length;
                _lodLvArr.Add(total);
                _subTree.AddRange(s._trees);
            }

            _mesh = new MeshData[_subTree.Count];
        }

        int GetLodLv(int index)
        {
            for (int i = 0; i < _lodLvArr.Count; ++i)
            {
                if (index < _lodLvArr[i])
                    return i;
            }

            return 0;
        }

        public override void Tick()
        {
            if (Done)
                return;
            var lodLv = GetLodLv(_currentIndex);
            _mesh[_currentIndex] = new MeshData(_currentIndex, _subTree[_currentIndex]._bounds, lodLv);
            _mesh[_currentIndex]._lods = new MeshData.Lod[1];
            var lodData = new MeshData.Lod();
            var tree = _subTree[_currentIndex];
            RunTessellation(tree._vertices, lodData, MinTriArea);
            _mesh[_currentIndex]._lods[0] = lodData;
            
            //update idx
            ++_currentIndex;
        }
    };
};
