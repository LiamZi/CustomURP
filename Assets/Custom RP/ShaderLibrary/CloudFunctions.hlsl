#ifndef __SHADER_LIBRARY_CLOUD_FUNCTIONS_HLSL__
#define __SHADER_LIBRARY_CLOUD_FUNCTIONS_HLSL__

#define EARTH_RADIUS 6371000.0
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


float HeightPercent(float3 worldPos)
{
    float sqrMag = worldPos.x * worldPos.x + worldPos.z * worldPos.z;
    float heightOffset = EARTH_RADIUS - sqrt(max(0.0, EARTH_RADIUS * EARTH_RADIUS - sqrMag));
    return saturate((worldPos.y + heightOffset - _CloudStartHeight) / (_CloudEndHeight - _CloudStartHeight));
}

float Remap(float originalVal, float originalMin, float originalMax, float newMin, float newMax)
{
    return newMin + (((originalVal - originalMin) / (originalMax - originalMin)) * (newMax - newMin));
}

float RemapClamped(float originalVal, float originalMin, float originalMax, float newMin, float newMax)
{
    return newMin + (saturate((originalVal - originalMin) / (originalMax - originalMin)) * (newMax - newMin));
}

float3 ApplyWind(float3 worldPos)
{
    float heightPercent = HeightPercent(worldPos);
    
    worldPos.xz -=  (heightPercent) * _WindDirection.xy * _CloudTopOffset;

    worldPos.xz -= (_WindDirection.xy + float3(0.0, 1.0, 0.0)) * _Time.y * _WindDirection.z;
    worldPos.y -= _WindDirection.z * 0.4 * _Time.y;
    return worldPos;
    
}

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

float HenryGreenstein(float g, float cosTheta)
{
    float k = 3.0 / (8.0 * 3.1415926f) * (1.0 - g * g) / (2.0 + g * g);
    return k * (1.0 + cosTheta * cosTheta) / pow(abs(1.0 + g * g - 2.0 * g * cosTheta), 1.5);
}


float SampleDensity(float3 worldPos, int lod, bool cheap, out float wetness)
{
    float3 unwindWorldPos = worldPos;

    float4 coverageSampleUV = float4((unwindWorldPos.xz / _WeatherTexSize), 0, 0);
    coverageSampleUV.xy = (coverageSampleUV.xy + 0.5);
    float3 weatherData = SAMPLE_TEXTURE2D_LOD(_WeatherTex, sampler_WeatherTex, coverageSampleUV, coverageSampleUV.w);
    weatherData *= float3(_CloudCoverageModifier, 1.0, _CloudTypeModifier);
    float cloudCoverage = weatherData.r;
    float cloudType = weatherData.b;
    wetness = weatherData.g;

    //计算高度 [0, 1]
    float heightPercent = HeightPercent(worldPos);
    if(heightPercent <= 0.0f || heightPercent >= 1.0f)
    {
        return 0.0;
    }

    worldPos = ApplyWind(worldPos);
    float4 tempResult = SAMPLE_TEXTURE3D_LOD(_CloudTex, sampler_CloudTex, worldPos / _CloudSize * _CloudTile, lod).rgba;
    float sampleResult = ProcessCloudTex(tempResult);

    float2 densityAndErodeness = SAMPLE_TEXTURE2D_LOD(_HeightDensity, sampler_HeightDensity, float2(cloudType, heightPercent), 0.0).rg;
    sampleResult *= densityAndErodeness.x;

    sampleResult = ApplyCoverageToDensity(sampleResult, cloudCoverage);

    if(!cheap)
    {
        float2 curlNoise = SAMPLE_TEXTURE2D_LOD(_CurlNoise, sampler_CurlNoise, float2(unwindWorldPos.xz / _CloudSize * _CurlTile), 1.0).rg;
        worldPos.xz += curlNoise.rg * (1.0 - heightPercent) * _CloudSize * _CurlStrength;

        float3 tempResult2;
        tempResult2 = SAMPLE_TEXTURE3D_LOD(_DetailTex, sampler_DetailTex, worldPos / _CloudSize * _DetailTile, lod).rgb;
        float detailSampleResult = (tempResult2.r * 0.625) + (tempResult2.g * 0.25) + (tempResult2.b * 0.125);

        float detailModifier = lerp(1.0f - detailSampleResult, detailSampleResult, densityAndErodeness.y);
        sampleResult = RemapClamped(sampleResult, min(0.8, (1.0 - detailSampleResult) * _DetailStrength), 1.0, 0.0, 1.0);
    }
    else
    {
        sampleResult = RemapClamped(sampleResult, min(0.8, _DetailStrength * 0.5f), 1.0, 0.0, 1.0);
    }

    return max(0, sampleResult) * _CloudOverallDensity;
}


