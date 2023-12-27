#ifndef __SHADER_LIBRARY_CLOUD_FUNCTIONS_HLSL__
#define __SHADER_LIBRARY_CLOUD_FUNCTIONS_HLSL__

// #define EARTH_RADIUS 6371000.0
#define EARTH_RADIUS 6300000
#define EARTH_CENTER float3(0, -EARTH_RADIUS, 0)
#define TRANSMITTANCE_SAMPLE_STEP 512.0f

static const float ShadowSampleDistance[5] = { 0.5f, 1.5f, 3.0f, 6.0f, 12.0f };

static const float ShadowSampleContribution[5] = { 1.0f, 1.0f, 2.0f, 4.0f, 8.0f };

struct RaymarchStatus
{
    float intensity;
    float depth;
    float depthweightsum;
    float intTransmittance;
};

struct SamplingInfo
{
    float3 position;                
    float baseShapeTiling;         
    float3 baseShapeRatio;         
    float boundBoxScaleMax;         
    float3 boundBoxPosition;        
    float detailShapeTiling;        
    float weatherTexTiling;         
    float2 weatherTexOffset;        
    float baseShapeDetailEffect;  
    float detailEffect;            
    float densityMultiplier;       
    float cloudDensityAdjust;       
    float cloudAbsorbAdjust;        
    float3 windDirection;           
    float windSpeed;                
    float2 cloudHeightMinMax;      
    float3 stratusInfo;             
    float3 cumulusInfo;             
    float cloudOffsetLower;         
    float cloudOffsetUpper;         
    float feather;                  
    float3 sphereCenter;            
    float earthRadius;              
};

struct CloudInfo
{
    float density;          
    float absorptivity;     
    float sdf;              
    float lum;              
};

// float HeightPercent(float3 worldPos)
// {
//     float sqrMag = worldPos.x * worldPos.x + worldPos.z * worldPos.z;
//     float heightOffset = EARTH_RADIUS - sqrt(max(0.0, EARTH_RADIUS * EARTH_RADIUS - sqrMag));
//     return saturate((worldPos.y + heightOffset - _CloudStartHeight) / (_CloudEndHeight - _CloudStartHeight));
// }

float Remap(float originalVal, float originalMin, float originalMax, float newMin, float newMax)
{
    return newMin + (((originalVal - originalMin) / (originalMax - originalMin)) * (newMax - newMin));
}

float RemapClamped(float originalVal, float originalMin, float originalMax, float newMin, float newMax)
{
    return newMin + (saturate((originalVal - originalMin) / (originalMax - originalMin)) * (newMax - newMin));
}


float Beer(float density, float absorptivity = 1)
{
    return exp(-density * absorptivity);
}

float BeerPowder(float density, float absorptivity = 1)
{
    return 2.0 * exp(-density * absorptivity) * (1.0 - exp(-2.0 * density));
}

// float3 ApplyWind(float3 worldPos)
// {
//     float heightPercent = HeightPercent(worldPos);
//     
//     worldPos.xz -=  (heightPercent) * _WindDirection.xy * _CloudTopOffset;
//
//     worldPos.xz -= (_WindDirection.xy + float3(0.0, 1.0, 0.0)) * _Time.y * _WindDirection.z;
//     worldPos.y -= _WindDirection.z * 0.4 * _Time.y;
//     return worldPos;
//     
// }

float4 ProcessCloudTex(float4 texSample)
{
    texSample = saturate(texSample - 0.3f) / 0.7;
    float lowFreqFBM = (texSample.g * 0.625) + (texSample.b * 0.25) + (texSample.a * 0.125);
    float sampleResult = RemapClamped(lowFreqFBM, -0.3f * texSample.r, 1.0, 0.0, 1.0);
    return min(1.0f, sampleResult * 2.0f);
}

float ApplyCoverageToDensity(float sampleResult, float converage)
{
    sampleResult -= (1.0f - converage);
    return max(0.0f, sampleResult);
}

float HenyeyGreenstein(float g, float cosTheta)
{
    float k = 3.0 / (8.0 * 3.1415926f) * (1.0 - g * g) / (2.0 + g * g);
    return k * (1.0 + cosTheta * cosTheta) / pow(abs(1.0 + g * g - 2.0 * g * cosTheta), 1.5);
}

