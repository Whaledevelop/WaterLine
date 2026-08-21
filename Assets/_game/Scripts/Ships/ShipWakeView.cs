using System.Collections.Generic;
using UnityEngine;

namespace Game.Ships
{
    public sealed class ShipWakeView : MonoBehaviour
    {
        [SerializeField]
        private LineRenderer _lineRenderer;

        [SerializeField]
        private int _maximumPoints = 96;

        [SerializeField]
        private float _pointDistance = 0.12f;

        [SerializeField]
        private float _minimumSpeed = 0.08f;

        [SerializeField]
        private float _baseWidth = 0.1f;

        private readonly List<Vector3> _points = new();

        public void Tick(Vector2 sternPosition, float normalizedSpeed)
        {
            if (normalizedSpeed >= _minimumSpeed && ShouldAddPoint(sternPosition))
            {
                _points.Insert(0, sternPosition);
                if (_points.Count > _maximumPoints)
                {
                    _points.RemoveAt(_points.Count - 1);
                }
            }

            _lineRenderer.positionCount = _points.Count;
            for (var i = 0; i < _points.Count; i++)
            {
                _lineRenderer.SetPosition(i, _points[i]);
            }

            _lineRenderer.startWidth = _baseWidth + normalizedSpeed * _baseWidth;
            _lineRenderer.endWidth = 0f;
            var color = new Color(1f, 1f, 1f, Mathf.Lerp(0.15f, 0.75f, normalizedSpeed));
            _lineRenderer.startColor = color;
            _lineRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
        }

        private bool ShouldAddPoint(Vector2 sternPosition)
        {
            if (_points.Count == 0)
            {
                return true;
            }

            return Vector2.Distance(_points[0], sternPosition) >= _pointDistance;
        }
    }
}
