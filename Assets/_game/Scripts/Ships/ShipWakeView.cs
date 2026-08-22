using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Ships
{
    public sealed partial class ShipWakeView : MonoBehaviour
    {
        [SerializeField]
        [BoxGroup("Active Rendering")]
        private MeshFilter _centerMeshFilter;
        [SerializeField]
        [BoxGroup("Active Rendering")]
        private MeshFilter _sideMeshFilter;
        [SerializeField]
        [BoxGroup("Active Rendering")]
        private MeshFilter _residualMeshFilter;
        [SerializeField]
        [BoxGroup("Bow Rendering")]
        private MeshFilter _bowMeshFilter;
        [SerializeField]
        [BoxGroup("Bow Rendering")]
        private MeshRenderer _bowRenderer;
        [SerializeField]
        [BoxGroup("Trail")]
        private int _maximumPoints = 128;
        [SerializeField]
        [BoxGroup("Trail")]
        private float _pointDistance = 0.1f;
        [SerializeField]
        [BoxGroup("Trail")]
        private float _minimumSpeed = 0.08f;
        [SerializeField]
        [BoxGroup("Trail")]
        private float _baseWidth = 0.42f;
        [SerializeField]
        [BoxGroup("Trail")]
        private float _centerWakeWidth = 0.75f;
        [SerializeField]
        [BoxGroup("Trail")]
        private float _headBlendDistance = 0.85f;
        [SerializeField]
        [BoxGroup("Trail")]
        private float _lifetime = 5f;
        private Mesh _centerMesh;
        private Mesh _sideMesh;
        private Mesh _residualMesh;
        private Mesh _bowMesh;
        private MaterialPropertyBlock _bowProperties;
        private Vector2 _previousSternPosition;
        private float _distanceSinceLastPoint;
        private bool _hasPreviousPosition;
        private int _directionIndex;
        private WakeDirectionHistory _history;

        private void Awake()
        {
            _centerMesh = CreateMesh("Ship Wake Center", _centerMeshFilter);
            _sideMesh = CreateMesh("Ship Wake Sides", _sideMeshFilter);
            _residualMesh = CreateMesh("Ship Wake Residuals", _residualMeshFilter);
            _bowMesh = CreateMesh("Ship Bow Waves", _bowMeshFilter);
            _bowProperties = new MaterialPropertyBlock();
        }

        private void OnDestroy()
        {
            Destroy(_centerMesh);
            Destroy(_sideMesh);
            Destroy(_residualMesh);
            Destroy(_bowMesh);
        }

        public void Tick(ShipVisualPose pose, float normalizedSpeed, float deltaTime)
        {
            AgeSamples(deltaTime);
            _history ??= new WakeDirectionHistory();
            if (!_hasPreviousPosition)
            {
                _previousSternPosition = pose.Stern;
                _history.Initialize(pose.Heading);
                _directionIndex = pose.DirectionIndex;
                _hasPreviousPosition = true;
            }

            if (_directionIndex != pose.DirectionIndex)
            {
                _history.Initialize(pose.Heading);
                _directionIndex = pose.DirectionIndex;
                _distanceSinceLastPoint = 0f;
                _previousSternPosition = pose.Stern;
            }

            if (normalizedSpeed >= _minimumSpeed)
            {
                AddDistanceSamples(_previousSternPosition, pose.Stern, pose, normalizedSpeed);
            }

            _previousSternPosition = pose.Stern;
            BuildBowMesh(pose, normalizedSpeed);
            BuildWakeMeshes(pose, normalizedSpeed);
        }

        private void AddDistanceSamples(Vector2 from, Vector2 to, ShipVisualPose pose, float normalizedSpeed)
        {
            var movement = to - from;
            var distance = movement.magnitude;
            if (distance <= 0f)
            {
                return;
            }

            var direction = movement / distance;
            var travelled = 0f;
            var distanceToNextPoint = _pointDistance - _distanceSinceLastPoint;
            while (travelled + distanceToNextPoint <= distance)
            {
                travelled += distanceToNextPoint;
                _history.Current.Add(new WakeSample(from + direction * travelled, direction, pose.Heading,
                    normalizedSpeed, pose.HullHalfWidth), distanceToNextPoint);
                _distanceSinceLastPoint = 0f;
                distanceToNextPoint = _pointDistance;
            }

            _distanceSinceLastPoint += distance - travelled;
            TrimHistory();
        }

        private void TrimHistory()
        {
            var excess = GetSampleCount() - _maximumPoints;
            if (excess <= 0)
            {
                return;
            }

            RemoveOldestSamples(_history.Current, ref excess);
        }

        private int GetSampleCount()
        {
            return GetSampleCount(_history.Current);
        }

        private static int GetSampleCount(WakeSegment segment)
        {
            return segment?.Samples.Count ?? 0;
        }

        private static void RemoveOldestSamples(WakeSegment segment, ref int count)
        {
            if (segment == null || count <= 0)
            {
                return;
            }

            var removeCount = Mathf.Min(count, segment.Samples.Count);
            segment.Samples.RemoveRange(segment.Samples.Count - removeCount, removeCount);
            count -= removeCount;
        }

        private void AgeSamples(float deltaTime)
        {
            if (_history == null)
            {
                return;
            }

            AgeSegment(_history.Current, deltaTime);
        }

        private void AgeSegment(WakeSegment segment, float deltaTime)
        {
            if (segment == null)
            {
                return;
            }

            for (var i = segment.Samples.Count - 1; i >= 0; i--)
            {
                var sample = segment.Samples[i];
                sample.Age += deltaTime;
                if (sample.Age >= _lifetime)
                {
                    segment.Samples.RemoveAt(i);
                    continue;
                }

                segment.Samples[i] = sample;
            }
        }
    }
}
