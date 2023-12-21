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
}

half4 frag(Varyings input)
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

    float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv).x;
    float dstToObj = LinearEyeDepth(depth, _ZBufferParams);

    float3 viewDir = normalize(input.viewDir);
    float3 lightDir = normalize(_MainLightDirection);
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

    if(dstInCloud <= 0 || dstToObj <= dstToCloud)
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

    float phase = HGScatterMax(dot(viewDir, lightDir), _ScatterForward, _ScatterForwardIntensity, _ScatterBackward, _ScatterBackwardIntensity);
    phase = _ScatterBase + phase * _ScatterMultiply;

    float blueNoise = SAMPLE_TEXTURE2D(_BlueNoiseTex, sampler_BlueNoiseTex, input.uv * _BlueNoiseTexUV).r;
    float endPos = dstToCloud + dstInCloud;

#ifdef _RENDERMODE_BAKE
    bool isFirstSampleCloud = true;
    float currentMarchLength = dstToCloud;
#else
    float currentMarchLength = dstToCloud + _ShapeMarchLength * blueNoise * _BlueNoiseEffect;
#endif
    float3 currentPos = cameraPos + currentMarchLength * viewDir;
    float shapeMarchLength = _ShapeMarchLength;

#if _RENDERMODE_BAKE
    bool isBake = true;
#else
    bool isBake = false;
#endif

    float totalDensity = 0;
    float3 totalLumiance = 0;
    float lightAttenuation = 1.0;

    float densityTest = 0;
    float densityPrevious = 0;
    int densitySampleCountZero = 0;

    for(int marchNum = 0; marchNum < _ShapeMarchMax; marchNum++)
    {
        if(densityTest == 0 && !isBake)
        {
            currentMarchLength += _ShapeMarchLength * 2.0;
            currentPos = cameraPos + currentMarchLength * viewDir;

            if(dstToObj <= currentMarchLength || endPos <= currentMarchLength)
                break;

            dsi.position = currentPos;
            densityTest = SampleCloudDensity(dsi, true).density;

            if(densityTest > 0)
            {
                currentMarchLength -= _ShapeMarchLength;
            }
        }
        else
        {
            currentPos = cameraPos + currentMarchLength * viewDir;
            dsi.position = currentPos;
#ifdef _SHAPE_DETAIL_ON
            CloudInfo i = SampleCloudDensity(dsi, false);
#else
            CloudInfo i = SampleCloudDensity(dsi, true);
#endif

#if !_RENDERMODE_BAKE
            if(i.density == 0 && densityPrevious == 0)
            {
                densitySampleCountZero++;
                if(densitySampleCountZero >= 8)
                {
                    densityTest = 0;
                    densitySampleCountZero = 0;
                    continue;
                }
            }
#endif

#if _RENDERMODE_BAKE
            float density = i.density * shapeMarchLength;
#else
            float density = i.density * _ShapeMarchLength;
#endif
            float currentLumince = 0;

            if(density > 0.01)
            {
#if !_RENDERMODE_BAKE                
                float2 dstCloudLight = ResolveRayStartEnd(sphereCenter, EARTH_RADIUS, _CloudHeightRange.x, _CloudHeightRange.y, currentPos, lightDir, false);
                float lightMarchLength = dstCloudLight.y / _LightingMarchMax;
                float3 currentPosLight = currentPos;
                float totalDensityLight = 0;

                for(int marchNumLight =0; marchNumLight < _LightingMarchMax; marchNumLight++)
                {
                    currentPosLight += lightDir * lightMarchLength;
                    dsi.position = currentPosLight;
                    float densityLight = SampleCloudDensity(dsi, true).density * lightMarchLength;
                    totalDensityLight += densityLight;
                }

                currentLumince = BeerPowder(totalDensityLight, i.absorptivity);
#else
                currentLumince = i.lum;
#endif
                currentLumince = _DarknessThreshold + currentLumince * (1.0 - _DarknessThreshold);

                float3 cloudColor = Interpolation3(_ColorDark.rgb, _ColorCentral.rgb, _ColorBright.rgb, saturate(currentLumince), _ColorCentralOffset) * _MainLightColor;

                totalLumiance += lightAttenuation * cloudColor * density * phase;
                totalDensity += density;
                lightAttenuation *= Beer((density, i.absorptivity));

                if(lightAttenuation < 0.01)
                    break;
            }

#if _RENDERMODE_BAKE
            shapeMarchLength = max(_ShapeMarchLength, i.sdf * _SDFScale);
            if(density > 0.01 && isFirstSampleCloud)
            {
                shapeMarchLength *= blueNoise * _BlueNoiseEffect;
                isFirstSampleCloud = false;
            }

            currentMarchLength += shapeMarchLength;
#else
            currentMarchLength += _ShapeMarchLength;       
#endif
            if(dstToObj <= currentMarchLength || endPos <= currentMarchLength)
                break;

            densityPrevious = i.density;
        }
    }
    
    return half4(totalLumiance, lightAttenuation);
}

#endif