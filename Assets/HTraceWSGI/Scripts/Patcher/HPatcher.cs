#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HTraceWSGI.Scripts.Globals;
using HTraceWSGI.Scripts.PipelinesConfigurator;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = System.Object;

using UnityEngine.Rendering.HighDefinition;

namespace HTraceWSGI.Scripts.Patcher
{
	internal static class HPatcher
	{
		private static bool TryGetValue(object obj, string fieldName, out object value)
		{
			value = null;
			if (obj == null)
			{
				Debug.LogError($"obj is null, fieldName: {fieldName}");
				return false;
			}
			FieldInfo fi = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if (fi == null)
			{
				Debug.LogError($"FieldInfo is null, fieldName: {fieldName}");
				return false;
			}
			value = fi.GetValue(obj);
			return value != null;
		}
		
		private static bool TryGetField(object obj, string fieldName, out FieldInfo value)
		{
			value = null;
			if (obj == null)
			{
				Debug.LogError($"obj is null, fieldName: {fieldName}");
				return false;
			}
			FieldInfo fi = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if (fi == null)
			{
				Debug.LogError($"FieldInfo is null, fieldName: {fieldName}");
				return false;
			}
			value = fi;
			return value != null;
		}
		
		private static bool IsGlobalSettingsWithHtraceResources()
		{
			RenderPipelineGlobalSettings globalSettings = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();
			if (globalSettings == null)
			{
				Debug.LogError("GlobalSettings for HDRenderPipeline not found.");
				return false;
			}
			
#if  UNITY_6000_0_OR_NEWER
			if (!TryGetValue(globalSettings, "m_Settings",     out var mSettings) ||
			    !TryGetValue(mSettings,      "m_SettingsList", out var settingsList) ||
			    !TryGetValue(settingsList,   "m_List",         out var rawList) ||
			    rawList is not IList list)
			{
				Debug.LogError("List with shaders in RenderPipelineGlobalSettings not found.");
				return false;
			}
			
			ComputeShader ssgiCs  = null;
			ComputeShader rtDeferCs = null;
			
			foreach (var item in list)
			{
				if (item == null) continue;

				var t = item.GetType().Name;
				if (t.IndexOf("HDRenderPipelineRuntimeShaders", StringComparison.OrdinalIgnoreCase) >= 0 &&
				    TryGetValue(item, "m_ScreenSpaceGlobalIlluminationCS", out var ssgiCompute))
					ssgiCs = ssgiCompute as ComputeShader;

				if (t.IndexOf("HDRPRayTracingResources", StringComparison.OrdinalIgnoreCase) >= 0 &&
				    TryGetValue(item, "m_DeferredRayTracingCS", out var raytracingDeferredCompute))
					rtDeferCs = raytracingDeferredCompute as ComputeShader;
			}
#else
			if (!TryGetValue(globalSettings, "m_RenderPipelineResources", out var hdrpRes) ||
			    !TryGetValue(hdrpRes, "shaders",                         out var shaders) ||
			    !TryGetValue(shaders, "screenSpaceGlobalIlluminationCS", out var ssgiObj))
				return false;

			if (!TryGetValue(globalSettings, "m_RenderPipelineRayTracingResources", out var rtRes) ||
			    !TryGetValue(rtRes,          "deferredRaytracingCS",                out var deferObj))
				return false;

			var ssgiCs    = ssgiObj as ComputeShader;
			var rtDeferCs = deferObj as ComputeShader;
#endif
			
			return ssgiCs  != null &&
			       rtDeferCs != null &&
			       ssgiCs.name.IndexOf("HTrace", StringComparison.OrdinalIgnoreCase)  >= 0 &&
			       rtDeferCs.name.IndexOf("HTrace", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		
		//Project settings - Global Settings - Resources
		public static void RenderPipelineRuntimeResourcesPatch(bool forceReplace = false)
		{
			if (IsGlobalSettingsWithHtraceResources() == true)
				return;

			CreateRpResourcesFolders();
			CopyAndChangeSSGICompute(forceReplace: forceReplace);
			CopyAndChangeRaytracingDeferredCompute(forceReplace: forceReplace);

#if  UNITY_6000_0_OR_NEWER
			SetNewRuntimeResourcesInUnity6000();
#else
			SetNewRuntimeResourcesInUnity();
#endif
		}
		

		private static void CreateRpResourcesFolders()
		{
			if (!Directory.Exists(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources")))
			{
				Directory.CreateDirectory(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources"));
				AssetDatabase.Refresh();
			}
			
			if (!Directory.Exists(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "HDRP")))
			{
				Directory.CreateDirectory(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "HDRP"));
				AssetDatabase.Refresh();
			}
			
			// if (!Directory.Exists(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "URP")))
			// {
			// 	Directory.CreateDirectory(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "URP"));
			// 	AssetDatabase.Refresh();
			// }
			
			// if (!Directory.Exists(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "BIRP")))
			// {
			// 	Directory.CreateDirectory(Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "BIRP"));
			// 	AssetDatabase.Refresh();
			// }
		}
		
		public static void GlobalSettingsPlayerResourcesRestore()
		{
#if  UNITY_6000_0_OR_NEWER
			RenderPipelineGlobalSettings globalSettings = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();

			object    msettingsObject                      = globalSettings.GetType().GetField("m_Settings", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(globalSettings);
			object    settingsListObject                   = msettingsObject?.GetType().GetField("m_SettingsList", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(msettingsObject);
			object    mListObject                          = settingsListObject?.GetType().GetField("m_List", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(settingsListObject);
			object    hdRenderPipelineRuntimeShadersObject = null;
			FieldInfo ssgiComputeFieldInfo                 = null;
			foreach (object obj in (IList)mListObject)
			{
				if (obj.GetType().Name.Contains("HDRenderPipelineRuntimeShaders"))
				{
					ssgiComputeFieldInfo                 = obj.GetType().GetField("m_ScreenSpaceGlobalIlluminationCS", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
					hdRenderPipelineRuntimeShadersObject = obj;
				}
			}
				
			string originalSsgiComputePath = Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "HDRP Resources", "ScreenSpaceGlobalIlluminationOriginal.compute");
			string ssgiComputeRelativePath = Path.Combine("Assets",                        Path.GetRelativePath(Application.dataPath, originalSsgiComputePath)).ReplaceLeftSlashesToRight();
			Object originalSsgiComputeObject  = AssetDatabase.LoadMainAssetAtPath(ssgiComputeRelativePath);
			
			ssgiComputeFieldInfo?.SetValue(hdRenderPipelineRuntimeShadersObject, originalSsgiComputeObject);
#endif
		}

		private static void CopyAndChangeSSGICompute(bool revert = false, bool forceReplace = false)
		{
			string ssgiComputeFullPath       = Path.Combine(ConfiguratorUtils.GetFullHdrpPath(),    "Runtime", "Lighting", "ScreenSpaceLighting", "ScreenSpaceGlobalIllumination.compute");
			string ssgiComputeHtraceFullPath = Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(),  "RP Resources", "HDRP",      "ScreenSpaceGlobalIlluminationHTrace.compute");
			if (File.Exists(ssgiComputeHtraceFullPath) && forceReplace == false)
			{
				return;
			}
			try
			{
				File.Copy(ssgiComputeFullPath, ssgiComputeHtraceFullPath, true);
			}
			catch (Exception e)
			{
				Debug.LogError($"{ConfiguratorUtils.GetUnityAndHdrpVersion()} \n" +
				                                        $"Source path: {ssgiComputeFullPath} \n" +
				                                        $"Destination path: {ssgiComputeHtraceFullPath} \n" +
				                                        $"{e.Message}");
				return;
			}
			
			string[] pattern1 =
			{
				"// deferred opaque always use FPTL",
				"#define USE_FPTL_LIGHTLIST 1",
				"",
				"// HDRP generic includes",
			};

			
			string[] newpattern1 =
			{
				"// deferred opaque always use FPTL",
				"#define USE_FPTL_LIGHTLIST 1",
				"",
				"#pragma multi_compile _ HTRACE_OVERRIDE",
				"// HDRP generic includes",
			};
			
			string[] pattern2 =
			{
				"// Input depth pyramid texture",
				"TEXTURE2D_X(_DepthTexture);",
				"// Stencil buffer of the current frame",
				"TEXTURE2D_X_UINT2(_StencilTexture);",
				"// Input texture that holds the offset for every level of the depth pyramid",
				"StructuredBuffer<int2>  _DepthPyramidMipLevelOffsets;",
				"",
				"// Constant buffer that holds all scalar that we need",
				"CBUFFER_START(UnityScreenSpaceGlobalIllumination)",
			};

			
			string[] newpattern2 =
			{
				"// Input depth pyramid texture",
				"TEXTURE2D_X(_DepthTexture);",
				"// Stencil buffer of the current frame",
				"TEXTURE2D_X_UINT2(_StencilTexture);",
				"// Input texture that holds the offset for every level of the depth pyramid",
				"StructuredBuffer<int2>  _DepthPyramidMipLevelOffsets;",
				"// HTrace buffer",
				"TEXTURE2D_X(_HTraceBufferGI);",
				"",
				"// Constant buffer that holds all scalar that we need",
				"CBUFFER_START(UnityScreenSpaceGlobalIllumination)",
			};
			
			string[] pattern3 =
			{
				"[numthreads(INDIRECT_DIFFUSE_TILE_SIZE, INDIRECT_DIFFUSE_TILE_SIZE, 1)]",
				"void TRACE_GLOBAL_ILLUMINATION(uint3 dispatchThreadId : SV_DispatchThreadID, uint2 groupThreadId : SV_GroupThreadID, uint2 groupId : SV_GroupID)",
				"{",
				"    UNITY_XR_ASSIGN_VIEW_INDEX(dispatchThreadId.z);",
				"",
				"    // Compute the pixel position to process",
				"    uint2 currentCoord = dispatchThreadId.xy;",
				"    uint2 inputCoord = dispatchThreadId.xy;",
			};

			
			string[] newpattern3 =
			{
				"[numthreads(INDIRECT_DIFFUSE_TILE_SIZE, INDIRECT_DIFFUSE_TILE_SIZE, 1)]",
				"void TRACE_GLOBAL_ILLUMINATION(uint3 dispatchThreadId : SV_DispatchThreadID, uint2 groupThreadId : SV_GroupThreadID, uint2 groupId : SV_GroupID)",
				"{",
				"    UNITY_XR_ASSIGN_VIEW_INDEX(dispatchThreadId.z);",
				"",
				"#if defined HTRACE_OVERRIDE",
				"    return;",
				"#endif",
				"    // Compute the pixel position to process",
				"    uint2 currentCoord = dispatchThreadId.xy;",
				"    uint2 inputCoord = dispatchThreadId.xy;",
			};
			
			string[] pattern4 =
			{
				"[numthreads(INDIRECT_DIFFUSE_TILE_SIZE, INDIRECT_DIFFUSE_TILE_SIZE, 1)]",
				"void REPROJECT_GLOBAL_ILLUMINATION(uint3 dispatchThreadId : SV_DispatchThreadID, uint2 groupThreadId : SV_GroupThreadID, uint2 groupId : SV_GroupID)",
				"{",
				"    UNITY_XR_ASSIGN_VIEW_INDEX(dispatchThreadId.z);",
				"",
				"    // Compute the pixel position to process",
				"    uint2 inputCoord = dispatchThreadId.xy;",
				"    uint2 currentCoord = dispatchThreadId.xy;",
			};

			
			string[] newpattern4 =
			{
				"[numthreads(INDIRECT_DIFFUSE_TILE_SIZE, INDIRECT_DIFFUSE_TILE_SIZE, 1)]",
				"void REPROJECT_GLOBAL_ILLUMINATION(uint3 dispatchThreadId : SV_DispatchThreadID, uint2 groupThreadId : SV_GroupThreadID, uint2 groupId : SV_GroupID)",
				"{",
				"    UNITY_XR_ASSIGN_VIEW_INDEX(dispatchThreadId.z);",
				"",
				"#if defined HTRACE_OVERRIDE",
				"        _IndirectDiffuseTextureRW[COORD_TEXTURE2D_X(dispatchThreadId.xy)] = LOAD_TEXTURE2D_X(_HTraceBufferGI, dispatchThreadId.xy).xyz;",
				"        return;",
				"#endif",
				"    // Compute the pixel position to process",
				"    uint2 inputCoord = dispatchThreadId.xy;",
				"    uint2 currentCoord = dispatchThreadId.xy;",
			};
			
			List<string[]> patterns = new List<string[]>()
			{
				pattern1, pattern2, pattern3, pattern4,
			};
			
			List<string[]> newpatterns = new List<string[]>()
			{
				newpattern1, newpattern2, newpattern3, newpattern4,
			};
			
			List<string> resultLines = new List<string>();
			if(revert == false)
				PatcherUtils.ReplacePatterns(ssgiComputeHtraceFullPath, patterns, newpatterns, ref resultLines);
			else
				PatcherUtils.ReplacePatterns(ssgiComputeHtraceFullPath, newpatterns, patterns, ref resultLines);


			File.WriteAllLines(ssgiComputeHtraceFullPath, resultLines);
			AssetDatabase.Refresh();
		}
		
		private static void CopyAndChangeRaytracingDeferredCompute(bool revert = false, bool forceReplace = false)
		{
			string rtDeferredComputeFullPath = Path.Combine(ConfiguratorUtils.GetFullHdrpPath(), "Runtime", "RenderPipeline", "Raytracing", "Shaders",
				"Deferred", "RaytracingDeferred.compute");
			string rtDeferredComputeHtraceFullPath = Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "HDRP",           "RaytracingDeferredHTrace.compute");
			if (File.Exists(rtDeferredComputeHtraceFullPath) && forceReplace == false)
			{
				return;
			}
			try
			{
				File.Copy(rtDeferredComputeFullPath, rtDeferredComputeHtraceFullPath, true);
			}
			catch (Exception e)
			{
				Debug.LogError($"{ConfiguratorUtils.GetUnityAndHdrpVersion()} \n" +
				                                        $"Source path: {rtDeferredComputeFullPath} \n" +
				                                        $"Destination path: {rtDeferredComputeHtraceFullPath} \n" +
				                                        $"{e.Message}");
				return;
			}
			
			string[] pattern1 =
			{
				"#pragma kernel RaytracingDiffuseDeferred",
				"",
				"// Given that the algorithm requires BSDF evaluation, we need to define this macro",
				"#define HAS_LIGHTLOOP",
			};

			
			string[] newpattern1 =
			{
				"#pragma kernel RaytracingDiffuseDeferred",
				"",
				"#pragma multi_compile _ GI_APPROXIMATION_IN_REFLECTIONS GI_TRACING_IN_REFLECTIONS",
				"#pragma multi_compile _ GI_APPROXIMATION_IN_REFLECTIONS_OCCLUSION_CHECK",
				"#include \"../../Resources/HTraceWSGI/Includes/ReflectionGI.hlsl\"",
				"",
				"// Given that the algorithm requires BSDF evaluation, we need to define this macro",
				"#define HAS_LIGHTLOOP",
			};
			
			string[] pattern2 =
			{
				"    finalColor = (diffuseLighting + specularLighting);",
				"",
				"    // Apply fog attenuation",
				"    ApplyFogAttenuation(sourcePosInput.positionWS, rayDirection, rayDistance, finalColor, true);",
			};

			
			string[] newpattern2 =
			{
				"    finalColor = (diffuseLighting + specularLighting);",
				"",
				"    finalColor += HTraceIndirectLighting(currentCoord, intersectionPositionWS, bsdfData.normalWS, bsdfData.diffuseColor);",
				"",
				"    // Apply fog attenuation",
				"    ApplyFogAttenuation(sourcePosInput.positionWS, rayDirection, rayDistance, finalColor, true);",
			};
			
			List<string[]> patterns = new List<string[]>()
			{
				pattern1, pattern2,
			};
			
			List<string[]> newpatterns = new List<string[]>()
			{
				newpattern1, newpattern2,
			};
			
			List<string> resultLines = new List<string>();
			if(revert == false)
				PatcherUtils.ReplacePatterns(rtDeferredComputeHtraceFullPath, patterns, newpatterns, ref resultLines);
			else
				PatcherUtils.ReplacePatterns(rtDeferredComputeHtraceFullPath, newpatterns, patterns, ref resultLines);
			
			
			File.WriteAllLines(rtDeferredComputeHtraceFullPath, resultLines);
			AssetDatabase.Refresh();
		}

		private static void SetNewRuntimeResourcesInUnity()
		{
			RenderPipelineGlobalSettings globalSettings = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();
			if (globalSettings == null)
			{
				Debug.LogError("GlobalSettings for HDRenderPipeline not found.");
				return;
			}
			
			string runtimeResourcePath = Path.Combine(ConfiguratorUtils.GetFullHdrpPath(),    "Runtime", "RenderPipelineResources", "HDRenderPipelineRuntimeResources.asset");
			string runtimeResourcesRelativePath = Path.Combine("Assets", Path.GetRelativePath(Application.dataPath, runtimeResourcePath)).ReplaceLeftSlashesToRight();
			UnityEngine.Object unityRuntimeResourcesObject = AssetDatabase.LoadMainAssetAtPath(runtimeResourcesRelativePath);

			string             ssgiFullPath     = Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "HDRP", "ScreenSpaceGlobalIlluminationHTrace.compute");
			string             ssgiRelativePath = Path.Combine("Assets",                                Path.GetRelativePath(Application.dataPath, ssgiFullPath)).ReplaceLeftSlashesToRight();
			UnityEngine.Object ssgiObject       = AssetDatabase.LoadMainAssetAtPath(ssgiRelativePath);
			
			
			if (TryGetValue(globalSettings,   "m_RenderPipelineResources",       out var hdrpResourcesObj) &&
			    TryGetValue(hdrpResourcesObj, "shaders",                         out var shadersObj) &&
			    TryGetField(shadersObj,       "screenSpaceGlobalIlluminationCS", out var ssgiField))
			{
				ssgiField?.SetValue(shadersObj, ssgiObject);
			}
			
			//RT part
			
			string             rtResourcePath          = Path.Combine(ConfiguratorUtils.GetFullHdrpPath(), "Runtime", "RenderPipelineResources", "HDRenderPipelineRayTracingResources.asset");
			string             rtResourcesRelativePath = Path.Combine("Assets",                            Path.GetRelativePath(Application.dataPath, rtResourcePath)).ReplaceLeftSlashesToRight();
			UnityEngine.Object unityRTResourcesObject  = AssetDatabase.LoadMainAssetAtPath(rtResourcesRelativePath);
			
			string             raytracingDeferredComputeFullPath     = Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "HDRP", "RaytracingDeferredHTrace.compute");
			string             raytracingDeferredComputeRelativePath = Path.Combine("Assets",                                Path.GetRelativePath(Application.dataPath, raytracingDeferredComputeFullPath)).ReplaceLeftSlashesToRight();
			UnityEngine.Object raytracingDeferredComputeObject       = AssetDatabase.LoadMainAssetAtPath(raytracingDeferredComputeRelativePath);
			
			if (TryGetValue(globalSettings, "m_RenderPipelineRayTracingResources", out var hdrpRTResourcesObj) &&
			    TryGetField(hdrpRTResourcesObj, "deferredRaytracingCS", out var raytracingDeferredComputeField))
			{
				raytracingDeferredComputeField?.SetValue(hdrpRTResourcesObj, raytracingDeferredComputeObject);
			}
		}
		
		private static void SetNewRuntimeResourcesInUnity6000()
		{
			RenderPipelineGlobalSettings globalSettings = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();
			if (globalSettings == null)
			{
				Debug.LogError("GlobalSettings for HDRenderPipeline not found.");
				return;
			}

			if (!TryGetValue(globalSettings,     "m_Settings",     out var msettingsObject) ||
			    !TryGetValue(msettingsObject,    "m_SettingsList", out var settingsListObject) ||
			    !TryGetValue(settingsListObject, "m_List",         out var mListObject) ||
			    mListObject is not IList list)
			{
				Debug.LogError("Failed to get internal settings list.");
				return;
			}
			
			string ssgiComputePath               = Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "HDRP", "ScreenSpaceGlobalIlluminationHTrace.compute");
			string raytracingDeferredComputePath = Path.Combine(ConfiguratorUtils.GetHTraceFolderPath(), "RP Resources", "HDRP", "RaytracingDeferredHTrace.compute");
			
			string ssgiComputeRelativePath               = Path.Combine("Assets", Path.GetRelativePath(Application.dataPath, ssgiComputePath)).ReplaceLeftSlashesToRight();
			string raytracingDeferredComputeRelativePath = Path.Combine("Assets", Path.GetRelativePath(Application.dataPath, raytracingDeferredComputePath)).ReplaceLeftSlashesToRight();

			Object ssgiComputeObject   = AssetDatabase.LoadMainAssetAtPath(ssgiComputeRelativePath);
			Object rtDeferredComputeObject   = AssetDatabase.LoadMainAssetAtPath(raytracingDeferredComputeRelativePath);
			
			object    hdRenderPipelineRuntimeShadersObject = null;
			object    hdrpRayTracingResourcesObject        = null;
			FieldInfo ssgiComputeFieldInfo                 = null;
			FieldInfo raytracingDeferredComputeFieldInfo   = null;
			
			foreach (var obj in list)
			{
				if (obj == null)
					continue;

				var typeName = obj.GetType().Name;

				if (typeName.IndexOf("HDRenderPipelineRuntimeShaders", System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					if (TryGetField(obj, "m_ScreenSpaceGlobalIlluminationCS", out ssgiComputeFieldInfo))
						hdRenderPipelineRuntimeShadersObject = obj;
				}
				else if (typeName.IndexOf("HDRPRayTracingResources", System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					if (TryGetField(obj, "m_DeferredRayTracingCS", out raytracingDeferredComputeFieldInfo))
						hdrpRayTracingResourcesObject = obj;
				}
			}

			if (ssgiComputeFieldInfo != null && hdRenderPipelineRuntimeShadersObject != null)
				ssgiComputeFieldInfo.SetValue(hdRenderPipelineRuntimeShadersObject, ssgiComputeObject);

			if (raytracingDeferredComputeFieldInfo != null && hdrpRayTracingResourcesObject != null)
				raytracingDeferredComputeFieldInfo.SetValue(hdrpRayTracingResourcesObject, rtDeferredComputeObject);
			
			AssetDatabase.ImportAsset(ssgiComputeRelativePath,               ImportAssetOptions.ForceUpdate);
			AssetDatabase.ImportAsset(raytracingDeferredComputeRelativePath, ImportAssetOptions.ForceUpdate);
		}
	}
}
#endif