float HGScatterMax(float angle, float g_1, float intensity_1, float g_2, float intensity_2)
{
    return max(intensity_1 * HenyeyGreenstein(angle, g_1), intensity_2 * HenyeyGreenstein(angle, g_2));
}

float HGScatterLerp(float angle, float g_1, float g_2, float weight)
{
    return lerp(HenyeyGreenstein(angle, g_1), HenyeyGreenstein(angle, g_2), weight);
}

float GetLightEnergy(float density, float absorptivity, float darknessThreshold)
{
    float energy = BeerPowder(density, absorptivity);
    return darknessThreshold + (1.0 - darknessThreshold) * energy;
}


// float SampleDensity(float3 worldPos, int lod, bool cheap, out float wetness)
// {
//     float3 unwindWorldPos = worldPos;
//
//     float4 coverageSampleUV = float4((unwindWorldPos.xz / _WeatherTexSize), 0, 0);
//     coverageSampleUV.xy = (coverageSampleUV.xy + 0.5);
//     float3 weatherData = SAMPLE_TEXTURE2D_LOD(_WeatherTex, sampler_WeatherTex, coverageSampleUV, coverageSampleUV.w);
//     weatherData *= float3(_CloudCoverageModifier, 1.0, _CloudTypeModifier);
//     float cloudCoverage = weatherData.r;
//     float cloudType = weatherData.b;
//     wetness = weatherData.g;
//
//     //计算高度 [0, 1]
//     float heightPercent = HeightPercent(worldPos);
//     if(heightPercent <= 0.0f || heightPercent >= 1.0f)
//     {
//         return 0.0;
//     }
//
//     worldPos = ApplyWind(worldPos);
//     float4 tempResult = SAMPLE_TEXTURE3D_LOD(_CloudTex, sampler_CloudTex, worldPos / _CloudSize * _CloudTile, lod).rgba;
//     float sampleResult = ProcessCloudTex(tempResult);
//
//     float2 densityAndErodeness = SAMPLE_TEXTURE2D_LOD(_HeightDensity, sampler_HeightDensity, float2(cloudType, heightPercent), 0.0).rg;
//     sampleResult *= densityAndErodeness.x;
//
//     sampleResult = ApplyCoverageToDensity(sampleResult, cloudCoverage);
//
//     if(!cheap)
//     {
//         float2 curlNoise = SAMPLE_TEXTURE2D_LOD(_CurlNoise, sampler_CurlNoise, float2(unwindWorldPos.xz / _CloudSize * _CurlTile), 1.0).rg;
//         worldPos.xz += curlNoise.rg * (1.0 - heightPercent) * _CloudSize * _CurlStrength;
//
//         float3 tempResult2;
//         tempResult2 = SAMPLE_TEXTURE3D_LOD(_DetailTex, sampler_DetailTex, worldPos / _CloudSize * _DetailTile, lod).rgb;
//         float detailSampleResult = (tempResult2.r * 0.625) + (tempResult2.g * 0.25) + (tempResult2.b * 0.125);
//
//         float detailModifier = lerp(1.0f - detailSampleResult, detailSampleResult, densityAndErodeness.y);
//         sampleResult = RemapClamped(sampleResult, min(0.8, (1.0 - detailSampleResult) * _DetailStrength), 1.0, 0.0, 1.0);
//     }
//     else
//     {
//         sampleResult = RemapClamped(sampleResult, min(0.8, _DetailStrength * 0.5f), 1.0, 0.0, 1.0);
//     }
//
//     return max(0, sampleResult) * _CloudOverallDensity;
// }


// float SampleOpticsDistanceToSun(float3 worldPos, float4 worldLightPos)
// {
//     int mipmapOffset = 0.5;
//     float opticsDistance = 0.0f;
//     
//     UNITY_UNROLL
//     for(int i = 0; i < 5; i++)
//     {
//         half3 direction = worldLightPos;
//         float3 samplePoint = worldPos + direction * ShadowSampleDistance[i] * TRANSMITTANCE_SAMPLE_STEP;
//         float wetness;
//         float sampleResult = SampleDensity(samplePoint, mipmapOffset, true, wetness);
//         opticsDistance += ShadowSampleContribution[i] * TRANSMITTANCE_SAMPLE_STEP * sampleResult;
//         mipmapOffset += 0.5;
//     }
//
//     return opticsDistance;
// }


