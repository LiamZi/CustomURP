partial class CustomRenderPipelineAsset
{
#if UNITY_EDITOR
    static string [] RenderLayerNames;

    static CustomRenderPipelineAsset()
    {
        RenderLayerNames = new string[31];
        for(int i = 0; i < RenderLayerNames.Length; i++)
        {
            RenderLayerNames[i] = "Layer" + (i + 1);
        }
    }

    public override string[] renderingLayerMaskNames => RenderLayerNames;
#endif
}