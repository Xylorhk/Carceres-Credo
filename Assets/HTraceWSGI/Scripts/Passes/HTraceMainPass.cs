using System;
using HTraceWSGI.Scripts.Globals;
using HTraceWSGI.Scripts.Pipeline;
using HTraceWSGI.Scripts.Structs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;

namespace HTraceWSGI.Scripts.Passes
{
    internal class HTraceMainPass : CustomPass
    {
        private const string HRenderAO_SHADER_NAME              = "HRenderAO";
        private const string HTracingScreenSpace_SHADER_NAME    = "HTracingScreenSpace";
        private const string HTracingWorldSpace_SHADER_NAME     = "HTracingWorldSpace";
        private const string HRadianceCache_SHADER_NAME         = "HRadianceCache";
        private const string HTemporalReprojection_SHADER_NAME  = "HTemporalReprojection";
        private const string HSpatialPrepass_SHADER_NAME        = "HSpatialPrepass";
        private const string HProbeAmbientOcclusion_SHADER_NAME = "HProbeAmbientOcclusion";
        private const string HFilterOctahedral_SHADER_NAME      = "HReSTIR";
        private const string HCopy_SHADER_NAME                  = "HCopy";
        private const string HRayGeneration_SHADER_NAME         = "HRayGeneration";
        private const string VoxelVisualization_SHADER_NAME     = "VoxelVisualization";
        private const string HReservoirValidation_SHADER_NAME   = "HReservoirValidation";
        private const string HDepthPyramid_SHADER_NAME          = "HDepthPyramid";
        private const string HDebugPassthrough_SHADER_NAME      = "HDebugPassthrough";
        private const string HInterpolation_SHADER_NAME         = "HInterpolation";
        private const string HTemporalDenoiser_SHADER_NAME      = "HTemporalDenoiser";
        
        #region SHADER PROPERTIES IDS ----------------------------->

        private static readonly int _DepthIntermediate        = Shader.PropertyToID("_DepthIntermediate");
        private static readonly int _DepthIntermediate_Output = Shader.PropertyToID("_DepthIntermediate_Output");
        private static readonly int _DepthPyramid_OutputMIP0  = Shader.PropertyToID("_DepthPyramid_OutputMIP0");
        private static readonly int _DepthPyramid_OutputMIP1  = Shader.PropertyToID("_DepthPyramid_OutputMIP1");
        private static readonly int _DepthPyramid_OutputMIP2  = Shader.PropertyToID("_DepthPyramid_OutputMIP2");
        private static readonly int _DepthPyramid_OutputMIP3  = Shader.PropertyToID("_DepthPyramid_OutputMIP3");
        private static readonly int _DepthPyramid_OutputMIP4  = Shader.PropertyToID("_DepthPyramid_OutputMIP4");
        private static readonly int _DepthPyramid_OutputMIP5  = Shader.PropertyToID("_DepthPyramid_OutputMIP5");
        private static readonly int _DepthPyramid_OutputMIP6  = Shader.PropertyToID("_DepthPyramid_OutputMIP6");
        private static readonly int _DepthPyramid_OutputMIP7  = Shader.PropertyToID("_DepthPyramid_OutputMIP7");
        private static readonly int _DepthPyramid_OutputMIP8  = Shader.PropertyToID("_DepthPyramid_OutputMIP8");

        private static readonly int _DepthPyramid                      = Shader.PropertyToID("_DepthPyramid");
        private static readonly int _BentNormalAmbientOcclusion_Output = Shader.PropertyToID("_BentNormalAmbientOcclusion_Output");
        private static readonly int _NormalDepthHalf_Output            = Shader.PropertyToID("_NormalDepthHalf_Output");
        private static readonly int _Camera_FOV                        = Shader.PropertyToID("_Camera_FOV");
        private static readonly int _AmbientOcclusion                  = Shader.PropertyToID("_AmbientOcclusion");
        private static readonly int _NormalDepthHalf                   = Shader.PropertyToID("_NormalDepthHalf");
        private static readonly int _BentNormalAO_Output               = Shader.PropertyToID("_BentNormalAO_Output");
        private static readonly int _GeometryNormal_Output             = Shader.PropertyToID("_GeometryNormal_Output");

        private static readonly int _PointDistribution_Output    = Shader.PropertyToID("_PointDistribution_Output");
        private static readonly int _SpatialOffsetsBuffer_Output = Shader.PropertyToID("_SpatialOffsetsBuffer_Output");

        private static readonly int _GeometryNormal          = Shader.PropertyToID("_GeometryNormal");
        private static readonly int _ProbeNormalDepth_Output = Shader.PropertyToID("_ProbeNormalDepth_Output");
        private static readonly int _ProbeDiffuse_Output     = Shader.PropertyToID("_ProbeDiffuse_Output");
        private static readonly int _SSAO                    = Shader.PropertyToID("_SSAO");
        private static readonly int _ProbeSSAO_Output        = Shader.PropertyToID("_ProbeSSAO_Output");
        private static readonly int _ProbeNormalDepth        = Shader.PropertyToID("_ProbeNormalDepth");

        private static readonly int _HistoryIndirection                   = Shader.PropertyToID("_HistoryIndirection");
        private static readonly int _ProbeWorldPosNormal_History          = Shader.PropertyToID("_ProbeWorldPosNormal_History");
        private static readonly int _ReprojectionCoords_Output            = Shader.PropertyToID("_ReprojectionCoords_Output");
        private static readonly int _ReprojectionWeights_Output           = Shader.PropertyToID("_ReprojectionWeights_Output");
        private static readonly int _PersistentReprojectionWeights_Output = Shader.PropertyToID("_PersistentReprojectionWeights_Output");
        private static readonly int _PersistentReprojectionCoord_Output   = Shader.PropertyToID("_PersistentReprojectionCoord_Output");

        private static readonly int _ReprojectionCoords           = Shader.PropertyToID("_ReprojectionCoords");
        private static readonly int _RayDirectionsJittered_Output = Shader.PropertyToID("_RayDirectionsJittered_Output");
        private static readonly int _IndirectCoordsSS_Output      = Shader.PropertyToID("_IndirectCoordsSS_Output");
        private static readonly int _IndirectCoordsOV_Output      = Shader.PropertyToID("_IndirectCoordsOV_Output");
        private static readonly int _IndirectCoordsSF_Output      = Shader.PropertyToID("_IndirectCoordsSF_Output");
        private static readonly int _RayCounter_Output            = Shader.PropertyToID("_RayCounter_Output");
        private static readonly int _RayCounter                   = Shader.PropertyToID("_RayCounter");
        private static readonly int _TracingCoords                = Shader.PropertyToID("_TracingCoords");
        private static readonly int _IndirectArguments_Output     = Shader.PropertyToID("_IndirectArguments_Output");
        private static readonly int _RayCounterIndex              = Shader.PropertyToID("_RayCounterIndex");

        private static readonly int _NormalDepth_History  = Shader.PropertyToID("_NormalDepth_History");
        private static readonly int _RayDirection         = Shader.PropertyToID("_RayDirection");
        private static readonly int _HitDistance_Output   = Shader.PropertyToID("_HitDistance_Output");
        private static readonly int _HitCoord_Output      = Shader.PropertyToID("_HitCoord_Output");
        private static readonly int _IndexXR              = Shader.PropertyToID("_IndexXR");
        private static readonly int _ColorPyramid_History = Shader.PropertyToID("_ColorPyramid_History");
        private static readonly int _Radiance_History     = Shader.PropertyToID("_Radiance_History");
        private static readonly int _HitCoord             = Shader.PropertyToID("_HitCoord");
        private static readonly int _HitRadiance_Output   = Shader.PropertyToID("_HitRadiance_Output");

        private static readonly int _HitDistance              = Shader.PropertyToID("_HitDistance");
        private static readonly int _TracingRayCounter_Output = Shader.PropertyToID("_TracingRayCounter_Output");
        private static readonly int _TracingCoords_Output     = Shader.PropertyToID("_TracingCoords_Output");
        private static readonly int _VoxelPayload_Output      = Shader.PropertyToID("_VoxelPayload_Output");
        private static readonly int _PointDistribution        = Shader.PropertyToID("_PointDistribution");
        private static readonly int _RayLength                = Shader.PropertyToID("_RayLength");
        private static readonly int _VoxelPayload             = Shader.PropertyToID("_VoxelPayload");
        private static readonly int _HashUpdateFrameIndex     = Shader.PropertyToID("_HashUpdateFrameIndex");
        private static readonly int _RadianceAtlas            = Shader.PropertyToID("_RadianceAtlas");

        private static readonly int _RayDistanceSS                        = Shader.PropertyToID("_RayDistanceSS");
        private static readonly int _RayDistanceWS                        = Shader.PropertyToID("_RayDistanceWS");
        private static readonly int _ReprojectionWeights                  = Shader.PropertyToID("_ReprojectionWeights");
        private static readonly int _PersistentReprojectionCoord          = Shader.PropertyToID("_PersistentReprojectionCoord");
        private static readonly int _ProbeAmbientOcclusion_History        = Shader.PropertyToID("_ProbeAmbientOcclusion_History");
        private static readonly int _ProbeAmbientOcclusion_Output         = Shader.PropertyToID("_ProbeAmbientOcclusion_Output");
        private static readonly int _ProbeNormalDepth_History             = Shader.PropertyToID("_ProbeNormalDepth_History");
        private static readonly int _SpatialOffsets_Output                = Shader.PropertyToID("_SpatialOffsets_Output");
        private static readonly int _SpatialWeights_Output                = Shader.PropertyToID("_SpatialWeights_Output");
        private static readonly int _SpatialOffsetsBuffer                 = Shader.PropertyToID("_SpatialOffsetsBuffer");
        private static readonly int _SpatialWeightsPacked                 = Shader.PropertyToID("_SpatialWeightsPacked");
        private static readonly int _SpatialOffsetsPacked                 = Shader.PropertyToID("_SpatialOffsetsPacked");
        private static readonly int _ProbeAmbientOcclusion                = Shader.PropertyToID("_ProbeAmbientOcclusion");
        private static readonly int _ProbeAmbientOcclusion_OutputFiltered = Shader.PropertyToID("_ProbeAmbientOcclusion_OutputFiltered");

        private static readonly int _ShadowGuidanceMask                 = Shader.PropertyToID("_ShadowGuidanceMask");
        private static readonly int _RayDistance                        = Shader.PropertyToID("_RayDistance");
        private static readonly int _ProbeDiffuse                       = Shader.PropertyToID("_ProbeDiffuse");
        private static readonly int _ReservoirAtlas_Output              = Shader.PropertyToID("_ReservoirAtlas_Output");
        private static readonly int _ReservoirAtlas_History             = Shader.PropertyToID("_ReservoirAtlas_History");
        private static readonly int _ReservoirAtlasRayData_Output       = Shader.PropertyToID("_ReservoirAtlasRayData_Output");
        private static readonly int _ReservoirAtlasRadianceData_Output  = Shader.PropertyToID("_ReservoirAtlasRadianceData_Output");
        private static readonly int _UseDiffuseWeight                   = Shader.PropertyToID("_UseDiffuseWeight");
        private static readonly int _PassNumber                         = Shader.PropertyToID("_PassNumber");
        private static readonly int _ReservoirAtlasRayData              = Shader.PropertyToID("_ReservoirAtlasRayData");
        private static readonly int _ReservoirAtlasRadianceData         = Shader.PropertyToID("_ReservoirAtlasRadianceData");
        private static readonly int _ShadowGuidanceMask_History         = Shader.PropertyToID("_ShadowGuidanceMask_History");
        private static readonly int _ShadowGuidanceMask_Output          = Shader.PropertyToID("_ShadowGuidanceMask_Output");
        private static readonly int _ReservoirAtlasRayData_Disocclusion = Shader.PropertyToID("_ReservoirAtlasRayData_Disocclusion");
        private static readonly int _ReservoirAtlas                     = Shader.PropertyToID("_ReservoirAtlas");
        private static readonly int _ReservoirAtlasRadianceData_Inout   = Shader.PropertyToID("_ReservoirAtlasRadianceData_Inout");
        private static readonly int _SampleCount_History                = Shader.PropertyToID("_SampleCount_History");
        private static readonly int _SampleCount_Output                 = Shader.PropertyToID("_SampleCount_Output");
        private static readonly int _SampleCount                        = Shader.PropertyToID("_SampleCount");
        private static readonly int _ReprojectionCoord                  = Shader.PropertyToID("_ReprojectionCoord");
        private static readonly int _HistoryArrayIndex                  = Shader.PropertyToID("_HistoryArrayIndex");
        private static readonly int _ProbeWorldPosNormal_HistoryOutput  = Shader.PropertyToID("_ProbeWorldPosNormal_HistoryOutput");

        private static readonly int _Temp              = Shader.PropertyToID("_Temp");
        private static readonly int _PackedSH_A_Output = Shader.PropertyToID("_PackedSH_A_Output");
        private static readonly int _PackedSH_B_Output = Shader.PropertyToID("_PackedSH_B_Output");
        private static readonly int _ProbeSSAO         = Shader.PropertyToID("_ProbeSSAO");
        private static readonly int _PackedSH_A        = Shader.PropertyToID("_PackedSH_A");
        private static readonly int _PackedSH_B        = Shader.PropertyToID("_PackedSH_B");
        private static readonly int _BentNormalsAO     = Shader.PropertyToID("_BentNormalsAO");
        private static readonly int _Radiance_Output   = Shader.PropertyToID("_Radiance_Output");
        private static readonly int _AO_Intensity      = Shader.PropertyToID("_AO_Intensity");

        private static readonly int _Radiance                  = Shader.PropertyToID("_Radiance");
        private static readonly int _LuminanceDelta_Output     = Shader.PropertyToID("_LuminanceDelta_Output");
        private static readonly int _LuminanceDelta_History    = Shader.PropertyToID("_LuminanceDelta_History");
        private static readonly int _Radiance_HistoryOutput    = Shader.PropertyToID("_Radiance_HistoryOutput");
        private static readonly int _NormalDepth_HistoryOutput = Shader.PropertyToID("_NormalDepth_HistoryOutput");
        
        private static readonly int _ShadowGuidanceMask_Samplecount               = Shader.PropertyToID("_ShadowGuidanceMask_Samplecount");
        private static readonly int _ShadowGuidanceMask_SamplecountHistoryOutput  = Shader.PropertyToID("_ShadowGuidanceMask_SamplecountHistoryOutput");
        private static readonly int _ShadowGuidanceMask_CheckerboardHistoryOutput = Shader.PropertyToID("_ShadowGuidanceMask_CheckerboardHistoryOutput");
        private static readonly int _ShadowGuidanceMask_Accumulated               = Shader.PropertyToID("_ShadowGuidanceMask_Accumulated");
        private static readonly int _ShadowGuidanceMask_HistoryOutput             = Shader.PropertyToID("_ShadowGuidanceMask_HistoryOutput");

