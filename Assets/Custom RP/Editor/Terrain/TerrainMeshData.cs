using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;


namespace CustomURP
{
    [System.Serializable]
    public class TerrainMeshData
    {
        public UnityEngine.Terrain _terrainData;
    };

    public static class MeshUtils
    {
        public static void Serialize(Stream stream, MeshData.Lod lod)
        {
            FileUtility.WriteVector2(stream, lod._uvMin);
            FileUtility.WriteVector2(stream, lod._uvMax);
            //_vertices
            byte[] uBuff = BitConverter.GetBytes(lod._vertices.Length);
            stream.Write(uBuff, 0, uBuff.Length);
            foreach (var v in lod._vertices)
            {
                FileUtility.WriteVector3(stream, v);
            }
                
            //_normals
            uBuff = BitConverter.GetBytes(lod._normals.Length);
            stream.Write(uBuff, 0, uBuff.Length);
            foreach (var n in lod._normals)
            {
                FileUtility.WriteVector3(stream, n);
            }
               
            //_uvs
            uBuff = BitConverter.GetBytes(lod._uvs.Length);
            stream.Write(uBuff, 0, uBuff.Length);
            foreach (var uv in lod._uvs)
            {
                FileUtility.WriteVector2(stream, uv);
            }
                
            //_faces
            uBuff = BitConverter.GetBytes(lod._faces.Length);
            stream.Write(uBuff, 0, uBuff.Length);
            foreach (var face in lod._faces)
            {
                //强转为ushort
                ushort val = (ushort)face;
                uBuff = BitConverter.GetBytes(val);
                stream.Write(uBuff, 0, uBuff.Length);
            }
        }
        public static void Deserialize(Stream stream, RenderMesh rm)
        {
            rm._mesh = new Mesh();
            rm._uvMin = FileUtility.ReadVector2(stream);
            rm._uvMax = FileUtility.ReadVector2(stream);
            //_vertices
            List<Vector3> vec3Cache = new List<Vector3>();
            byte[] nBuff = new byte[sizeof(int)];
            stream.Read(nBuff, 0, sizeof(int));
            int len = BitConverter.ToInt32(nBuff, 0);
            for (int i = 0; i < len; ++i)
            {
                vec3Cache.Add(FileUtility.ReadVector3(stream));
            }
                
            rm._mesh.SetVertices(vec3Cache.ToArray());
            
            //_normals
            vec3Cache.Clear();
            stream.Read(nBuff, 0, sizeof(int));
            len = BitConverter.ToInt32(nBuff, 0);
            for (int i = 0; i < len; ++i)
            {
                vec3Cache.Add(FileUtility.ReadVector3(stream));
            }
            rm._mesh.SetNormals(vec3Cache.ToArray());
            
            //_uvs
            List<Vector2> vec2Cache = new List<Vector2>();
            stream.Read(nBuff, 0, sizeof(int));
            len = BitConverter.ToInt32(nBuff, 0);
            
            for (int i = 0; i < len; ++i)
            {
                vec2Cache.Add(FileUtility.ReadVector2(stream));
            }
            rm._mesh.SetUVs(0, vec2Cache.ToArray());
            
            //_faces
            List<int> intCache = new List<int>();
            stream.Read(nBuff, 0, sizeof(int));
            len = BitConverter.ToInt32(nBuff, 0);
            byte[] fBuff = new byte[sizeof(ushort)];
            
            for (int i = 0; i < len; ++i)
            {
                stream.Read(fBuff, 0, sizeof(ushort));
                intCache.Add(BitConverter.ToUInt16(fBuff, 0));
            }
            rm._mesh.SetTriangles(intCache.ToArray(), 0);
        }
    };

}
