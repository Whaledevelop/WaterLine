using UnityEngine;

namespace Game.Ships
{
    public sealed partial class ShipWakeView
    {
        private static Mesh CreateMesh(string name, MeshFilter meshFilter)
        {
            var mesh = new Mesh
            {
                name = name
            };
            mesh.MarkDynamic();
            meshFilter.sharedMesh = mesh;

            return mesh;
        }

        private void BuildMeshes(Vector2 bowPosition, Vector2 sternPosition, float hullHalfWidth,
            float normalizedSpeed)
        {
            BuildBowMesh(bowPosition, sternPosition, normalizedSpeed);
            var pointCount = _points.Count + 1;
            if (pointCount < 2)
            {
                _centerMesh.Clear();
                _sideMesh.Clear();
                _residualMesh.Clear();

                return;
            }

            var wakeOrigin = sternPosition;
            var forward = (bowPosition - sternPosition).normalized;
            var exitPosition = sternPosition - forward * 0.2f;
            var positions = BuildSmoothedPositions(wakeOrigin, exitPosition);
            BuildCenterDecals(positions, hullHalfWidth, normalizedSpeed);
            BuildSideDecals(positions, hullHalfWidth, normalizedSpeed);
            BuildResidualMesh(positions);
        }

        private Vector2[] BuildSmoothedPositions(Vector2 sternPosition, Vector2 exitPosition)
        {
            var positions = new Vector2[_points.Count + 1];
            positions[0] = sternPosition;
            positions[1] = exitPosition;
            for (var i = 2; i < positions.Length; i++)
            {
                positions[i] = _points[i - 1].Position;
            }

            for (var pass = 0; pass < 4; pass++)
            {
                var previousPositions = (Vector2[])positions.Clone();
                for (var i = 2; i < positions.Length - 1; i++)
                {
                    positions[i] = previousPositions[i - 1] * 0.25f + previousPositions[i] * 0.5f +
                        previousPositions[i + 1] * 0.25f;
                }
            }

            return BuildCurvedPositions(positions, 3);
        }

        private static Vector2[] BuildCurvedPositions(Vector2[] controlPoints, int subdivisions)
        {
            var curvedPositions = new Vector2[(controlPoints.Length - 1) * subdivisions + 1];
            var curvedIndex = 0;
            for (var i = 0; i < controlPoints.Length - 1; i++)
            {
                var first = controlPoints[Mathf.Max(0, i - 1)];
                var second = controlPoints[i];
                var third = controlPoints[i + 1];
                var fourth = controlPoints[Mathf.Min(controlPoints.Length - 1, i + 2)];
                for (var step = 0; step < subdivisions; step++)
                {
                    var factor = (float)step / subdivisions;
                    curvedPositions[curvedIndex++] = i < 2
                        ? Vector2.Lerp(second, third, factor)
                        : CalculateCatmullRom(first, second, third, fourth, factor);
                }
            }

            curvedPositions[curvedIndex] = controlPoints[^1];

            return curvedPositions;
        }

        private static Vector2 CalculateCatmullRom(Vector2 first, Vector2 second, Vector2 third, Vector2 fourth,
            float factor)
        {
            var squaredFactor = factor * factor;
            var cubedFactor = squaredFactor * factor;

            return 0.5f * (2f * second + (third - first) * factor +
                (2f * first - 5f * second + 4f * third - fourth) * squaredFactor +
                (-first + 3f * second - 3f * third + fourth) * cubedFactor);
        }

        private void BuildCenterMesh(Vector2[] positions, float hullHalfWidth, float normalizedSpeed)
        {
            var vertices = new Vector3[positions.Length * 2];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[(positions.Length - 1) * 6];
            var accumulatedLength = 0f;
            for (var i = 0; i < positions.Length; i++)
            {
                if (i > 0)
                {
                    accumulatedLength += Vector2.Distance(positions[i - 1], positions[i]);
                }

                GetRibbonJoin(positions, i, out var normal, out var joinScale);
                var tailFactor = (float)i / (positions.Length - 1);
                var intensity = GetIntensity(i, positions.Length, normalizedSpeed);
                var headFactor = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(accumulatedLength / _headBlendDistance));
                var trailWidth = Mathf.Lerp(_baseWidth * Mathf.Lerp(0.28f, 0.48f, intensity),
                    _baseWidth * 0.045f, Mathf.Pow(tailFactor, 0.72f));
                var sternWidth = Mathf.Lerp(hullHalfWidth * 0.2f, trailWidth, headFactor);
                var width = Mathf.Lerp(sternWidth, trailWidth, headFactor);
                var alpha = GetAlpha(i, positions.Length, tailFactor, intensity);
                alpha *= Mathf.Lerp(0.15f, 1f, Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(0f, 0.35f, accumulatedLength)));
                var vertexIndex = i * 2;
                vertices[vertexIndex] = positions[i] - normal * width * joinScale;
                vertices[vertexIndex + 1] = positions[i] + normal * width * joinScale;
                uv[vertexIndex] = new Vector2(accumulatedLength * 0.42f, 0f);
                uv[vertexIndex + 1] = new Vector2(accumulatedLength * 0.42f, 1f);
                colors[vertexIndex] = new Color(1f, 1f, 1f, alpha);
                colors[vertexIndex + 1] = new Color(1f, 1f, 1f, alpha);
                SetQuadTriangles(triangles, i, vertexIndex);
            }