float SampleOpticsDistanceToSun(float3 worldPos, float4 worldLightPos)
{
    int mipmapOffset = 0.5;
    float opticsDistance = 0.0f;
    
    UNITY_UNROLL
    for(int i = 0; i < 5; i++)
    {
        half3 direction = worldLightPos;
        float3 samplePoint = worldPos + direction * ShadowSampleDistance[i] * TRANSMITTANCE_SAMPLE_STEP;
        float wetness;
        float sampleResult = SampleDensity(samplePoint, mipmapOffset, true, wetness);
        opticsDistance += ShadowSampleContribution[i] * TRANSMITTANCE_SAMPLE_STEP * sampleResult;
        mipmapOffset += 0.5;
    }

    return opticsDistance;
}


float SampleEnergy(float3 worldPos, float3 viewDir, float4 worldLightPos)
{
    float opticsDistance = SampleOpticsDistanceToSun(worldPos, worldLightPos);
    float result = 0.0f;
    float cosTheta = dot(viewDir, worldLightPos);
    
    UNITY_UNROLL
    for(int octaveIndex = 0; octaveIndex < 2; octaveIndex++)
    {
        float transmittance = exp(-_ExtinctionCoefficient * pow(_MultiScatteringB, octaveIndex) * opticsDistance);
        float ecMult = pow(_MultiScatteringC, octaveIndex);
        float phase = lerp(HenryGreenstein(0.1f * ecMult, cosTheta), HenryGreenstein((0.99 - _SilverSpread) * ecMult, cosTheta), 0.5);
        result += phase * transmittance * _ScatteringCoefficient * pow(_MultiScatteringA, octaveIndex);
    }

    return result;
}

bool RayTraceSphere(float3 center, float3 rd, float3 offset, float radius, out float t1, out float t2)
{
    float3 p = center - offset;
    float b = dot(p, rd);
    float c = dot(p, p) - (radius * radius);

    float f = b * b - c;
    if(f >= 0.0)
    {
        float dem = sqrt(f);
        t1 = -b - dem;
        t2 = -b + dem;
        return true;
    }

    return false;
}

bool ResolveRayStartEnd(float3 wsOrigin, float3 wsRay, out float start, out float end)
{
    float ot1, ot2, it1, it2;
    bool outIntersected = RayTraceSphere(wsOrigin, wsRay, EARTH_CENTER, EARTH_RADIUS + _CloudEndHeight, ot1, ot2);
    if(!outIntersected || ot2 < 0.0f) return false;

    bool inIntersected = RayTraceSphere(wsOrigin, wsRay, EARTH_CENTER, EARTH_RADIUS + _CloudStartHeight, it1, it2);
    if(inIntersected)
    {
        if(it1 * it2 < 0)
        {
            //we're on the ground
            start = max(it2, 0);
            end = ot2;
        }
        else
        {
            //we're inside atm , or above atm.
            if(ot1 * ot2 < 0) // inside atm
            {
                if(it1 > 0.0)
                {
                    end = it1;
                }
                else
                {
                    //look up
                    end = ot2;
                }
            }
            else  //outside atm
            {
                if(ot1 < 0.0)
                {
                    return false;
                }
                else
                {
                    start = ot1;
                    end = it1;
                }
            }
        }
    }
    else
    {
        end = ot2;
        start = max(ot1, 0);
    }
    return true;
}

void InitRayMarchStatus(inout RaymarchStatus result)
{
    result.intTransmittance = 1.0f;
    result.intensity = 0.0f;
    result.depthweightsum = 0.00001f;
    result.depth = 0.0f;
}

void IntegrateRaymarch(float3 startPos, float3 rayPos, float3 viewDir, float stepSize, float4 worldLightPos, inout RaymarchStatus result)
{
    float wetness;
    float density = SampleDensity(rayPos, 0, false, wetness);
    if(density <= 0.0f) return;

    float extinction = _ExtinctionCoefficient * density;
    float clampedExtinction = max(extinction, 1e-7);
    float transmittance = exp(-extinction * stepSize);

    float luminance = SampleEnergy(rayPos, viewDir, worldLightPos) * lerp(1.0f, 0.3f, wetness);
    float integScatt = (luminance - luminance * transmittance) / clampedExtinction;
    float depthWeight = result.intTransmittance;

    result.intensity += result.intTransmittance * integScatt;
    result.depth += depthWeight * length(rayPos - startPos);
    result.depthweightsum += depthWeight;
    result.intTransmittance *= transmittance;
}

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