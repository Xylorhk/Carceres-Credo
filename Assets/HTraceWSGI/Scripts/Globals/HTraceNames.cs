namespace HTraceWSGI.Scripts.Globals
{
	public static class HTraceNames
	{
		public const string HTRACE_NAME = "HTraceWSGI";
		
		public const string HTRACE_PRE_PASS_NAME = "HTraceWSGI Pre Pass";
		public const string HTRACE_VOXEL_PASS_NAME = "HTraceWSGI Voxelization Pass";
		public const string HTRACE_MAIN_PASS_NAME = "HTraceWSGI Main Pass";
		public const string HTRACE_FINAL_PASS_NAME = "HTraceWSGI Final Pass";

		public const string HTRACE_VOXEL_CAMERA_NAME                     = "___HTraceWSGI Voxelization Camera";
		public const string HTRACE_VOXEL_CULLING_CAMERA_NAME             = "___HTraceWSGI Voxelization Culling Camera";
		public const string HTRACE_VOXEL_OCTANT_CAMERA_NAME              = "___HTraceWSGI Voxelization Octant Camera";

		public const string HTRACE_PRE_PASS_NAME_FRAME_DEBUG             = "HTraceWSGI Pre Pass";
		public const string HTRACE_VOXEL_CONSTANT_PASS_NAME_FRAME_DEBUG  = "HTraceWSGI Voxelizaion Constant";
		public const string HTRACE_VOXEL_PARTIAL_PASS_NAME_FRAME_DEBUG   = "HTraceWSGI Voxelizaion Partial";
		public const string HTRACE_MAIN_PASS_NAME_FRAME_DEBUG            = "HTraceWSGI Main Pass";
		public const string HTRACE_FINAL_PASS_NAME_FRAME_DEBUG           = "HTraceWSGI Final Pass";

		public const string KEYWORD_SWITCHER = "HTRACE_OVERRIDE";

		public const string HTRACE_VOXELIZATION_SHADER_TAG_ID = "HTraceVoxelization";
	}
}
