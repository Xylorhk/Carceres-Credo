#include "../Headers/HPacking.hlsl"
#include "../Includes/SpatialHash.hlsl"
#include "../Includes/VoxelTraversal.hlsl"
#include "../Includes/VoxelLightingEvaluation.hlsl"

#pragma once
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"

uint _HTraceReflectionsGI_JitterRadius;
uint _HTraceReflectionsGI_TemporalJitter;
uint _HTraceReflectionsGI_SpatialFilteringRadius;

float _HTraceReflectionsGI_RayBias;
float _HTraceReflectionsGI_MaxRayLength;

float3 HTraceIndirectLighting(uint2 pixCoord, float3 RayOriginWS, float3 NormalWS, float3 DiffuseColor)
{
	return float3(0, 0, 0);
}
