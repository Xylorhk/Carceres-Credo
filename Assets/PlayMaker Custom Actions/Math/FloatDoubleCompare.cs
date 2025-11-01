using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory(ActionCategory.Logic)]
    [Tooltip("Compares two floats with their respective thresholds and sends an event only when BOTH conditions are met.")]
    public class FloatDoubleCompare : FsmStateAction
    {
        [RequiredField]
        [Tooltip("The first float variable to compare.")]
        public FsmFloat float1;

        [Tooltip("The comparison type for the first float.")]
        public FloatCompareType compareType1;

        [Tooltip("The target value for the first comparison.")]
        public FsmFloat float1Target;

        [RequiredField]
        [Tooltip("The second float variable to compare.")]
        public FsmFloat float2;

        [Tooltip("The comparison type for the second float.")]
        public FloatCompareType compareType2;

        [Tooltip("The target value for the second comparison.")]
        public FsmFloat float2Target;

        [Tooltip("Event to send when BOTH comparisons are true.")]
        public FsmEvent bothTrueEvent;

        [Tooltip("Repeat every frame.")]
        public bool everyFrame;

        public override void Reset()
        {
            float1 = null;
            float1Target = null;
            float2 = null;
            float2Target = null;
            bothTrueEvent = null;
            compareType1 = FloatCompareType.GreaterThan;
            compareType2 = FloatCompareType.GreaterThan;
            everyFrame = true;
        }

        public override void OnEnter()
        {
            DoCompare();

            if (!everyFrame)
                Finish();
        }

        public override void OnUpdate()
        {
            DoCompare();
        }

        private void DoCompare()
        {
            bool cond1 = Compare(float1.Value, float1Target.Value, compareType1);
            bool cond2 = Compare(float2.Value, float2Target.Value, compareType2);

            if (cond1 && cond2)
            {
                Fsm.Event(bothTrueEvent);
            }
        }

        private bool Compare(float a, float b, FloatCompareType type)
        {
            switch (type)
            {
                case FloatCompareType.GreaterThan: return a > b;
                case FloatCompareType.GreaterThanOrEqual: return a >= b;
                case FloatCompareType.LessThan: return a < b;
                case FloatCompareType.LessThanOrEqual: return a <= b;
                case FloatCompareType.Equal: return Mathf.Approximately(a, b);
                default: return false;
            }
        }
    }

    public enum FloatCompareType
    {
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Equal
    }
}
