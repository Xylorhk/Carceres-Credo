using HTraceWSGI.Scripts.Globals;
using HTraceWSGI.Scripts.Structs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace HTraceWSGI.Scripts.Passes
{
	internal class HTracePrePass : CustomPass
	{
		private static readonly int g_HTraceStencilBuffer = Shader.PropertyToID("_HTraceStencilBuffer");

		private static readonly ProfilingSampler s_CopyingStencilMovingObject = new ProfilingSampler("Copying stencil moving object");
		
		RTHandle         HTraceStencilBuffer;

		private void AllocateBuffers(bool onlyRelease = false)
		{
			void ReleaseTextures()
			{
				HExtensions.HRelease(HTraceStencilBuffer);
			}
            
			if (onlyRelease)
			{
				ReleaseTextures();
				return;
			}

			ReleaseTextures();
			
			if (Application.isPlaying == false)
				TextureXR.maxViews = 1;
			
			// HDRenderPipelineAsset currentAsset = HExtensions.currentAsset;
			// var format = currentAsset.currentPlatformRenderPipelineSettings.colorBufferFormat == RenderPipelineSettings.ColorBufferFormat.R11G11B10 ? 
			// 	GraphicsFormat.B10G11R11_UFloatPack32 : GraphicsFormat.R16G16B16A16_SFloat;
			
			HTraceStencilBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.Depth32, dimension: TextureXR.dimension,
				colorFormat: GraphicsFormat.R32_SFloat, name: "_HTraceStencilBuffer", useDynamicScale: true);

		}

		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			name = HTraceNames.HTRACE_PRE_PASS_NAME_FRAME_DEBUG;

			AllocateBuffers();
			VoxelizationRuntimeData.FullVoxelization = true;
		}

		protected override void Execute(CustomPassContext ctx)
		{
			VoxelizationRuntimeData.FrameCount += 1;
			
			// Copying stencil moving object bit before it's overwritten by Unity. Needed for denoising (for both patched and unpatched versions).
			using (new ProfilingScope(ctx.cmd, s_CopyingStencilMovingObject))
			{
				if (ctx.cameraDepthBuffer.rt.volumeDepth == TextureXR.slices)
					ctx.cmd.CopyTexture(ctx.cameraDepthBuffer, HTraceStencilBuffer);
				
				ctx.cmd.SetGlobalTexture(g_HTraceStencilBuffer, HTraceStencilBuffer, RenderTextureSubElement.Stencil);
			}
		}

		protected override void Cleanup()
		{
		    base.Cleanup();
		    AllocateBuffers(true);
		}
	}
}
