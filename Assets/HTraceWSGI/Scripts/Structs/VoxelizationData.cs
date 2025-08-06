using System;
using HTraceWSGI.Scripts.Globals;
using UnityEngine;

namespace HTraceWSGI.Scripts.Structs
{
	[Serializable]
	public class VoxelizationData
	{
		public VoxelizationData()
		{
			UpdateData();
		}

		public void UpdateData()
		{
			ExactData.UpdateData(this);
		}

		[SerializeField]
		internal VoxelizationExactData ExactData = new VoxelizationExactData();
		
		//Voxelization ------------------------------------------------------------------------------------------------
		
		/// <summary>
		/// Exclude objects (on a per-layer basis) from voxelization and has the highest priority over all other layer masks.
		/// </summary>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		public LayerMask VoxelizationMask = ~0;

		/// <summary>
		/// This mode defines how voxel data will be updated.
		/// </summary>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		public VoxelizationUpdateMode VoxelizationUpdateMode = VoxelizationUpdateMode.Constant;
		
		[SerializeField]
		private int _updatePeriod = 1;

		/// <summary>
		/// Update Period
		/// </summary>
		/// <value>[1;4]</value>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		[HExtensions.HRangeAttribute(1, 4)]
		public int UpdatePeriod
		{
			get { return _updatePeriod; }
			set
			{
				if (value == _updatePeriod)
					return;

				_updatePeriod = HExtensions.Clamp(value, typeof(VoxelizationData), nameof(VoxelizationData.UpdatePeriod));
			}
		}

		/// <summary>
		/// Anchor object for the voxelization bound. Voxelization will occur around this object and will follow it when it moves.
		/// </summary>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		public Transform AttachTo;

		/// <summary>
		/// Main shadow-casting Directional light. It is usually set up automatically.
		/// </summary>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		public Light DirectionalLight;

		[SerializeField]
		private float _expandShadowmap = 1f;

		/// <summary>
		/// Controls the area covered by the custom directional shadowmap. The shadowmap is used to evaluate direct lighting and shadowing at hit points of world-space rays.
		/// </summary>
		/// <value>[0.0;3.0]</value>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		[HExtensions.HRangeAttribute(1.0f, 3.0f)]
		public float ExpandShadowmap
		{
			get { return _expandShadowmap; }
			set
			{
				if (Mathf.Abs(value - _expandShadowmap) < Mathf.Epsilon)
					return;

				_expandShadowmap = HExtensions.Clamp(value, typeof(VoxelizationData), nameof(VoxelizationData.ExpandShadowmap));
			}
		}

		[SerializeField]
		private int _lodMax = 0;

		/// <summary>
		/// Maximum LOD level
		/// </summary>
		/// <value>[0;10]</value>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		[HExtensions.HRangeAttribute(0, HConstants.MAX_LOD_LEVEL)]
		public int LODMax
		{
			get { return _lodMax; }
			set
			{
				if (value == _lodMax)
					return;
				
				_lodMax = HExtensions.Clamp(value, typeof(VoxelizationData), nameof(VoxelizationData.LODMax));
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public LayerMask InstancedTerrains = 0;

		//Update Options ------------------------------------------------------------------------------------------------

		public LayerMask CulledObjectsMask = 0;

		[SerializeField] private int _expandCullFov = 0;

		/// <summary>
		/// Expand Cull Fov
		/// </summary>
		/// <value>[0;20]</value>
		[HExtensions.HRangeAttribute(0, 20)]
		public int ExpandCullFov
		{
			get { return _expandCullFov; }
			set
			{
				if (value == _expandCullFov)
					return;
				
				_expandCullFov = HExtensions.Clamp(value, typeof(VoxelizationData), nameof(VoxelizationData.ExpandCullFov));
			}
		}

		[SerializeField]
		private float _expandCullRadius = 1f;

		/// <summary>
		/// Expand Cull Radius
		/// </summary>
		/// <value>[0.0;3.0]</value>
		[HExtensions.HRangeAttribute(0.0f, 3.0f)]
		public float ExpandCullRadius
		{
			get { return _expandCullRadius; }
			set
			{
				if (Mathf.Abs(value - _expandCullRadius) < Mathf.Epsilon)
					return;
				
				_expandCullRadius  = HExtensions.Clamp(value, typeof(VoxelizationData), nameof(VoxelizationData.ExpandCullRadius));
			}
		}

		public LayerMask DynamicObjectsMask = 0;
		
		//Voxels parameters ------------------------------------------------------------------------------------------------

		[SerializeField]
		private float _voxelDensity = 0.64f;

		/// <summary>
		/// Controls the resolution of the voxel volume (3D Texture). Lower values reduce the volume resolution, while higher values provide finer detail. The Voxel Density is limited by
		/// the Voxel Bounds parameter to ensure voxel sizes remain between 4 and 8 centimeters.
		/// </summary>
		/// <value>[0.0;1.0]</value>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		[HExtensions.HRangeAttribute(0.0f, 1.0f)]
		public float VoxelDensity
		{
			get { return _voxelDensity; }
			set
			{
				if (Mathf.Abs(value - _voxelDensity) < Mathf.Epsilon)
					return;

				_voxelDensity = HExtensions.Clamp(value, typeof(VoxelizationData), nameof(VoxelizationData.VoxelDensity));
				ExactData.UpdateData(this);
			}
		}

		[SerializeField] private int _voxelBounds = 35;

		/// <summary>
		/// Controls the maximum size (in meters) that the voxelization bound can cover.
		/// </summary>
		/// <value>[1;80]</value>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		[HExtensions.HRangeAttribute(1, HConfig.MAX_VOXEL_BOUNDS)]
		public int VoxelBounds
		{
			get { return _voxelBounds; }
			set
			{
				if (value == _voxelBounds)
					return;

				_voxelBounds = HExtensions.Clamp(value, typeof(VoxelizationData), nameof(VoxelizationData.VoxelBounds));
				ExactData.UpdateData(this);
			}
		}

		[SerializeField]
		private bool _overrideBoundsHeightEnable = false;

		/// <summary>
		/// Enable Bounds height override.
		/// </summary>
		/// <value>[1;80]</value>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		public bool OverrideBoundsHeightEnable
		{
			get { return _overrideBoundsHeightEnable; }
			set { _overrideBoundsHeightEnable = value; }
		}

		[SerializeField]
		private int _overrideBoundsHeight = 10;

		/// <summary>
		/// The maximum height of the voxelization bound.
		/// This parameter is particularly useful for "flat" levels with low verticality, where the scene extends along the X and Z axes but has minimal content along the Y axis
		/// (e.g., indoor scenes where the distance to walls is typically greater than the distance to the floor or ceiling).
		/// </summary>
		/// <value>[1;200]</value>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		[HExtensions.HRangeAttribute(1, 200)]
		public int OverrideBoundsHeight
		{
			get { return _overrideBoundsHeight; }
			set
			{
				if (value == _overrideBoundsHeight)
					return;

				_overrideBoundsHeight = HExtensions.Clamp(value, typeof(VoxelizationData), nameof(VoxelizationData.OverrideBoundsHeight));
				ExactData.UpdateData(this);
			}
		}

		/// <summary>
		/// Shift Center of voxelization bound
		/// </summary>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		public float CenterShift = 0f;

		/// <summary>
		/// Enable Ground level for voxelization.
		/// </summary>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		public bool GroundLevelEnable = false;
		/// <summary>
		/// Ensures that the voxelization bounds will always remain above this specified level.
		/// This option is useful when you know the base level of your scene and there is no need to voxelize anything below it.
		/// </summary>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		public float GroundLevel = 0f;
	}
}
