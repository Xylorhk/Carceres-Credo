﻿using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;

namespace Micosmo.SensorToolkit {

    /**
     * The Steering Sensor is a unique sensor for implementing steering behaviour. It's an implementation of 
     * Context Based Steering as described here. The sensor can operate in a spherical mode suitable for flying 
     * agents, or a planar mode for ground-based agents.
     */
    [AddComponentMenu("Sensors/Steering Sensor")]
    [HelpURL("https://micosmo.com/sensortoolkit2/docs/manual/sensors/steering")]
    public class SteeringSensor : BasePulsableSensor, IPulseRoutine, ISteeringSensor {

        #region Configurations
        [SerializeField]
        [Tooltip("Steering Vectors are 3D when this is true and they are planar when this is false.")]
        ObservableBool isSpherical = new ObservableBool();

        [SerializeField]
        [Tooltip("The up-drection of the sensor when using circular grids.")]
        ObservableVector3 upDirection = new ObservableVector3() { Value = Vector3.up };

        [SerializeField]
        [Tooltip("Determines the number of discrete buckets that directions around the sensor are boken up into.")]
        ObservableInt resolution = new ObservableInt() { Value = 3 };

        [SerializeField] SteerSeek seek = new SteerSeek();

        [SerializeField] SteerInterest interest = new SteerInterest();

        [SerializeField] SteerDanger danger = new SteerDanger();

        [SerializeField, FormerlySerializedAs("velocityObstacles")] SteerVO velocity = new SteerVO();

        [SerializeField] SteerDecision decision = new SteerDecision();

        [SerializeField]
        PulseRoutine pulseRoutine = new PulseRoutine();

        [Tooltip("Enables the built-in locomotion if this is any value other then None.")]
        public LocomotionMode LocomotionMode;

        [Tooltip("The RigidBody to control with built-in locomotion.")]
        public Rigidbody RigidBody;

        [Tooltip("The CharacterController to control with built-in locomotion.")]
        public CharacterController CharacterController;

        // Configurations struct for the built-in locomotion behaviours.
        [SerializeField, FormerlySerializedAs("Locomotion")] LocomotionSystem locomotion;
        #endregion

        #region Events
        public override event System.Action OnPulsed;
        #endregion

        #region Public
        // Change IsSpherical value at runtime
        public bool IsSpherical {
            get => isSpherical.Value;
            set => isSpherical.Value = value;
        }

        public Vector3 UpDirection {
            get => upDirection.Value;
            set => upDirection.Value = value;
        }
        
        // Change Resolution at runtime
        public int Resolution {
            get => Mathf.Abs(resolution.Value);
            set => resolution.Value = value;
        }

        // Change the pulse mode at runtime
        public PulseRoutine.Modes PulseMode {
            get => pulseRoutine.Mode.Value;
            set => pulseRoutine.Mode.Value = value;
        }

        // Change the pulse interval at runtime
        public float PulseInterval {
            get => pulseRoutine.Interval.Value;
            set => pulseRoutine.Interval.Value = value;
        }

        // Change at runtime if the sensor will pulse in Update or FixedUpdate
        public PulseRoutine.UpdateFunctions PulseUpdateFunction {
            get => pulseRoutine.UpdateFunction;
            set => pulseRoutine.UpdateFunction = value;
        }

        public SteerSeek Seek => seek;
        
        public SteerInterest Interest => interest;

        public SteerDanger Danger => danger;

        public SteerVO Velocity => velocity;

        public SteerDecision Decision => decision;

        public LocomotionSystem Locomotion => locomotion;

        // Is true when we are within the desired range from the target seek position.
        public bool IsDestinationReached => seek.GetIsDestinationReached(this);

        // Is true when we have not yet reached the destination.
        public bool IsSeeking => !IsDestinationReached;

