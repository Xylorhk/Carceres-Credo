using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Micosmo.SensorToolkit.Experimental {

    public struct AngleEnumerator {
        public enum AngleMethodType { Center, Origin, BoundingBox }
        public enum SortByType { HorizontalAngle, CentralAngle }

        public AngleMethodType AngleMethod;
        public SortByType SortBy;
        
        public List<AngleResult> results { get; private set; }

        public static AngleEnumerator Create() => new AngleEnumerator {
            results = new List<AngleResult>()
        };

        public void Clear() {
            results.Clear();
        }

        public void Calculate(ReferenceFrame frame, FOVRange fov, List<Signal> signals) {
            Clear();
            fov.Distance *= fov.Distance;
            foreach (var signal in signals) {
                var coords =
                    AngleMethod == AngleMethodType.Origin ? frame.AngleTo(signal.Object.transform.position) :
                    AngleMethod == AngleMethodType.Center ? frame.AngleTo(signal.Bounds.center) :
                    AngleMethod == AngleMethodType.BoundingBox ? frame.AngleTo(signal.Bounds) :
                    default;
                var quadrance = signal.Bounds.SqrDistance(frame.Position);
                if (!fov.Contains(coords, quadrance)) {
                    continue;
                }
                var result = new AngleResult {
                    Object = signal.Object,
                    Coords = coords,
                    Distance = quadrance
                };
                results.Add(result);
            }
            if (SortBy == SortByType.HorizontalAngle) {
                results.Sort(AngleResult.CompareHorizAngle);
            } else if (SortBy == SortByType.CentralAngle) {
                results.Sort(AngleResult.CompareCentralAngle);
            }
        }

        public void DrawGizmos() {
            SensorGizmos.PushColor(Color.black);
            int i = 0;
            foreach (var result in results) {
                SensorGizmos.Label(result.Object.transform.position, $"Index: {i}\n({result.Coords.HorizAngle.ToString("N1")},{result.Coords.VertAngle.ToString("N1")})");
                i++;
            }
            SensorGizmos.PopColor();
        }
        
        public struct AngleResult {
            public GameObject Object;
            public HorizontalCoords Coords;
            public float Distance;
            public static int CompareCentralAngle(AngleResult r1, AngleResult r2) {
                var angleDiff = r1.Coords.CentralAngle - r2.Coords.CentralAngle;
                if (angleDiff != 0f) {
                    return angleDiff > 0f ? 1 : -1;
                }
                var distanceDiff = r1.Distance - r2.Distance;
                if (distanceDiff != 0f) {
                    return distanceDiff > 0f ? 1 : -1;
                }
                return 0;
            }
            public static int CompareHorizAngle(AngleResult r1, AngleResult r2) {
                //var a1 = r1.Coords.HorizAngle >= 0 ? r1.Coords.HorizAngle : 360f + r1.Coords.HorizAngle;
                //var a2 = r2.Coords.HorizAngle >= 0 ? r2.Coords.HorizAngle : 360f + r2.Coords.HorizAngle;
                var a1 = r1.Coords.HorizAngle;
                var a2 = r2.Coords.HorizAngle;
                var angleDiff = a1 - a2;
                if (angleDiff != 0f) {
                    return angleDiff > 0f ? 1 : -1;
                }
                var distanceDiff = r1.Distance - r2.Distance;
                if (distanceDiff != 0f) {
                    return distanceDiff > 0f ? 1 : -1;
                }
                return 0;
            }
        }
    }

}

