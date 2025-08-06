#pragma once

#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_COLOR

#define EXTRA_ATTRIBUTES 1

#define EVALUATE_TVE 0
#define EVALUATE_LIT 1
#define EVALUATE_UNLIT 1
#define EVALUATE_TERRAIN 1
#define EVALUATE_EMISSION 1
#define EVALUATE_SPEEDTREE 1
#define EVALUATE_LAYERED_LIT 0

#define AXIS_X 0
#define AXIS_Y 1
#define AXIS_Z 2

#include "../Headers/HPacking.hlsl"
#include "VoxelizationCommon.hlsl"
#include "VoxelMaterialEvaluation.hlsl" 

H_RW_TEXTURE3D(uint, _VoxelColor);

// Terrain instancing properties
float4 _TerrainHeightmapRecipSize;
float4 _TerrainHeightmapScale;  
UNITY_INSTANCING_BUFFER_START(Terrain)
UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)  
UNITY_INSTANCING_BUFFER_END(Terrain)
TEXTURE2D(_TerrainHeightmapTexture);

int _OffsetAxisIndex;
int2 _CullingTrim;
int2 _CullingTrimAxis;
float3 _AxisOffset;
float3 _OctantOffset;
float3 _VoxelCameraPosActual;


// ------------------------ SHARED STRUCTS ------------------------
struct VertexToGeometry
{
    float4 PositionCS      : POSITION;
    float4 TextureCoords   : TEXCOORD0;

    #ifdef PARTIAL_VOXELIZATION
    int CullingTest        : TEXCOORD1;
    #endif
    
    #if EXTRA_ATTRIBUTES
    float3 PositionWS      : TEXCOORD2;
    float3 PivotWS         : TEXCOORD3;
    float3 VertexColor     : COLOR0;
    #endif
};

struct GeometryToFragment
{
    float4 PositionCS      : POSITION;
    float4 TextureCoords   : TEXCOORD0;
    float Axis             : TEXCOORD1;

    #if EXTRA_ATTRIBUTES
    float3 PositionWS      : TEXCOORD2;
    float3 NormalWS        : TEXCOORD3;
    float3 PivotWS         : TEXCOORD4;
    float3 VertexColor     : COLOR0;
    #endif
};


// ------------------------ SHARED FUNCTIONS ------------------------
float3 SwizzleAxis(float3 Position, uint Axis)
{
    uint a = Axis + 1;
    float3 p = Position;
    Position.x = p[(0 + a) % 3];
    Position.y = p[(1 + a) % 3];
    Position.z = p[(2 + a) % 3];

    return Position;
}

float3 RestoreAxis(float3 Position, uint Axis)
{
    uint a = 2 - Axis;
    float3 p = Position;
    Position.x = p[(0 + a) % 3];
    Position.y = p[(1 + a) % 3];
    Position.z = p[(2 + a) % 3]; 
    
    return Position;
}

void ModifyForTerrainInstancing(inout AttributesMesh InputMesh)
{   
    float2 PatchVertex = InputMesh.positionOS.xy;
    float4 InstanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);

    float2 TextureCoord = (PatchVertex.xy + InstanceData.xy) * InstanceData.z;
    float HeightmapTexture = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(TextureCoord, 0)));
    float HolesTexture = _TerrainHolesTexture.Load(int3(TextureCoord, 0)).x;

    if (abs(HolesTexture.x - 0.5) == 0.5) //HolesTexture.x == 1 || HolesTexture.x == 0)
    {
        InputMesh.positionOS.xz = TextureCoord * _TerrainHeightmapScale.xz;
        InputMesh.positionOS.y = HeightmapTexture * _TerrainHeightmapScale.y;
        
        float4 Scale = InstanceData.z * _TerrainHeightmapRecipSize;
        float4 Offset = InstanceData.xyxy * Scale;
        Offset.xy += 0.5f * _TerrainHeightmapRecipSize.xy;
        InputMesh.uv0 = (PatchVertex.xy * Scale.zw + Offset.zw);

        InputMesh.positionOS.xyz = mul(GetRawUnityObjectToWorld(), float4(InputMesh.positionOS.xyz - _WorldSpaceCameraPos, 1.0)).xyz;
    }
    else
    {
       InputMesh.positionOS.xyz = mul(GetObjectToWorldMatrix(), float4(InputMesh.positionOS, 1.0)).xyz;
    }
}


