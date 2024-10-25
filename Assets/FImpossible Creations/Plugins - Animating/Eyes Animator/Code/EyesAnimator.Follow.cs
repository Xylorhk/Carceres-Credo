using UnityEngine;

namespace FIMSpace.FEyes
{
    public partial class FEyesAnimator
    {
        public float HeadToTargetAngle { get; private set; }
        public Vector3 HeadRotation { get; private set; }
        public Vector3 TargetLookDirection { get; private set; }
        public Vector3 LookRotationBase { get; private set; }
        public Vector3 DeltaVector { get; private set; }
        public Vector3 LookStartPositionBase { get; private set; }

        private void ComputeBaseRotations(ref Quaternion lookRotationBase)
        {
            // Look position referencing from middle of head for unsquinted look rotation
            LookStartPositionBase = GetStartLookPosition();
            TargetLookDirection = targetLookPosition - LookStartPositionBase;

            HeadToTargetAngle = Vector3.Angle(HeadReference.transform.TransformDirection(headForward), TargetLookDirection.normalized);

            Quaternion lookRotationQuatBase = Quaternion.LookRotation(TargetLookDirection, WorldUp);
            Quaternion fadedRot = lookRotationQuatBase;

            // Just look towards target rotation
            if (FollowTargetAmount * conditionalFollowBlend < 1f)
            {   // Look towards target with default eye rotation blend
                Quaternion defaultLookRot = Quaternion.LookRotation(HeadReference.rotation * headForward * 100f, WorldUp);

                if (FollowTargetAmount * conditionalFollowBlend > 0f)
                    fadedRot = Quaternion.SlerpUnclamped(defaultLookRot, Quaternion.LookRotation(targetLookPosition - LookStartPositionBase), FollowTargetAmount * conditionalFollowBlend);
                else
                    fadedRot = defaultLookRot;
            }

            LookRotationBase = fadedRot.eulerAngles;

            // Head rotation to offset clamp ranges in head rotates in animation clip of skeleton
            HeadRotation = (HeadReference.rotation * Quaternion.FromToRotation(headForwardFromTo, Vector3.forward)).eulerAngles;

            CheckLookRanges();
        }


        public bool OutOfRange { get; private set; }
        public bool OutOfDistance { get; private set; }

        /// <summary>
        /// Checking if look target is not out of follow angle range or distance
        /// </summary>
        private void CheckLookRanges()
        {
            if (StopLookAbove >= 180)
            {
                OutOfRange = false;
            }
            else
            {
                // Range blending out eyes animation
                if (Mathf.Abs(HeadToTargetAngle) < StopLookAbove)
                {
                    OutOfRange = false;
                }
                else
                {
                    if (Mathf.Abs(HeadToTargetAngle) > StopLookAbove * 1.2f + 10)
                        OutOfRange = true;
                }
            }

            if (MaxTargetDistance > 0f)
            {
                float distance = Vector3.Distance(LookStartPositionBase, targetLookPosition);

                if (distance < MaxTargetDistance) OutOfDistance = false;
                else
                if (distance > MaxTargetDistance + MaxTargetDistance * GoOutFactor) OutOfDistance = true;
            }
            else
                OutOfDistance = false;

            if (OutOfRange || OutOfDistance)
            {
                conditionalFollowBlend = Mathf.Max(0f, conditionalFollowBlend - Time.deltaTime * 5f);
            }
            else
            {
                conditionalFollowBlend = Mathf.Min(1f, conditionalFollowBlend + Time.deltaTime * 5f);
            }
        }


        /// <summary>
        /// Computing rotation for single eye using shared variables
        /// </summary>
        private void ComputeLookingRotation(ref Quaternion lookRotationBase, int randomIndex = 0, int lagId = 0)
        {
            Vector3 lookRotation = this.LookRotationBase;
            if (eyesData[randomIndex].randomDir != Vector3.zero) lookRotation += Vector3.Lerp(Vector3.zero, eyesData[randomIndex].randomDir, EyesRandomMovement);

            // Vector with degrees differences to all needed axes
            DeltaVector = new Vector3(Mathf.DeltaAngle(lookRotation.x, HeadRotation.x), Mathf.DeltaAngle(lookRotation.y, HeadRotation.y));

            // Clamping look rotation
            ClampDetection(DeltaVector, ref lookRotation, HeadRotation);

            lookRotationBase = Quaternion.Euler(lookRotation);
        }



        Vector3 GetStartLookPosition()
        {
            Vector3 lookStartPositionBase;
            lookStartPositionBase = BaseTransform.position;
            lookStartPositionBase.y = HeadReference.position.y;
            lookStartPositionBase += HeadReference.TransformVector(StartLookOffset);
            return lookStartPositionBase;
        }

        public int clampedHorizontal = 0;
        public int clampedVertical = 0;
        public bool IsClamping { get; private set; }
        protected virtual void ClampDetection(Vector2 deltaVector, ref Vector3 lookRotation, Vector3 rootOffset)
        {

            // Limit when looking left or right
            if (deltaVector.y > -EyesClampHorizontal.x)
            {
                //clampDelta.y = -EyesClampHorizontal.x;
                lookRotation.y = rootOffset.y + EyesClampHorizontal.x;
                clampedHorizontal = -1;
            }
            else if (deltaVector.y < -EyesClampHorizontal.y)
            {
                //clampDelta.y = -EyesClampHorizontal.y;
                lookRotation.y = rootOffset.y + EyesClampHorizontal.y;
                clampedHorizontal = 1;
            }
            else clampedHorizontal = 0;

            // Limit when looking up or down
            if (deltaVector.x > EyesClampVertical.y)
            {
                //clampDelta.x = EyesClampVertical.y;
                clampedVertical = 1;
                lookRotation.x = rootOffset.x - EyesClampVertical.y;
            }
            else if (deltaVector.x < EyesClampVertical.x)
            {
                //clampDelta.x = EyesClampVertical.x;
                clampedVertical = -1;
                lookRotation.x = rootOffset.x - EyesClampVertical.x;
            }
            else clampedVertical = 0;

            deltaV = deltaVector;

            if (clampedHorizontal != 0 || clampedVertical != 0) IsClamping = true;
        }

    }
}