using UnityEngine;

namespace Game.Ships
{
    public sealed class ShipFoamView : MonoBehaviour
    {
        [SerializeField]
        private LineRenderer _lineRenderer;

        [SerializeField]
        private Color _idleColor = new(0.85f, 0.95f, 1f, 0.35f);

        [SerializeField]
        private Color _movingColor = new(1f, 1f, 1f, 0.9f);

        private readonly Vector3[] _positions = new Vector3[5];

        public void Tick(Vector2 shipPosition, ShipVisualAnchors anchors, float normalizedSpeed)
        {
            _positions[0] = shipPosition + anchors.Bow;
            _positions[1] = shipPosition + anchors.Port;
            _positions[2] = shipPosition + anchors.Stern;
            _positions[3] = shipPosition + anchors.Starboard;
            _positions[4] = _positions[0];
            _lineRenderer.positionCount = _positions.Length;
            _lineRenderer.SetPositions(_positions);
            _lineRenderer.startWidth = anchors.FoamWidth;
            _lineRenderer.endWidth = anchors.FoamWidth;
            var color = Color.Lerp(_idleColor, _movingColor, normalizedSpeed);
            _lineRenderer.startColor = color;
            _lineRenderer.endColor = color;
        }
    }
}
