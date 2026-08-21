using System;
using UnityEngine;

namespace Game.Ships
{
    [Serializable]
    public sealed class ShipMovementSettings
    {
        [field: SerializeField]
        public float MaximumSpeed { get; private set; } = 2.5f;

        [field: SerializeField]
        public float Acceleration { get; private set; } = 1.8f;

        [field: SerializeField]
        public float Braking { get; private set; } = 2.5f;

        [field: SerializeField]
        public float TurnSpeed { get; private set; } = 100f;

        [field: SerializeField]
        public float ArrivalRadius { get; private set; } = 0.15f;

        [field: SerializeField]
        public float SlowdownDistance { get; private set; } = 2f;
    }
}
