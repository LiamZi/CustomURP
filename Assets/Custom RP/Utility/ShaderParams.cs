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

        public static readonly int _CameraColorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
        public static readonly int _CameraDepthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");
        public static readonly int _CameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");
        public static readonly int _CameraDepthTextureId = Shader.PropertyToID("_CameraDepthTexture");
        public static readonly int _SourceTextureId = Shader.PropertyToID("_SourceTexture");
        public static readonly int _CameraSrcBlendId = Shader.PropertyToID("_CameraSrcBlend");
        public static readonly int _CameraDstBlendId = Shader.PropertyToID("_CameraDstBlend");
        public static readonly int _CamerabufferSizeId = Shader.PropertyToID("_CameraBufferSize");

        public static readonly int _clusterZFarId = Shader.PropertyToID("_cluster_zFar");
        public static readonly int _clusterZNearId = Shader.PropertyToID("_cluster_zNear");
        public static readonly int _clusterDataId = Shader.PropertyToID("_cluster_Data");
        public static readonly int _clusterLightListId = Shader.PropertyToID("_cluster_LightList");
        public static readonly int _clusterLightCountId = Shader.PropertyToID("_clusterLightCount");
        public static readonly int _clusterGridRWId = Shader.PropertyToID("_cluster_Grid_RW");
        public static readonly int _clusterLightIndexRWId = Shader.PropertyToID("_cluster_LightIndex_RW");
        public static readonly int _clusterLightIndexId = Shader.PropertyToID("_cluster_LightIndex");
        public static readonly int _clusterGrirdLightRWId = Shader.PropertyToID("_cluster_GridIndex_RW");
        public static readonly int _clusterGridLightId = Shader.PropertyToID("_cluster_GridIndex");
        
        public static readonly int _clusterDirectionalLightCountId = Shader.PropertyToID("_cluster_directionalLightCount");
        public static readonly int _clusterDirectionalLightColorId = Shader.PropertyToID("_cluster_directionalLightColor");
        public static readonly int _clusterDirectionalLightDirAndMasksId = Shader.PropertyToID("_cluster_directionalLightDirectionAndMasks");
        public static readonly int _clusterDirectionalLightShadowDataId = Shader.PropertyToID("_cluster_directionalLightShadowData");
        public static readonly int _clusterOtherLightShadowDataId = Shader.PropertyToID("_cluster_otherLightShadowData");
        
        
        public static readonly int _skyGradientTex = Shader.PropertyToID("_SkyGradientTex");
        public static readonly int _starIntensity = Shader.PropertyToID("_StarIntensity");
        public static readonly int _milkyWayIntensity = Shader.PropertyToID("_MilkywayIntensity");
        public static readonly int _sunDirectionWS = Shader.PropertyToID("_SunDirectionWS");
        public static readonly int _moonDreictinWS = Shader.PropertyToID("_MoonDirectionWS");
        public static readonly int _starTex = Shader.PropertyToID("_StarTex");
        public static readonly int _moonTex = Shader.PropertyToID("_MoonTex");
        public static readonly int _moonWorld2Local = Shader.PropertyToID("_MoonWorld2Obj");
        public static readonly int _milkyWayWorld2Local = Shader.PropertyToID("_MilkyWayWorld2Local");
        public static readonly int _scatteringIntensity = Shader.PropertyToID("_ScatteringIntensity");
        public static readonly int _sunIntensity = Shader.PropertyToID("_SunIntensity");
        
        //Cloud
        public static int _cloudStartHeight = Shader.PropertyToID("_CloudStartHeight");
        public static int _cloudEndHeight = Shader.PropertyToID("_CloudEndHeight");

        public static int _cloudTex = Shader.PropertyToID("_CloudTex");
        public static int _cloudTile = Shader.PropertyToID("_CloudTile");
        public static int _detailTex = Shader.PropertyToID("_DetailTex");
        public static int _detailTile = Shader.PropertyToID("_DetailTile");

        public static int _curlNoise = Shader.PropertyToID("_CurlNoise");
        public static int _curlTile = Shader.PropertyToID("_CurlTile");
        public static int _curlStrength = Shader.PropertyToID("_CurlStrength");
        public static int _heightDensity = Shader.PropertyToID("_HeightDensity");

        public static int _topOffst = Shader.PropertyToID("_CloudTopOffset");
        public static int _cloudSize = Shader.PropertyToID("_CloudSize");

        public static int _cloudOverallDensity = Shader.PropertyToID("_CloudOverallDensity");
        public static int _cloudTypeModifier = Shader.PropertyToID("_CloudTypeModifier");
        public static int _cloudCoverageModifier = Shader.PropertyToID("_CloudConverageModifier");

        public static int _windDirection = Shader.PropertyToID("_WindDirection");
        public static int _weatherTex = Shader.PropertyToID("_WeatherTex");
        public static int _weatherTexSize = Shader.PropertyToID("_WeatherTexSize");
        public static int _scatteringCoefficient = Shader.PropertyToID("_ScatteringCoefficient");
        public static int _extinctionCoefficient = Shader.PropertyToID("_ExtinctionCoefficient");
        public static int _multiScatteringA = Shader.PropertyToID("_MultiScatteringA");
        public static int _multiScatteringB = Shader.PropertyToID("_MultiScatteringB");
        public static int _multiScatteringC = Shader.PropertyToID("_MultiScatteringC");
        public static int _silverSpread = Shader.PropertyToID("_SilverSpread");
        public static int _atmosphereColorSaturateDistance = Shader.PropertyToID("_AtmosphereColorSaturateDistance");
        public static int _atmosphereColor = Shader.PropertyToID("_AtmosphereColor");
        public static int _ambientColor = Shader.PropertyToID("_AmbientColor");


        
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