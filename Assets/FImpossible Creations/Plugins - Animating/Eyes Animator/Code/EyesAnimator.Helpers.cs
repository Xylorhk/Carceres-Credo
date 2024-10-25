using UnityEngine;

namespace FIMSpace.FEyes
{
    public partial class FEyesAnimator : UnityEngine.EventSystems.IDropHandler, IFHierarchyIcon
    {
        public bool debugSwitch = false;
        public string EditorIconPath { get { if (PlayerPrefs.GetInt("EyesH", 1) == 0) return ""; else return "Eyes Animator/EyesAnimator_IconSmall"; } }
        public void OnDrop(UnityEngine.EventSystems.PointerEventData data) { }

        void Reset()
        {
            _baseTransform = transform;
        }

        /// <summary>
        /// TODO: Full implementation of all arrays inside just this class
        /// </summary>
        [System.Serializable]
        public class Eye
        {
            // Info
            public Vector3 forward;
            public Quaternion initLocalRotation;
            public Quaternion lerpRotation;

            // Random motion
            public Vector3 randomDir;
            public float randomTimer;

            // Lag motion
            public float lagTimer;
            public float lagProgress;
            public Quaternion lagStartRotation;
            public float changeSmoother;
        }
    }
}