using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomURP
{
    public static partial class ShaderParams
    {
        public static readonly int _MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int _Count = Shader.PropertyToID("_Count");

        public static readonly int _SourceTex = Shader.PropertyToID("_SourceTex");
        public static readonly int _DestTex = Shader.PropertyToID("_DestTex");
        public static readonly int _FxSourceId = Shader.PropertyToID("_PostFXSource");
        public static readonly int _FxSourceId2 = Shader.PropertyToID("_PostFXSource2");
        public static readonly int _BloomBucibicUpSamplingId = Shader.PropertyToID("_BloomBicubicUpSampling");
        public static readonly int _BloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
        public static readonly int _BloomThresholdId = Shader.PropertyToID("_BloomThreshold");
        public static readonly int _BloomIntensityId = Shader.PropertyToID("_BloomIntensity");
        public static readonly int _BloomResultId = Shader.PropertyToID("_BloomResult");
        public static readonly int _ColorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments");
        public static readonly int _ColorFilterId = Shader.PropertyToID("_ColorFilter");
        public static readonly int _WhiteBalanceId = Shader.PropertyToID("_WhiteBalance");
        public static readonly int _SplitToningShadowsId = Shader.PropertyToID("_SplitToningShadows");
        public static readonly int _SplitToningHighlightsId = Shader.PropertyToID("_SplitToningHighlights");
        public static readonly int _ChannelMixerRedId = Shader.PropertyToID("_ChannelMixerRed");
        public static readonly int _ChannelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen");
        public static readonly int _ChannelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue");
        public static readonly int _SmhShadowsId = Shader.PropertyToID("_SMHShadows");
        public static readonly int _SmhMidtonesId = Shader.PropertyToID("_SMHMidtones");
        public static readonly int _SmhHighlightsId = Shader.PropertyToID("_SMHHighlights"); 
        public static readonly int _SmhRangeId = Shader.PropertyToID("_SMHRange");
        public static readonly int _ColorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT");
        public static readonly int _ColorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters"); 
        public static readonly int _ColorGradingLUTInLogCId = Shader.PropertyToID("_ColorGradingLUTInLogC");
        public static readonly int _FinalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend");
        public static readonly int _FinalDstBlendId = Shader.PropertyToID("_FinalDstBlend");
        public static readonly int _ColorGradingResultId = Shader.PropertyToID("_ColorGradingResult");
        public static readonly int _FinalResultId = Shader.PropertyToID("_FinalResult");
        public static readonly int _CopyBicubicId = Shader.PropertyToID("_CopyBicubic");
        public static readonly int _FxaaConfigId = Shader.PropertyToID("_FXAAConfig");
        
        public static readonly int _DirLightCountId = Shader.PropertyToID("_directionalLightCount");
        public static readonly int _DirLightColorId = Shader.PropertyToID("_directionalLightColor");
        public static readonly int _DirLightDirectionId = Shader.PropertyToID("_directionalLightDirectionAndMasks");
        public static readonly int _DirLightShadowDataId = Shader.PropertyToID("_directionalLightShadowData");
        public static readonly int _OtherLightSizeId = Shader.PropertyToID("_otherLightSize");
        public static readonly int _OtherLightColorsId = Shader.PropertyToID("_otherLightColors");
        public static readonly int _OtherLightPositionsId = Shader.PropertyToID("_otherLightPositions");
        public static readonly int _OtherLightDirectionsId = Shader.PropertyToID("_otherLightDirectionsAndMasks");
        public static readonly int _OtherLightSpotAnglesId = Shader.PropertyToID("_otherLightAngles");
        public static readonly int _OtherLightShadowDataId = Shader.PropertyToID("_otherLightShadowData");
        
        // public static readonly int _CameraColorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
        // public static readonly int _CameraDepthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");
        public static readonly int _CameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");
        public static readonly int _CameraDepthTextureId = Shader.PropertyToID("_CameraDepthTexture");
        public static readonly int _SourceTextureId = Shader.PropertyToID("_SourceTexture");
        public static readonly int _CameraSrcBlendId = Shader.PropertyToID("_CameraSrcBlend");
        public static readonly int _CameraDstBlendId = Shader.PropertyToID("_CameraDstBlend");
        public static readonly int _CamerabufferSizeId = Shader.PropertyToID("_CameraBufferSize");
        
        public static ShaderTagId[] _LegacyShaderTagIdsagId =
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM"),
        };
    };
    
    


};