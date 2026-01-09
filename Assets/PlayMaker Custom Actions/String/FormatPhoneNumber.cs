using UnityEngine;
using HutongGames.PlayMaker;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory("String")]
    [Tooltip("Formats a digit-only string as a phone number (XXX-XXX-XXXX).")]
    public class FormatPhoneNumber : FsmStateAction
    {
        [RequiredField]
        [Tooltip("Raw phone number string (digits only).")]
        public FsmString rawNumber;

        [RequiredField]
        [UIHint(UIHint.Variable)]
        [Tooltip("Formatted phone number output.")]
        public FsmString formattedNumber;

        public override void OnEnter()
        {
            formattedNumber.Value = Format(rawNumber.Value);
            Finish();
        }

        private string Format(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            // Clamp to 10 digits
            if (input.Length > 10)
                input = input.Substring(0, 10);

            if (input.Length <= 3)
                return input;

            if (input.Length <= 6)
                return input.Substring(0, 3) + "-" +
                       input.Substring(3);

            return input.Substring(0, 3) + "-" +
                   input.Substring(3, 3) + "-" +
                   input.Substring(6);
        }
    }
}
