#ifndef __SHADER_LIBRARY_FXAA_PASS_HLSL__
#define __SHADER_LIBRARY_FXAA_PASS_HLSL__


float4 _FXAAConfig;

#if defined(FXAA_QUALITY_LOW)
    #define EXTRA_EDGE_STEPS 3
    #define EDGE_STEP_SIZES 1.5, 2.0, 2.0
    #define LAST_EDGE_STEP_GUESS 8.0
#elif defined(FXAA_QUALITY_MEDIUM)
    #define EXTRA_EDGE_STEPS 8
    #define EDGE_STEP_SIZES 1.5, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 4.0
    #define LAST_EDGE_STEP_GUESS 8.0
#else
    #define EXTRA_EDGE_STEPS 10
    #define EDGE_STEP_SIZES 1.0, 1.0, 1.0, 1.0, 1.5, 2.0, 2.0, 2.0, 2.0, 4.0
    #define LAST_EDGE_STEP_GUESS 8.0
#endif
static const float edgeStepSizes[EXTRA_EDGE_STEPS] = { EDGE_STEP_SIZES };



struct LumaNeighborhood
{
    float m, n, e, s, w, ne, se, sw, nw;
    float highest, lowest, range;
};

struct FXAAEdge
{
    bool isHorizontal;
    float pixelStep;
    float lumaGradient;
    float otherLuma;
};

bool IsHorizontalEdge(LumaNeighborhood luma)
{
    float horizontal = 2.0 * abs(luma.n + luma.s - 2.0 * luma.m) + abs(luma.ne + luma.se - 2.0 * luma.e) + abs(luma.nw + luma.sw - 2.0 * luma.w);
    float vertical = 2.0 * abs(luma.e + luma.w - 2.0 * luma.m) + abs(luma.ne + luma.nw - 2.0 * luma.n) + abs(luma.se + luma.sw - 2.0 * luma.s);
    return horizontal >= vertical;
}

FXAAEdge GetFXAAEdge(LumaNeighborhood luma)
{
    FXAAEdge edge;
    float lumaP, lumaN;
    edge.isHorizontal = IsHorizontalEdge(luma);
    if(edge.isHorizontal)
    {
        edge.pixelStep = GetSourceTexelSize().y;
        lumaP = luma.n;
        lumaN = luma.s;
    }
    else
    {
        edge.pixelStep = GetSourceTexelSize().x;
        lumaP = luma.e;
        lumaN = luma.w;
    }

    float graidentP = abs(lumaP - luma.m);
    float graidentN = abs(lumaN - luma.m);

    if(graidentP < graidentN)
    {
        edge.pixelStep = -edge.pixelStep;
        edge.lumaGradient = graidentN;
        edge.otherLuma = lumaN;
    }
    else
    {
        edge.lumaGradient = graidentP;
        edge.otherLuma = lumaP;
    }

    return edge;
}

float GetLuma(float2 uv, float uOffset = 0.0, float vOffset = 0.0)
{
    uv += float2(uOffset , vOffset) * GetSourceTexelSize().xy;

#if defined(FXAA_ALPHA_CONTAINS_LUMA)
    return GetSource(uv).a;
#else
    return GetSource(uv).g;
#endif
}

/////////////////////////////
// +1    NW  N  NE
// +0     W  M  E
// -1    SW  S  SE
//       -1  0  1
/////////////////////////////

LumaNeighborhood GetLumaNeighborhood(float2 uv)
{
    LumaNeighborhood luma;
    luma.m = GetLuma(uv);
    luma.n = GetLuma(uv, 0.0, 1.0);
    luma.e = GetLuma(uv, 1.0, 0.0);
    luma.s = GetLuma(uv, 0.0, -1.0);
    luma.w = GetLuma(uv, -1.0, 0.0);
    luma.ne = GetLuma(uv, 1.0, 1.0);
    luma.se = GetLuma(uv, 1.0, -1.0);
    luma.sw = GetLuma(uv, -1.0, -1.0);
    luma.nw = GetLuma(uv, -1.0, 1.0);
    luma.highest = max(max(max(max(luma.m, luma.n), luma.e), luma.s), luma.w);
    luma.lowest = min(min(min(min(luma.m, luma.n), luma.e), luma.s), luma.w);
    luma.range = luma.highest - luma.lowest;
    return luma;
}

bool CanSkipFXAA(LumaNeighborhood luma)
{
    return luma.range <  max(_FXAAConfig.x, _FXAAConfig.y * luma.highest);
}


// Neighbor weights This acts like a 3x3 tent filter, without the middle
/////////////////////////////
//          1   2   1
//          2       2
//          1   2   1
/////////////////////////////

float GetSubPixelBlendFactor(LumaNeighborhood luma)
{
    float filter = 2.0 * (luma.n + luma.e + luma.s + luma.w);
    filter += luma.ne + luma.nw + luma.se + luma.sw;
    // filter *= 1.0 / 4;
    filter *= 1.0 / 12.0;  
    filter = abs(filter - luma.m);
    filter = saturate(filter / luma.range);
    filter = smoothstep(0, 1, filter);
    return filter * filter * _FXAAConfig.z;
}

