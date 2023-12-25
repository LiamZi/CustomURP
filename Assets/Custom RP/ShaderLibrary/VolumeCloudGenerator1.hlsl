#ifndef __SHADER_LIBRARY_VOLUME_CLOUD_HLSL__
#define __SHADER_LIBRARY_VOLUME_CLOUD_HLSL__

#include "../ShaderLibrary/CloudFunctions.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 viewDir : TEXCOORD1;
};

Varyings vert(Attributes input)
{
    Varyings o;
    o.positionCS = TransformObjectToHClip(input.positionOS);
    o.uv = input.uv;

    float3 viewDir = mul(unity_CameraInvProjection, float4(input.uv * 2.0 - 1.0, 0, -1)).xyz;
    o.viewDir = mul(unity_CameraToWorld, float4(viewDir, 0)).xyz;

    return o;
}

half4 frag(Varyings input) : SV_Target
{
    half4 backColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

#ifndef _OFF
    int iterationCount = 4;
#ifdef _2X2
    iterationCount = 2;
#endif
    int frameOrder = GetIndex(input.uv, _Width, _Height, iterationCount);
    if(frameOrder != _FrameCount)
    {
        return backColor;
    }
#endif

    float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_linear_clamp, input.uv).x;
    float dstToObj = LinearEyeDepth(depth, _ZBufferParams);

    float3 viewDir = normalize(input.viewDir);
    // float3 lightDir = normalize(_MainLightDirection);
    float3 lightDir = normalize(float4(0, 1, 0, 0));
    float3 cameraPos = _WorldSpaceCameraPos;
    

    float3 sphereCenter = float3(cameraPos.x, -EARTH_RADIUS, cameraPos.z);
    float boundBoxScaleMax = 1;
    float3 boundBoxPosition = (_BoundBoxMax + _BoundBoxMin) / 2.0;

#if _RENDERMODE_BAKE
    float2 dstCloud = RayBoxTest(_BoundBoxMin, _BoundBoxMax, cameraPos, viewDir);
    float3 boundBoxScale = (_BoundBoxMax - _BoundBoxMin) / _BaseShapeRatio;
    _BoundBoxMax = max(max(boundBoxScale.x, boundBoxScale.y), boundBoxScale.z);
#else
    float2 dstCloud = ResolveRayStartEnd(sphereCenter, EARTH_RADIUS, _CloudHeightRange.x, _CloudHeightRange.y, cameraPos, viewDir);
#endif

    float dstToCloud = dstCloud.x;
    float dstInCloud = dstCloud.y;

    // if(dstInCloud <= 0 || dstToObj <= dstToCloud)
    if(dstInCloud <= 0)
    {
        return half4(0, 0, 0, 1);
    }

     SamplingInfo dsi;
     dsi.baseShapeTiling = _BaseShapeTexTiling;
     dsi.baseShapeRatio = _BaseShapeRatio;
     dsi.boundBoxScaleMax = boundBoxScaleMax;
     dsi.boundBoxPosition = boundBoxPosition;
     dsi.detailShapeTiling = _DetailShapeTexTiling;
     dsi.weatherTexTiling = _WeatherTexTiling;
     dsi.weatherTexOffset = _WeatherTexOffset;
     dsi.baseShapeDetailEffect = _BaseShapeDetailEffect;
     dsi.detailEffect = _DetailEffect;
     dsi.densityMultiplier = _DensityScale;
     dsi.cloudDensityAdjust = _CloudCover;
     dsi.cloudAbsorbAdjust = _CloudAbsorb;
     dsi.windDirection = normalize(_WindDirecton);
     dsi.windSpeed = _WindSpeed;
     dsi.cloudHeightMinMax = _CloudHeightRange.xy;
     dsi.stratusInfo = float3(_StratusRange.xy, _StratusFeather);
     dsi.cumulusInfo = float3(_CumulusRange.xy, _CumulusFeather);
     dsi.cloudOffsetLower = _CloudOffsetLower;
     dsi.cloudOffsetUpper = _CloudOffsetUpper;
     dsi.feather = _CloudFeather;
     dsi.sphereCenter = sphereCenter;
     dsi.earthRadius = EARTH_RADIUS;
//
     float phase = HGScatterMax(dot(viewDir, lightDir), _ScatterForward, _ScatterForwardIntensity, _ScatterBackward, _ScatterBackwardIntensity);
     phase = _ScatterBase + phase * _ScatterMultiply;
//
     float blueNoise = SAMPLE_TEXTURE2D(_BlueNoiseTex, sampler_BlueNoiseTex, input.uv * _BlueNoiseTexUV).r;
     float endPos = dstToCloud + dstInCloud;
//
#ifdef _RENDERMODE_BAKE
    bool isFirstSampleCloud = true;
    float currentMarchLength = dstToCloud;
#else
    float currentMarchLength = dstToCloud + _ShapeMarchLength * blueNoise * _BlueNoiseEffect;
