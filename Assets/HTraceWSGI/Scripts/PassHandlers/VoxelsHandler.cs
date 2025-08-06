using System;
using HTraceWSGI.Scripts.Globals;
using HTraceWSGI.Scripts.Infrastructure;
using HTraceWSGI.Scripts.Passes;
using HTraceWSGI.Scripts.Pipeline;
using HTraceWSGI.Scripts.Structs;
using HTraceWSGI.Scripts.VoxelCameras;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace HTraceWSGI.Scripts.PassHandlers
{
	[ExecuteInEditMode]
	internal class VoxelsHandler : PassHandler
	{
		private bool  _initialized;
		
		//Debug fields
		public   bool   ShowVoxelBounds = false;
		internal Bounds BoundsGizmo;
		internal Bounds BoundsGizmoFromUI = new Bounds();

		private VoxelizationPassConstant  _voxelizationPassConstant;
		private VoxelizationPassPartial   _voxelizationPassPartial;

		private Transform              _prevAttachTo;
		private VoxelizationUpdateMode _prevVoxelizationUpdateMode;
		private int _prevLodMax;

		public bool NeedToReallocForUI
		{
			get { return _needToReallocForUI; }
		}

		//UI fields for Apply Params button
		private bool  _needToReallocForUI = false;
		private float _prevDensityUI;
		private int   _prevVoxelBoundsUI;
		private int   _prevOverrideBoundsHeightUI;

		private void OnEnable()
		{
		}

		public void Initialize(CustomPass[] passes)
		{
			for (int index = 0; index < passes.Length; index++)
			{
				if (passes[index] is VoxelizationPassConstant voxelizationPassConstant)
					_voxelizationPassConstant = voxelizationPassConstant;
				if (passes[index] is VoxelizationPassPartial voxelizationPassPassPartial)
					_voxelizationPassPartial = voxelizationPassPassPartial;
			}

			CreateVoxelCamera();
			CreateVoxelCullingCamera();
			CreateVoxelOctantCamera();
			CreateHTraceCameraDirectional();

			SwitchPass();

			VoxelizationRuntimeData.FullVoxelization  =  false;
			VoxelizationRuntimeData.OnReallocTextures += ReallocTextures;

			if (HResources.VoxelizationData.AttachTo == null && Camera.main != null)
				HResources.VoxelizationData.AttachTo = Camera.main.transform;
			_prevAttachTo = HResources.VoxelizationData.AttachTo;

			_initialized = true;
		}

		private void ReallocTextures()
		{
			_needToReallocForUI = false;
		}

		private void SwitchPass()
		{
			switch (_prevVoxelizationUpdateMode)
			{
				case VoxelizationUpdateMode.Constant:
					_voxelizationPassConstant.Release();
					_voxelizationPassConstant.enabled = false;
					break;
				case VoxelizationUpdateMode.Partial:
					_voxelizationPassPartial.Release();
					_voxelizationPassPartial.enabled = false;
					break;
			}
			
			switch (HResources.VoxelizationData.VoxelizationUpdateMode)
			{
				case VoxelizationUpdateMode.Constant:
					_voxelizationPassConstant.Initialize();
					break;
				case VoxelizationUpdateMode.Partial:
					_voxelizationPassPartial.Initialize();
					break;
			}

			_prevVoxelizationUpdateMode = HResources.VoxelizationData.VoxelizationUpdateMode;
			VoxelizationRuntimeData.VoxelizationModeChanged = true;
		}

		protected override void Update()
		{
			base.Update();
			
			if (!_initialized || HResources.VoxelizationData == null || VoxelizationRuntimeData.FakeDirectionalCamera == null)
				return;

			if (_prevVoxelizationUpdateMode != HResources.VoxelizationData.VoxelizationUpdateMode)
				SwitchPass();
			
			//gizmo update for camera bounds
#if UNITY_EDITOR
			if (Application.isEditor)
			{
				BoundsGizmo = GetVoxelCameraBounds();
				SetVoxelCameraBounds(BoundsGizmo);
			}
#endif

			CheckBounds();
			HResources.VoxelizationData.UpdateData();
			CheckPrevValues();
		}

		public void OnSceneGUI()
		{
			//gizmo update
			BoundsGizmo = GetVoxelCameraBounds();

			if (BoundsGizmoFromUI.size != BoundsGizmo.size /* && _boundsGizmoFromUIEdited*/)
			{
				SetVoxelCameraBounds(BoundsGizmoFromUI);
				BoundsGizmo = GetVoxelCameraBounds();
				// _boundsGizmoFromUIEdited = false;
			}
		}

		public Bounds GetVoxelCameraBounds()
		{
			Vector3 boundCenter = VoxelizationRuntimeData.VoxelCamera.transform.position;

			float height = HResources.VoxelizationData.OverrideBoundsHeightEnable == false ? HResources.VoxelizationData.VoxelBounds : HResources.VoxelizationData.OverrideBoundsHeight;
			if (HResources.VoxelizationData.GroundLevelEnable == true && (VoxelizationRuntimeData.VoxelCamera.transform.position.y - height / 2) < HResources.VoxelizationData.GroundLevel)
			{
				boundCenter = new Vector3(VoxelizationRuntimeData.VoxelCamera.transform.position.x, HResources.VoxelizationData.GroundLevel + height / 2, VoxelizationRuntimeData.VoxelCamera.transform.position.z);
			}

			BoundsGizmo.center = boundCenter;

			BoundsGizmo.size = new Vector3(
				HResources.VoxelizationData.VoxelBounds,
				height,
				HResources.VoxelizationData.VoxelBounds);

			return BoundsGizmo;
		}

		public void SetVoxelCameraBounds(Bounds newBounds)
		{
			if (newBounds.size.x < 1 || newBounds.size.y < 1 || newBounds.size.z < 1)
				return;
			
			int newWidthDepth = 0;
			if ((int)BoundsGizmo.size.x != (int)newBounds.size.x)
				newWidthDepth = (int)newBounds.size.x;
			if ((int)BoundsGizmo.size.z != (int)newBounds.size.z)
				newWidthDepth = (int)newBounds.size.z;
			if (newWidthDepth == 0)
			{
				if (HResources.VoxelizationData.OverrideBoundsHeightEnable == false)
					newWidthDepth = (int)newBounds.size.y;
				else
					newWidthDepth = (int)newBounds.size.x;
			}

			HResources.VoxelizationData.VoxelBounds = newWidthDepth;
			HResources.VoxelizationData.OverrideBoundsHeight = HResources.VoxelizationData.OverrideBoundsHeightEnable == true ? (int)newBounds.size.y : HResources.VoxelizationData.OverrideBoundsHeight;

			if (HResources.VoxelizationData.OverrideBoundsHeightEnable == true)
				HResources.VoxelizationData.OverrideBoundsHeight = Mathf.Clamp(HResources.VoxelizationData.OverrideBoundsHeight, 1, HResources.VoxelizationData.VoxelBounds);
			
			// float height = HResources.VoxelizationData.OverrideBoundsHeightEnable == false ? HResources.VoxelizationData.VoxelBounds : HResources.VoxelizationData.OverrideBoundsHeight;
			// if (HResources.VoxelizationData.GroundLevelEnable == true && (VoxelizationRuntimeData.VoxelCamera.transform.position.y - height / 2) <= HResources.VoxelizationData.GroundLevel)
			// {
			// 	_boundsGizmo.center = newBounds.center;
			// }

			//_boundsGizmo.size = newBounds.size;
			BoundsGizmo = GetVoxelCameraBounds();
		}

		private void CheckBounds()
		{
			if (VoxelizationRuntimeData.CheckBounds(HResources.VoxelizationData.VoxelDensity, HResources.VoxelizationData.VoxelBounds, HResources.VoxelizationData.OverrideBoundsHeight))
			{
				if (Time.frameCount > 3) // hack for enter and exit in Play mode
					_needToReallocForUI = true;

				VoxelizationRuntimeData.SetParamsForApplyButton(HResources.VoxelizationData.VoxelDensity, HResources.VoxelizationData.VoxelBounds, HResources.VoxelizationData.OverrideBoundsHeight);
			}
		}

		private void CheckPrevValues()
		{
			if (HResources.VoxelizationData.AttachTo != _prevAttachTo)
			{
				_prevAttachTo = HResources.VoxelizationData.AttachTo;
				VoxelizationRuntimeData.OnReallocTextures?.Invoke();
			}
			if (HResources.VoxelizationData.LODMax != _prevLodMax)
			{
				_prevLodMax = HResources.VoxelizationData.LODMax;
				VoxelizationRuntimeData.FullVoxelization = true;
			}
		}

		private void CreateVoxelCamera()
		{
			if (VoxelizationRuntimeData.VoxelCamera != null)
			{
				VoxelizationRuntimeData.VoxelCamera.Initialize(this);
				return;
			}

			GameObject cameraGO = new GameObject(HTraceNames.HTRACE_VOXEL_CAMERA_NAME);
			cameraGO.layer     = gameObject.layer;
			cameraGO.hideFlags = HResources.DebugData.ShowBowels ? HideFlags.None : HideFlags.HideAndDontSave;
			// cameraGO.transform.parent = Camera.main.transform;
			// cameraGO.transform.localPosition = Vector3.zero;
			VoxelizationRuntimeData.VoxelCamera = cameraGO.AddComponent<VoxelCamera>();
			VoxelizationRuntimeData.VoxelCamera.Initialize(this);
		}

		private void CreateVoxelCullingCamera()
		{
			if (VoxelizationRuntimeData.CullingCamera != null)
			{
				VoxelizationRuntimeData.CullingCamera.Initialize();
				return;
			}

			GameObject cameraGO = new GameObject(HTraceNames.HTRACE_VOXEL_CULLING_CAMERA_NAME);
			cameraGO.layer            = gameObject.layer;
			cameraGO.transform.parent = VoxelizationRuntimeData.VoxelCamera.gameObject.transform;
			cameraGO.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			cameraGO.hideFlags = HResources.DebugData.ShowBowels ? HideFlags.None : HideFlags.HideAndDontSave;
			// cameraGO.transform.parent = Camera.main.transform;
			// cameraGO.transform.localPosition = Vector3.zero;
			VoxelizationRuntimeData.CullingCamera = cameraGO.AddComponent<VoxelCullingCamera>();
			VoxelizationRuntimeData.CullingCamera.Initialize();
		}

		private void CreateVoxelOctantCamera()
		{
			if (VoxelizationRuntimeData.VoxelOctantCamera != null)
			{
				VoxelizationRuntimeData.VoxelOctantCamera.Initialize();
				return;
			}

			GameObject cameraGO = new GameObject(HTraceNames.HTRACE_VOXEL_OCTANT_CAMERA_NAME);
			cameraGO.layer            = gameObject.layer;
			cameraGO.transform.parent = VoxelizationRuntimeData.VoxelCamera.gameObject.transform;
			cameraGO.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			cameraGO.hideFlags = HResources.DebugData.ShowBowels ? HideFlags.None : HideFlags.HideAndDontSave;
			// cameraGO.transform.parent = Camera.main.transform;
			// cameraGO.transform.localPosition = Vector3.zero;
			VoxelizationRuntimeData.VoxelOctantCamera = cameraGO.AddComponent<VoxelOctantCamera>();
			VoxelizationRuntimeData.VoxelOctantCamera.Initialize();
		}

		private void CreateHTraceCameraDirectional()
		{
			if (VoxelizationRuntimeData.FakeDirectionalCamera != null)
			{
				VoxelizationRuntimeData.FakeDirectionalCamera.Initialize(this);
				return;
			}

			GameObject cameraFromFakeDirLightGo = new GameObject("HTraceDirectionalCameraHandler");
			cameraFromFakeDirLightGo.layer = gameObject.layer;
			cameraFromFakeDirLightGo.hideFlags = HResources.DebugData.ShowBowels ? HideFlags.None : HideFlags.HideAndDontSave;
			VoxelizationRuntimeData.FakeDirectionalCamera = cameraFromFakeDirLightGo.AddComponent<HTraceDirectionalCamera>();

			VoxelizationRuntimeData.FakeDirectionalCamera.Initialize(this);
		}

		public bool PingVoxelsHandler(VoxelCamera voxelCamera)
		{
			return VoxelizationRuntimeData.VoxelCamera != voxelCamera;
		}

		public bool PingFakeDirLight(HTraceDirectionalCamera camera)
		{
			return VoxelizationRuntimeData.FakeDirectionalCamera != camera;
		}

		private void OnDestroy()
		{
			Release();
		}

		internal void Release()
		{
			_voxelizationPassConstant?.Release();
			_voxelizationPassPartial?.Release();
			
			VoxelizationRuntimeData.OnReallocTextures -= ReallocTextures;

			_initialized = false;
		}
	}
}