        public void SeekTo(Transform destination, float distanceOffset = 0f) {
            Seek.SeekMode = SeekMode.Position;
            Seek.SeekPosition = new SeekPosition(destination, false, distanceOffset);
        }
        public void SeekTo(Vector3 destination, float distanceOffset = 0f) {
            Seek.SeekMode = SeekMode.Position;
            Seek.SeekPosition = new SeekPosition(destination, false, distanceOffset);
        }
        public void ArriveTo(Transform destination, float distanceOffset = 0f) {
            Seek.SeekMode = SeekMode.Position;
            Seek.SeekPosition = new SeekPosition(destination, true, distanceOffset);
        }
        public void ArriveTo(Vector3 destination, float distanceOffset = 0f) {
            Seek.SeekMode = SeekMode.Position;
            Seek.SeekPosition = new SeekPosition(destination, true, distanceOffset);
        }
        public void SeekDirection(Vector3 direction) {
            Seek.SeekMode = SeekMode.Direction;
            Seek.SeekDirection = direction;
        }
        public void Wander() {
            Seek.SeekMode = SeekMode.Wander;
        }
        public void Stop() {
            Seek.SeekMode = SeekMode.Stop;
        }

        public Vector3 GetSteeringVector() => seek.GetSteeringVector(this);

        public float GetSpeedCandidate(Vector3 direction) => velocity.GetSpeedCandidate(direction);

        public override void PulseAll() {
            interest.PulseSensors();
            danger.PulseSensors();
            velocity.PulseSensors();
            Pulse();
        }

        public override void Clear() {
            ClearPendingPulse();
            interest.Clear();
            danger.Clear();
            velocity.Clear();
            Decision.Clear();
            if (!Application.isPlaying) {
                DisposeGrids();
            }
        }
        #endregion

        #region Internals
        bool isControlling => LocomotionMode != LocomotionMode.None;
        
        ObservableEffect gridConfigEffect;
        ObservableEffect upDirectionEffect;

        PulseJob pulseJob;

        void CreatePulseJob() {
            pulseJob = new PulseJob(new PulseJob.Step[] {
                (isRun) => {
                    var interestJob = interest.ScheduleJob(this);
                    var dangerJob = danger.ScheduleJob(this);
                    var velocityJob = velocity.ScheduleJob(this);
                    return decision.ScheduleJob(this, interestJob, dangerJob, velocityJob);
                },
                (isRun) => {
                    interest.ManagedFinish();
                    danger.ManagedFinish();
                    velocity.ManagedFinish();
                    decision.ManagedFinish();

                    OnPulsed?.Invoke();

                    return default;
                }
            });
        }
        
        protected override PulseJob GetPulseJob() {
            if (!Application.isPlaying) {
                RecreateGrids();
            }
            if (!pulseJob.IsCreated) {
                CreatePulseJob();
            }
            return pulseJob;
        }

        Coroutine StartEachFrame(IEnumerator routine) => StartCoroutine(PulseEachFrame(routine));
        IEnumerator PulseEachFrame(IEnumerator routine) {
            while (routine.MoveNext()) {
                yield return routine.Current;
            }
        }
        Coroutine StartFixedInterval(IEnumerator routine) => StartCoroutine(PulseFixedInterval(routine));
        IEnumerator PulseFixedInterval(IEnumerator routine) {
            while (routine.MoveNext()) {
                yield return routine.Current;
            }
        }

        void Awake() {
            if (isSpherical == null) {
                isSpherical = new ObservableBool();
            }
            if (resolution == null) {
                resolution = new ObservableInt() { Value = 3 };
            }
            gridConfigEffect = ObservableEffect.Create(RecreateGrids, new Observable[] { isSpherical, resolution }, false);
            upDirectionEffect = ObservableEffect.Create(UpdateUpDirection, new Observable[] { upDirection }, false);
            
            if (pulseRoutine == null) {
                pulseRoutine = new PulseRoutine();
            }
            pulseRoutine.Awake(this, StartEachFrame, StartFixedInterval);
        }