float GetEdgeBlendFactor(LumaNeighborhood luma, FXAAEdge edge, float2 uv)
{
    float2 edgeUV = uv;
    float2 uvStep = 0.0;
    if(edge.isHorizontal)
    {
        edgeUV.y += 0.5 * edge.pixelStep;
        uvStep.x = GetSourceTexelSize().x;
    }
    else
    {
        edgeUV.x += 0.5 * edge.pixelStep;
        uvStep.y = GetSourceTexelSize().y;
    }

    float edgeLuma = 0.5 * (luma.m + edge.otherLuma);
    float gradientThreshold = 0.25 * edge.lumaGradient;

    float2 uvP = edgeUV + uvStep;
    // float lumaGradientP = abs(GetLuma(uvP) - edgeLuma);
    float lumaDeltaP = GetLuma(uvP) - edgeLuma;

    // bool atEndP = lumaGradientP >= gradientThreshold;
    bool atEndP = abs(lumaDeltaP) >= gradientThreshold;

    int i;
    UNITY_UNROLL
    for(i = 0; i < EXTRA_EDGE_STEPS && !atEndP; i++)
    {
        uvP += uvStep * edgeStepSizes[i];
        // lumaGradientP = abs(GetLuma(uvP) - edgeLuma);
        lumaDeltaP = GetLuma(uvP) - edgeLuma;
        // atEndP = lumaGradientP >= gradientThreshold;
        atEndP = abs(lumaDeltaP) >= gradientThreshold;
    }

    if(!atEndP)
    {
        uvP += uvStep * LAST_EDGE_STEP_GUESS;
    }

    float2 uvN = edgeUV - uvStep;
    // float lumaGradientN = abs(GetLuma(uvN) - edgeLuma);
    float lumaDeltaN = GetLuma(uvN) - edgeLuma;
    // bool atEndN = lumaGradientN >= gradientThreshold;
    bool atEndN = abs(lumaDeltaN) >= gradientThreshold;

    UNITY_UNROLL
    for(i = 0; i < EXTRA_EDGE_STEPS && !atEndN; i++)
    {
        uvN -= uvStep * edgeStepSizes[i];
        // lumaGradientN = abs(GetLuma(uvN) - edgeLuma);
        lumaDeltaN = GetLuma(uvN) - edgeLuma;
        // atEndN = lumaGradientN >= gradientThreshold;
        atEndN = abs(lumaDeltaN) >= gradientThreshold;
    }

    if(!atEndN)
    {
        uvN -= uvStep * LAST_EDGE_STEP_GUESS;
    }

    float distanceToEndP;
    float distanceToEndN;
    if(edge.isHorizontal)
    {
        distanceToEndP = uvP.x - uv.x;
        distanceToEndN = uv.x - uvN.x;
    }
    else
    {
        distanceToEndP = uvP.y - uv.y;
        distanceToEndN = uv.y - uvN.y;
    }

    float distanceToNearestEnd;
    bool deltaSign;
    if(distanceToEndP <= distanceToEndN)
    {
        distanceToNearestEnd = distanceToEndP;
        deltaSign = lumaDeltaP >= 0;
    }
    else
    {
        distanceToNearestEnd = distanceToEndN;
        deltaSign = lumaDeltaN >= 0;
    }
    // return edge.lumaGradient;
    // return atEndP;

    if(deltaSign == (luma.m - edgeLuma) >= 0)
    {
        return 0.0;
    }
    else
    {
        // return 10 * distanceToNearestEnd;
        return 0.5 - distanceToNearestEnd / (distanceToEndP + distanceToEndN);
    }

    
}


float4 FXAAPassFragment(Varyings input) : SV_TARGET
{
    LumaNeighborhood luma = GetLumaNeighborhood(input.screenUV);
    if(CanSkipFXAA(luma))
    {
        // return 0.0;
        return GetSource(input.screenUV);
    }
     
    // return luma.m;
    // return GetSubPixelBlendFactor(luma);
    FXAAEdge edge = GetFXAAEdge(luma);

    // float blendFactor = GetSubPixelBlendFactor(luma);
    float blendFactor = max(GetSubPixelBlendFactor(luma), GetEdgeBlendFactor(luma, edge, input.screenUV)) ;
    // return blendFactor;

    float2 blendUV = input.screenUV;
    if(edge.isHorizontal)
    {
        blendUV.y += blendFactor * edge.pixelStep;
    }
    else
    {
        blendUV.x += blendFactor * edge.pixelStep;
    }
    // return edge.isHorizontal ? float4(1.0, 0.0, 0.0, 1.0) : 1.0;
    // return edge.pixelStep > 0.0 ? float4(1.0, 0.0, 0.0, 1.0) : 1.0;
    return GetSource(blendUV);
}

#endif