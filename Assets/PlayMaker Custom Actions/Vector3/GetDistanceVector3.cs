using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory(ActionCategory.Vector3)]
    [Tooltip("Gets the distance between two Vector3 values. Optionally, repeat every frame.")]
    public class GetDistanceVector3 : FsmStateAction
    {
        [RequiredField]
        [Tooltip("First Vector3 value.")]
        public FsmVector3 vector1;

        [RequiredField]
        [Tooltip("Second Vector3 value.")]
        public FsmVector3 vector2;

        [UIHint(UIHint.Variable)]
        [Tooltip("Store the result of the distance calculation.")]
        public FsmFloat storeResult;

        [Tooltip("Repeat every frame.")]
        public bool everyFrame;

        public override void Reset()
        {
            vector1 = null;
            vector2 = null;
            storeResult = null;
            everyFrame = false;
        }

        public override void OnEnter()
        {
            DoCalculateDistance();

            if (!everyFrame)
                Finish();
        }

        public override void OnUpdate()
        {
            DoCalculateDistance();
        }

        private void DoCalculateDistance()
        {
            if (!vector1.IsNone && !vector2.IsNone)
            {
                storeResult.Value = Vector3.Distance(vector1.Value, vector2.Value);
            }
        }
    }
}