// --- Vertex Stage ---
VertexToGeometry VoxelizationVert(AttributesMesh inputMesh)
{
    VertexToGeometry Output;

    float3 PositionWS;
    
    // Process instancing
    #ifdef UNITY_INSTANCING_ENABLED
    UNITY_SETUP_INSTANCE_ID(inputMesh);
    ModifyForTerrainInstancing(inputMesh);
    Output.PositionCS = mul(GetWorldToHClipMatrix(), float4(inputMesh.positionOS, 1.0));
    PositionWS = inputMesh.positionOS;
    #else
    Output.PositionCS = TransformObjectToHClip(inputMesh.positionOS);
    PositionWS = TransformObjectToWorld(inputMesh.positionOS);
    #endif

    // Output uv channels
    Output.TextureCoords = float4(inputMesh.uv0, inputMesh.uv1);
   
    #ifdef PARTIAL_VOXELIZATION
    // Transform world position to voxel coordinate
    float3 WorldPosition = PositionWS + (_WorldSpaceCameraPos - _VoxelCameraPosActual);
    int3 VoxelCoord = AbsoluteWorldPositionToVoxelCoord(WorldPosition);
    
    // Check if the vertex is behind culling camera
    Output.CullingTest = 0; 
    if ((VoxelCoord[_OffsetAxisIndex] < _CullingTrim.x) * _CullingTrimAxis.x || (VoxelCoord[_OffsetAxisIndex] > _CullingTrim.y + 1) * _CullingTrimAxis.y)
    Output.CullingTest = 1;
    #endif
    
    #ifdef UNITY_REVERSED_Z
    Output.PositionCS.z = mad(Output.PositionCS.z, -2.0, 1.0);
    #endif

    #if EXTRA_ATTRIBUTES
    Output.PositionWS = GetAbsolutePositionWS(PositionWS);
    Output.PivotWS = GetAbsolutePositionWS(float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w));
    //Alternative if unity complains about using the matrix
    //Output.PivotWS =  GetAbsolutePositionWS((TransformObjectToWorld(float3(0, 0, 0))));
    Output.VertexColor = inputMesh.color;
    #endif
    
    return Output;
}

// --- Geometry Stage ---
[maxvertexcount(3)]
void VoxelizationGeom(triangle VertexToGeometry i[3], inout TriangleStream<GeometryToFragment> Stream)
{
    // If all 3 vertices are behind culling camera - early out
    #ifdef PARTIAL_VOXELIZATION
    if (i[0].CullingTest + i[1].CullingTest + i[2].CullingTest == 3)
        return;
    #endif
    
    float3 Normal = normalize(abs(cross(i[1].PositionCS.xyz - i[0].PositionCS.xyz, i[2].PositionCS.xyz - i[0].PositionCS.xyz)));
    
    uint Axis = AXIS_Z;
    if  (Normal.x > Normal.y && Normal.x > Normal.z)
         Axis = AXIS_X;
    else if (Normal.y > Normal.x && Normal.y > Normal.z)
         Axis = AXIS_Y;
    
    [unroll]
    for (int j = 0; j < 3; j++)
    {
        GeometryToFragment Output;

        Output.PositionCS = float4(SwizzleAxis(i[j].PositionCS.xyz, Axis), 1); 

        #ifdef UNITY_UV_STARTS_AT_TOP
        Output.PositionCS.y = -Output.PositionCS.y;
        #endif
        
        #ifdef UNITY_REVERSED_Z
        Output.PositionCS.z = mad(Output.PositionCS.z, 0.5, 0.5);
        #endif
        
        Output.TextureCoords = i[j].TextureCoords;
        Output.Axis = Axis;
        
        #if EXTRA_ATTRIBUTES
        Output.VertexColor = i[j].VertexColor;
        Output.PositionWS = i[j].PositionWS;
        Output.PivotWS = i[j].PivotWS;
        Output.NormalWS = Normal;
        #endif
        
        Stream.Append(Output);
    }
}