// float SampleEnergy(float3 worldPos, float3 viewDir, float4 worldLightPos)
// {
//     float opticsDistance = SampleOpticsDistanceToSun(worldPos, worldLightPos);
//     float result = 0.0f;
//     float cosTheta = dot(viewDir, worldLightPos);
//     
//     UNITY_UNROLL
//     for(int octaveIndex = 0; octaveIndex < 2; octaveIndex++)
//     {
//         float transmittance = exp(-_ExtinctionCoefficient * pow(_MultiScatteringB, octaveIndex) * opticsDistance);
//         float ecMult = pow(_MultiScatteringC, octaveIndex);
//         float phase = lerp(HenryGreenstein(0.1f * ecMult, cosTheta), HenryGreenstein((0.99 - _SilverSpread) * ecMult, cosTheta), 0.5);
//         result += phase * transmittance * _ScatteringCoefficient * pow(_MultiScatteringA, octaveIndex);
//     }
//
//     return result;
// }

float GetHeightFraction(float3 sphereCenter, float earthRadius, float3 pos, float height_min, float height_max)
{
    float height = length(pos - sphereCenter) - earthRadius;
    return(height - height_min) / (height_max - height_min);
}

float GetCloudTypeDensity(float heightFraction, float cloud_min, float cloud_max, float feather)
{
    return saturate(Remap(heightFraction, cloud_min, cloud_min + feather * 0.5, 0, 1)) * saturate(Remap(heightFraction, cloud_max - feather, cloud_max, 1, 0));
}

float Interpolation3(float value1, float value2, float value3, float x, float offset = 0.5)
{
    offset = clamp(offset, 0.0001, 0.9999);
    return lerp(lerp(value1, value2, min(x, offset) / offset), value3, max(0, x - offset) / (1.0 - offset));
}

float3 Interpolation3(float3 value1, float3 value2, float3 value3, float x, float offset = 0.5)
{
    offset = clamp(offset, 0.0001, 0.9999);
    return lerp(lerp(value1, value2, min(x, offset) / offset), value3, max(0, x - offset) / (1.0 - offset));
}


float2 RayBoxTest(float3 boxMin, float3 boxMax, float3 pos, float rayDir)
{
    float3 t0 = (boxMin - pos) / rayDir;
    float3 t1 = (boxMax - pos) / rayDir;

    float3 tmin = min(t0, t1);
    float3 tmax = max(t0, t1);

    float dstA = max(max(tmin.x, tmin.y), tmin.z);
    float dstB = min(min(tmax.x, tmax.y), tmax.z);

    float dstToBox = max(0, dstA);
    float dstInBox = max(0, dstB - dstA);

    return float2(dstToBox, dstInBox);
}

// bool RayTraceSphere(float3 center, float3 rd, float3 offset, float radius, out float t1, out float t2)
// {
//     float3 p = center - offset;
//     float b = dot(p, rd);
//     float c = dot(p, p) - (radius * radius);
//
//     float f = b * b - c;
//     if(f >= 0.0)
//     {
//         float dem = sqrt(f);
//         t1 = -b - dem;
//         t2 = -b + dem;
//         return true;
//     }
//
//     return false;
// }

float2 RaySphereDst(float3 sphereCenter, float sphereRadius, float3 pos, float3 rayDir)
{
    float3 oc = pos - sphereCenter;
    float b = dot(rayDir, oc);
    float c = dot(oc, oc) - sphereRadius * sphereRadius;
    float t = b * b - c;//t > 0有两个交点, = 0 相切， < 0 不相交
    
    float delta = sqrt(max(t, 0));
    float dstToSphere = max(-b - delta, 0);
    float dstInSphere = max(-b + delta - dstToSphere, 0);
    return float2(dstToSphere, dstInSphere);
}


