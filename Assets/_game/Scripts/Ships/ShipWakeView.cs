using System.Collections.Generic;
using UnityEngine;

namespace Game.Ships
{
    public sealed partial class ShipWakeView : MonoBehaviour
    {
        [SerializeField]
        private MeshFilter _centerMeshFilter;

        [SerializeField]
        private MeshFilter _sideMeshFilter;

        [SerializeField]
        private MeshFilter _residualMeshFilter;

        [SerializeField]
        private int _maximumPoints = 128;

        [SerializeField]
        private float _pointDistance = 0.1f;

        [SerializeField]
        private float _minimumSpeed = 0.08f;

        [SerializeField]
        private float _baseWidth = 0.42f;

        [SerializeField]
        private float _lifetime = 8f;

        private readonly List<WakePoint> _points = new();
        private Mesh _centerMesh;
        private Mesh _sideMesh;
        private Mesh _residualMesh;
        private Vector2 _previousSternPosition;
        private float _distanceSinceLastPoint;
        private bool _hasPreviousPosition;

        private void Awake()
        {
            _centerMesh = CreateMesh("Ship Wake Center", _centerMeshFilter);
            _sideMesh = CreateMesh("Ship Wake Sides", _sideMeshFilter);
            _residualMesh = CreateMesh("Ship Wake Residuals", _residualMeshFilter);
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
            BuildMeshes(sternPosition, normalizedSpeed);
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