// --- Fragment Stage ---
float VoxelizationFrag(GeometryToFragment Input) : SV_TARGET
{
    float VoxelRes = _VoxelResolution.x;

    #ifndef PARTIAL_VOXELIZATION
    VoxelRes = _VoxelResolution.x * 2;
    #endif
    
    float3 VoxelPos = float3(Input.PositionCS.x, Input.PositionCS.y, Input.PositionCS.z * VoxelRes);
    VoxelPos = RestoreAxis(VoxelPos, Input.Axis);
    
    // Modify Axes for non-cubic bounds
    VoxelPos.xyz = VoxelPos.xzy;
    VoxelPos.y *= (_VoxelBounds.z / _VoxelBounds.y);
    VoxelPos.xz = VoxelRes - VoxelPos.xz;

    // Calculate octants for the first 8 bits
    uint3 VoxelPosInt = floor(VoxelPos);
    uint BitShift = (1 * (VoxelPosInt.x % 2)) + (2 * (VoxelPosInt.y % 2)) + (4 * (VoxelPosInt.z % 2));
    uint OctantBits = (1 << BitShift) << 24;
    int StaticBitFlag = 1 << 23;

    int3 VoxelPosRounded = floor(VoxelPos / 2);
    
    #ifdef PARTIAL_VOXELIZATION
    //Offset by axis
    VoxelPosRounded.xyz += _AxisOffset.xyz; 

    // Culling trim
    if (VoxelPosRounded[_OffsetAxisIndex] < _CullingTrim.x || VoxelPosRounded[_OffsetAxisIndex] > _CullingTrim.y)
    return 0.0f;
    
    //Offset by octant
    VoxelPosRounded.xyz += _OctantOffset.xyz;
    #endif

    // Fill inout Surface Data with input information
    VoxelSurfaceData SurfaceData = (VoxelSurfaceData)0;
    SurfaceData.TexCoord0 = Input.TextureCoords.xy;
    SurfaceData.TexCoord1 = Input.TextureCoords.zw;
    
    #if EXTRA_ATTRIBUTES
    SurfaceData.VertexColor = Input.VertexColor;
    SurfaceData.PositionWS = Input.PositionWS;
    SurfaceData.NormalWS = Input.NormalWS;
    SurfaceData.PivotWS = Input.PivotWS;
    #endif
    
    // Evaluate all material attributes
    EvaluateSurfaceColor(SurfaceData);

    if (!SurfaceData.IsEmissive)
        SurfaceData.Color = ClampDiffuseColor(SurfaceData.Color);
            
    if (SurfaceData.Alpha == 1)
        OctantBits = 0;
    
    //SurfaceData.Color = Input.VertexColor;
    // Pack color for the last 24 bits
    uint PackedColor = PackVoxelColor(SurfaceData.Color, SurfaceData.IsEmissive);

    #ifdef DYNAMIC_VOXELIZATION
    uint OriginalValue;
    InterlockedCompareExchange(_VoxelColor[VoxelPosRounded], 0, 0, OriginalValue);
    
    if (((OriginalValue >> 23) & 0x1) != 1)
    {
        InterlockedOr(_VoxelColor[VoxelPosRounded], OctantBits, OriginalValue);  
        InterlockedMax(_VoxelColor[VoxelPosRounded], PackedColor | (OriginalValue & 0xFF000000) | OctantBits );
    }
    
    #else
    uint OriginalValue;
    InterlockedOr(_VoxelColor[VoxelPosRounded],  StaticBitFlag | OctantBits, OriginalValue);
    InterlockedMax(_VoxelColor[VoxelPosRounded], StaticBitFlag | PackedColor | (OriginalValue & 0xFF000000) | OctantBits);
    #endif
    
    return 0.0f;
}