#endif
     float3 currentPos = cameraPos + currentMarchLength * viewDir;
     float shapeMarchLength = _ShapeMarchLength;
//
#if _RENDERMODE_BAKE
    bool isBake = true;
#else
    bool isBake = false;
#endif
//
     float totalDensity = 0;
     float3 totalLumiance = 0;
     float lightAttenuation = 1.0;

     float densityTest = 0;
     float densityPrevious = 0;
     int densitySampleCountZero = 0;
//
    for(int marchNum = 0; marchNum < _ShapeMarchMax; marchNum++)
    {
        if(densityTest == 0 && !isBake)
        {
            currentMarchLength += _ShapeMarchLength * 2.0;
            currentPos = cameraPos + currentMarchLength * viewDir;
            
            //如果步进到被物体遮挡,或穿出云覆盖范围时,跳出循环
            if (dstToObj <= currentMarchLength || endPos <= currentMarchLength)
            {
                break;
            }
                        
            //进行密度采样，测试是否继续大步前进
            dsi.position = currentPos;
            densityTest = SampleCloudDensity(dsi, true).density;
                        
            //如果检测到云，往后退一步(因为我们可能错过了开始位置)
            if (densityTest > 0)
            {
                currentMarchLength -= _ShapeMarchLength;
            }
        }
        else
        {
                currentPos = cameraPos + currentMarchLength * viewDir;
                dsi.position = currentPos;
#ifdef _SHAPE_DETAIL_ON
                CloudInfo ci = SampleCloudDensity(dsi, false);
#else
                CloudInfo ci = SampleCloudDensity(dsi, true);
#endif
            

#if !_RENDERMODE_BAKE
                if (ci.density == 0 && densityPrevious == 0)
                {
                    densitySampleCountZero ++ ;
                    //累计检测到指定数值，切换到大步进
                    if (densitySampleCountZero >= 8)
                    {
                        densityTest = 0;
                        densitySampleCountZero = 0;
                        continue;
                    }
                }
#endif
            
#if _RENDERMODE_BAKE
                float density = ci.density * shapeMarchLength;
#else
                float density = ci.density * _ShapeMarchLength;
#endif
            
            float currentLumince = 0;

            if(density > 0.01)
            {
#if !_RENDERMODE_BAKE
                //计算该区域的光照贡献，从当前点向灯光方向步进
                float2 dstCloud_light = ResolveRayStartEnd(sphereCenter, EARTH_RADIUS, _CloudHeightRange.x, _CloudHeightRange.y, currentPos, lightDir, false);
                //灯光步进长度
                float lightMarchLength = dstCloud_light.y / _LightingMarchMax;
                //当前步进位置
                float3 currentPos_light = currentPos;
                //灯光方向密度
                float totalDensity_light = 0;
            
                //向灯光方向进行步进
                for (int marchNumber_light = 0; marchNumber_light < _LightingMarchMax; marchNumber_light ++)
                {
                    currentPos_light += lightDir * lightMarchLength;
                    dsi.position = currentPos_light;
                    float density_Light = SampleCloudDensity(dsi, true).density * lightMarchLength;
                    totalDensity_light += density_Light;
                }
                //光照强度
                currentLumince = BeerPowder(totalDensity_light, ci.absorptivity);
#else
                currentLumince = ci.lum;
#endif
                
                currentLumince = _DarknessThreshold + currentLumince * (1.0 - _DarknessThreshold);
                //
                float3 cloudColor = Interpolation3(_ColorDark.rgb, _ColorCentral.rgb, _ColorBright.rgb, saturate(currentLumince), _ColorCentralOffset) * half3(1.0, 1.0, 1.0);
                //
                totalLumiance += lightAttenuation * cloudColor * density * phase;
                totalDensity += density;
                lightAttenuation *= Beer(density, ci.absorptivity);
                //
                if(lightAttenuation < 0.01)
                    break;
                
            }


#if _RENDERMODE_BAKE
                shapeMarchLength = max(_ShapeMarchLength, ci.sdf * _SDFScale);
                //添加蓝噪声影响
                if(density > 0.01 && isFirstSampleCloud)
                {
                    shapeMarchLength *= blueNoise * _BlueNoiseEffect;
                    isFirstSampleCloud = false;
                }
                currentMarchLength += shapeMarchLength;
#else
            currentMarchLength += _ShapeMarchLength;
#endif
                //如果步进到被物体遮挡,或穿出云覆盖范围时,跳出循环
                if (dstToObj <= currentMarchLength || endPos <= currentMarchLength)
                {
                    break;
                }
                densityPrevious = ci.density;
        }
    }
                
                //最后的颜色应当为backColor.rgb * lightAttenuation + totalLum, 但是因为分帧渲染，混合需要到下一pass
                // return half4(backColor.rgb * lightAttenuation + totalLum, lightAttenuation);
                return half4(totalLumiance, lightAttenuation);
}

#endif