            ApplyMesh(_centerMesh, vertices, uv, colors, triangles);
        }

        private void BuildSideMesh(Vector2[] positions, float hullHalfWidth, float normalizedSpeed)
        {
            var vertices = new Vector3[positions.Length * 4];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[(positions.Length - 1) * 12];
            var accumulatedLength = 0f;
            for (var i = 0; i < positions.Length; i++)
            {
                if (i > 0)
                {
                    accumulatedLength += Vector2.Distance(positions[i - 1], positions[i]);
                }

                GetRibbonJoin(positions, i, out var normal, out var joinScale);
                var tailFactor = (float)i / (positions.Length - 1);
                var intensity = GetIntensity(i, positions.Length, normalizedSpeed);
                var headFactor = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(accumulatedLength / _headBlendDistance));
                var sideOffset = Mathf.Lerp(hullHalfWidth * 0.72f,
                    _baseWidth * Mathf.Lerp(1.55f, 3.15f, Mathf.Pow(tailFactor, 0.76f)), headFactor);
                var middleFactor = Mathf.Sin(Mathf.Clamp01(tailFactor) * Mathf.PI);
                var stripWidth = Mathf.Lerp(_baseWidth * 0.04f,
                    _baseWidth * Mathf.Lerp(0.45f, 0.86f, middleFactor), headFactor);
                stripWidth *= Mathf.Lerp(1f, 0.42f, tailFactor);
                var alpha = GetAlpha(i, positions.Length, tailFactor, intensity) * Mathf.Lerp(0.08f,
                    Mathf.Lerp(0.92f, 0.3f, tailFactor), headFactor);
                alpha *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.55f, accumulatedLength));
                var vertexIndex = i * 4;
                SetStrip(vertices, uv, colors, vertexIndex, positions[i] - normal * sideOffset * joinScale,
                    normal, stripWidth * joinScale, accumulatedLength, alpha);
                SetStrip(vertices, uv, colors, vertexIndex + 2, positions[i] + normal * sideOffset * joinScale,
                    normal, stripWidth * joinScale, accumulatedLength, alpha);

                if (i >= positions.Length - 1)
                {
                    continue;
                }

                SetStripTriangles(triangles, i * 12, vertexIndex, vertexIndex + 4);
                SetStripTriangles(triangles, i * 12 + 6, vertexIndex + 2, vertexIndex + 6);
            }

            ApplyMesh(_sideMesh, vertices, uv, colors, triangles);
        }

        private void BuildResidualMesh(Vector2[] positions)
        {
            var decalCount = Mathf.Min((positions.Length - 1) / 7, 14);
            var vertices = new Vector3[decalCount * 4];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[decalCount * 6];
            for (var decalIndex = 0; decalIndex < decalCount; decalIndex++)
            {
                var pointIndex = Mathf.Min(4 + decalIndex * 7, positions.Length - 2);
                GetFrame(positions, pointIndex, out var normal);
                var tangent = new Vector2(normal.y, -normal.x);
                var random = Mathf.Repeat(decalIndex * 0.618f, 1f);
                var center = positions[pointIndex] + normal * Mathf.Lerp(-0.55f, 0.55f, random);
                var halfWidth = Mathf.Lerp(0.16f, 0.34f, random);
                var halfLength = Mathf.Lerp(0.35f, 0.7f, 1f - random);
                var vertexIndex = decalIndex * 4;
                vertices[vertexIndex] = center - normal * halfWidth - tangent * halfLength;
                vertices[vertexIndex + 1] = center + normal * halfWidth - tangent * halfLength;
                vertices[vertexIndex + 2] = center - normal * halfWidth + tangent * halfLength;
                vertices[vertexIndex + 3] = center + normal * halfWidth + tangent * halfLength;
                uv[vertexIndex] = Vector2.zero;
                uv[vertexIndex + 1] = Vector2.right;
                uv[vertexIndex + 2] = Vector2.up;
                uv[vertexIndex + 3] = Vector2.one;
                var intensity = GetIntensity(pointIndex, positions.Length, 0f);
                var alpha = GetAlpha(pointIndex, positions.Length, (float)pointIndex / positions.Length,
                    intensity) * 0.38f;
                for (var colorIndex = 0; colorIndex < 4; colorIndex++)
                {
                    colors[vertexIndex + colorIndex] = new Color(1f, 1f, 1f, alpha);
                }

                SetDecalTriangles(triangles, decalIndex * 6, vertexIndex);
            }

            ApplyMesh(_residualMesh, vertices, uv, colors, triangles);
        }
    }
}
