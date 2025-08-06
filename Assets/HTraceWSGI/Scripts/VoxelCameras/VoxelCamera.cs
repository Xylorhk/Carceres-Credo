using System;
using HTraceWSGI.Scripts.Globals;
using HTraceWSGI.Scripts.PassHandlers;
using HTraceWSGI.Scripts.Pipeline;
using HTraceWSGI.Scripts.Structs;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HTraceWSGI.Scripts.VoxelCameras
{
	[ExecuteInEditMode]
	internal class VoxelCamera : MonoBehaviour
	{
		public Camera Camera
		{
			get { return _camera; }
		}

		private Camera                  _camera;
		private VoxelsHandler           _voxelsHandler;

		private bool    _dirtyBounds = false;
		private Vector3 _prevVoxelBounds;
		private Vector3 _rememberPos;

		public void Initialize(VoxelsHandler voxelsHandler)
		{
			_voxelsHandler           = voxelsHandler;
			CreateCamera();
			ExecuteUpdate(null);
			VoxelizationRuntimeData.OnReallocTextures += UpdateCameraFromUI;

			_prevVoxelBounds = HResources.VoxelizationData.ExactData.Bounds;
		}

		public void ExecuteUpdate(Camera ctxCamera)
		{
			if (_dirtyBounds == true)
			{
				_dirtyBounds     = false;
				_prevVoxelBounds = HResources.VoxelizationData.ExactData.Bounds;
			}

			_camera.cullingMask      = ~0; //voxelizationData.VoxelizationMask;
			_camera.orthographic     = true;
			_camera.farClipPlane     = .5f * HResources.VoxelizationData.ExactData.Bounds.z;
			_camera.nearClipPlane    = -.5f * HResources.VoxelizationData.ExactData.Bounds.z;
			_camera.orthographicSize = .5f * _prevVoxelBounds.x;
			_camera.aspect           = (.5f * _prevVoxelBounds.x) / (.5f * _prevVoxelBounds.x);
			
			if (true)
			{
				HResources.VoxelizationData.ExactData.PreviousVoxelCameraPosition = new Vector3(_camera.transform.position.x, _camera.transform.position.y, _camera.transform.position.z);
			}

			AttachedCameraTranslate(ctxCamera);
		}

		private void AttachedCameraTranslate(Camera ctxCamera)
		{
			bool attachToSceneCamera = ctxCamera != null && ctxCamera.cameraType == CameraType.SceneView && HResources.DebugData.AttachToSceneCamera == true;
			if (HResources.VoxelizationData.AttachTo == null && attachToSceneCamera == false)
			{
				GroundLevelTranslate();
				_camera.transform.position = _camera.transform.position.OptimizeForVoxelization(HResources.VoxelizationData.ExactData);
				return;
			}
			
			Transform attachToTransform = HResources.VoxelizationData.AttachTo;
#if UNITY_EDITOR
			if (attachToSceneCamera)
			{
				attachToTransform = SceneView.lastActiveSceneView.camera.transform;
			}
#endif
			_camera.transform.parent      =  attachToTransform;
			_camera.transform.rotation    =  Quaternion.identity;
			_camera.transform.eulerAngles += new Vector3(-90f, 0, 180f);

			switch (HResources.VoxelizationData.VoxelizationUpdateMode)
			{
				case VoxelizationUpdateMode.Constant:
					_camera.transform.localPosition = Vector3.zero;

					CenterShiftTranslate(attachToTransform);
					GroundLevelTranslate();
					_camera.transform.position = _camera.transform.position.OptimizeForVoxelization(HResources.VoxelizationData.ExactData);
					break;
				case VoxelizationUpdateMode.Partial:
					
					VoxelizationRuntimeData.OctantIndex = VoxelizationRuntimeData.OctantIndex.Next();
					
					if (VoxelizationRuntimeData.FrameCount % HConstants.OCTANTS_FRAMES_LENGTH == 0) // || VoxelizationRuntimeData.FullVoxelization)
					{
						VoxelizationRuntimeData.OctantIndex = OctantIndex.OctantA;
						_camera.transform.localPosition      = Vector3.zero;

						CenterShiftTranslate(attachToTransform);
						GroundLevelTranslate();
						_camera.transform.position = _camera.transform.position.OptimizeForVoxelization(HResources.VoxelizationData.ExactData);

						VoxelizationRuntimeData.OffsetAxisIndex = CalculateOffsetPositionAndTargetAxis();

						if (VoxelizationRuntimeData.CullingCamera != null && VoxelizationRuntimeData.VoxelOctantCamera != null && VoxelizationRuntimeData.FakeDirectionalCamera != null) //first frame exceprion
						{
							VoxelizationRuntimeData.VoxelOctantCamera.UpdateCamera();
							VoxelizationRuntimeData.CullingCamera.UpdateCamera();
							//VoxelizationRuntimeData.FakeDirectionalCamera.UpdateCamera(); we do it every frame in ExecuteUpdate()
						}
					}
					else
					{
						_camera.transform.position = _rememberPos;
					}
					break;
			}
			
			if (VoxelizationRuntimeData.CullingCamera != null && VoxelizationRuntimeData.VoxelOctantCamera != null && VoxelizationRuntimeData.FakeDirectionalCamera != null) //first frame exceprion
			{
				// Debug.Log(VoxelizationRuntimeData.OctantIndex);
				VoxelizationRuntimeData.VoxelOctantCamera.ExecuteUpdate();
				VoxelizationRuntimeData.CullingCamera.ExecuteUpdate();
				VoxelizationRuntimeData.FakeDirectionalCamera.ExecuteUpdate();
			} 
		}

		private OffsetAxisIndex CalculateOffsetPositionAndTargetAxis()
		{
			Vector3 offsetWorldPosition = _rememberPos;
			_rememberPos = _camera.transform.position;

			offsetWorldPosition = _rememberPos - offsetWorldPosition;

			VoxelizationRuntimeData.OffsetWorldPosition = new OffsetWorldPosition(
				VoxelizationRuntimeData.OffsetAxisIndex == OffsetAxisIndex.AxisXPos ? 0.0f : VoxelizationRuntimeData.OffsetWorldPosition.AxisXPos,
				VoxelizationRuntimeData.OffsetAxisIndex == OffsetAxisIndex.AxisYPos ? 0.0f : VoxelizationRuntimeData.OffsetWorldPosition.AxisYPos,
				VoxelizationRuntimeData.OffsetAxisIndex == OffsetAxisIndex.AxisZPos ? 0.0f : VoxelizationRuntimeData.OffsetWorldPosition.AxisZPos,
				VoxelizationRuntimeData.OffsetAxisIndex == OffsetAxisIndex.AxisXNeg ? 0.0f : VoxelizationRuntimeData.OffsetWorldPosition.AxisXNeg,
				VoxelizationRuntimeData.OffsetAxisIndex == OffsetAxisIndex.AxisYNeg ? 0.0f : VoxelizationRuntimeData.OffsetWorldPosition.AxisYNeg,
				VoxelizationRuntimeData.OffsetAxisIndex == OffsetAxisIndex.AxisZNeg ? 0.0f : VoxelizationRuntimeData.OffsetWorldPosition.AxisZNeg
			);

			VoxelizationRuntimeData.OffsetWorldPosition += new OffsetWorldPosition(
				offsetWorldPosition.x > Mathf.Epsilon ? offsetWorldPosition.x : 0f,
				offsetWorldPosition.y > Mathf.Epsilon ? offsetWorldPosition.y : 0f,
				offsetWorldPosition.z > Mathf.Epsilon ? offsetWorldPosition.z : 0f,
				offsetWorldPosition.x < Mathf.Epsilon ? -offsetWorldPosition.x : 0f,
				offsetWorldPosition.y < Mathf.Epsilon ? -offsetWorldPosition.y : 0f,
				offsetWorldPosition.z < Mathf.Epsilon ? -offsetWorldPosition.z : 0f
			);

			return VoxelizationRuntimeData.OffsetWorldPosition.MaxAxisOffset();
		}

		private void CenterShiftTranslate(Transform attachToTransform)
		{
			if (attachToTransform.GetComponent<Camera>() && Mathf.Abs(HResources.VoxelizationData.CenterShift) > 0.01f)
			{
				var forward = attachToTransform.forward;
				_camera.transform.position += new Vector3(forward.x, 0f, forward.z) * HResources.VoxelizationData.CenterShift;
			}
		}

		private void GroundLevelTranslate()
		{
			float height = HResources.VoxelizationData.ExactData.Bounds.z;
			if (HResources.VoxelizationData.GroundLevelEnable == true && (_camera.transform.position.y - height / 2) < HResources.VoxelizationData.GroundLevel) 
			{
				_camera.transform.position = new Vector3(_camera.transform.position.x, HResources.VoxelizationData.GroundLevel + height / 2,
					_camera.transform.position.z);
			}
		}

		private void UpdateCameraFromUI()
		{
			_dirtyBounds = true;
		}

		private void CreateCamera()
		{
			if (_camera == null)
			{
				_camera              = gameObject.AddComponent<Camera>();
				_camera.aspect       = 1f;
				_camera.orthographic = true;
				_camera.enabled      = false;
				_camera.hideFlags    = HResources.DebugData.ShowBowels ? HideFlags.None : HideFlags.HideInHierarchy;

				var HDCameraData = gameObject.AddComponent<HDAdditionalCameraData>();
			}
		}

		private void Update()
		{	
			if (_voxelsHandler == null || _voxelsHandler.PingVoxelsHandler(this))
			{
				DestroyImmediate(this.gameObject);
			}
		}

		private void OnDestroy()
		{
			VoxelizationRuntimeData.OnReallocTextures -= UpdateCameraFromUI;
		}
	}
}
