using UnityEngine.Rendering.HighDefinition;

namespace HTraceWSGI.Scripts.Globals
{
	public enum DebugModeWS
	{
		None = 0,
		GlobalIllumination,
		GeometryNormals,
		Shadowmap,
		VoxelizedColor,
		VoxelizedLighting,
	}

	public enum VoxelizationUpdateMode
	{
		//todo: stuggered uncomment
		Constant = 0,
		Partial
	}

	public enum RayCountMode
	{
		Performance = 0,
		Quality,
		Cinematic
	}

	public enum IndirectEvaluationMethod
	{
		None = 0,
		Tracing,
		Approximation
	}

	public enum SpatialRadius
	{
		None = 0,
		Medium,
		Wide
	}

	public enum HInjectionPoint
	{
		AfterOpaqueDepthAndNormal = CustomPassInjectionPoint.AfterOpaqueDepthAndNormal,
		BeforeTransparent = CustomPassInjectionPoint.BeforeTransparent,
		BeforePostProcess = CustomPassInjectionPoint.BeforePostProcess,
	}

	public enum Multibounce
	{
		None = 0,
		IrradianceCache = 1,
		AdaptiveProbeVolumes,
	}
}
