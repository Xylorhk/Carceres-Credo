#ifndef MOTION_VEC_VERTEX_COMMON_INCLUDED
#define MOTION_VEC_VERTEX_COMMON_INCLUDED
 
// Available semantic start from TEXCOORD4
struct AttributesPass
{
    float3 previousPositionOS : TEXCOORD4; // Contain previous transform position (in case of skinning for example)
    #if defined (_ADD_PRECOMPUTED_VELOCITY)
    float3 precomputedVelocity    : TEXCOORD5; // Add Precomputed Velocity (Alembic computes velocities on runtime side).
    #endif
};

struct VaryingsPassToPS
{
    // Note: Z component is not use currently
    // This is the clip space position. Warning, do not confuse with the value of positionCS in PackedVarying which is SV_POSITION and store in positionSS
    float4 positionCS;
    float4 previousPositionCS;
};

// Available interpolator start from TEXCOORD8
struct PackedVaryingsPassToPS
{
    // Note: Z component is not use
    float3 interpolators0 : TEXCOORD8;
    float3 interpolators1 : TEXCOORD9;
};

PackedVaryingsPassToPS PackVaryingsPassToPS(VaryingsPassToPS input)
{
    PackedVaryingsPassToPS output;
    output.interpolators0 = float3(input.positionCS.xyw);
    output.interpolators1 = float3(input.previousPositionCS.xyw);

    return output;
}

VaryingsPassToPS UnpackVaryingsPassToPS(PackedVaryingsPassToPS input)
{
    VaryingsPassToPS output;
    output.positionCS = float4(input.interpolators0.xy, 0.0, input.interpolators0.z);
    output.previousPositionCS = float4(input.interpolators1.xy, 0.0, input.interpolators1.z);

    return output;
}

#ifdef TESSELLATION_ON

// Same as ToPS here
#define VaryingsPassToDS VaryingsPassToPS
#define PackedVaryingsPassToDS PackedVaryingsPassToPS
#define PackVaryingsPassToDS PackVaryingsPassToPS
#define UnpackVaryingsPassToDS UnpackVaryingsPassToPS

VaryingsPassToDS InterpolateWithBaryCoordsPassToDS(VaryingsPassToDS input0, VaryingsPassToDS input1, VaryingsPassToDS input2, float3 baryCoords)
{
    VaryingsPassToDS output;

    TESSELLATION_INTERPOLATE_BARY(positionCS, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(previousPositionCS, baryCoords);

    return output;
}

#endif // TESSELLATION_ON

#ifdef TESSELLATION_ON
#define VaryingsPassType VaryingsPassToDS
#else
#define VaryingsPassType VaryingsPassToPS
#endif

#define VARYINGS_NEED_PASS
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

void MotionVectorPositionZBias(VaryingsToPS input)
{
    #if UNITY_REVERSED_Z
    input.vmesh.positionCS.z -= unity_MotionVectorsParams.z * input.vmesh.positionCS.w;
    #else
    input.vmesh.positionCS.z += unity_MotionVectorsParams.z * input.vmesh.positionCS.w;
    #endif
}

PackedVaryingsType MotionVectorVS(inout VaryingsType varyingsType, AttributesMesh inputMesh)
{
    varyingsType.vpass.positionCS = mul(UNITY_MATRIX_UNJITTERED_VP, float4(varyingsType.vmesh.positionRWS, 1.0));
    float3 previousPositionRWS = TransformPreviousObjectToWorld(float3(inputMesh.uv2, inputMesh.uv3.x)); //We packed the prev pos in here
    varyingsType.vpass.previousPositionCS = mul(UNITY_MATRIX_PREV_VP, float4(previousPositionRWS, 1.0));
    return PackVaryingsType(varyingsType);
}

#endif