float2 ResolveRayStartEnd(float3 sphereCenter, float earthRadius, float heightMin, float heightMax, float3 pos, float3 rayDir, bool isShape = true)
{
    float2 cloudDstMin = RaySphereDst(sphereCenter, heightMin + earthRadius, pos, rayDir);
    float2 cloudDstMax = RaySphereDst(sphereCenter, heightMax + earthRadius, pos, rayDir);
    
    //射线到云层的最近距离
    float dstToCloudLayer = 0;
    //射线穿过云层的距离
    float dstInCloudLayer = 0;
    
    //形状步进时计算相交
    if (isShape)
    {
        
        //在地表上
        if (pos.y <= heightMin)
        {
            float3 startPos = pos + rayDir * cloudDstMin.y;
            //开始位置在地平线以上时，设置距离
            if (startPos.y >= 0)
            {
                dstToCloudLayer = cloudDstMin.y;
                dstInCloudLayer = cloudDstMax.y - cloudDstMin.y;
            }
            return float2(dstToCloudLayer, dstInCloudLayer);
        }
        
        //在云层内
        if (pos.y > heightMin && pos.y <= heightMax)
        {
            dstToCloudLayer = 0;
            dstInCloudLayer = cloudDstMin.y > 0 ? cloudDstMin.x: cloudDstMax.y;
            return float2(dstToCloudLayer, dstInCloudLayer);
        }
        
        //在云层外
        dstToCloudLayer = cloudDstMax.x;
        dstInCloudLayer = cloudDstMin.y > 0 ? cloudDstMin.x - dstToCloudLayer: cloudDstMax.y;
    }
    else//光照步进时，步进开始点一定在云层内
    {
        dstToCloudLayer = 0;
        dstInCloudLayer = cloudDstMin.y > 0 ? cloudDstMin.x: cloudDstMax.y;
    }
    
    return float2(dstToCloudLayer, dstInCloudLayer);
}

float2 GetWeatherTexUV(float3 sphereCenter, float3 pos, float weatherTexTiling, float weatherTexRepair)
{
    float3 direction = normalize(pos - sphereCenter);
    float2 uv = pos.xz / pow(abs(direction.y), weatherTexRepair);
    return uv * weatherTexTiling;
}

int GetIndex(float2 uv, int width, int height, int iterationCount)
{
    int FrameOrder_2x2[] = {
        0, 2, 3, 1
    };
    int FrameOrder_4x4[] = {
        0, 8, 2, 10,
        12, 4, 14, 6,
        3, 11, 1, 9,
        15, 7, 13, 5
    };
    
    int x = floor(uv.x * width / 8) % iterationCount;
    int y = floor(uv.y * height / 8) % iterationCount;
    int index = x + y * iterationCount;
    
    if (iterationCount == 2)
    {
        index = FrameOrder_2x2[index];
    }
    if(iterationCount == 4)
    {
        index = FrameOrder_4x4[index];
    }
    return index;
}

CloudInfo SampleCloudDensity_No3DTex(SamplingInfo dsi)
{
    CloudInfo o;
    
    float heightFraction = GetHeightFraction(dsi.sphereCenter, dsi.earthRadius, dsi.position, dsi.cloudHeightMinMax.x, dsi.cloudHeightMinMax.y);
    
    float3 wind = dsi.windDirection * dsi.windSpeed * _Time.y;
    float3 position = dsi.position + wind * 100;
    
    float2 weatherTexUV = dsi.position.xz * dsi.weatherTexTiling;
    float4 weatherData = SAMPLE_TEXTURE2D_LOD(_WeatherTex, sampler_WeatherTex, weatherTexUV * 0.000001 + dsi.weatherTexOffset +wind.xz * 0.01, 0);
    weatherData.r = Interpolation3(0, weatherData.r, 1, dsi.cloudDensityAdjust);
    weatherData.b = saturate(weatherData.b + dsi.cloudOffsetLower);
    weatherData.a = saturate(weatherData.a + dsi.cloudOffsetUpper);
    float lowerLayerHeight = Interpolation3(weatherData.b, weatherData.b, 0, dsi.cloudDensityAdjust);//云底部的高度
    float upperLayerHeight = Interpolation3(weatherData.a, weatherData.a, 1, dsi.cloudDensityAdjust);//云顶部的高度
    if (weatherData.r <= 0)
    {
        o.density = 0;
        o.absorptivity = 1;
        return o;
    }
    
    float cloudDensity = GetCloudTypeDensity(heightFraction, min(lowerLayerHeight, upperLayerHeight), max(lowerLayerHeight, upperLayerHeight), dsi.feather);
    if (cloudDensity <= 0)
    {
        o.density = 0;
        o.absorptivity = 1;
        return o;
    }
    
    float cloudAbsorptivity = Interpolation3(0, weatherData.g, 1, dsi.cloudAbsorbAdjust);
    
    cloudDensity *= weatherData.r;
    
    o.density = cloudDensity * dsi.densityMultiplier * 0.01;
    o.absorptivity = cloudAbsorptivity;
    
    return o;
}


