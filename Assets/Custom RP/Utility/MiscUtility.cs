using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;

namespace CustomPipeline
{
    public class MiscUtility
    {
        public static Texture2D MissingTexture;
        public static Rect FullViewRect = new Rect(0f, 0f, 1f, 1f);
        public const float RTScaleMin = 0.1f;
        public const float RTScaleMax = 2f;
        public static bool CopyTextureSupported = false;
        
        public static void Initialization()
        {
            MissingTexture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = "Missing"
            };
            
            MissingTexture.SetPixel(0, 0, Color.white * 0.5f);
            MissingTexture.Apply(true, true);
        }
    }
}