using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    public class QuadTreeMapEditor
    {
        int _maxLodSize;
        int _lodCount;
        ComputeShader _shader;

        public QuadTreeMapEditor(int maxLodSize, int lodCount)
        {
            _maxLodSize = maxLodSize;
            _lodCount = lodCount;
        }

        ComputeShader Shader
        {
            get
            {
                if (!_shader)
                {
                    _shader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Custom RP/ShaderLibrary/ComputeShader/QuadTreeMipMapBuilder.compute");
                }

                return _shader;
            }
        }

        void QuadTreeMapMipBuilder(int mip, int nodeIdOffset)
        {
            var mipTexSize = (int)(_maxLodSize * Mathf.Pow(2, _lodCount - 1 - mip));
            var desc = new RenderTextureDescriptor(mipTexSize, mipTexSize, RenderTextureFormat.R16, 0, 1);
            desc.autoGenerateMips = false;
            desc.enableRandomWrite = true;
            
            var rt = new RenderTexture(desc);
            rt.filterMode = FilterMode.Point;
            rt.Create();
            
            Shader.SetTexture(0, ShaderParams._quadTreeMapId, rt);
            Shader.SetInt(ShaderParams._nodeIdOffsetId, nodeIdOffset);
            Shader.SetInt(ShaderParams._mapSizeId, mipTexSize);
            var group = (int)Mathf.Pow(2, _lodCount - mip - 1);
            Shader.Dispatch(0, group, group, 1);
            
            var req = AsyncGPUReadback.Request(rt, 0, 0, mipTexSize, 0, mipTexSize, 0, 1, TextureFormat.R16, request =>
            {
                if (request.hasError) return;

                var tex = TerrainEditor.ConvertToTexture2D(rt, TextureFormat.R16);
                var bytes = tex.EncodeToPNG();
                var dir = TerrainEditor.GetSelectedDir();
                System.IO.File.WriteAllBytes($"{dir}/QuadTreeMap_" + mip + ".png", bytes);
                if (mip > 0)
                {
                    QuadTreeMapMipBuilder(mip - 1, nodeIdOffset + mipTexSize * mipTexSize);
                }
                else
                {
                    AssetDatabase.Refresh();
                }
            });
            TerrainEditor.UpdateGpuAsyncRequest(req);
        }

        public void BuildAsync()
        {
            QuadTreeMapMipBuilder(_lodCount - 1, 0);
        }

        [MenuItem("Terrain/QuadTreeMipMapsGenerate")]
        public static void QuadTreeMipMapsGenerate()
        {
            new QuadTreeMapEditor(5, 6).BuildAsync();
        }
    };
};
