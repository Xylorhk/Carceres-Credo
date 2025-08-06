using HTraceWSGI.Scripts.Globals;
using HTraceWSGI.Scripts.Pipeline;
using HTraceWSGI.Scripts.Structs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace HTraceWSGI.Scripts.Passes
{
	internal class HTraceFinalPass : CustomPass
	{
		private static readonly int g_HTraceShadowmap = Shader.PropertyToID("_HTraceShadowmap");
		
		private static readonly int _Debug_Output_Name = Shader.PropertyToID("_Debug_Output");
		private static readonly int _DebugModeEnumWs_Name = Shader.PropertyToID("_DebugModeEnumWS");

		private static ProfilingSampler s_DebugProfilingSampler = new ProfilingSampler("Debug");

		RTHandle OutputTarget;
		
		ComputeShader HDebug;
		ComputeShader HReflectionProbeCompose;

		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			name = HTraceNames.HTRACE_FINAL_PASS_NAME_FRAME_DEBUG;
			
			HDebug = HExtensions.LoadComputeShader("HDebug");
			HReflectionProbeCompose = HExtensions.LoadComputeShader("HReflectionProbeCompose");
			
			AllocateDebugBuffer();
		}

		private void AllocateDebugBuffer()
		{
			HExtensions.HRelease(OutputTarget);

			if (Application.isPlaying == false)
				TextureXR.maxViews = 1;

			var colorBufferFormat = HExtensions.HdrpAsset?.currentPlatformRenderPipelineSettings.colorBufferFormat == RenderPipelineSettings.ColorBufferFormat.R11G11B10 ? GraphicsFormat.B10G11R11_UFloatPack32 : GraphicsFormat.R16G16B16A16_SFloat;

			OutputTarget = RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
				colorFormat: colorBufferFormat, name: "_OutputTarget", enableRandomWrite: true); 
		}

		protected override void Execute(CustomPassContext ctx)
		{
			Texture ShadowmapData = Shader.GetGlobalTexture(g_HTraceShadowmap);
			if (ShadowmapData == null || ShadowmapData.width != 2048)
				return;
			
			var cmdList = ctx.cmd;
			var hdCamera = ctx.hdCamera.camera;
			
			if (hdCamera.cameraType == CameraType.Reflection)
				return;
			
			int DebugDispatchX = (ctx.hdCamera.actualWidth + 8 - 1) / 8;
			int DebugDispatchY = (ctx.hdCamera.actualHeight + 8 - 1) / 8;
			
			// if (hdCamera.cameraType == CameraType.Reflection)
			// {	
			// 	// Render to real-time reflection probe
			// 	int reflection_probe_compose_kernel = HReflectionProbeCompose.FindKernel("RenderVoxelsForReflectionProbes");
			// 	ctx.cmd.SetComputeTextureParam(HReflectionProbeCompose, reflection_probe_compose_kernel, "_Output", OutputTarget);
			// 	ctx.cmd.DispatchCompute(HReflectionProbeCompose, reflection_probe_compose_kernel, DebugDispatchX, DebugDispatchY, 1);
			//
			// 	// Copy to camera color buffer
			// 	ctx.cmd.CopyTexture(OutputTarget, ctx.cameraColorBuffer);
			// 	return;	
			// }

			VoxelizationRuntimeData.VoxelizationModeChanged = false;

			if (HResources.GeneralData.DebugModeWS == DebugModeWS.None) 
				return;
			
			using (new ProfilingScope(cmdList, s_DebugProfilingSampler))
			{
				// Render debug
				int DebugKernel = HDebug.FindKernel("Debug");
				cmdList.SetComputeTextureParam(HDebug, DebugKernel, _Debug_Output_Name, OutputTarget, 0);
				cmdList.SetComputeIntParam(HDebug, _DebugModeEnumWs_Name, (int)HResources.GeneralData.DebugModeWS); 
				cmdList.DispatchCompute(HDebug, DebugKernel, DebugDispatchX, DebugDispatchY, TextureXR.slices);
				
				// Copy to camera color buffer
				ctx.cmd.CopyTexture(OutputTarget, ctx.cameraColorBuffer);
			}
		}
		
		protected override void Cleanup()
		{	
			HExtensions.HRelease(OutputTarget);
		}
	}
}