CloudInfo SampleCloudDensity_RealTime(SamplingInfo dsi, bool isCheaply = true)
{
    CloudInfo o;
    
    float heightFraction = GetHeightFraction(dsi.sphereCenter, dsi.earthRadius, dsi.position, dsi.cloudHeightMinMax.x, dsi.cloudHeightMinMax.y);
    
    float3 wind = dsi.windDirection * dsi.windSpeed * _Time.y;
    float3 position = dsi.position + wind * 100;
    
    float2 weatherTexUV = dsi.position.xz * dsi.weatherTexTiling;
    float4 weatherData = SAMPLE_TEXTURE2D_LOD(_WeatherTex, sampler_WeatherTex, weatherTexUV * 0.000001 + dsi.weatherTexOffset +wind.xz * 0.01, 0);
    weatherData.r = Interpolation3(0, weatherData.r, 1, dsi.cloudDensityAdjust);
    weatherData.b = Interpolation3(0, weatherData.b, 1, dsi.cloudDensityAdjust);
    if (weatherData.r <= 0)
    {
        o.density = 0;
        o.absorptivity = 1;
        return o;
    }
    
    float stratusDensity = GetCloudTypeDensity(heightFraction, dsi.stratusInfo.x, dsi.stratusInfo.y, dsi.stratusInfo.z);
    float cumulusDensity = GetCloudTypeDensity(heightFraction, dsi.cumulusInfo.x, dsi.cumulusInfo.y, dsi.cumulusInfo.z);
    float cloudTypeDensity = lerp(stratusDensity, cumulusDensity, weatherData.b);
    if (cloudTypeDensity <= 0)
    {
        o.density = 0;
        o.absorptivity = 1;
        return o;
    }
    
    float cloudAbsorptivity = Interpolation3(0, weatherData.g, 1, dsi.cloudAbsorbAdjust);
    

    float4 baseTex = SAMPLE_TEXTURE3D_LOD(_BaseShapeTex, sampler_BaseShapeTex, position * dsi.baseShapeTiling * 0.0001, 0);

    float baseTexFBM = dot(baseTex.gba, float3(0.5, 0.25, 0.125));

    float baseShape = Remap(baseTex.r, saturate((1.0 - baseTexFBM) * dsi.baseShapeDetailEffect), 1.0, 0, 1.0);
    
    float cloudDensity = baseShape * weatherData.r * cloudTypeDensity;
    

    if (cloudDensity > 0 && !isCheaply)
    {

        position += (dsi.windDirection + float3(0, 0.1, 0)) * dsi.windSpeed * _Time.y * 0.1;
        float3 detailTex = SAMPLE_TEXTURE3D_LOD(_DetailShapeTex, sampler_DetailShapeTex, position * dsi.detailShapeTiling * 0.0001, 0).rgb;
        float detailTexFBM = dot(detailTex, float3(0.5, 0.25, 0.125));
        

        float detailNoise = detailTexFBM;//lerp(detailTexFBM, 1.0 - detailTexFBM,saturate(heightFraction * 1.0));
        
        cloudDensity = Remap(cloudDensity, detailNoise * dsi.detailEffect, 1.0, 0.0, 1.0);
    }
    
    o.density = cloudDensity * dsi.densityMultiplier * 0.01;
    o.absorptivity = cloudAbsorptivity;
    
    return o;
}

