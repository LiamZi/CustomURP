using System.IO;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace CustomURP
{
    public class TextureUtility
    {
        public static RenderTexture CreateRenderTextureWithMipTextures(ref Command cmd, Texture2D[] mipmaps, RenderTextureFormat format)
        {
            var mip0 = mipmaps[0];
            RenderTextureDescriptor desc = new RenderTextureDescriptor(mip0.width, mip0.height, format, 0, mipmaps.Length);
            desc.autoGenerateMips = false;
            desc.useMipMap = true;
            RenderTexture rt = new RenderTexture(desc);
            rt.filterMode = mip0.filterMode;
            rt.Create();

            for (var i = 0; i < mipmaps.Length; ++i)
            {
                cmd.CopyTexture(mipmaps[i], 0, 0, rt, 0, i);
            }

            return rt;
        }

        public static RenderTexture CreateLodMap(int size)
        {
            RenderTextureDescriptor desc = new RenderTextureDescriptor(size, size, RenderTextureFormat.R8, 0, 1);
            desc.autoGenerateMips = false;
            desc.enableRandomWrite = true;
            RenderTexture rt = new RenderTexture(desc);
            rt.filterMode = FilterMode.Point;
            rt.Create();
            return rt;
        }
    }
}
