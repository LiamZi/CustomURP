using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    public sealed unsafe partial class PostProcessPass : CoreAction
    {
        partial void DrawGizmosBeforePostPass();
        partial void DrawGizmosAfterPostPass();
        
#if UNITY_EDITOR
        partial void DrawGizmosBeforePostPass()
        {
            
        }

        partial void DrawGizmosAfterPostPass()
        {
            
        }
#endif
    };
}
