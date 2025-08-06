using HTraceWSGI.Scripts.Structs;
using UnityEngine;

namespace HTraceWSGI.Scripts.Pipeline
{
	public class HResources
	{
		// Datas
		[SerializeField]
		internal static GeneralData GeneralData;
		[SerializeField]
		internal static ScreenSpaceLightingData ScreenSpaceLightingData;
		[SerializeField]
		internal static ReflectionIndirectLightingData ReflectionIndirectLightingData;
		[SerializeField]
		internal static DebugData DebugData;
		[SerializeField]
		internal static VoxelizationData VoxelizationData;
	}
}
