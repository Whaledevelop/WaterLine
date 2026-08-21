using UnityEngine;

namespace Game.Ships
{
    public sealed class ShipModel
    {
        public Vector2 Position { get; set; }

        public Vector2 TargetPosition { get; private set; }

        public float Heading { get; set; }

        public float Speed { get; set; }

        public bool HasTarget { get; private set; }

        public ShipMovementState State { get; set; }

        public void SetTarget(Vector2 targetPosition)
        {
            TargetPosition = targetPosition;
            HasTarget = true;
        }

        public void CompleteMovement()
        {
            HasTarget = false;
            Speed = 0f;
            State = ShipMovementState.Idle;
        }
    }
}
