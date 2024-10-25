using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Micosmo.SensorToolkit {

    [System.Serializable]
    public struct FOVRange {
        public float HorizAngle;
        public float VertAngle;
        public float Distance;
        public static FOVRange Of(float horizAngle, float vertAngle, float distance = float.PositiveInfinity) => new FOVRange {
            HorizAngle = Mathf.Clamp(horizAngle, 0f, 180f),
            VertAngle = Mathf.Clamp(vertAngle, 0f, 180f),
            Distance = distance
        };
        public bool ContainsAngles(HorizontalCoords coords) =>
            Mathf.Abs(coords.HorizAngle) <= HorizAngle && Mathf.Abs(coords.VertAngle) <= VertAngle;
        public bool ContainsDistance(float distance) => distance <= Distance;
        public bool Contains(HorizontalCoords coords, float distance) =>
            ContainsAngles(coords) && ContainsDistance(distance);
        public void DrawGizmos(ReferenceFrame frame) {
            SensorGizmos.FOVGizmo(frame, float.IsInfinity(Distance) ? 1f : Distance, HorizAngle, VertAngle);
        }
    }

    public struct FOVRange2D {
        public float Angle;
        public float Distance;
        public static FOVRange2D Of(float angle, float distance = float.PositiveInfinity) => new FOVRange2D {
            Angle = Mathf.Clamp(angle, 0f, 180f),
            Distance = distance
        };
        public void DrawGizmos(ReferenceFrame frame) {
            SensorGizmos.FOVGizmo(frame, float.IsInfinity(Distance) ? 1f : Distance, Angle, 0f);
        }
    }

}