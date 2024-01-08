using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace CustomURP
{
    public class TerrainEditor
    {
        public static string GetSelectedDir()
        {
            string path = null;
            if (Selection.activeObject)
            {
                path = AssetDatabase.GetAssetPath(Selection.activeObject);
            }
            
            if(!string.IsNullOrEmpty(path))
            {
                if (!System.IO.Directory.Exists(path))
                {
                    path = System.IO.Path.GetDirectoryName(path);
                }
            }

            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }

            return path;
        }
        
        [MenuItem("Terrain/CreatePlaneMesh")]
        public static void CreatePlaneMeshAsset()
        {
            var mesh = MeshUtility.CreatePlaneMesh(16);
            string path = GetSelectedDir();
            path += "/Plane.mesh";
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.Refresh();
        }

        [MenuItem("Terrain/CenerateNormalMapFromHeightMap")]
        public static void GenerateNormalMapFromHeightMap()
        {
            if (Selection.activeObject is Texture2D heightMap)
            {
                GenerateNormalMapFromHeightMap(heightMap, (texture2D => { }));
            }
            else
            {
                Debug.LogWarning("it has to choose a texture2d.");
            }
        }

        public static void GenerateNormalMapFromHeightMap(Texture2D heightmap, System.Action<Texture2D> cb)
        {
            var desc = new RenderTextureDescriptor(heightmap.width, heightmap.height, RenderTextureFormat.RG32);
            desc.enableRandomWrite = true;
            var rt = RenderTexture.GetTemporary(desc);
            ComputeShader cs = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Custom RP/ShaderLibrary/ComputeShader/HeightToNormal.compute");
            cs.SetTexture(0, ShaderParams._heightTexId, heightmap, 0);
            cs.SetTexture(0, ShaderParams._normalTexId, rt, 0);

            uint tx, ty, tz;
            cs.GetKernelThreadGroupSizes(0, out tx, out ty, out tz);
            cs.SetVector(ShaderParams._texSizeId, new Vector4(heightmap.width, heightmap.height, 0, 0));
            cs.SetVector(ShaderParams._worldSizeId, new Vector3(10240, 2048, 10240));
            cs.Dispatch(0, (int)(heightmap.width / tx), (int)(heightmap.height / ty), 1);
            var req = AsyncGPUReadback.Request(rt, 0, 0, rt.width, 0, rt.height, 0, 1, request =>
            {
                if (request.hasError)
                {
                    Debug.LogError("Error");
                }
                else
                {
                    Debug.Log("success");
                    SaveRenderTexture(rt, "Assets/Resources/Textures/Terrain/TerrainNormal.png");
                }
                RenderTexture.ReleaseTemporary(rt);
                cb(null);
            });

            UpdateGpuAsyncRequest(req);
        }

        public static void UpdateGpuAsyncRequest(AsyncGPUReadbackRequest req)
        {
            EditorApplication.CallbackFunction cbf = null;
            cbf = () =>
            {
                if (req.done) return;
                
                req.Update();
                EditorApplication.delayCall += cbf;
            };
            cbf();
        }

        public static void SaveRenderTexture(RenderTexture rt, string path)
        {
            var tex = ConvertToTexture2D(rt, TextureFormat.ARGB32);
            var bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
        }

        public static Texture2D ConvertToTexture2D(RenderTexture rt, TextureFormat format)
        {
            var origin = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, format, 0, false);
            tex.filterMode = rt.filterMode;
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
            tex.Apply(false, false);
            RenderTexture.active = origin;
            return tex;
        }
    }
}