        private static readonly int _StencilRef  = Shader.PropertyToID("_StencilRef");
        private static readonly int _StencilMask = Shader.PropertyToID("_StencilMask");

        private static readonly int _InputA               = Shader.PropertyToID("_InputA");
        private static readonly int _InputB               = Shader.PropertyToID("_InputB");
        private static readonly int _Output               = Shader.PropertyToID("_Output");
        private static readonly int _DebugCameraFrustum   = Shader.PropertyToID("_DebugCameraFrustum");
        private static readonly int _DebugRayDirection    = Shader.PropertyToID("_DebugRayDirection");
        private static readonly int _Visualization_Output = Shader.PropertyToID("_Visualization_Output");
        private static readonly int _MultibounceMode      = Shader.PropertyToID("_MultibounceMode");
        
        //Globals
        private static readonly int g_VoxelPositionPyramid      = Shader.PropertyToID("_VoxelPositionPyramid");
        private static readonly int g_HTraceShadowmap           = Shader.PropertyToID("_HTraceShadowmap");
        private static readonly int g_HTraceGBuffer0            = Shader.PropertyToID("g_HTraceGBuffer0");
        private static readonly int g_HTraceGBuffer3            = Shader.PropertyToID("g_HTraceGBuffer3");
        private static readonly int g_Gbuffertexture0           = Shader.PropertyToID("_GBufferTexture0");
        private static readonly int g_CustomMotionVectors       = Shader.PropertyToID("g_CustomMotionVectors");
        private static readonly int g_HTraceBufferGI            = Shader.PropertyToID("_HTraceBufferGI");
        private static readonly int g_TestCheckbox              = Shader.PropertyToID("_TestCheckbox");
        private static readonly int g_ProbeSize                 = Shader.PropertyToID("_ProbeSize");
        private static readonly int g_OctahedralSize            = Shader.PropertyToID("_OctahedralSize");
        private static readonly int g_HFrameIndex               = Shader.PropertyToID("_HFrameIndex");
        private static readonly int g_ReprojectSkippedFrame     = Shader.PropertyToID("_ReprojectSkippedFrame");
        private static readonly int g_PersistentHistorySamples  = Shader.PropertyToID("_PersistentHistorySamples");
        private static readonly int g_SkyOcclusionCone          = Shader.PropertyToID("_SkyOcclusionCone");
        private static readonly int g_DirectionalLightIntensity = Shader.PropertyToID("_DirectionalLightIntensity");
        private static readonly int g_SurfaceDiffuseIntensity   = Shader.PropertyToID("_SurfaceDiffuseIntensity");
        private static readonly int g_SkyLightIntensity         = Shader.PropertyToID("_SkyLightIntensity");
        private static readonly int g_HashBuffer_Key            = Shader.PropertyToID("_HashBuffer_Key");
        private static readonly int g_HashBuffer_Payload        = Shader.PropertyToID("_HashBuffer_Payload");
        private static readonly int g_HashBuffer_Counter        = Shader.PropertyToID("_HashBuffer_Counter");
        private static readonly int g_HashBuffer_Radiance       = Shader.PropertyToID("_HashBuffer_Radiance");
        private static readonly int g_HashBuffer_Position       = Shader.PropertyToID("_HashBuffer_Position");
        private static readonly int g_HashStorageSize           = Shader.PropertyToID("_HashStorageSize");
        private static readonly int g_HashUpdateFraction        = Shader.PropertyToID("_HashUpdateFraction");
        private static readonly int g_GeometryNormal            = Shader.PropertyToID("_GeometryNormal");

        #endregion  SHADER PROPERTIES IDS ----------------------------->

        private static readonly ProfilingSampler s_ComposeMotionVectorsProfilingSampler         = new ProfilingSampler("Compose Motion Vectors");
        private static readonly ProfilingSampler s_DepthPyramidGenerationProfilingSampler       = new ProfilingSampler("Depth Pyramid Generation");
        private static readonly ProfilingSampler s_RenderAmbientOcclsuionProfilingSampler       = new ProfilingSampler("Render Ambient Occlsuion");
        private static readonly ProfilingSampler s_ProbeGBufferDownsamplingProfilingSampler     = new ProfilingSampler("Probe GBuffer Downsampling");
        private static readonly ProfilingSampler s_ProbeTemporalReprojectionProfilingSampler    = new ProfilingSampler("Probe Temporal Reprojection");
        private static readonly ProfilingSampler s_RayGenerationProfilingSampler                = new ProfilingSampler("Ray Generation");
        private static readonly ProfilingSampler s_ClearTargetsProfilingSampler                 = new ProfilingSampler("Clear Targets");
        private static readonly ProfilingSampler s_ScreenSpaceLightingProfilingSampler          = new ProfilingSampler("Screen Space Lighting");
        private static readonly ProfilingSampler s_RayCompactionProfilingSampler                = new ProfilingSampler("Ray Compaction");
        private static readonly ProfilingSampler s_WorldSpaceLightingProfilingSampler           = new ProfilingSampler("World Space Lighting");
        private static readonly ProfilingSampler s_WorldSpaceTracingProfilingSampler            = new ProfilingSampler("World Space Tracing");
        private static readonly ProfilingSampler s_LightEvaluationProfilingSampler              = new ProfilingSampler("Light Evaluation");
        private static readonly ProfilingSampler s_RadianceCachingProfilingSampler              = new ProfilingSampler("Radiance Caching");
        private static readonly ProfilingSampler s_CacheTracingUpdateProfilingSampler           = new ProfilingSampler("Cache Tracing Update");
        private static readonly ProfilingSampler s_CacheLightEvaluationProfilingSampler         = new ProfilingSampler("Cache Light Evaluation");
        private static readonly ProfilingSampler s_PrimaryCacheSpawnProfilingSampler            = new ProfilingSampler("Primary Cache Spawn");
        private static readonly ProfilingSampler s_CacheDataUpdateProfilingSampler              = new ProfilingSampler("Cache Data Update");
        private static readonly ProfilingSampler s_SpatialPrepassProfilingSampler               = new ProfilingSampler("Spatial Prepass");
        private static readonly ProfilingSampler s_ReSTIRTemporalReuseProfilingSampler          = new ProfilingSampler("ReSTIR Temporal Reuse");
        private static readonly ProfilingSampler s_ReservoirOcclusionValidationProfilingSampler = new ProfilingSampler("Reservoir Occlusion Validation");
        private static readonly ProfilingSampler s_ReSTIRSpatialReuseProfilingSampler           = new ProfilingSampler("ReSTIR Spatial Reuse");
        private static readonly ProfilingSampler s_PersistentHistoryUpdateProfilingSampler      = new ProfilingSampler("Persistent History Update");
        private static readonly ProfilingSampler s_InterpolationProfilingSampler                = new ProfilingSampler("Interpolation");
        private static readonly ProfilingSampler s_TemporalDenoisingProfilingSampler            = new ProfilingSampler("Temporal Denoising");
        private static readonly ProfilingSampler s_SpatialCleanupProfilingSampler               = new ProfilingSampler("Spatial Cleanup");
        private static readonly ProfilingSampler s_CopyBuffersProfilingSampler                  = new ProfilingSampler("Copy Buffers");
        private static readonly ProfilingSampler s_DebugPassthroughProfilingSampler             = new ProfilingSampler("Debug Passthrough");
        private static readonly ProfilingSampler s_VisualizeVoxelsProfilingSampler              = new ProfilingSampler("Visualize Voxels");
        
        // Local variables
        private int HashStorageSize = 512000 * 2;
        private int HashUpdateFraction = 10;
        private int HashUpdateFrameIndex = 0;
        private int HFrameIndex = 0;
       
        private Vector3 PrevVoxelCameraPos = new Vector3(0, 0, 0);
        private Vector2Int PrevScreenResolution = new Vector2Int(0, 0);
        private Vector2Int DepthPyramidResolution = new Vector2Int(16, 16);
        private Matrix4x4 DepthMatrixPrev = new Matrix4x4();
        private Vector3 cameraPosistionPrev = new Vector3();

        private int  PersistentHistorySamples = 4;
        private int  _probeSize = 6;
        private int  _octahedralSize = 4;
        private int  _startFrameCounter = 0;
        private bool _firstFrame = true;
        private bool _prevDirectionalOcclusion;
        private bool _prevDebugModeEnabled;
        private RayCountMode _prevRayCountMode = RayCountMode.Performance;
        
        // Computes
        private ComputeShader VoxelVisualization = null;
        private ComputeShader HReservoirValidation = null;
        private ComputeShader HTracingScreenSpace = null;
        private ComputeShader HTracingWorldSpace = null;
        private ComputeShader HRadianceCache = null;
        private ComputeShader HTemporalReprojection = null;
        private ComputeShader HReSTIR = null;
        private ComputeShader HCopy = null;
        private ComputeShader HSpatialPrepass = null;
        private ComputeShader HProbeAmbientOcclusion = null;
        private ComputeShader HProbeAtlasAccumulation = null;
        private ComputeShader HRayGeneration = null;
        private ComputeShader HRenderAO = null;
        private ComputeShader HDepthPyramid = null;
        private ComputeShader HPrefilterTemporal = null;
        private ComputeShader HPrefilterSpatial = null;
        private ComputeShader HDebugPassthrough = null;
        private ComputeShader HInterpolation = null;
        private ComputeShader HTemporalDenoiser = null;

        // Materials, shaders, compute buffers
        Material MotionVectorsMaterial;
        Material VoxelVisualizationMaterial;
            
        // Indirection dispatch buffers
        ComputeBuffer RayCounter;
        ComputeBuffer RayCounterWS;
        ComputeBuffer IndirectArgumentsSS;
        ComputeBuffer IndirectArgumentsWS;
        ComputeBuffer IndirectArgumentsOV;
        ComputeBuffer IndirectArgumentsSF;
        ComputeBuffer IndirectCoordsSS;
        ComputeBuffer IndirectCoordsWS;
        ComputeBuffer IndirectCoordsOV;
        ComputeBuffer IndirectCoordsSF;
        
        // Spatial offsets buffers
        ComputeBuffer PointDistributionBuffer;
        ComputeBuffer SpatialOffsetsBuffer;
        
        // Hash buffers
        ComputeBuffer HashBuffer_Key;
        ComputeBuffer HashBuffer_Payload;
        ComputeBuffer HashBuffer_Counter;
        ComputeBuffer HashBuffer_Radiance;
        ComputeBuffer HashBuffer_Position;
        
        
        #region RT HADNLES ------------------------------------>
                
        // SSAO RT
        RTHandle ProbeSSAO;
        RTHandle NormalDepthHalf;
        RTHandle BentNormalsAO;
        RTHandle BentNormalsAO_Interpolated;
        RTHandle BentNormalsAO_History;
        RTHandle BentNormalsAO_Accumulated;
        RTHandle BentNormalsAO_Samplecount;
        RTHandle BentNormalsAO_SamplecountHistory;
            
        // TRACING RT
        RTHandle VoxelPayload;
        RTHandle RayDirections;
        RTHandle HitRadiance;
        RTHandle HitDistanceScreenSpace;
        RTHandle HitDistanceWorldSpace;
        RTHandle HitCoordScreenSpace;
        
        // PROBE AO RT
        RTHandle ProbeAmbientOcclusion;
        RTHandle ProbeAmbientOcclusion_History;
        RTHandle ProbeAmbientOcclusion_Filtered;
            
        // GBUFFER RT
        RTHandle CustomCameraMotionVectors;
        RTHandle GeometryNormal;
        RTHandle NormalDepth_History;
        RTHandle ProbeNormalDepth;
        RTHandle ProbeNormalDepth_History;
        RTHandle ProbeWorldPosNormal_History;
        RTHandle ProbeNormalDepth_Intermediate;
        RTHandle ProbeDiffuse;
        
        RTHandle DepthPyramid;
        RTHandle DepthIntermediate_Pyramid;
        
        // REPROJECTION RT
        RTHandle HistoryIndirection;
        RTHandle ReprojectionCoord;
        RTHandle PersistentReprojectionCoord;
        RTHandle ReprojectionWeights;
        RTHandle PersistentReprojectionWeights;
        
        // SPATIAL PREPASS RT
        RTHandle SpatialOffsetsPacked;
        RTHandle SpatialWeightsPacked;
            
        // RESERVOIR RT
        RTHandle ReservoirAtlas;
        RTHandle ReservoirAtlas_History;
        RTHandle ReservoirAtlasRadianceData_A;
        RTHandle ReservoirAtlasRadianceData_B;
        RTHandle ReservoirAtlasRadianceData_C;
        RTHandle ReservoirAtlasRayData_A;
        RTHandle ReservoirAtlasRayData_B;
        RTHandle ReservoirAtlasRayData_C;
            
        // SHADOW GUIDANCE MASK RT
        RTHandle ShadowGuidanceMask;
        RTHandle ShadowGuidanceMask_Accumulated;
        RTHandle ShadowGuidanceMask_Filtered;
        RTHandle ShadowGuidanceMask_History;
        RTHandle ShadowGuidanceMask_CheckerboardHistory;
        RTHandle ShadowGuidanceMask_Samplecount;
        RTHandle ShadowGuidanceMask_SamplecountHistory;
            
        // INTERPOLATION RT
        RTHandle PackedSH_A;
        RTHandle PackedSH_B;
        RTHandle Radiance_Interpolated;
        
        // TEMPORAL DENOISER RT
        RTHandle RadianceAccumulated;
        RTHandle RadianceAccumulated_History;
        RTHandle LuminanceDelta;
        RTHandle LuminanceDelta_History;
        
        // DEBUG RT
        RTHandle VoxelVisualizationRayDirections;
        RTHandle DebugOutput;
        
        RTHandle DummyBlack;
        
        RTHandle RadianceCacheFiltered;
        
        #endregion

        
        #region MATERIAL & RESOURCE LOAD --------------------->

