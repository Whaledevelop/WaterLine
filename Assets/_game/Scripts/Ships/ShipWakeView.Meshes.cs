using System.Collections.Generic;
using UnityEngine;

namespace Game.Ships
{
    public sealed partial class ShipWakeView
    {
        private void BuildWakeMeshes(ShipVisualPose pose, float normalizedSpeed)
        {
            var activePath = BuildActivePath(pose.Stern, normalizedSpeed);
            BuildPathMeshes(activePath, pose.HullHalfWidth, normalizedSpeed, 1f, _centerMesh, _sideMesh,
                _residualMesh);
        }

        private List<WakePathPoint> BuildActivePath(Vector2 stern, float normalizedSpeed)
        {
            var currentPath = BuildSegmentPath(_history.Current);
            currentPath.Insert(0, new WakePathPoint(stern, normalizedSpeed, 0f));

            return SmoothPath(currentPath);
        }

        private static List<WakePathPoint> BuildSegmentPath(WakeSegment segment)
        {
            var path = new List<WakePathPoint>();
            if (segment == null)
            {
                return path;
            }

            foreach (var sample in segment.Samples)
            {
                path.Add(new WakePathPoint(sample.Position, sample.Intensity, sample.Age));
            }

            return path;
        }

        private static List<WakePathPoint> SmoothPath(List<WakePathPoint> path)
        {
            if (path.Count < 3)
            {
                return path;
            }

            var smoothed = new List<WakePathPoint>(path);
            for (var pass = 0; pass < 2; pass++)
            {
                var source = smoothed.ToArray();
                for (var i = 1; i < smoothed.Count - 1; i++)
                {
                    var position = source[i - 1].Position * 0.25f + source[i].Position * 0.5f +
                        source[i + 1].Position * 0.25f;
                    smoothed[i] = source[i].WithPosition(position);
                }
            }

            var curved = new List<WakePathPoint>((smoothed.Count - 1) * 3 + 1);
            for (var i = 0; i < smoothed.Count - 1; i++)
            {
                for (var step = 0; step < 3; step++)
                {
                    var factor = step / 3f;
                    curved.Add(WakePathPoint.Lerp(smoothed[i], smoothed[i + 1], factor));
                }
            }

            curved.Add(smoothed[^1]);

            return curved;
        }

        private void BuildPathMeshes(List<WakePathPoint> path, float hullHalfWidth, float normalizedSpeed,
            float alphaMultiplier, Mesh centerMesh, Mesh sideMesh, Mesh residualMesh)
        {
            if (path.Count < 2 || alphaMultiplier <= 0f)
            {
                centerMesh.Clear();
                sideMesh.Clear();
                residualMesh.Clear();

                return;
            }

            BuildCenterDecals(path, hullHalfWidth, normalizedSpeed, alphaMultiplier, centerMesh);
            BuildSideRibbon(path, hullHalfWidth, normalizedSpeed, alphaMultiplier, sideMesh);
            BuildResidualDecals(path, alphaMultiplier, residualMesh);
        }

        private float GetAlpha(WakePathPoint point, float tailFactor, float alphaMultiplier)
        {
            var lifeFactor = 1f - Mathf.Clamp01(point.Age / _lifetime);

            return lifeFactor * lifeFactor * Mathf.Lerp(0.08f, 1f, 1f - tailFactor) *
                Mathf.Lerp(0.45f, 1f, point.Intensity) * alphaMultiplier;
        }

        private static void GetFrame(List<WakePathPoint> path, int index, out Vector2 tangent, out Vector2 normal)
        {
            var previous = path[Mathf.Max(0, index - 1)].Position;
            var next = path[Mathf.Min(path.Count - 1, index + 1)].Position;
            tangent = (next - previous).normalized;
            normal = new Vector2(-tangent.y, tangent.x);
        }

        private static void GetRibbonJoin(Vector2[] positions, int index, out Vector2 normal, out float scale)
        {
            var previousIndex = Mathf.Max(0, index - 1);
            var nextIndex = Mathf.Min(positions.Length - 1, index + 1);
            var incoming = (positions[index] - positions[previousIndex]).normalized;
            var outgoing = (positions[nextIndex] - positions[index]).normalized;
            if (index == 0)
            {
                incoming = outgoing;
            }
            else if (index == positions.Length - 1)
            {
                outgoing = incoming;
            }

            if (Vector2.Dot(incoming, outgoing) < -0.35f)
            {
                normal = new Vector2(-outgoing.y, outgoing.x);
                scale = 1f;

                return;
            }

            var incomingNormal = new Vector2(-incoming.y, incoming.x);
            var outgoingNormal = new Vector2(-outgoing.y, outgoing.x);
            normal = (incomingNormal + outgoingNormal).normalized;
            var denominator = Mathf.Abs(Vector2.Dot(normal, outgoingNormal));
            scale = Mathf.Min(1.35f, 1f / Mathf.Max(0.35f, denominator));
        }

        private readonly struct WakePathPoint
        {
            public WakePathPoint(Vector2 position, float intensity, float age)
            {
                Position = position;
                Intensity = intensity;
                Age = age;
            }

            public Vector2 Position { get; }
            public float Intensity { get; }
            public float Age { get; }

            public WakePathPoint WithPosition(Vector2 position)
            {
                return new WakePathPoint(position, Intensity, Age);
            }

            public static WakePathPoint Lerp(WakePathPoint first, WakePathPoint second, float factor)
            {
                return new WakePathPoint(Vector2.Lerp(first.Position, second.Position, factor),
                    Mathf.Lerp(first.Intensity, second.Intensity, factor), Mathf.Lerp(first.Age, second.Age, factor));
            }
        }
    }
}