CloudInfo SampleCloudDensity_Bake(SamplingInfo dsi)
{
    CloudInfo o;
   
    float3 position = dsi.position - dsi.boundBoxPosition - dsi.boundBoxScaleMax * dsi.baseShapeRatio / 2.0;
    position = position / dsi.baseShapeRatio / dsi.boundBoxScaleMax;
    
    float4 baseTex = SAMPLE_TEXTURE3D_LOD(_BaseShapeTex, sampler_BaseShapeTex, position, 0);

    o.density = baseTex.r * dsi.densityMultiplier * 0.02;
    o.sdf = baseTex.g * dsi.boundBoxScaleMax;
    o.lum = baseTex.b;
    o.absorptivity = dsi.cloudAbsorbAdjust;
    return o;
}

CloudInfo SampleCloudDensity(SamplingInfo dsi, bool isCheaply = true)
{
    #ifdef _RENDERMODE_REALTIME
    return SampleCloudDensity_RealTime(dsi, isCheaply);
    #elif _RENDERMODE_NO3DTEX
    return SampleCloudDensity_No3DTex(dsi);
    #else
    return SampleCloudDensity_Bake(dsi);
    #endif
}



// bool ResolveRayStartEnd(float3 wsOrigin, float3 wsRay,  out float start, out float end)
// {
//     float ot1, ot2, it1, it2;
//     bool outIntersected = RayTraceSphere(wsOrigin, wsRay, EARTH_CENTER, EARTH_RADIUS + _CloudEndHeight, ot1, ot2);
//     if(!outIntersected || ot2 < 0.0f) return false;
//
//     bool inIntersected = RayTraceSphere(wsOrigin, wsRay, EARTH_CENTER, EARTH_RADIUS + _CloudStartHeight, it1, it2);
//     if(inIntersected)
//     {
//         if(it1 * it2 < 0)
//         {
//             //we're on the ground
//             start = max(it2, 0);
//             end = ot2;
//         }
//         else
//         {
//             //we're inside atm , or above atm.
//             if(ot1 * ot2 < 0) // inside atm
//             {
//                 if(it1 > 0.0)
//                 {
//                     end = it1;
//                 }
//                 else
//                 {
//                     //look up
//                     end = ot2;
//                 }
//             }
//             else  //outside atm
//             {
//                 if(ot1 < 0.0)
//                 {
//                     return false;
//                 }
//                 else
//                 {
//                     start = ot1;
//                     end = it1;
//                 }
//             }
//         }
//     }
//     else
//     {
//         end = ot2;
//         start = max(ot1, 0);
//     }
//     return true;
// }

void InitRayMarchStatus(inout RaymarchStatus result)
{
    result.intTransmittance = 1.0f;
    result.intensity = 0.0f;
    result.depthweightsum = 0.00001f;
    result.depth = 0.0f;
}

// void IntegrateRaymarch(float3 startPos, float3 rayPos, float3 viewDir, float stepSize, float4 worldLightPos, inout RaymarchStatus result)
// {
//     float wetness;
//     float density = SampleDensity(rayPos, 0, false, wetness);
//     if(density <= 0.0f) return;
//
//     float extinction = _ExtinctionCoefficient * density;
//     float clampedExtinction = max(extinction, 1e-7);
//     float transmittance = exp(-extinction * stepSize);
//
//     float luminance = SampleEnergy(rayPos, viewDir, worldLightPos) * lerp(1.0f, 0.3f, wetness);
//     float integScatt = (luminance - luminance * transmittance) / clampedExtinction;
//     float depthWeight = result.intTransmittance;
//
//     result.intensity += result.intTransmittance * integScatt;
//     result.depth += depthWeight * length(rayPos - startPos);
//     result.depthweightsum += depthWeight;
//     result.intTransmittance *= transmittance;
// }

float GetRaymarchEndFromSceneDepth(float depth)
{
    float raymarchEnd = 0.0f;
#if defined(ALLOW_CLOUD_FRONT_OBJECT)
    if(depth == 1.0f)
    {
        raymarchEnd = 1e7;
    }
    else
    {
        raymarchEnd = depth * _ProjectionParams.z;
    }
#else
    raymarchEnd = 1e8;
#endif
    return raymarchEnd;
}

#endif