        void OnEnable() {
            RecreateGrids();
            pulseRoutine.OnEnable();
        }

        protected override void OnDisable() {
            base.OnDisable();
            pulseRoutine.OnDisable();
            DisposeGrids();
        }

        void OnDestroy() {
            gridConfigEffect?.Dispose();
            upDirectionEffect?.Dispose();
        }

        void OnValidate() {
            isSpherical?.OnValidate();
            upDirection?.OnValidate();
            resolution?.OnValidate();
            pulseRoutine?.OnValidate();
        }

        void Update() {
            decision.Interpolate(Time.deltaTime);

            if (LocomotionMode == LocomotionMode.UnityCharacterController) {
                locomotion.CharacterSeek(CharacterController, GetSteeringVector(), IsSpherical ? Vector3.up : GetSafeUpDirection());
            }
        }

        void FixedUpdate() {
            if (LocomotionMode == LocomotionMode.RigidBodyFlying) {
                locomotion.FlyableSeek(RigidBody, GetSteeringVector());
            } else if (LocomotionMode == LocomotionMode.RigidBodyCharacter) {
                locomotion.CharacterSeek(RigidBody, GetSteeringVector(), IsSpherical ? Vector3.up : GetSafeUpDirection());
            }
        }
        
        void DisposeGrids() {
            ClearPendingPulse();
            velocity.Dispose();
            interest.Dispose();
            danger.Dispose();
            velocity.Dispose();
            decision.Dispose();
        }
        
        void RecreateGrids() {
            ClearPendingPulse();
            var up = GetSafeUpDirection();
            interest.RecreateGrids(Resolution, IsSpherical, up);
            danger.RecreateGrids(Resolution, IsSpherical, up);
            velocity.RecreateGrids(Resolution, IsSpherical, up);
            decision.RecreateGrids(Resolution, IsSpherical, up);
        }

        void UpdateUpDirection() {
            var up = GetSafeUpDirection();
            interest.UpdateUpDirection(up);
            danger.UpdateUpDirection(up);
            velocity.UpdateUpDirection(up);
            decision.UpdateUpDirection(up);
        }

        Vector3 GetSafeUpDirection() {
            if (UpDirection == Vector3.zero) {
                return Vector3.up;
            }
            return UpDirection.normalized;
        }

        public static bool ShowInterestGizmos = false;
        public static bool ShowDangerGizmos = false;
        public static bool ShowVelocityGizmos = false;
        public static bool ShowDecisionGizmos = false;

        const float rayScaleMult = 0.05f;
        const float minRayScale = 1f;
        const float rayWidth = 3.5f;
        const float rayGap = 1.1f;
        void OnDrawGizmosSelected() {
            if (!ShowDetectionGizmos) {
                return;
            }

            var camera = Camera.current;
            var distance = Vector3.Distance(camera.transform.position, transform.position);
            var rayScale = Mathf.Max(distance * rayScaleMult, minRayScale);

            int nShown = 1;

            if (ShowInterestGizmos) {
                interest.DrawGizmos(this, nShown * rayScale * rayGap, rayScale, rayWidth);
                nShown++;
            }
            if (ShowDangerGizmos) {
                danger.DrawGizmos(this, nShown * rayScale * rayGap, rayScale, rayWidth);
                nShown++;
            }
            if (ShowVelocityGizmos) {
                velocity.DrawGizmos(this, nShown * rayScale * rayGap, rayScale, rayWidth);
                nShown++;
            }
            if (ShowDecisionGizmos) {
                decision.DrawGizmos(this, nShown * rayScale * rayGap, rayScale, rayWidth);
                nShown++;
            }

            SensorGizmos.PushColor(STPrefs.SteeringVectorColour);
            SensorGizmos.ThickLineNoZTest(transform.position, transform.position + GetSteeringVector(), rayWidth);
            SensorGizmos.PopColor();
        }
        #endregion
    }
}
 