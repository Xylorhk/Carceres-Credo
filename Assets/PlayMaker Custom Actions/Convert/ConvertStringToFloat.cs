using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory(ActionCategory.Convert)]
    [Tooltip("Converts a String Variable to a Float Variable.")]
    public class ConvertStringToFloat : FsmStateAction
    {
        [RequiredField]
        [UIHint(UIHint.Variable)]
        [Tooltip("The String variable to convert.")]
        public FsmString stringVariable;

        [RequiredField]
        [UIHint(UIHint.Variable)]
        [Tooltip("Store the result in a Float variable.")]
        public FsmFloat floatVariable;

        [Tooltip("Event to send if the string cannot be converted to a float.")]
        public FsmEvent failureEvent;

        [Tooltip("Repeat every frame.")]
        public bool everyFrame;

        public override void Reset()
        {
            stringVariable = null;
            floatVariable = null;
            failureEvent = null;
            everyFrame = false;
        }

        public override void OnEnter()
        {
            DoConvert();

            if (!everyFrame)
            {
                Finish();
            }
        }

        public override void OnUpdate()
        {
            DoConvert();
        }

        void DoConvert()
        {
            if (string.IsNullOrEmpty(stringVariable.Value))
            {
                Fsm.Event(failureEvent);
                return;
            }

            float result;
            if (float.TryParse(stringVariable.Value, out result))
            {
                floatVariable.Value = result;
            }
            else
            {
                Fsm.Event(failureEvent);
            }
        }
    }
}