            private void ResourcesLoad()
            {
                VoxelVisualization      = HExtensions.LoadComputeShader(VoxelVisualization_SHADER_NAME);
                HReservoirValidation    = HExtensions.LoadComputeShader(HReservoirValidation_SHADER_NAME);
                HTracingScreenSpace     = HExtensions.LoadComputeShader(HTracingScreenSpace_SHADER_NAME);
                HTracingWorldSpace      = HExtensions.LoadComputeShader(HTracingWorldSpace_SHADER_NAME);
                HRadianceCache          = HExtensions.LoadComputeShader(HRadianceCache_SHADER_NAME);
                HTemporalReprojection   = HExtensions.LoadComputeShader(HTemporalReprojection_SHADER_NAME);
                HSpatialPrepass         = HExtensions.LoadComputeShader(HSpatialPrepass_SHADER_NAME);
                HProbeAmbientOcclusion  = HExtensions.LoadComputeShader(HProbeAmbientOcclusion_SHADER_NAME);
                HReSTIR                 = HExtensions.LoadComputeShader(HFilterOctahedral_SHADER_NAME);
                HCopy                   = HExtensions.LoadComputeShader(HCopy_SHADER_NAME);
                HRayGeneration          = HExtensions.LoadComputeShader(HRayGeneration_SHADER_NAME);
                HRenderAO               = HExtensions.LoadComputeShader(HRenderAO_SHADER_NAME);
                HDepthPyramid           = HExtensions.LoadComputeShader(HDepthPyramid_SHADER_NAME);
                HDebugPassthrough       = HExtensions.LoadComputeShader(HDebugPassthrough_SHADER_NAME);
                HInterpolation          = HExtensions.LoadComputeShader(HInterpolation_SHADER_NAME);
                HTemporalDenoiser       = HExtensions.LoadComputeShader(HTemporalDenoiser_SHADER_NAME);
            }
            
            private void MaterialSetup()
            {
                MotionVectorsMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/HDRP/CameraMotionVectors"));
                VoxelVisualizationMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("HTrace/VoxelVisualization"));
            }

        #endregion

        
        #region TEXTURE AND BUFFER ALLOCATION --------------------------->

        private void AllocateMainRT(bool onlyRelease = false)
        {
            void ReleaseTextures()
            {
                HExtensions.HRelease(VoxelPayload);
                HExtensions.HRelease(RayDirections);
                HExtensions.HRelease(HitRadiance);
                HExtensions.HRelease(HitDistanceScreenSpace);
                HExtensions.HRelease(HitDistanceWorldSpace);
                HExtensions.HRelease(HitCoordScreenSpace);
                
                HExtensions.HRelease(ProbeAmbientOcclusion);
                HExtensions.HRelease(ProbeAmbientOcclusion_History);
                HExtensions.HRelease(ProbeAmbientOcclusion_Filtered);
                
                HExtensions.HRelease(CustomCameraMotionVectors);
                HExtensions.HRelease(GeometryNormal);
                HExtensions.HRelease(NormalDepth_History);
                HExtensions.HRelease(ProbeNormalDepth);
                HExtensions.HRelease(ProbeNormalDepth_History);
                HExtensions.HRelease(ProbeWorldPosNormal_History);
                HExtensions.HRelease(ProbeNormalDepth_Intermediate);
                HExtensions.HRelease(ProbeDiffuse);
                HExtensions.HRelease(DummyBlack);
                
                HExtensions.HRelease(HistoryIndirection);
                HExtensions.HRelease(ReprojectionWeights);
                HExtensions.HRelease(PersistentReprojectionWeights);
                HExtensions.HRelease(ReprojectionCoord);
                HExtensions.HRelease(PersistentReprojectionCoord);
                
                HExtensions.HRelease(SpatialOffsetsPacked);
                HExtensions.HRelease(SpatialWeightsPacked);
                
                HExtensions.HRelease(ReservoirAtlas);
                HExtensions.HRelease(ReservoirAtlas_History);
                HExtensions.HRelease(ReservoirAtlasRadianceData_A);
                HExtensions.HRelease(ReservoirAtlasRadianceData_B);
                HExtensions.HRelease(ReservoirAtlasRadianceData_C);
                HExtensions.HRelease(ReservoirAtlasRayData_A);
                HExtensions.HRelease(ReservoirAtlasRayData_B);
                HExtensions.HRelease(ReservoirAtlasRayData_C);
                
                HExtensions.HRelease(ShadowGuidanceMask);
                HExtensions.HRelease(ShadowGuidanceMask_Accumulated);
                HExtensions.HRelease(ShadowGuidanceMask_Filtered);
                HExtensions.HRelease(ShadowGuidanceMask_History);
                HExtensions.HRelease(ShadowGuidanceMask_CheckerboardHistory);
                HExtensions.HRelease(ShadowGuidanceMask_Samplecount);
                HExtensions.HRelease(ShadowGuidanceMask_SamplecountHistory);
                
                HExtensions.HRelease(PackedSH_A);
                HExtensions.HRelease(PackedSH_B);
                HExtensions.HRelease(Radiance_Interpolated);
                
                HExtensions.HRelease(RadianceAccumulated);
                HExtensions.HRelease(RadianceAccumulated_History);
                HExtensions.HRelease(LuminanceDelta);
                HExtensions.HRelease(LuminanceDelta_History);

                HExtensions.HRelease(RadianceCacheFiltered);
                
                HExtensions.HRelease(RayCounter);
                HExtensions.HRelease(RayCounterWS);
                HExtensions.HRelease(IndirectArgumentsSS);
                HExtensions.HRelease(IndirectArgumentsWS);
                HExtensions.HRelease(IndirectArgumentsOV);
                HExtensions.HRelease(IndirectArgumentsSF);

                HExtensions.HRelease(PointDistributionBuffer);
                HExtensions.HRelease(SpatialOffsetsBuffer);
                
                HExtensions.HRelease(HashBuffer_Key);
                HExtensions.HRelease(HashBuffer_Payload);
                HExtensions.HRelease(HashBuffer_Counter);
                HExtensions.HRelease(HashBuffer_Radiance);
                HExtensions.HRelease(HashBuffer_Position);
            }
            
            if (onlyRelease)
            {   
                ReleaseTextures();
                return;
            }

            ReleaseTextures();

            Vector2 FullRes       = Vector2.one;
            Vector2 HalfRes       = Vector2.one / 2;
            Vector2 ProbeRes      = Vector2.one / _probeSize;
            Vector2 ProbeAtlasRes = Vector2.one / (float)_probeSize * (float)_octahedralSize;

            // -------------------------------------- BUFFERS -------------------------------------- //
                
            // Indirection dispatch buffers
            RayCounter          = new ComputeBuffer(10 * TextureXR.slices, sizeof(uint));
            RayCounterWS        = new ComputeBuffer(10 * TextureXR.slices, sizeof(uint)); 
            IndirectArgumentsSS = new ComputeBuffer(3 * TextureXR.slices, sizeof(uint), ComputeBufferType.IndirectArguments);
            IndirectArgumentsWS = new ComputeBuffer(3 * TextureXR.slices, sizeof(uint), ComputeBufferType.IndirectArguments);
            IndirectArgumentsOV = new ComputeBuffer(3 * TextureXR.slices, sizeof(uint), ComputeBufferType.IndirectArguments);
            IndirectArgumentsSF = new ComputeBuffer(3 * TextureXR.slices, sizeof(uint), ComputeBufferType.IndirectArguments);
            
            // Spatial offsets buffers
            PointDistributionBuffer = new ComputeBuffer(TextureXR.slices * 32 * 4, 2 * sizeof(float));
            SpatialOffsetsBuffer    = new ComputeBuffer(9 * 9, 2 * sizeof(int));
            
            // Hash buffers
            HashBuffer_Key      = new ComputeBuffer(HashStorageSize, 1 * sizeof(uint));
            HashBuffer_Payload  = new ComputeBuffer(HashStorageSize / HashUpdateFraction, 2 * sizeof(uint)); 
            HashBuffer_Counter  = new ComputeBuffer(HashStorageSize, 1 * sizeof(uint));
            HashBuffer_Radiance = new ComputeBuffer(HashStorageSize, 4 * sizeof(uint)); 
            HashBuffer_Position = new ComputeBuffer(HashStorageSize, 4 * sizeof(uint));
            
            
            // -------------------------------------- TRACING RT -------------------------------------- //
            VoxelPayload = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_VoxelPayload", enableRandomWrite: true);
       
            RayDirections = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R8G8B8A8_UNorm, name: "_RayDirections", enableRandomWrite: true);  
            
