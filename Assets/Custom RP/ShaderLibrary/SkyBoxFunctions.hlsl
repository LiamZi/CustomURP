#ifndef __SHADER_LIBRARY_SKY_BOX_FUNCTIONS_HLSL__
#define __SHADER_LIBRARY_SKY_BOX_FUNCTIONS_HLSL__


// caclculates the mie scattering
half GetMiePhase(half eyeCos, half eyeCos2)
{
    half temp = 1.0 + MIE_G2 - 2.0 *  MIE_G * eyeCos;
    temp = pow(temp, pow(_SunSize, 0.65) * 10);
    temp = max(temp, 1.0e-4);
    temp = 1.5 * ((1.0 - MIE_G2) / (2.0 + MIE_G2)) * (1.0 + eyeCos2) / temp;
    return  temp;
}

//Rayleigh phase
half GetRayleighPhase(half eyeCos2)
{
    return 0.75 + 0.75 * eyeCos2;
}

half GetRayleighPhase(half3 light, half3 ray)
{
    half eyeCos = dot(light, ray);
    return GetRayleighPhase(eyeCos);
}

// sun shape
half CalcSunAttenuation(half3 lightPos, half3 ray)
{
    const half focusedEyeCos = pow(saturate(dot(lightPos, ray)), 5);
    return GetMiePhase(-focusedEyeCos, focusedEyeCos * focusedEyeCos);
}

float2 VoronoiRandomVector(float2 uv, float offset)
{
    float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
    uv = frac(sin(mul(uv, m)) * 46839.32);
    return float2(sin(uv.y *+ offset) * 0.5 + 0.5, cos(uv.x * offset) * 0.5 + 0.5);
}

void VoronoiNoise(float2 uv, float angleOffset, float cellDensity, out float output, out float cells)
{
    float2 g = floor(uv * cellDensity);
    float2 f = frac(uv * cellDensity);
    float t = 8.0;
    float3 res = float3(t, 0.0, 0.0);

    UNITY_LOOP
    for(int y = -1; y <= 1; y++)
    {
        for(int x = -1; x <= 1; x++)
        {
            float2 lattice = float2(x, y);
            float2 offset = VoronoiRandomVector(lattice + g, angleOffset);
            float d = distance(lattice + offset, f);
            if(d < res.x)
            {
                res = float3(d, offset.x, offset.y);
                output = res.x;
                cells = res.y;
            }
        }
    }
}

float SoftLight(float s, float d)
{
    return (s < 0.5) ? d - (1.0 - 2.0 * s) * d * (1.0 - d) 
            : (d < 0.25) ? d + (2.0 * s - 1.0) * d * ((16.0 * d - 12.0) * d + 3.0) 
            : d + (2.0 * s - 1.0) * (sqrt(d) - d); 
}

float3 SoftLight(float3 s, float3 d)
{
    return float3(SoftLight(s.x, d.x), SoftLight(s.y, d.y), SoftLight(s.z, d.z));
}


#endif