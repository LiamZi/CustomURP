using System.Collections.Generic;
using UnityEngine;


namespace CustomURP
{
    public class MeshUtility
    {
        public static Mesh CreatePlanemesh(int size)
        {
            var mesh = new Mesh();
            var sizePerGrid = 0.5f;
            var totalMeterSize = size * sizePerGrid;
            var gridCount = size * size;
            var triangleCount = gridCount * 2;

            var vOffset = -totalMeterSize * 0.5f;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            float uvStrip = 1f / size;

            for (var z = 0; z <= size; z++)
            {
                for (var x = 0; x <= size; x++)
                {
                    vertices.Add(new Vector3(vOffset + x * 0.5f, 0, vOffset + z * 0.5f));
                    uvs.Add(new Vector2(x * uvStrip, z * uvStrip));
                }
            }
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);

            int[] indices = new int[triangleCount * 3];

            for (var gridIndex = 0; gridIndex < gridCount; gridIndex++)
            {
                var offset = gridIndex * 6;
                var index = (gridIndex / size) * (size + 1) + (gridIndex % size);

                indices[offset] = index;
                indices[offset + 1] = index + size + 1;
                indices[offset + 2] = index + 1;
                indices[offset + 3] = index + 1;
                indices[offset + 4] = index + size + 1;
                indices[offset + 5] = index + size + 2;
            }

            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.UploadMeshData(false);
            return mesh;
        }

        public static Mesh CreateCube(float size)
        {
            var mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            float extent = size * 0.5f;
            
            vertices.Add(new Vector3(-extent, -extent, -extent));
            vertices.Add(new Vector3(extent,-extent,-extent));
            vertices.Add(new Vector3(extent,extent,-extent));
            vertices.Add(new Vector3(-extent,extent,-extent));
            
            vertices.Add(new Vector3(-extent,extent,extent));
            vertices.Add(new Vector3(extent,extent,extent));
            vertices.Add(new Vector3(extent,-extent,extent));
            vertices.Add(new Vector3(-extent,-extent,extent));

            int[] triangles =
            {
                0, 2, 1, //face front
                0, 3, 2, 
                2, 3, 4, //face top
                2, 4, 5, 
                1, 2, 5, //face right
                1, 5, 6, 
                0, 7, 4, //face left
                0, 4, 3, 
                5, 4, 7, //face back
                5, 7, 6, 
                0, 6, 7, //face bottom
                0, 1, 6
            };
            
            mesh.SetVertices(vertices);
            mesh.triangles = triangles;
            mesh.UploadMeshData(false);
            return mesh;
        }
    };
};
