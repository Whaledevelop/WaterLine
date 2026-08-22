using System.Collections.Generic;
using UnityEngine;

namespace Game.Ships
{
    public sealed class ShipWakeView : MonoBehaviour
    {
        [SerializeField]
        private MeshFilter _meshFilter;

        [SerializeField]
        private int _maximumPoints = 96;

        [SerializeField]
        private float _pointDistance = 0.12f;

        [SerializeField]
        private float _minimumSpeed = 0.08f;

        [SerializeField]
        private float _baseWidth = 0.12f;

        [SerializeField]
        private float _lifetime = 7f;

        private readonly List<WakePoint> _points = new();
        private Mesh _mesh;
        private Vector2 _previousSternPosition;
        private float _distanceSinceLastPoint;
        private bool _hasPreviousPosition;

        private void Awake()
        {
            _mesh = new Mesh
            {
                name = "Ship Wake Ribbon"
            };
            _mesh.MarkDynamic();
            _meshFilter.sharedMesh = _mesh;
        }

        public void Tick(Vector2 sternPosition, float normalizedSpeed, float deltaTime)
        {
            AgePoints(deltaTime);
            if (!_hasPreviousPosition)
            {
                _previousSternPosition = sternPosition;
                _hasPreviousPosition = true;
            }

            if (normalizedSpeed >= _minimumSpeed)
            {
                AddDistanceSamples(_previousSternPosition, sternPosition, normalizedSpeed);
            }

            _previousSternPosition = sternPosition;
            BuildMesh(sternPosition, normalizedSpeed);
        }

        private void AddDistanceSamples(Vector2 from, Vector2 to, float normalizedSpeed)
        {
            var segment = to - from;
            var distance = segment.magnitude;
            if (distance <= 0f)
            {
                return;
            }

            var direction = segment / distance;
            var travelled = 0f;
            var distanceToNextPoint = _pointDistance - _distanceSinceLastPoint;
            while (travelled + distanceToNextPoint <= distance)
            {
                travelled += distanceToNextPoint;
                _points.Insert(0, new WakePoint(from + direction * travelled, normalizedSpeed));
                _distanceSinceLastPoint = 0f;
                distanceToNextPoint = _pointDistance;
            }

            _distanceSinceLastPoint += distance - travelled;

            if (_points.Count > _maximumPoints)
            {
                _points.RemoveRange(_maximumPoints, _points.Count - _maximumPoints);
            }
        }

        private void AgePoints(float deltaTime)
        {
            for (var i = _points.Count - 1; i >= 0; i--)
            {
                var point = _points[i];
                point.Age += deltaTime;
                if (point.Age >= _lifetime)
                {
                    _points.RemoveAt(i);
                    continue;
                }

                _points[i] = point;
            }
        }

        private void BuildMesh(Vector2 sternPosition, float normalizedSpeed)
        {
            var pointCount = _points.Count + 1;
            if (pointCount < 2)
            {
                _mesh.Clear();

                return;
            }

            var vertices = new Vector3[pointCount * 2];
            var uv = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[(pointCount - 1) * 6];
            for (var i = 0; i < pointCount; i++)
            {
                var position = i == 0 ? sternPosition : _points[i - 1].Position;
                var previous = i <= 1 ? sternPosition : _points[i - 2].Position;
                var next = i == pointCount - 1 ? position : _points[i].Position;
                var tangent = (next - previous).normalized;
                var normal = new Vector2(-tangent.y, tangent.x);
                var age = i == 0 ? 0f : _points[i - 1].Age;
                var intensity = i == 0 ? normalizedSpeed : _points[i - 1].Intensity;
                var lifeFactor = 1f - Mathf.Clamp01(age / _lifetime);
                var lengthFactor = 1f - (float)i / pointCount;
                var halfWidth = _baseWidth * Mathf.Lerp(0.35f, 1.5f, intensity) * Mathf.Lerp(1f, 0.25f, lengthFactor);
                var vertexIndex = i * 2;
                vertices[vertexIndex] = position - normal * halfWidth;
                vertices[vertexIndex + 1] = position + normal * halfWidth;
                uv[vertexIndex] = new Vector2(i * 0.18f, 0f);
                uv[vertexIndex + 1] = new Vector2(i * 0.18f, 1f);
                var alpha = lifeFactor * lengthFactor * Mathf.Lerp(0.25f, 0.9f, intensity);
                colors[vertexIndex] = new Color(1f, 1f, 1f, alpha);
                colors[vertexIndex + 1] = new Color(1f, 1f, 1f, alpha);

                if (i >= pointCount - 1)
                {
                    continue;
                }

                var triangleIndex = i * 6;
                triangles[triangleIndex] = vertexIndex;
                triangles[triangleIndex + 1] = vertexIndex + 3;
                triangles[triangleIndex + 2] = vertexIndex + 1;
                triangles[triangleIndex + 3] = vertexIndex;
                triangles[triangleIndex + 4] = vertexIndex + 2;
                triangles[triangleIndex + 5] = vertexIndex + 3;
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.uv = uv;
            _mesh.colors = colors;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();
        }

        private struct WakePoint
        {
            public WakePoint(Vector2 position, float intensity)
            {
                Position = position;
                Intensity = intensity;
                Age = 0f;
            }

            public Vector2 Position;
            public float Intensity;
            public float Age;
        }
    }
}
