Shader "Custom RP/VolumeCloud2"
{
    Properties
    {
        [Enum_Switch(RealTime, No3DTex, Bake)]_RenderMode ("渲染模式", float) = 0
        
        [Foldout]_Shape ("形状_Foldout", float) = 1
        [Tex(_WeatherTexTiling, RealTime, No3DTex)][NoScaleOffset]_WeatherTex ("天气纹理", 2D) = "white" { }
        [HideInInspector]_WeatherTexTiling ("天气纹理平铺", Range(0.1, 30)) = 1
        [Switch(RealTime, No3DTex)]_WeatherTexOffset ("天气纹理偏移", vector) = (0, 0, 0, 0)
        // _WeatherTexRepair ("天气纹理边缘拉伸修复", Range(0, 0.2)) = 0.05
        [Tex(_BaseShapeTexTiling, RealTime, Bake)][NoScaleOffset]_BaseShapeTex ("基础形状纹理", 3D) = "white" { }
        [HideInInspector]_BaseShapeTexTiling ("基础形状纹理平铺", Range(0.1, 5)) = 1
        [Switch(RealTime)]_BaseShapeDetailEffect ("基础形状细节影响", Range(0, 1)) = 0.5
        [Switch(Bake)]_BaseShapeRatio("基础形状比例", vector) = (1, 1, 1, 1)
        
        [Foldout(2, 2, 1, 0, RealTime)]_Shape_Detail ("细节形状_Foldout", float) = 1
        [Tex(_DetailShapeTexTiling)][NoScaleOffset]_DetailShapeTex ("细节形状纹理", 3D) = "white" { }
        [HideInInspector]_DetailShapeTexTiling ("细节形状纹理平铺", Range(0.1, 3)) = 1
        _DetailEffect ("细节影响强度", Range(0, 1)) = 1
        
        [Foldout(2, 2, 0, 0, RealTime, No3DTex)]_Shape_Weather ("天气设置_Foldout", float) = 1
        [Header]
        [Range]_CloudHeightRange ("云层高度  最小/最大范围", vector) = (1500, 4000, 0, 8000)
        [Range(RealTime)]_StratusRange ("层云范围", vector) = (0.1, 0.4, 0, 1)
        [Switch(RealTime)]_StratusFeather ("层云边缘羽化", Range(0, 1)) = 0.2
        [Range(RealTime)]_CumulusRange ("积云范围", vector) = (0.15, 0.8, 0, 1)
        [Switch(RealTime)]_CumulusFeather ("积云边缘羽化", Range(0, 1)) = 0.2
        [PowerSlider(0.7)]_CloudCover ("云层覆盖率", Range(0, 1)) = 0.5
        [Switch(No3DTex)]_CloudOffsetLower ("云底层偏移", Range(-1, 1)) = 0
        [Switch(No3DTex)]_CloudOffsetUpper ("云顶层偏移", Range(-1, 1)) = 0
        [Switch(No3DTex)]_CloudFeather ("云层边缘羽化", Range(0, 1)) = 0.2
        
        [Foldout(2, 2, 0, 0)]_Shape_Effect ("性能_Foldout", float) = 1
        [Switch(Bake)]_SDFScale("SDF缩放", Range(0, 2)) = 1
        _ShapeMarchLength ("形状单次步进长度", Range(0.001, 800)) = 300
        _ShapeMarchMax ("形状最大步进次数", Range(3, 100)) = 30
        
        [Foldout(2, 2, 0, 0)]_Shape_Other ("形状其他设置_Foldout", float) = 1
        _BlueNoiseEffect ("蓝噪声影响程度", Range(0, 1)) = 1
        [Vector3(RealTime, No3DTex)]_WindDirecton ("风向", vector) = (1, 0, 0, 0)
        [Switch(RealTime, No3DTex)]_WindSpeed ("风速", Range(0, 5)) = 1
        _DensityScale ("密度缩放", Range(0, 2)) = 1
        
        
        [Foldout]_Lighting ("光照_Foldout", float) = 1
        [Foldout(2, 2, 0, 0)]_Lighting_Convention ("常规_Foldout", float) = 1
        _CloudAbsorb ("云层吸收率", Range(0, 4)) = 0.5
        [Space]
        _ScatterForward ("向前散射", Range(0, 0.99)) = 0.5
        _ScatterForwardIntensity ("向前散射强度", Range(0, 1)) = 1
        [Space]
        _ScatterBackward ("向后散射", Range(0, 0.99)) = 0.4
        _ScatterBackwardIntensity ("向后散射强度", Range(0, 1)) = 0.4
        [Space]
        _ScatterBase ("基础散射", Range(0, 1)) = 0.2
        _ScatterMultiply ("散射乘数", Range(0, 1)) = 0.7
        
        [Foldout(2, 2, 0, 0)]_Lighting_Color ("颜色_Foldout", float) = 1
        [HDR]_ColorBright ("亮面颜色", color) = (1, 1, 1, 1)
        [HDR]_ColorCentral ("中间颜色", color) = (0.5, 0.5, 0.5, 1)
        [HDR]_ColorDark ("暗面颜色", color) = (0.1, 0.1, 0.1, 1)
        _ColorCentralOffset ("中间颜色偏移", Range(0, 1)) = 0.5
        [Space]
        _DarknessThreshold ("暗部阈值", Range(0, 1)) = 0.3
        
        [Foldout(2, 2, 0, 0, RealTime, No3DTex)]_Lighting_Effect ("性能_Foldout", float) = 1
        _LightingMarchMax ("光照最大步进次数", Range(1, 15)) = 8
        
        [Foldout_Out(1)]_FoldoutOut ("跳出折叠页_Foldout", float) = 1
        
        [HideInInspector]_MainTex ("Texture", 2D) = "white" { }
        [HideInInspector]_BoundBoxMin ("_BoundBoxMin", vector) = (-1, -1, -1, -1)
        [HideInInspector]_BoundBoxMax ("_BoundBoxMax", vector) = (1, 1, 1, 1)
        _MainLightDirection("Light Direction", Vector) = (0, 1, 0, 0)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "PreviewType"="Sphere"
            "RenderPipeline" = "UniversalRenderPipeline"
        }
        LOD 100
        


        Pass
        {
            
            HLSLINCLUDE
            #include "../ShaderLibrary/Common.hlsl"
            #include "../ShaderLibrary/CloudInput.hlsl"
            ENDHLSL

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _RENDERMODE_REALTIME _RENDERMODE_NO3DTEX _RENDERMODE_BAKE
            #pragma shader_feature _SHAPE_DETAIL_ON
            #pragma multi_compile _OFF _2X2 _4X4

            
            #include "../ShaderLibrary/VolumeCloudGenerator1.hlsl"
            ENDHLSL
        }

        Pass
        {
            Blend One SrcAlpha
            
            HLSLINCLUDE
            #include "../ShaderLibrary/Common.hlsl"
            ENDHLSL
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // TEXTURE2D(_MainTex);
            // SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            }
            
            ENDHLSL
        }
    }
    CustomEditor "Scarecrow.SimpleShaderGUI"
}