            HitDistanceScreenSpace = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_UInt, name: "_HitDistanceScreenSpace", enableRandomWrite: true);
            
            HitDistanceWorldSpace = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_SFloat, name: "_HitDistanceWorldSpace", enableRandomWrite: true);

            HitRadiance = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_HitRadiance", enableRandomWrite: true);
            
            HitCoordScreenSpace = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension, useDynamicScale: true,
                colorFormat: GraphicsFormat.R16G16_UInt, name: "_HitCoordScreenSpace", enableRandomWrite: true);
            
            
            // -------------------------------------- PROBE AO RT -------------------------------------- //
            
            ProbeAmbientOcclusion = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_UInt, name: "_ProbeAmbientOcclusion", enableRandomWrite: true);
            
            ProbeAmbientOcclusion_History = RTHandles.Alloc(ProbeRes, TextureXR.slices * PersistentHistorySamples, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_UInt, name: "_ProbeAmbientOcclusion_History", enableRandomWrite: true);
            
            ProbeAmbientOcclusion_Filtered = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R8_UNorm, name: "_ProbeAmbientOcclusion_Filtered", enableRandomWrite: true);

            
            // -------------------------------------- GBUFFER RT -------------------------------------- //
            
            CustomCameraMotionVectors = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16_SFloat, name: "_CustomCameraMotionVectors", enableRandomWrite: true);

            GeometryNormal = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_GeometryNormal", enableRandomWrite: true);

            NormalDepth_History = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_NormalDepth_History", enableRandomWrite: true);

            ProbeNormalDepth = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_ProbeNormalDepth", enableRandomWrite: true);
            
            ProbeNormalDepth_History = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_ProbeNormalDepth_History", enableRandomWrite: true);
            
            ProbeWorldPosNormal_History = RTHandles.Alloc(ProbeRes, TextureXR.slices * PersistentHistorySamples, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32B32A32_UInt, name: "_ProbeWorldPosNormal_History", enableRandomWrite: true);

            ProbeNormalDepth_Intermediate = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_ProbeNormalDepth_Intermediate", enableRandomWrite: true);
        
            ProbeDiffuse = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R8G8B8A8_UNorm, name: "_ProbeDiffuse", enableRandomWrite: true);
            
            DummyBlack = RTHandles.Alloc(4, 4, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R8_UNorm, name: "_DummyBlack", enableRandomWrite: true);

                
            // -------------------------------------- REPROJECTION RT -------------------------------------- //
            
            HistoryIndirection = RTHandles.Alloc(ProbeRes, TextureXR.slices * PersistentHistorySamples, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16_UInt, name: "_HistoryIndirection", enableRandomWrite: true);

            ReprojectionWeights = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R8G8B8A8_UNorm, name: "_ReprojectionWeights", enableRandomWrite: true);
                
            PersistentReprojectionWeights = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R8G8B8A8_UNorm, name: "_PersistentReprojectionWeights", enableRandomWrite: true);

            ReprojectionCoord = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16_UInt, name: "_ReprojectionCoord", enableRandomWrite: true);

            PersistentReprojectionCoord = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16_UInt, name: "_PersistentReprojectionCoord", enableRandomWrite: true);

            
            // -------------------------------------- SPATIAL PREPASS RT -------------------------------------- //
            
            SpatialOffsetsPacked = RTHandles.Alloc(ProbeRes, TextureXR.slices * 4, dimension: TextureXR.dimension, useDynamicScale: true,
                colorFormat: GraphicsFormat.R32G32B32A32_UInt, name: "_SpatialOffsetsPacked", enableRandomWrite: true);
            
            SpatialWeightsPacked = RTHandles.Alloc(ProbeRes, TextureXR.slices * 4, dimension: TextureXR.dimension, useDynamicScale: true,
                colorFormat: GraphicsFormat.R16G16B16A16_UInt, name: "_SpatialWeightsPacked", enableRandomWrite: true);


            // -------------------------------------- RESERVOIR RT -------------------------------------- //
            
            ReservoirAtlas = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32B32A32_UInt, name: "_ReservoirAtlas", enableRandomWrite: true);
            
            ReservoirAtlas_History = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices * PersistentHistorySamples, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32B32A32_UInt, name: "_ReservoirAtlas_History", enableRandomWrite: true);
            
            ReservoirAtlasRadianceData_A = RTHandles.Alloc(ProbeAtlasRes , TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_ReservoirAtlasRadianceData_A", enableRandomWrite: true);
            
            ReservoirAtlasRadianceData_B = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_ReservoirAtlasRadianceData_B", enableRandomWrite: true);
            
            ReservoirAtlasRadianceData_C = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32_UInt, name: "_ReservoirAtlasRadianceData_C", enableRandomWrite: true);
            
            ReservoirAtlasRayData_A = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R32_UInt, name: "_ReservoirAtlasRayData_A", enableRandomWrite: true);
            
            ReservoirAtlasRayData_B = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices * PersistentHistorySamples, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R32_UInt, name: "_ReservoirAtlasRayData_B", enableRandomWrite: true);
            
            ReservoirAtlasRayData_C = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R32_UInt, name: "_ReservoirAtlasRayData_C", enableRandomWrite: true);


            
            // -------------------------------------- SHADOW GUIDANCE MASK RT -------------------------------------- //
            
            ShadowGuidanceMask = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices , dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R8_UNorm, name: "_ShadowGuidanceMask", enableRandomWrite: true);
            
            ShadowGuidanceMask_Accumulated = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R8_UNorm, name: "_ShadowGuidanceMask_Accumulated", enableRandomWrite: true);
                
            ShadowGuidanceMask_Filtered  = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R8_UNorm, name: "_ShadowGuidanceMask_Filtered", enableRandomWrite: true);

            ShadowGuidanceMask_History = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R8_UNorm, name: "_ShadowGuidanceMask_History", enableRandomWrite: true);
            
            ShadowGuidanceMask_CheckerboardHistory = RTHandles.Alloc(ProbeAtlasRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R8_UNorm, name: "_ShadowGuidanceMask_CheckerboardHistory", enableRandomWrite: true); 
            
            ShadowGuidanceMask_Samplecount = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_SFloat, name: "_ShadowGuidanceMask_Samplecount", enableRandomWrite: true);
            
            ShadowGuidanceMask_SamplecountHistory = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_SFloat, name: "_ShadowGuidanceMask_SamplecountHistory", enableRandomWrite: true);

            
            // -------------------------------------- INTERPOLATION RT -------------------------------------- //
            
            PackedSH_A = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32B32A32_UInt, name: "_PackedSH_A", enableRandomWrite: true);
            
            PackedSH_B = RTHandles.Alloc(ProbeRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32G32B32A32_UInt, name: "_PackedSH_B", enableRandomWrite: true);
            
            Radiance_Interpolated = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.R32_UInt, name: "_Radiance_Interpolated", enableRandomWrite: true); 
            
            
            // -------------------------------------- TEMPORAL DENOISER RT -------------------------------------- //
           
            RadianceAccumulated = RTHandles.Alloc(FullRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_RadianceAccumulated", enableRandomWrite: true);
            
            RadianceAccumulated_History = RTHandles.Alloc(FullRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_RadianceAccumulated_History", enableRandomWrite: true);
            
            LuminanceDelta = RTHandles.Alloc(FullRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_SFloat, name: "_RadianceLumaDelta", enableRandomWrite: true);
            
            LuminanceDelta_History = RTHandles.Alloc(FullRes, TextureXR.slices, dimension:  TextureXR.dimension,
                colorFormat: GraphicsFormat.R16_SFloat, name: "_RadianceLumaDelta_History", enableRandomWrite: true);
            
            // TODO: figure out if we need this, do not delete and do not allocate for now
            // RadianceCacheFiltered = RTHandles.Alloc(600, 100, 100, dimension: TextureDimension.Tex3D,
            //     colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, name: "_RadianceCacheFiltered", enableRandomWrite: true);
        }

        private void AllocateDepthPyramidRT(Vector2Int PyramidRes, bool onlyRelease = false)
        {
            void ReleaseTextures()
            {
                HExtensions.HRelease(DepthPyramid);
                HExtensions.HRelease(DepthIntermediate_Pyramid);
            }
            
            if (onlyRelease)
            {   
                ReleaseTextures();
                return;
            }

            ReleaseTextures();

            DepthPyramid = RTHandles.Alloc(PyramidRes.x, PyramidRes.y, TextureXR.slices, dimension: TextureDimension.Tex2DArray, useMipMap: true, autoGenerateMips: false,
                colorFormat: GraphicsFormat.R16_SFloat, name: "_DepthPyramid", enableRandomWrite: true);
            
            DepthIntermediate_Pyramid = RTHandles.Alloc(PyramidRes.x / 16, PyramidRes.y / 16, TextureXR.slices, dimension: TextureDimension.Tex2DArray,
                colorFormat: GraphicsFormat.R16_SFloat, name: "_DepthIntermediate_Pyramid", enableRandomWrite: true);
        }

        private void AllocateSSAO_RT(bool onlyRelease = false)
        {
            void ReleaseTextures()
            {
                HExtensions.HRelease(ProbeSSAO);
                HExtensions.HRelease(NormalDepthHalf);
                HExtensions.HRelease(BentNormalsAO);
                HExtensions.HRelease(BentNormalsAO_Interpolated);
                HExtensions.HRelease(BentNormalsAO_History);
                HExtensions.HRelease(BentNormalsAO_Accumulated);
                HExtensions.HRelease(BentNormalsAO_Samplecount);
                HExtensions.HRelease(BentNormalsAO_SamplecountHistory);
            }
            
            if (onlyRelease)
            {   
                ReleaseTextures();
                return;
            }

            ReleaseTextures();
            
            Vector2 fullRes = Vector2.one;
            Vector2 halfRes = Vector2.one / 2;
            Vector2 probeSize = Vector2.one / _probeSize;
       
            // -------------------------------------- SSAO RT -------------------------------------- //

            if (HResources.ScreenSpaceLightingData.DirectionalOcclusion)
            {
                ProbeSSAO = RTHandles.Alloc(probeSize, TextureXR.slices, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R8_UNorm, name: "_ProbeSSAO", enableRandomWrite: true);
                
                BentNormalsAO = RTHandles.Alloc(fullRes, TextureXR.slices, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_BentNormalsAO", enableRandomWrite: true);
                
                BentNormalsAO_Interpolated = RTHandles.Alloc(fullRes, TextureXR.slices, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_BentNormalsAO_Interpolated", enableRandomWrite: true);
                
                NormalDepthHalf = RTHandles.Alloc(fullRes / 2, TextureXR.slices, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_NormalDepthHalf", enableRandomWrite: true);

                // TODO: porbably we will no need these, leave them for now, but don't allocate.
                // BentNormalsAO_History = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                //     colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_BentNormalsAO_History", enableRandomWrite: true);
                //
                // BentNormalsAO_Accumulated = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                //     colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_BentNormalsAO_Accumulated", enableRandomWrite: true);
                //
                // BentNormalsAO_SamplecountHistory = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                //     colorFormat: GraphicsFormat.R8_UInt, name: "_BentNormalsAO_SamplecountHistory", enableRandomWrite: true);
                //
                // BentNormalsAO_Samplecount = RTHandles.Alloc(FullRes, TextureXR.slices, dimension: TextureXR.dimension,
                //     colorFormat: GraphicsFormat.R8_UInt, name: "_BentNormalsAO_Samplecount", enableRandomWrite: true);
            }
            else //Warnings suppression
            {
                ProbeSSAO = RTHandles.Alloc(1,1, 1, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R8_UNorm, name: "_ProbeSSAO", enableRandomWrite: true);
                
                BentNormalsAO = RTHandles.Alloc(1,1, 1, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_BentNormalsAO", enableRandomWrite: true);
                
                BentNormalsAO_Interpolated = RTHandles.Alloc(1,1, 1, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_BentNormalsAO_Interpolated", enableRandomWrite: true);
                
                NormalDepthHalf = RTHandles.Alloc(1,1, 1, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_NormalDepthHalf", enableRandomWrite: true);
            }
        }

        private void AllocateDebugRT(bool onlyRelease = false)
        {
            void ReleaseTextures()
            {
                HExtensions.HRelease(VoxelVisualizationRayDirections);
                HExtensions.HRelease(DebugOutput);
            }
            
            if (onlyRelease)
            {   
                ReleaseTextures();
                return;
            }

            ReleaseTextures();
            
            Vector2 fullRes = Vector2.one;
            
            // -------------------------------------- DEBUG RT -------------------------------------- //

            if (HResources.GeneralData.DebugModeWS != DebugModeWS.None)
            {
                VoxelVisualizationRayDirections = RTHandles.Alloc(fullRes, TextureXR.slices, dimension: TextureXR.dimension, useDynamicScale: true,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, name: "_VoxelVisualizationRayDirections", enableRandomWrite: true);
            
                DebugOutput = RTHandles.Alloc(fullRes,  TextureXR.slices, dimension: TextureDimension.Tex2DArray,
                    colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, name: "_DebugOutput", enableRandomWrite: true);
            }
        }

        private void AllocateIndirectionBuffers(Vector2Int resolution, bool onlyRelease = false)
        {
            void ReleaseTextures()
            {
                HExtensions.HRelease(IndirectCoordsSS);
                HExtensions.HRelease(IndirectCoordsWS);
                HExtensions.HRelease(IndirectCoordsOV);
                HExtensions.HRelease(IndirectCoordsSF);
            }
            
            if (onlyRelease)
            {   
                ReleaseTextures();
                return;
            }
            
            ReleaseTextures();
            
            // -------------------------------------- BUFFERS -------------------------------------- //

            int resolutionMul = resolution.x * resolution.y;
            IndirectCoordsSS = new ComputeBuffer(resolutionMul * TextureXR.slices, 2 * sizeof(uint));
            IndirectCoordsWS = new ComputeBuffer(resolutionMul * TextureXR.slices, 2 * sizeof(uint));
            IndirectCoordsOV = new ComputeBuffer(resolutionMul * TextureXR.slices, 2 * sizeof(uint));
            IndirectCoordsSF = new ComputeBuffer(resolutionMul * TextureXR.slices, 2 * sizeof(uint));
        }

        private void ReallocHashBuffers(bool onlyRelease = false)
        {
            void ReleaseTextures()
            {
                HExtensions.HRelease(HashBuffer_Key);
                HExtensions.HRelease(HashBuffer_Payload);
                HExtensions.HRelease(HashBuffer_Counter);
                HExtensions.HRelease(HashBuffer_Radiance);
                HExtensions.HRelease(HashBuffer_Position);
            }
            
            if (onlyRelease)
            {   
                ReleaseTextures();
                return;
            }

            ReleaseTextures();

            HashBuffer_Key      = new ComputeBuffer(HashStorageSize, 1 * sizeof(uint));
            HashBuffer_Payload  = new ComputeBuffer(HashStorageSize / HashUpdateFraction, 2 * sizeof(uint));
            HashBuffer_Counter  = new ComputeBuffer(HashStorageSize, 1 * sizeof(uint));
            HashBuffer_Radiance = new ComputeBuffer(HashStorageSize, 4 * sizeof(uint));
            HashBuffer_Position = new ComputeBuffer(HashStorageSize, 4 * sizeof(uint));
        }

        #endregion

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            
            name = HTraceNames.HTRACE_MAIN_PASS_NAME_FRAME_DEBUG;

            _probeSize        = HResources.GeneralData.RayCountMode.ParseToProbeSize();
            _prevRayCountMode = HResources.GeneralData.RayCountMode;
            
            ResourcesLoad();
            MaterialSetup();
            AllocTextures();
            _firstFrame = true;
            
            VoxelizationRuntimeData.OnReallocTextures -= () => ReallocHashBuffers();
            VoxelizationRuntimeData.OnReallocTextures += () => ReallocHashBuffers();
        }

        private void AllocTextures()
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false) // if unity_editor we need only 1 eye render
                TextureXR.maxViews = 1;          
#endif

            AllocateMainRT();
            AllocateSSAO_RT();
            ReallocHashBuffers();
            AllocateDebugRT();
        }

        Vector2Int CalculateDepthPyramidResolution(Vector2Int screenResolution, int lowestMipLevel)
        {
            int lowestMipScale = (int)Mathf.Pow(2.0f, lowestMipLevel);
            Vector2Int lowestMipResolutiom = new Vector2Int(Mathf.CeilToInt( (float)screenResolution.x / (float)lowestMipScale), 
                                                            Mathf.CeilToInt( (float)screenResolution.y / (float)lowestMipScale));

            Vector2Int paddedDepthPyramidResolution = lowestMipResolutiom * lowestMipScale;
            return paddedDepthPyramidResolution; 
        }
        
        Matrix4x4 ComputeFrustumCorners(Camera cam)
        {
            Transform cameraTransform = cam.transform;
            
            Vector3[] frustumCorners = new Vector3[4];
            cam.CalculateFrustumCorners(new Rect(0, 0, 1 / cam.rect.xMax, 1 / cam.rect.yMax), cam.farClipPlane, cam.stereoActiveEye, frustumCorners); 
                
            Vector3 bottomLeft = cameraTransform.TransformVector(frustumCorners[1]);
            Vector3 topLeft = cameraTransform.TransformVector(frustumCorners[0]);
            Vector3 bottomRight = cameraTransform.TransformVector(frustumCorners[2]); 

            Matrix4x4 frustumVectorsArray = Matrix4x4.identity;
            frustumVectorsArray.SetRow(0, bottomLeft);
            frustumVectorsArray.SetRow(1, bottomLeft + (bottomRight - bottomLeft) * 2);
            frustumVectorsArray.SetRow(2, bottomLeft + (topLeft - bottomLeft) * 2);

            return frustumVectorsArray;
            
        }


        protected override void Execute(CustomPassContext ctx)
        {
            var cmdList = ctx.cmd;
            var camera = ctx.hdCamera.camera;
            cmdList.SetGlobalTexture(g_HTraceBufferGI, RadianceAccumulated);

            var GBuffer0 = Shader.GetGlobalTexture("_GBufferTexture0");
            var GBuffer3 = Shader.GetGlobalTexture("_GBufferTexture3");
            if (GBuffer0 == null || GBuffer3 == null)
            {
                cmdList.SetGlobalTexture(g_HTraceGBuffer0, DummyBlack);
                cmdList.SetGlobalTexture(g_HTraceGBuffer3, DummyBlack); 
            }
            else
            {
                cmdList.SetGlobalTexture(g_HTraceGBuffer0, GBuffer0);
                cmdList.SetGlobalTexture(g_HTraceGBuffer3, GBuffer3); 
            }
    
#if UNITY_EDITOR
            if (HExtensions.PipelineSupportsSSGI == false)
                return;
#endif

            Texture VoxelData = Shader.GetGlobalTexture(g_VoxelPositionPyramid);
            if (VoxelData == null || VoxelData.width != HResources.VoxelizationData.ExactData.Resolution.x)
                return;
            
            Texture ShadowmapData = Shader.GetGlobalTexture(g_HTraceShadowmap);
            if (ShadowmapData == null || ShadowmapData.width != 2048)
                return;
            
            bool UseAPVMultibounce = HResources.DebugData.TestCheckbox;
            ReflectionIndirectLighting(ctx);

            cmdList.SetGlobalInt(g_TestCheckbox, HResources.DebugData.TestCheckbox ? 1 : 0);
            
            HFrameIndex = HFrameIndex > 15 ? 0 : HFrameIndex;
            HashUpdateFrameIndex = HashUpdateFrameIndex > HashUpdateFraction ? 0 : HashUpdateFrameIndex;
            
            cmdList.SetGlobalInt(g_ProbeSize, _probeSize);
            cmdList.SetGlobalInt(g_OctahedralSize, _octahedralSize);
            cmdList.SetGlobalInt(g_HFrameIndex, HFrameIndex);
            cmdList.SetGlobalInt(g_ReprojectSkippedFrame, Time.frameCount % 8 == 0 ? 1 : 0);
            cmdList.SetGlobalInt(g_PersistentHistorySamples, PersistentHistorySamples);
            
            // Constants set
            cmdList.SetGlobalFloat(g_SkyOcclusionCone, HConfig.SkyOcclusionCone);
            cmdList.SetGlobalFloat(g_DirectionalLightIntensity, HConfig.DirectionalLightIntensity);
            cmdList.SetGlobalFloat(g_SurfaceDiffuseIntensity, HConfig.SurfaceDiffuseIntensity);
            cmdList.SetGlobalFloat(g_SkyLightIntensity, HConfig.SkyLightIntensity);
            
            //cmdList.SetGlobalInt("_TestCheckbox", DebugData.TestCheckbox == true ? 1 : 0);
            
            cmdList.SetGlobalInt(g_ReprojectSkippedFrame, 0);

            if (_prevRayCountMode != HResources.GeneralData.RayCountMode)
            {
                _prevRayCountMode = HResources.GeneralData.RayCountMode;
                _probeSize        = _prevRayCountMode.ParseToProbeSize();
                AllocTextures();
            }
            
            int screenResX = ctx.hdCamera.actualWidth;
            int screenResY = ctx.hdCamera.actualHeight;
            if (screenResX != PrevScreenResolution.x || screenResY != PrevScreenResolution.y)
            { 
                DepthPyramidResolution = CalculateDepthPyramidResolution(new Vector2Int(screenResX, screenResY), 7);
                AllocateDepthPyramidRT(DepthPyramidResolution);
                AllocateIndirectionBuffers(new Vector2Int(screenResX, screenResY));
            }
            
            PrevScreenResolution = new Vector2Int(screenResX, screenResY);

            // Calculate Resolution for Compute Shaders
            Vector2Int runningRes = new Vector2Int(screenResX, screenResY);
            Vector2 probeAtlasRes = runningRes * Vector2.one / _probeSize * _octahedralSize;
            
            //Dispatch resolutions
            int fullResX_8  = Mathf.CeilToInt((float)runningRes.x / 8);
            int fullResY_8  = Mathf.CeilToInt((float)runningRes.y / 8);
            int probeResX_8 = Mathf.CeilToInt(((float)runningRes.x / (float)_probeSize / 8.0f));
            int probeResY_8 = Mathf.CeilToInt(((float)runningRes.y / (float)_probeSize / 8.0f));
            int probeAtlasResX_8 = Mathf.CeilToInt((Mathf.CeilToInt((float)runningRes.x / (float)_probeSize) * (float)_octahedralSize) / 8);
            int probeAtlasResY_8 = Mathf.CeilToInt((Mathf.CeilToInt((float)runningRes.y / (float)_probeSize) * (float)_octahedralSize) / 8);
            
            bool useDirectionalOcclusion = HResources.ScreenSpaceLightingData.DirectionalOcclusion && HResources.ScreenSpaceLightingData.OcclusionIntensity > Single.Epsilon;

            if (_prevDirectionalOcclusion != useDirectionalOcclusion)
                AllocateSSAO_RT(!useDirectionalOcclusion); //release when disable occlusion
            _prevDirectionalOcclusion = useDirectionalOcclusion;

            if (_prevDebugModeEnabled == (HResources.GeneralData.DebugModeWS == DebugModeWS.None))
                AllocateDebugRT(HResources.GeneralData.DebugModeWS == DebugModeWS.None); //release when disable debug
            _prevDebugModeEnabled = HResources.GeneralData.DebugModeWS != DebugModeWS.None;
            
            bool DiffuseBufferUnavailable = false;
            Texture DiffuseBuffer = Shader.GetGlobalTexture(g_Gbuffertexture0);
            if (DiffuseBuffer == null || HExtensions.HdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly || ctx.hdCamera.frameSettings.litShaderMode == LitShaderMode.Forward)
                DiffuseBufferUnavailable = true;
            
            ctx.cmd.SetGlobalBuffer(g_HashBuffer_Key, HashBuffer_Key);
            ctx.cmd.SetGlobalBuffer(g_HashBuffer_Payload, HashBuffer_Payload);
            ctx.cmd.SetGlobalBuffer(g_HashBuffer_Counter, HashBuffer_Counter);
            ctx.cmd.SetGlobalBuffer(g_HashBuffer_Radiance, HashBuffer_Radiance);
            ctx.cmd.SetGlobalBuffer(g_HashBuffer_Position, HashBuffer_Position);
            
            ctx.cmd.SetGlobalInt(g_HashStorageSize, HashStorageSize);
            ctx.cmd.SetGlobalInt(g_HashUpdateFraction, HashUpdateFraction);
            
            cmdList.SetGlobalTexture(g_GeometryNormal, GeometryNormal);
            cmdList.SetGlobalTexture(g_HTraceBufferGI, RadianceAccumulated);
            
            if (camera.cameraType == CameraType.Reflection)
                return;
            
            if (_startFrameCounter < 2) { _startFrameCounter++; return; }
            
            // ---------------------------------------- Compose Motion Vectors ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_ComposeMotionVectorsProfilingSampler))
            {
                MotionVectorsMaterial.SetInt(_StencilRef,  32);
                MotionVectorsMaterial.SetInt(_StencilMask, 32);
                CoreUtils.SetRenderTarget(cmdList, CustomCameraMotionVectors, ClearFlag.Color);
                if (ctx.cameraDepthBuffer.rt.volumeDepth == TextureXR.slices)
                    cmdList.CopyTexture(ctx.cameraMotionVectorsBuffer, CustomCameraMotionVectors);
                CoreUtils.DrawFullScreen(ctx.cmd, MotionVectorsMaterial, CustomCameraMotionVectors, ctx.cameraDepthBuffer, shaderPassId: 0, properties: ctx.propertyBlock);
                
                cmdList.SetGlobalTexture(g_CustomMotionVectors, CustomCameraMotionVectors);
            }
            
      
            // ---------------------------------------- DEPTH PYRAMID GENERATION ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_DepthPyramidGenerationProfilingSampler))
            {
                int depthPyramidResX = DepthPyramidResolution.x / 16;
                int depthPyramidResY = DepthPyramidResolution.y / 16;

                // Generate 0-4 mip levels
                int generateDepthPyramid_1_Kernel = HDepthPyramid.FindKernel("GenerateDepthPyramid_1");
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_1_Kernel, _DepthPyramid_OutputMIP0, DepthPyramid, 0); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_1_Kernel, _DepthPyramid_OutputMIP1, DepthPyramid, 1); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_1_Kernel, _DepthPyramid_OutputMIP2, DepthPyramid, 2); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_1_Kernel, _DepthPyramid_OutputMIP3, DepthPyramid, 3); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_1_Kernel, _DepthPyramid_OutputMIP4, DepthPyramid, 4);
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_1_Kernel, _DepthIntermediate_Output, DepthIntermediate_Pyramid);
                cmdList.DispatchCompute(HDepthPyramid, generateDepthPyramid_1_Kernel, depthPyramidResX, depthPyramidResY, TextureXR.slices);
                
                // Generate 5-7 mip levels
                int generateDepthPyramid_2_Kernel = HDepthPyramid.FindKernel("GenerateDepthPyramid_2");
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_2_Kernel, _DepthIntermediate, DepthIntermediate_Pyramid); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_2_Kernel, _DepthPyramid_OutputMIP5, DepthPyramid, 5); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_2_Kernel, _DepthPyramid_OutputMIP6, DepthPyramid, 6); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_2_Kernel, _DepthPyramid_OutputMIP7, DepthPyramid, 7); 
                cmdList.SetComputeTextureParam(HDepthPyramid, generateDepthPyramid_2_Kernel, _DepthPyramid_OutputMIP8, DepthPyramid, 8); 
                cmdList.DispatchCompute(HDepthPyramid, generateDepthPyramid_2_Kernel, depthPyramidResX / 8, depthPyramidResY / 8, TextureXR.slices);
            }

            using (new ProfilingScope(cmdList, s_RenderAmbientOcclsuionProfilingSampler))
            {
                int computeResX_GI = (runningRes.x / 2  + 8 - 1) / 8;
                int computeResY_GI = (runningRes.y / 2  + 8 - 1) / 8;
                
                if (useDirectionalOcclusion)
                {
                    HInterpolation.EnableKeyword("USE_DIRECTIONAL_OCCLUSION");
                    HSpatialPrepass.EnableKeyword("USE_DIRECTIONAL_OCCLUSION");
                    
                    int horizonTracing_Kernel = HRenderAO.FindKernel("HorizonTracing");
                    cmdList.SetComputeTextureParam(HRenderAO, horizonTracing_Kernel, _DepthPyramid, DepthPyramid);
                    cmdList.SetComputeTextureParam(HRenderAO, horizonTracing_Kernel, _BentNormalAmbientOcclusion_Output, BentNormalsAO);
                    cmdList.SetComputeTextureParam(HRenderAO, horizonTracing_Kernel, _NormalDepthHalf_Output, NormalDepthHalf);
                    cmdList.SetComputeFloatParam(HRenderAO, _Camera_FOV, camera.fieldOfView);
                    cmdList.DispatchCompute(HRenderAO, horizonTracing_Kernel, computeResX_GI, computeResY_GI, TextureXR.slices);
                    
                    int occlusionInterpolation_Kernel = HRenderAO.FindKernel("OcclusionInterpolation");
                    cmdList.SetComputeTextureParam(HRenderAO, occlusionInterpolation_Kernel, _AmbientOcclusion, BentNormalsAO);
                    cmdList.SetComputeTextureParam(HRenderAO, occlusionInterpolation_Kernel, _NormalDepthHalf, NormalDepthHalf);
                    cmdList.SetComputeTextureParam(HRenderAO, occlusionInterpolation_Kernel, _BentNormalAO_Output, BentNormalsAO_Interpolated);
                    cmdList.SetComputeTextureParam(HRenderAO, occlusionInterpolation_Kernel, _GeometryNormal_Output, GeometryNormal);
                    cmdList.DispatchCompute(HRenderAO, occlusionInterpolation_Kernel, fullResX_8, fullResY_8, TextureXR.slices);
                }
                else
                {
                    HInterpolation.DisableKeyword("USE_DIRECTIONAL_OCCLUSION");
                    HSpatialPrepass.DisableKeyword("USE_DIRECTIONAL_OCCLUSION");
                }
            }
 
            // ---------------------------------------- PROBE GBUFFER DOWNSAMPLING ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_ProbeGBufferDownsamplingProfilingSampler))
            {
                // Fill sample offsets for disk filters
                int pointDistributionFill_Kernel = HSpatialPrepass.FindKernel("PointDistributionFill");
                cmdList.SetComputeBufferParam(HSpatialPrepass, pointDistributionFill_Kernel, _PointDistribution_Output, PointDistributionBuffer);
                cmdList.DispatchCompute(HSpatialPrepass, pointDistributionFill_Kernel, 1, 1, 1);
                
                // Fill 4x4 spatial offset buffer
                int spatialOffsetsBufferFill_Kernel = HSpatialPrepass.FindKernel("SpatialOffsetsBufferFill");
                cmdList.SetComputeBufferParam(HSpatialPrepass, spatialOffsetsBufferFill_Kernel, _SpatialOffsetsBuffer_Output, SpatialOffsetsBuffer);
                cmdList.DispatchCompute(HSpatialPrepass, spatialOffsetsBufferFill_Kernel, 1, 1, 1);
                
                // Calculate geometry normals 
                if (!useDirectionalOcclusion)
                {
                    int geometryNormals_Kernel = HSpatialPrepass.FindKernel("GeometryNormals");
                    cmdList.SetComputeTextureParam(HSpatialPrepass, geometryNormals_Kernel, _GeometryNormal_Output, GeometryNormal);
                    cmdList.DispatchCompute(HSpatialPrepass, geometryNormals_Kernel, fullResX_8, fullResY_8, TextureXR.slices);
                }
                
                if (DiffuseBufferUnavailable) HSpatialPrepass.EnableKeyword("DIFFUSE_BUFFER_UNAVAILABLE");
                if (!DiffuseBufferUnavailable) HSpatialPrepass.DisableKeyword("DIFFUSE_BUFFER_UNAVAILABLE");

                // Downsample depth, normal, diffuse and ambient occlusion 
                int gBufferDownsample_Kernel = HSpatialPrepass.FindKernel("GBufferDownsample");
                cmdList.SetComputeTextureParam(HSpatialPrepass, gBufferDownsample_Kernel, _GeometryNormal, GeometryNormal);
                cmdList.SetComputeTextureParam(HSpatialPrepass, gBufferDownsample_Kernel, _ProbeNormalDepth_Output, ProbeNormalDepth_Intermediate);
                cmdList.SetComputeTextureParam(HSpatialPrepass, gBufferDownsample_Kernel, _ProbeDiffuse_Output, ProbeDiffuse); 
                cmdList.SetComputeTextureParam(HSpatialPrepass, gBufferDownsample_Kernel, _SSAO, BentNormalsAO_Interpolated);
                cmdList.SetComputeTextureParam(HSpatialPrepass, gBufferDownsample_Kernel, _ProbeSSAO_Output, ProbeSSAO);
                cmdList.DispatchCompute(HSpatialPrepass, gBufferDownsample_Kernel, probeResX_8, probeResY_8, TextureXR.slices);

                // Smooth geometry normals
                int geometryNormalsSmoothing_Kernel = HSpatialPrepass.FindKernel("GeometryNormalsSmoothing");
                cmdList.SetComputeTextureParam(HSpatialPrepass, geometryNormalsSmoothing_Kernel, _ProbeNormalDepth, ProbeNormalDepth_Intermediate);
                cmdList.SetComputeTextureParam(HSpatialPrepass, geometryNormalsSmoothing_Kernel, _ProbeNormalDepth_Output, ProbeNormalDepth);
                cmdList.DispatchCompute(HSpatialPrepass, geometryNormalsSmoothing_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
            }
            
            
            // ---------------------------------------- PROBE TEMPORAL REPROJECTION ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_ProbeTemporalReprojectionProfilingSampler))
            {
                int probeReprojection_Kernel = HTemporalReprojection.FindKernel("ProbeReprojection");
                cmdList.SetComputeTextureParam(HTemporalReprojection, probeReprojection_Kernel, _HistoryIndirection, HistoryIndirection);
                cmdList.SetComputeTextureParam(HTemporalReprojection, probeReprojection_Kernel, _ProbeNormalDepth, ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HTemporalReprojection, probeReprojection_Kernel, _ProbeWorldPosNormal_History, ProbeWorldPosNormal_History);
                cmdList.SetComputeTextureParam(HTemporalReprojection, probeReprojection_Kernel, _ReprojectionCoords_Output, ReprojectionCoord);
                cmdList.SetComputeTextureParam(HTemporalReprojection, probeReprojection_Kernel, _ReprojectionWeights_Output, ReprojectionWeights);
                cmdList.SetComputeTextureParam(HTemporalReprojection, probeReprojection_Kernel, _PersistentReprojectionWeights_Output, PersistentReprojectionWeights);
                cmdList.SetComputeTextureParam(HTemporalReprojection, probeReprojection_Kernel, _PersistentReprojectionCoord_Output, PersistentReprojectionCoord);
                cmdList.DispatchCompute(HTemporalReprojection, probeReprojection_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
            }
            
               
            // ---------------------------------------- RAY GENERATION ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_RayGenerationProfilingSampler))
            {
                // Generate ray directions and compute lists of indirectly dispatched threads
                int rayGeneration_Kernel = HRayGeneration.FindKernel("RayGeneration");
                cmdList.SetComputeTextureParam(HRayGeneration, rayGeneration_Kernel, _ProbeNormalDepth, ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HRayGeneration, rayGeneration_Kernel, _ReprojectionCoords, ReprojectionCoord);
                cmdList.SetComputeTextureParam(HRayGeneration, rayGeneration_Kernel, _RayDirectionsJittered_Output, RayDirections);
                cmdList.SetComputeBufferParam(HRayGeneration, rayGeneration_Kernel, _IndirectCoordsSS_Output, IndirectCoordsSS);
                cmdList.SetComputeBufferParam(HRayGeneration, rayGeneration_Kernel, _IndirectCoordsOV_Output, IndirectCoordsOV);
                cmdList.SetComputeBufferParam(HRayGeneration, rayGeneration_Kernel, _IndirectCoordsSF_Output, IndirectCoordsSF);
                cmdList.SetComputeBufferParam(HRayGeneration, rayGeneration_Kernel, _RayCounter_Output, RayCounter);
                cmdList.DispatchCompute(HRayGeneration, rayGeneration_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);
                
                // Prepare arguments for screen space indirect dispatch
                int indirectArguments_Kernel = HRayGeneration.FindKernel("IndirectArguments");
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, _RayCounter, RayCounter);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, _TracingCoords, IndirectCoordsSS);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, _IndirectArguments_Output, IndirectArgumentsSS);
                cmdList.SetComputeIntParam(HRayGeneration, _RayCounterIndex, 0);
                cmdList.DispatchCompute(HRayGeneration, indirectArguments_Kernel, 1, 1, TextureXR.slices);
                
                // Prepare arguments for occlusion validation indirect dispatch
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, _RayCounter, RayCounter);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, _TracingCoords, IndirectCoordsOV);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, _IndirectArguments_Output, IndirectArgumentsOV);
                cmdList.SetComputeIntParam(HRayGeneration, _RayCounterIndex, 1);
                cmdList.DispatchCompute(HRayGeneration, indirectArguments_Kernel, 1, 1, TextureXR.slices);
                
                // Prepare arguments for spatial filter indirect dispatch
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, _RayCounter, RayCounter);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, _TracingCoords, IndirectCoordsSF);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArguments_Kernel, _IndirectArguments_Output, IndirectArgumentsSF);
                cmdList.SetComputeIntParam(HRayGeneration, _RayCounterIndex, 2);
                cmdList.DispatchCompute(HRayGeneration, indirectArguments_Kernel, 1, 1, TextureXR.slices);
            }
            
            
            // ---------------------------------------- CLEAR CHECKERBOARD TARGETS ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_ClearTargetsProfilingSampler))
            {
                // Clear hit targets
                CoreUtils.SetRenderTarget(ctx.cmd, HitDistanceScreenSpace, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
                CoreUtils.SetRenderTarget(ctx.cmd, HitDistanceWorldSpace, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
                CoreUtils.SetRenderTarget(ctx.cmd, HitCoordScreenSpace, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
                CoreUtils.SetRenderTarget(ctx.cmd, HitRadiance, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
                
                // Clear voxel payload targets
                CoreUtils.SetRenderTarget(ctx.cmd, VoxelPayload, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
            }

            if (VoxelizationRuntimeData.VoxelizationModeChanged == true) return;
            
            // ---------------------------------------- SCREEN SPACE LIGHTING ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_ScreenSpaceLightingProfilingSampler))
            {   
                var color_History = ctx.hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
                
                if (HResources.ScreenSpaceLightingData.EvaluateHitLighting && !DiffuseBufferUnavailable) HTracingScreenSpace.EnableKeyword("HIT_SCREEN_SPACE_LIGHTING");
                if (!HResources.ScreenSpaceLightingData.EvaluateHitLighting || DiffuseBufferUnavailable) HTracingScreenSpace.DisableKeyword("HIT_SCREEN_SPACE_LIGHTING");

                // Trace screen-space rays
                int tracingSS_Kernel = HTracingScreenSpace.FindKernel("ScreenSpaceTracing");   
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, _ColorPyramid_History, color_History);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, _ProbeNormalDepth, ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, _NormalDepth_History, NormalDepth_History);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, _DepthPyramid, DepthPyramid);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, _GeometryNormal, GeometryNormal);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, _RayDirection, RayDirections);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, _HitRadiance_Output, HitRadiance);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, _HitDistance_Output, HitDistanceScreenSpace);
                cmdList.SetComputeTextureParam(HTracingScreenSpace, tracingSS_Kernel, _HitCoord_Output, HitCoordScreenSpace);
                cmdList.SetComputeBufferParam(HTracingScreenSpace, tracingSS_Kernel, _RayCounter, RayCounter);
                cmdList.SetComputeBufferParam(HTracingScreenSpace, tracingSS_Kernel, _TracingCoords, IndirectCoordsSS);
                cmdList.SetComputeIntParam(HTracingScreenSpace, _IndexXR, 0);
                cmdList.DispatchCompute(HTracingScreenSpace, tracingSS_Kernel, IndirectArgumentsSS, 0);
                if (TextureXR.slices > 1)
                {
                    cmdList.SetComputeIntParam(HTracingScreenSpace, _IndexXR, 1);
                    cmdList.DispatchCompute(HTracingScreenSpace, tracingSS_Kernel, IndirectArgumentsSS, sizeof(uint) * 3);  
                }

                
                // Evaluate screen-space hit it requested 
                if (HResources.ScreenSpaceLightingData.EvaluateHitLighting && !DiffuseBufferUnavailable)
                {
                    int lightEvaluationSS_Kernel = HTracingScreenSpace.FindKernel("LightEvaluation");
                    cmdList.SetComputeTextureParam(HTracingScreenSpace, lightEvaluationSS_Kernel, _ColorPyramid_History, color_History);
                    cmdList.SetComputeTextureParam(HTracingScreenSpace, lightEvaluationSS_Kernel, _Radiance_History, RadianceAccumulated);
                    cmdList.SetComputeTextureParam(HTracingScreenSpace, lightEvaluationSS_Kernel, _GeometryNormal, GeometryNormal);
                    cmdList.SetComputeTextureParam(HTracingScreenSpace, lightEvaluationSS_Kernel, _HitCoord, HitCoordScreenSpace);
                    cmdList.SetComputeTextureParam(HTracingScreenSpace, lightEvaluationSS_Kernel, _HitRadiance_Output, HitRadiance);
                    cmdList.SetComputeBufferParam(HTracingScreenSpace, lightEvaluationSS_Kernel, _RayCounter, RayCounter);
                    cmdList.SetComputeBufferParam(HTracingScreenSpace, lightEvaluationSS_Kernel, _TracingCoords, IndirectCoordsSS);
                    cmdList.SetComputeIntParam(HTracingScreenSpace, _IndexXR, 0);
                    cmdList.DispatchCompute(HTracingScreenSpace, lightEvaluationSS_Kernel, IndirectArgumentsSS, 0);
                    if (TextureXR.slices > 1)
                    {
                        cmdList.SetComputeIntParam(HTracingScreenSpace, _IndexXR, 1);
                        cmdList.DispatchCompute(HTracingScreenSpace, lightEvaluationSS_Kernel, IndirectArgumentsSS, sizeof(uint) * 3);  
                    }
                }
            }
            
            // ---------------------------------------- RAY COMPACTION ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_RayCompactionProfilingSampler))
            {
                // Compact rays
                int rayCompactionKernel = HRayGeneration.FindKernel("RayCompaction");
                cmdList.SetComputeTextureParam(HRayGeneration, rayCompactionKernel, _HitDistance, HitDistanceScreenSpace);
                cmdList.SetComputeTextureParam(HRayGeneration, rayCompactionKernel, _HitDistance_Output, HitDistanceWorldSpace);
                cmdList.SetComputeBufferParam(HRayGeneration, rayCompactionKernel, _RayCounter, RayCounter);
                cmdList.SetComputeBufferParam(HRayGeneration, rayCompactionKernel, _TracingCoords, IndirectCoordsSS);
                cmdList.SetComputeBufferParam(HRayGeneration, rayCompactionKernel, _TracingRayCounter_Output, RayCounterWS);
                cmdList.SetComputeBufferParam(HRayGeneration, rayCompactionKernel, _TracingCoords_Output, IndirectCoordsWS);
                cmdList.SetComputeIntParam(HRayGeneration, _IndexXR, 0);
                cmdList.DispatchCompute(HRayGeneration, rayCompactionKernel, IndirectArgumentsSS, 0);
                if (TextureXR.slices > 1)
                {
                    cmdList.SetComputeIntParam(HRayGeneration, _IndexXR, 1);
                    cmdList.DispatchCompute(HRayGeneration, rayCompactionKernel, IndirectArgumentsSS, sizeof(uint) * 3);
                }

                // Prepare indirect arguments for world space lighting
                int indirectArgumentsKernel = HRayGeneration.FindKernel("IndirectArguments");
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArgumentsKernel, _RayCounter, RayCounterWS);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArgumentsKernel, _TracingCoords, IndirectCoordsWS);
                cmdList.SetComputeBufferParam(HRayGeneration, indirectArgumentsKernel, _IndirectArguments_Output, IndirectArgumentsWS); 
                cmdList.SetComputeIntParam(HRayGeneration, _RayCounterIndex, 0);
                cmdList.DispatchCompute(HRayGeneration, indirectArgumentsKernel, 1, 1, TextureXR.slices);
            }
            
            // TDR timeout protection
            if (_firstFrame == true) { _firstFrame = false; return; }
            
            // ---------------------------------------- WORLD SPACE LIGHTING ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_WorldSpaceLightingProfilingSampler))
            {
                if (HResources.GeneralData.Multibounce == Multibounce.None) { HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_CACHE"); HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_APV"); HTracingWorldSpace.EnableKeyword("MULTIBOUNCE_OFF"); }
                if (HResources.GeneralData.Multibounce == Multibounce.IrradianceCache) { HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_OFF"); HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_APV"); HTracingWorldSpace.EnableKeyword("MULTIBOUNCE_CACHE"); }
                if (HResources.GeneralData.Multibounce == Multibounce.AdaptiveProbeVolumes) { HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_CACHE"); HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_OFF"); HTracingWorldSpace.EnableKeyword("MULTIBOUNCE_APV"); }

                // Trace world-space rays
                using (new ProfilingScope(cmdList, s_WorldSpaceTracingProfilingSampler)) 
                {
                    int wsTracingKernel = HTracingWorldSpace.FindKernel("WorldSpaceTracing");
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, _DepthPyramid, DepthPyramid);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, _HitDistance, HitDistanceScreenSpace);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, _ProbeNormalDepth, ProbeNormalDepth);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, _GeometryNormal, GeometryNormal);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, _RayDirection, RayDirections);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, _HitDistance_Output, HitDistanceWorldSpace);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, _VoxelPayload_Output, VoxelPayload);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, wsTracingKernel, _HitRadiance_Output, HitRadiance);
                    cmdList.SetComputeBufferParam(HTracingWorldSpace, wsTracingKernel, _PointDistribution, PointDistributionBuffer);
                    cmdList.SetComputeBufferParam(HTracingWorldSpace, wsTracingKernel, _TracingCoords, IndirectCoordsWS);
                    cmdList.SetComputeBufferParam(HTracingWorldSpace, wsTracingKernel, _RayCounter, RayCounterWS);
                    cmdList.SetComputeFloatParam(HTracingWorldSpace, _RayLength, HResources.GeneralData.RayLength);
                    cmdList.SetComputeIntParam(HTracingWorldSpace, _IndexXR, 0);
                    cmdList.DispatchCompute(HTracingWorldSpace, wsTracingKernel, IndirectArgumentsWS, 0);
                    if (TextureXR.slices > 1)
                    {
                        cmdList.SetComputeIntParam(HTracingWorldSpace, _IndexXR, 1);
                        cmdList.DispatchCompute(HTracingWorldSpace, wsTracingKernel, IndirectArgumentsWS, sizeof(uint) * 3);
                    }
                }
                
                // Evaluate world-space lighting
                using (new ProfilingScope(cmdList, s_LightEvaluationProfilingSampler))
                {
                    int lightEvaluation_Kernel = HTracingWorldSpace.FindKernel("LightEvaluation");
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, lightEvaluation_Kernel, _VoxelPayload, VoxelPayload);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, lightEvaluation_Kernel, _ProbeNormalDepth, ProbeNormalDepth);
                    cmdList.SetComputeTextureParam(HTracingWorldSpace, lightEvaluation_Kernel, _HitRadiance_Output, HitRadiance);
                    cmdList.SetComputeBufferParam(HTracingWorldSpace, lightEvaluation_Kernel, _TracingCoords, IndirectCoordsWS);
                    cmdList.SetComputeBufferParam(HTracingWorldSpace, lightEvaluation_Kernel, _RayCounter, RayCounterWS);
                    cmdList.SetComputeIntParam(HTracingWorldSpace, _IndexXR, 0);
                    cmdList.DispatchCompute(HTracingWorldSpace, lightEvaluation_Kernel, IndirectArgumentsWS, 0);
                    if (TextureXR.slices > 1)
                    {
                        cmdList.SetComputeIntParam(HTracingWorldSpace, _IndexXR, 1);
                        cmdList.DispatchCompute(HTracingWorldSpace, lightEvaluation_Kernel, IndirectArgumentsWS, sizeof(uint) * 3);
                    }
                }
            } 
            
            // ---------------------------------------- RADIANCE CACHING ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_RadianceCachingProfilingSampler))
            {
                if (HResources.GeneralData.Multibounce == Multibounce.IrradianceCache)
                {
                    HRadianceCache.SetInt(_HashUpdateFrameIndex, HashUpdateFrameIndex);

                    // Cache tracing update
                    using (new ProfilingScope(cmdList, s_CacheTracingUpdateProfilingSampler))
                    {
                        int cacheTracingUpdate_Kernel = HRadianceCache.FindKernel("CacheTracingUpdate");
                        cmdList.SetComputeFloatParam(HRadianceCache, _RayLength, HResources.GeneralData.RayLength);
                        cmdList.DispatchCompute(HRadianceCache, cacheTracingUpdate_Kernel, (HashStorageSize / HashUpdateFraction) / 64, 1, 1); 
                    }

                    // Cache light evaluation at hit points
                    using (new ProfilingScope(cmdList, s_CacheLightEvaluationProfilingSampler))
                    {
                        int cacheLightEvaluation_Kernel = HRadianceCache.FindKernel("CacheLightEvaluation");
                        cmdList.DispatchCompute(HRadianceCache, cacheLightEvaluation_Kernel, (HashStorageSize / HashUpdateFraction) / 64, 1, 1); 
                    }

                    // Cache writing at primary surfaces
                    using (new ProfilingScope(cmdList, s_PrimaryCacheSpawnProfilingSampler))
                    {  
                        int cachePrimarySpawn_Kernel = HRadianceCache.FindKernel("CachePrimarySpawn");
                        cmdList.SetComputeTextureParam(HRadianceCache, cachePrimarySpawn_Kernel, _ReprojectionCoords, ReprojectionCoord);
                        cmdList.SetComputeTextureParam(HRadianceCache, cachePrimarySpawn_Kernel, _ProbeNormalDepth, ProbeNormalDepth);
                        cmdList.SetComputeTextureParam(HRadianceCache, cachePrimarySpawn_Kernel, _GeometryNormal, GeometryNormal);
                        cmdList.SetComputeTextureParam(HRadianceCache, cachePrimarySpawn_Kernel, _RadianceAtlas, HitRadiance);
                        cmdList.DispatchCompute(HRadianceCache, cachePrimarySpawn_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                    }

                    // Cache counter update, deallocation of out-of-bounds entries, filtered cache population
                    using (new ProfilingScope(cmdList, s_CacheDataUpdateProfilingSampler))
                    {   
                        // Clear filtered cache every frame before writing to it
                        // CoreUtils.SetRenderTarget(ctx.cmd, RadianceCacheFiltered, ClearFlag.Color, Color.clear, 0, CubemapFace.Unknown, -1);
                        
                        int cacheDataUpdate_Kernel = HRadianceCache.FindKernel("CacheDataUpdate");
                        cmdList.DispatchCompute(HRadianceCache, cacheDataUpdate_Kernel, HashStorageSize / 64, 1, 1);
                    }  
                }
                
            }

            
            // ---------------------------------------- SPATIAL PREPASS ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_SpatialPrepassProfilingSampler))
            {   
                // Gather probe ambient occlusion from ray hit distance and temporally accumulate
                int probeAmbientOcclusion_Kernel = HProbeAmbientOcclusion.FindKernel("ProbeAmbientOcclusion");
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, _RayDistanceSS, HitDistanceScreenSpace);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, _RayDistanceWS, HitDistanceWorldSpace);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, _ProbeNormalDepth, ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, _ReprojectionWeights, PersistentReprojectionWeights);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, _PersistentReprojectionCoord, PersistentReprojectionCoord);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, _ProbeAmbientOcclusion_History, ProbeAmbientOcclusion_History);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, _ProbeAmbientOcclusion_Output, ProbeAmbientOcclusion);
                cmdList.DispatchCompute(HProbeAmbientOcclusion, probeAmbientOcclusion_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                
                // Prepare offsets and weights for further spatial passes
                int spatialPrepass_Kernel = HSpatialPrepass.FindKernel("SpatialPrepass");
                cmdList.SetComputeTextureParam(HSpatialPrepass, spatialPrepass_Kernel, _ProbeNormalDepth, ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HSpatialPrepass, spatialPrepass_Kernel, _ProbeAmbientOcclusion, ProbeAmbientOcclusion);
                cmdList.SetComputeTextureParam(HSpatialPrepass, spatialPrepass_Kernel, _ProbeNormalDepth_History, ProbeNormalDepth_History);
                cmdList.SetComputeTextureParam(HSpatialPrepass, spatialPrepass_Kernel, _SpatialOffsets_Output, SpatialOffsetsPacked);
                cmdList.SetComputeTextureParam(HSpatialPrepass, spatialPrepass_Kernel, _SpatialWeights_Output, SpatialWeightsPacked);
                cmdList.SetComputeBufferParam(HSpatialPrepass, spatialPrepass_Kernel, _PointDistribution, PointDistributionBuffer);
                cmdList.SetComputeBufferParam(HSpatialPrepass, spatialPrepass_Kernel, _SpatialOffsetsBuffer, SpatialOffsetsBuffer);
                cmdList.DispatchCompute(HSpatialPrepass, spatialPrepass_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
       
                // Spatially filter probe ambient occlusion
                int probeAmbientOcclusionSpatialFilter_Kernel = HProbeAmbientOcclusion.FindKernel("ProbeAmbientOcclusionSpatialFilter");
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusionSpatialFilter_Kernel, _SpatialWeightsPacked, SpatialWeightsPacked);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusionSpatialFilter_Kernel, _SpatialOffsetsPacked, SpatialOffsetsPacked);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusionSpatialFilter_Kernel, _ProbeAmbientOcclusion, ProbeAmbientOcclusion);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusionSpatialFilter_Kernel, _ProbeAmbientOcclusion_OutputFiltered, ProbeAmbientOcclusion_Filtered);
                cmdList.DispatchCompute(HProbeAmbientOcclusion, probeAmbientOcclusionSpatialFilter_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
            }
            

            // ---------------------------------------- ReSTIR TEMPORAL REUSE ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_ReSTIRTemporalReuseProfilingSampler))
            {
                int probeAtlasTemporalReuse_Kernel = HReSTIR.FindKernel("ProbeAtlasTemporalReuse");
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, _DepthPyramid, DepthPyramid);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, _ShadowGuidanceMask, ShadowGuidanceMask_Filtered);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, _RayDirection, RayDirections);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, _RayDistance, HitDistanceWorldSpace);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, _RadianceAtlas, HitRadiance);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, _ProbeDiffuse, ProbeDiffuse); 
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, _ProbeNormalDepth, ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, _ReprojectionWeights, PersistentReprojectionWeights);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, _PersistentReprojectionCoord, PersistentReprojectionCoord);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, _ReservoirAtlas_Output, ReservoirAtlas);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, _ReservoirAtlas_History, ReservoirAtlas_History);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, _ReservoirAtlasRayData_Output, ReservoirAtlasRayData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasTemporalReuse_Kernel, _ReservoirAtlasRadianceData_Output, ReservoirAtlasRadianceData_A);
                cmdList.SetComputeIntParam(HReSTIR, _UseDiffuseWeight, HResources.GeneralData.DebugModeWS == DebugModeWS.None ? 1 : 0);
                cmdList.DispatchCompute(HReSTIR, probeAtlasTemporalReuse_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);
            }
            
         
            // ---------------------------------------- RESERVOIR OCCLUSION VALIDATION ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_ReservoirOcclusionValidationProfilingSampler))
            {
                // Run one pass of spatial reuse in disocclusion areas to generate shadow guidance mask
                int probeAtlasSpatialReuse_Kernel = HReSTIR.FindKernel("ProbeAtlasSpatialReuseDisocclusion");
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ProbeDiffuse, ProbeDiffuse); 
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _SpatialWeightsPacked, SpatialWeightsPacked);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _SpatialOffsetsPacked, SpatialOffsetsPacked);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRayData, ReservoirAtlasRayData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRadianceData, ReservoirAtlasRadianceData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRayData_Output, ReservoirAtlasRayData_C);
                cmdList.SetComputeBufferParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _TracingCoords, IndirectCoordsSF);
                cmdList.SetComputeBufferParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _RayCounter, RayCounter);
                cmdList.SetComputeIntParam(HReSTIR, _IndexXR, 0);
                cmdList.DispatchCompute(HReSTIR, probeAtlasSpatialReuse_Kernel, IndirectArgumentsSF, 0);
                if (TextureXR.slices > 1)
                {
                    cmdList.SetComputeIntParam(HReSTIR, _IndexXR, 1);
                    cmdList.DispatchCompute(HReSTIR, probeAtlasSpatialReuse_Kernel, IndirectArgumentsSF, sizeof(uint) * 3);
                }

                // Reproject occlusion checkerboarded history
                int reservoirOcclusionReprojection_Kernel = HReservoirValidation.FindKernel("OcclusionReprojection");
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionReprojection_Kernel, _ReprojectionCoords, ReprojectionCoord);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionReprojection_Kernel, _ProbeAmbientOcclusion, ProbeAmbientOcclusion_Filtered);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionReprojection_Kernel, _ShadowGuidanceMask_History, ShadowGuidanceMask_CheckerboardHistory);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionReprojection_Kernel, _ShadowGuidanceMask_Output, ShadowGuidanceMask);
                cmdList.DispatchCompute(HReservoirValidation, reservoirOcclusionReprojection_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);

                // Validate reservoir occlusion
                int reservoirOcclusionValidation_Kernel = HReservoirValidation.FindKernel("OcclusionValidation");
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, _DepthPyramid, DepthPyramid);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, _ProbeNormalDepth, ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, _ReprojectionCoords, ReprojectionCoord);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, _ReservoirAtlasRayData,  ReservoirAtlasRayData_B);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, _ReservoirAtlasRayData_Disocclusion,  ReservoirAtlasRayData_C);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, _ReservoirAtlas, ReservoirAtlas);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, _ReservoirAtlasRadianceData_Inout, ReservoirAtlasRadianceData_B);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, _ShadowGuidanceMask_Output, ShadowGuidanceMask);
                cmdList.SetComputeTextureParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, _ProbeAmbientOcclusion, ProbeAmbientOcclusion_Filtered);
                cmdList.SetComputeBufferParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, _PointDistribution, PointDistributionBuffer);
                cmdList.SetComputeBufferParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, _RayCounter, RayCounter);
                cmdList.SetComputeBufferParam(HReservoirValidation, reservoirOcclusionValidation_Kernel, _TracingCoords, IndirectCoordsOV);
                cmdList.SetComputeIntParam(HReservoirValidation, _IndexXR, 0);
                cmdList.DispatchCompute(HReservoirValidation, reservoirOcclusionValidation_Kernel, IndirectArgumentsOV, 0);
                if (TextureXR.slices > 1)
                {
                    cmdList.SetComputeIntParam(HReservoirValidation, _IndexXR, 1);
                    cmdList.DispatchCompute(HReservoirValidation, reservoirOcclusionValidation_Kernel, IndirectArgumentsOV, sizeof(uint) * 3);
                }

                // Temporal accumulation pass
                int occlusionTemporalFilter_Kernel = HReservoirValidation.FindKernel("OcclusionTemporalFilter");
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionTemporalFilter_Kernel, _ReprojectionWeights, ReprojectionWeights);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionTemporalFilter_Kernel, _ReprojectionCoords, ReprojectionCoord);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionTemporalFilter_Kernel, _ShadowGuidanceMask, ShadowGuidanceMask);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionTemporalFilter_Kernel, _SampleCount_History, ShadowGuidanceMask_SamplecountHistory);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionTemporalFilter_Kernel, _SampleCount_Output, ShadowGuidanceMask_Samplecount);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionTemporalFilter_Kernel, _ShadowGuidanceMask_History, ShadowGuidanceMask_History);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionTemporalFilter_Kernel, _ShadowGuidanceMask_Output, ShadowGuidanceMask_Accumulated);
                cmdList.DispatchCompute(HReservoirValidation, occlusionTemporalFilter_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);

                // Spatial filtering pass
                int occlusionSpatialFilter_Kernel = HReservoirValidation.FindKernel("OcclusionSpatialFilter");
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionSpatialFilter_Kernel, _SpatialWeightsPacked, SpatialWeightsPacked);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionSpatialFilter_Kernel, _SpatialOffsetsPacked, SpatialOffsetsPacked);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionSpatialFilter_Kernel, _SampleCount, ShadowGuidanceMask_Samplecount);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionSpatialFilter_Kernel, _ShadowGuidanceMask, ShadowGuidanceMask_Accumulated);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionSpatialFilter_Kernel, _ShadowGuidanceMask_Output, ShadowGuidanceMask_Filtered);
                cmdList.SetComputeTextureParam(HReservoirValidation, occlusionSpatialFilter_Kernel, _ReservoirAtlasRadianceData_Inout, ReservoirAtlasRadianceData_A);
                cmdList.DispatchCompute(HReservoirValidation, occlusionSpatialFilter_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);
            }
            
            
            // ---------------------------------------- ReSTIR SPATIAL REUSE ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_ReSTIRSpatialReuseProfilingSampler))
            {
                // Prepare spatial kernel
                int probeAtlasSpatialReuse_Kernel = HReSTIR.FindKernel("ProbeAtlasSpatialReuse");
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ProbeDiffuse, ProbeDiffuse);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _SpatialWeightsPacked, SpatialWeightsPacked);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _SpatialOffsetsPacked, SpatialOffsetsPacked);
                
                // 1st spatial disk pass
                cmdList.SetComputeIntParam(HReSTIR, _PassNumber, 1);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRayData, ReservoirAtlasRayData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRadianceData, ReservoirAtlasRadianceData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRayData_Output, ReservoirAtlasRayData_B);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRadianceData_Output, ReservoirAtlasRadianceData_B);
                cmdList.DispatchCompute(HReSTIR, probeAtlasSpatialReuse_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);
                
                // 2nd spatial disk pass
                cmdList.SetComputeIntParam(HReSTIR, _PassNumber, 2);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRayData, ReservoirAtlasRayData_B);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRadianceData, ReservoirAtlasRadianceData_B);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRayData_Output, ReservoirAtlasRayData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRadianceData_Output, ReservoirAtlasRadianceData_A);
                cmdList.DispatchCompute(HReSTIR, probeAtlasSpatialReuse_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);

                // 3rd spatial disk pass
                cmdList.SetComputeIntParam(HReSTIR, _PassNumber, 3);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRayData, ReservoirAtlasRayData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRadianceData, ReservoirAtlasRadianceData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRayData_Output, ReservoirAtlasRayData_B);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRadianceData_Output, ReservoirAtlasRadianceData_B);
                cmdList.DispatchCompute(HReSTIR, probeAtlasSpatialReuse_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);
                
                // 3rd spatial disk pass
                cmdList.SetComputeIntParam(HReSTIR, _PassNumber, 2);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRayData, ReservoirAtlasRayData_B);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRadianceData, ReservoirAtlasRadianceData_B);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRayData_Output, ReservoirAtlasRayData_A);
                cmdList.SetComputeTextureParam(HReSTIR, probeAtlasSpatialReuse_Kernel, _ReservoirAtlasRadianceData_Output, ReservoirAtlasRadianceData_A);
                cmdList.DispatchCompute(HReSTIR, probeAtlasSpatialReuse_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);
            }   
            
      
            // ---------------------------------------- PERSISTENT HISTORY UPDATE ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_PersistentHistoryUpdateProfilingSampler))
            {   
                // Scroll history indirection array slice by slice
                int historyIndirectionScroll_Kernel = HTemporalReprojection.FindKernel("HistoryIndirectionScroll");
                cmdList.SetComputeTextureParam(HTemporalReprojection, historyIndirectionScroll_Kernel, _ReprojectionCoord, ReprojectionCoord);
                cmdList.SetComputeTextureParam(HTemporalReprojection, historyIndirectionScroll_Kernel, _HistoryIndirection, HistoryIndirection);
                
                // Scrolling cycle
                for (int i = PersistentHistorySamples - 1; i > 0; i--)
                {
                    cmdList.SetComputeIntParam(HTemporalReprojection, _HistoryArrayIndex, i);
                    cmdList.DispatchCompute(HTemporalReprojection, historyIndirectionScroll_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                }
                
                // Update history indirection coord buffer
                int historyIndirectionUpdate_Kernel = HTemporalReprojection.FindKernel("HistoryIndirectionUpdate");
                cmdList.SetComputeTextureParam(HTemporalReprojection, historyIndirectionUpdate_Kernel, _HistoryIndirection, HistoryIndirection);
                cmdList.DispatchCompute(HTemporalReprojection, historyIndirectionUpdate_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                
                // Update probe world position & normal history buffer
                int historyProbeBuffersUpdate_Kernel = HTemporalReprojection.FindKernel("HistoryProbeBuffersUpdate");
                cmdList.SetComputeTextureParam(HTemporalReprojection, historyProbeBuffersUpdate_Kernel, _ProbeNormalDepth, ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HTemporalReprojection, historyProbeBuffersUpdate_Kernel, _ProbeWorldPosNormal_HistoryOutput, ProbeWorldPosNormal_History);
                cmdList.DispatchCompute(HTemporalReprojection, historyProbeBuffersUpdate_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                
                // Update probe ambient occlusion history buffer
                int probeAmbientOcclusionHistoryUpdate_Kernel = HProbeAmbientOcclusion.FindKernel("ProbeAmbientOcclusionHistoryUpdate");
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusionHistoryUpdate_Kernel, _ProbeAmbientOcclusion, ProbeAmbientOcclusion);
                cmdList.SetComputeTextureParam(HProbeAmbientOcclusion, probeAmbientOcclusionHistoryUpdate_Kernel, _ProbeAmbientOcclusion_Output, ProbeAmbientOcclusion_History);
                cmdList.DispatchCompute(HProbeAmbientOcclusion, probeAmbientOcclusionHistoryUpdate_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                
                // Update reserovir history buffer
                int reservoirHistoryUpdate_Kernel = HReSTIR.FindKernel("ReservoirHistoryUpdate");
                cmdList.SetComputeTextureParam(HReSTIR, reservoirHistoryUpdate_Kernel, _ReservoirAtlas, ReservoirAtlas);
                cmdList.SetComputeTextureParam(HReSTIR, reservoirHistoryUpdate_Kernel, _ReservoirAtlas_Output, ReservoirAtlas_History);
                cmdList.DispatchCompute(HReSTIR, reservoirHistoryUpdate_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);
            }
            
            
            // ---------------------------------------- INTERPOLATION ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_InterpolationProfilingSampler))
            {   
                // Spherical harmonics gather
                int gatherSH_Kernel = HInterpolation.FindKernel("GatherSH");
                cmdList.SetComputeTextureParam(HInterpolation, gatherSH_Kernel, _ShadowGuidanceMask, ShadowGuidanceMask_Accumulated);
                cmdList.SetComputeTextureParam(HInterpolation, gatherSH_Kernel, _ReservoirAtlasRadianceData, ReservoirAtlasRadianceData_A);
                cmdList.SetComputeTextureParam(HInterpolation, gatherSH_Kernel, _ReservoirAtlasRayData, ReservoirAtlasRayData_A);
                cmdList.SetComputeTextureParam(HInterpolation, gatherSH_Kernel, _ProbeNormalDepth, ProbeNormalDepth);
                cmdList.SetComputeTextureParam(HInterpolation, gatherSH_Kernel, _Temp, ShadowGuidanceMask_Accumulated);
                cmdList.SetComputeTextureParam(HInterpolation, gatherSH_Kernel, _PackedSH_A_Output, PackedSH_A);
                cmdList.SetComputeTextureParam(HInterpolation, gatherSH_Kernel, _PackedSH_B_Output, PackedSH_B);
                cmdList.DispatchCompute(HInterpolation, gatherSH_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                
                // Interpolation to the final resolution
                int interpolation_Kernel = HInterpolation.FindKernel("Interpolation");
                cmdList.SetComputeTextureParam(HInterpolation, interpolation_Kernel, _ProbeSSAO, ProbeSSAO);
                cmdList.SetComputeTextureParam(HInterpolation, interpolation_Kernel, _PackedSH_A, PackedSH_A);
                cmdList.SetComputeTextureParam(HInterpolation, interpolation_Kernel, _PackedSH_B, PackedSH_B);
                cmdList.SetComputeTextureParam(HInterpolation, interpolation_Kernel, _GeometryNormal, GeometryNormal);
                cmdList.SetComputeTextureParam(HInterpolation, interpolation_Kernel, _BentNormalsAO, BentNormalsAO_Interpolated);
                cmdList.SetComputeTextureParam(HInterpolation, interpolation_Kernel, _Radiance_Output, Radiance_Interpolated);
                cmdList.SetComputeTextureParam(HInterpolation, interpolation_Kernel, _ProbeNormalDepth, ProbeNormalDepth);
                cmdList.SetComputeFloatParam(HInterpolation, _AO_Intensity, HResources.ScreenSpaceLightingData.OcclusionIntensity);
                cmdList.DispatchCompute(HInterpolation, interpolation_Kernel, fullResX_8, fullResY_8, TextureXR.slices);
            }

            
            // ---------------------------------------- TEMPORAL DENOISER ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_TemporalDenoisingProfilingSampler))
            {
                int temporalDenoising_Kernel = HTemporalDenoiser.FindKernel("TemporalDenoising");
                cmdList.SetComputeTextureParam(HTemporalDenoiser, temporalDenoising_Kernel, _GeometryNormal, GeometryNormal);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, temporalDenoising_Kernel, _NormalDepth_History, NormalDepth_History);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, temporalDenoising_Kernel, _Radiance, Radiance_Interpolated);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, temporalDenoising_Kernel, _Radiance_History, RadianceAccumulated_History);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, temporalDenoising_Kernel, _Radiance_Output, RadianceAccumulated);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, temporalDenoising_Kernel, _LuminanceDelta_Output, LuminanceDelta);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, temporalDenoising_Kernel, _LuminanceDelta_History, LuminanceDelta_History);
                cmdList.DispatchCompute(HTemporalDenoiser, temporalDenoising_Kernel, fullResX_8, fullResY_8, TextureXR.slices);
            }
            
            
            // ---------------------------------------- SPATIAL CLEANUP ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_SpatialCleanupProfilingSampler))
            {
                int spatialCleanup_Kernel = HTemporalDenoiser.FindKernel("SpatialCleanup");
                cmdList.SetComputeTextureParam(HTemporalDenoiser, spatialCleanup_Kernel, _GeometryNormal, GeometryNormal);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, spatialCleanup_Kernel, _Radiance, RadianceAccumulated);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, spatialCleanup_Kernel, _Radiance_HistoryOutput, RadianceAccumulated_History);
                cmdList.SetComputeTextureParam(HTemporalDenoiser, spatialCleanup_Kernel, _NormalDepth_HistoryOutput, NormalDepth_History);
                cmdList.DispatchCompute(HTemporalDenoiser, spatialCleanup_Kernel, fullResX_8, fullResY_8, TextureXR.slices);
            }
            
            
             // ---------------------------------------- COPY BUFFERS ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_CopyBuffersProfilingSampler))
            {
                int hCopyProbeBuffers_Kernel = HCopy.FindKernel("CopyProbeBuffers");
                cmdList.SetComputeTextureParam(HCopy, hCopyProbeBuffers_Kernel, _ShadowGuidanceMask_Samplecount, ShadowGuidanceMask_Samplecount);
                cmdList.SetComputeTextureParam(HCopy, hCopyProbeBuffers_Kernel, _ShadowGuidanceMask_SamplecountHistoryOutput, ShadowGuidanceMask_SamplecountHistory);
                cmdList.DispatchCompute(HCopy, hCopyProbeBuffers_Kernel, probeResX_8, probeResY_8, TextureXR.slices);
                
                int hCopyProbeAtlases_Kernel = HCopy.FindKernel("CopyProbeAtlases");
                cmdList.SetComputeTextureParam(HCopy, hCopyProbeAtlases_Kernel, _ShadowGuidanceMask, ShadowGuidanceMask);
                cmdList.SetComputeTextureParam(HCopy, hCopyProbeAtlases_Kernel, _ShadowGuidanceMask_CheckerboardHistoryOutput, ShadowGuidanceMask_CheckerboardHistory);
                cmdList.SetComputeTextureParam(HCopy, hCopyProbeAtlases_Kernel, _ShadowGuidanceMask_Accumulated, ShadowGuidanceMask_Accumulated);
                cmdList.SetComputeTextureParam(HCopy, hCopyProbeAtlases_Kernel, _ShadowGuidanceMask_HistoryOutput, ShadowGuidanceMask_History);
                cmdList.DispatchCompute(HCopy, hCopyProbeAtlases_Kernel, probeAtlasResX_8, probeAtlasResY_8, TextureXR.slices);

                int hCopyFullResBuffers_Kernel = HCopy.FindKernel("CopyFullResBuffers");
                cmdList.SetComputeTextureParam(HCopy, hCopyFullResBuffers_Kernel, _GeometryNormal, GeometryNormal);
                cmdList.SetComputeTextureParam(HCopy, hCopyFullResBuffers_Kernel, _NormalDepth_HistoryOutput, NormalDepth_History);
                cmdList.DispatchCompute(HCopy, hCopyFullResBuffers_Kernel, fullResX_8, fullResY_8, TextureXR.slices);
            }
            
            // Final output
            cmdList.SetGlobalTexture(g_HTraceBufferGI, RadianceAccumulated);
            
            
            // ---------------------------------------- DEBUG (DON'T SHIP!) ---------------------------------------- //
            using (new ProfilingScope(cmdList, s_DebugPassthroughProfilingSampler))
            {
                if (HResources.GeneralData.DebugModeWS != DebugModeWS.None)
                {
                    int hDebugPassthrough_Kernel = HDebugPassthrough.FindKernel("DebugPassthrough");
                    cmdList.SetComputeTextureParam(HDebugPassthrough, hDebugPassthrough_Kernel, _InputA, RadianceAccumulated);
                    cmdList.SetComputeTextureParam(HDebugPassthrough, hDebugPassthrough_Kernel, _InputB, VoxelVisualizationRayDirections);
                    cmdList.SetComputeTextureParam(HDebugPassthrough, hDebugPassthrough_Kernel, _Output, DebugOutput);
                    cmdList.DispatchCompute(HDebugPassthrough, hDebugPassthrough_Kernel, fullResX_8, fullResY_8, TextureXR.slices); 
                }
            }
            
            // Disable visualization keywords by default
            VoxelVisualization.EnableKeyword("VISUALIZE_OFF"); VoxelVisualization.DisableKeyword("VISUALIZE_LIGHTING"); VoxelVisualization.DisableKeyword("VISUALIZE_COLOR");
            
            // Visualize voxels if requested
            if (HResources.GeneralData.DebugModeWS == DebugModeWS.VoxelizedLighting || HResources.GeneralData.DebugModeWS == DebugModeWS.VoxelizedColor)
            {
                using (new ProfilingScope(ctx.cmd, s_VisualizeVoxelsProfilingSampler))
                {
                    if (HResources.GeneralData.DebugModeWS == DebugModeWS.VoxelizedLighting)
                    {VoxelVisualization.EnableKeyword("VISUALIZE_LIGHTING"); VoxelVisualization.DisableKeyword("VISUALIZE_COLOR"); VoxelVisualization.DisableKeyword("VISUALIZE_OFF");}
                    
                    if (HResources.GeneralData.DebugModeWS == DebugModeWS.VoxelizedColor)
                    {VoxelVisualization.EnableKeyword("VISUALIZE_COLOR"); VoxelVisualization.DisableKeyword("VISUALIZE_LIGHTING"); VoxelVisualization.DisableKeyword("VISUALIZE_OFF");}
                    
                    if (HResources.GeneralData.Multibounce == Multibounce.None) { HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_CACHE"); HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_APV"); HTracingWorldSpace.EnableKeyword("MULTIBOUNCE_OFF"); }
                    if (HResources.GeneralData.Multibounce == Multibounce.IrradianceCache) { HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_OFF"); HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_APV"); HTracingWorldSpace.EnableKeyword("MULTIBOUNCE_CACHE"); }
                    if (HResources.GeneralData.Multibounce == Multibounce.AdaptiveProbeVolumes) { HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_CACHE"); HTracingWorldSpace.DisableKeyword("MULTIBOUNCE_OFF"); HTracingWorldSpace.EnableKeyword("MULTIBOUNCE_APV"); }
                    
                    // Calculate rays in camera frustum
                    var debugCameraFrustum = ComputeFrustumCorners(ctx.hdCamera.camera);  
                    
                    // Interpolate rays in vf shader
                    VoxelVisualizationMaterial.SetMatrix(_DebugCameraFrustum, debugCameraFrustum);
                    CoreUtils.DrawFullScreen(ctx.cmd, VoxelVisualizationMaterial, VoxelVisualizationRayDirections, shaderPassId: 0, properties: ctx.propertyBlock);
                    
                    // Trace into voxels for debug
                    int voxelVisualization_Kernel = VoxelVisualization.FindKernel("VisualizeVoxels");
                    ctx.cmd.SetComputeTextureParam(VoxelVisualization, voxelVisualization_Kernel, _DebugRayDirection, VoxelVisualizationRayDirections);
                    ctx.cmd.SetComputeTextureParam(VoxelVisualization, voxelVisualization_Kernel, _Visualization_Output, DebugOutput);
                    cmdList.SetComputeIntParam(VoxelVisualization, _MultibounceMode, (int)HResources.GeneralData.Multibounce);
                    ctx.cmd.DispatchCompute(VoxelVisualization, voxelVisualization_Kernel, fullResX_8, fullResY_8, TextureXR.slices);
                }
            }
            
            if (Time.frameCount % 2 == 0)
                HFrameIndex++;
            
            HashUpdateFrameIndex++;

            if (HResources.GeneralData.DebugModeWS !=  DebugModeWS.None)
                cmdList.SetGlobalTexture(g_HTraceBufferGI, DebugOutput);
        }

        private void ReflectionIndirectLighting(CustomPassContext ctx)
        {
        }
        
        private static readonly int _HTraceReflectionsGI_SpatialFilteringRadius = Shader.PropertyToID("_HTraceReflectionsGI_SpatialFilteringRadius");
        private static readonly int _HTraceReflectionsGI_JitterRadius           = Shader.PropertyToID("_HTraceReflectionsGI_JitterRadius");
        private static readonly int _HTraceReflectionsGI_TemporalJitter         = Shader.PropertyToID("_HTraceReflectionsGI_TemporalJitter");
        private static readonly int _HTraceReflectionsGI_RayBias                = Shader.PropertyToID("_HTraceReflectionsGI_RayBias");
        private static readonly int _HTraceReflectionsGI_MaxRayLength           = Shader.PropertyToID("_HTraceReflectionsGI_MaxRayLength");

        protected override void Cleanup()
        {
            base.Cleanup();
            
            HExtensions.HRelease(PointDistributionBuffer);
            AllocateMainRT(true);
            AllocateDepthPyramidRT(new Vector2Int(0,0) ,true);
            AllocateSSAO_RT(true);
            AllocateDebugRT(true);
            AllocateIndirectionBuffers(new Vector2Int(0,0), true);
            ReallocHashBuffers(true);
            PrevScreenResolution = Vector2Int.zero;
        }
        
        // Using Cleanup is not ideal, because after launch it's called (again?), but he have to use it because
        // otherwise we get "RTHandleSystem.Initialize should be called once..." error.
        protected internal void Release() 
        {
        }
    }
}
