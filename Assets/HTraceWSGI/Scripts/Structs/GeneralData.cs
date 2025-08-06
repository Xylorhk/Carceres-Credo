using System;
using HTraceWSGI.Scripts.Globals;
using UnityEngine;

namespace HTraceWSGI.Scripts.Structs
{
	[Serializable]
	public class GeneralData
	{
		[SerializeField]
		private RayCountMode _rayCountMode = RayCountMode.Quality;
		/// <summary>
		/// Defines the pixel spacing between screen-space probes, affecting the number of probes spawned.
		/// Ray Count has the biggest impact on the overall performance , memory consumption and visual quality. 
		/// </summary>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		public RayCountMode RayCountMode
		{
			get
			{
				return _rayCountMode;
			}
			set
			{
				if (value == _rayCountMode)
					return;
				
				_rayCountMode = value;
			}
		}
		
		[SerializeField]
		private int _rayLength = 50;

		/// <summary>
		/// The maximum distance a ray can travel in world space.
		/// </summary>
		/// <value>[1;100]</value>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		[HExtensions.HRangeAttribute(0, 100)]
		public int RayLength
		{
			get { return _rayLength; }
			set
			{
				if (value == _rayLength)
					return;

				_rayLength = HExtensions.Clamp(value, typeof(GeneralData), nameof(GeneralData.RayLength));
			}
		}
		
		public Multibounce Multibounce = Multibounce.IrradianceCache;
		
		/// <summary>
		/// Debug different stages and resources of HTrace.
		/// </summary>
		/// <Docs><see href="https://ipgames.gitbook.io/htrace-wsgi/settings-and-properties">More information</see></Docs>
		public DebugModeWS DebugModeWS = DebugModeWS.None;
	}
}
