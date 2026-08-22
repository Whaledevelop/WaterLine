using System;
using UnityEngine;

namespace Game.Ships
{
    [Serializable]
    public sealed class ShipDirectionVisual
    {
        [field: SerializeField]
        public ShipDirection Direction { get; private set; }

        [field: SerializeField]
        public Sprite Sprite { get; private set; }

        [field: SerializeField]
        public Texture2D SubmersionMask { get; private set; }

        [field: SerializeField]
        public Texture2D WaterlineMask { get; private set; }

        [field: SerializeField]
        public Vector2 VisualOffset { get; private set; }

        [field: SerializeField]
        public Vector2 BowAnchor { get; private set; }

        [field: SerializeField]
        public Vector2 SternAnchor { get; private set; }

        [field: SerializeField]
        public Vector2 PortAnchor { get; private set; }

        [field: SerializeField]
        public Vector2 StarboardAnchor { get; private set; }

        [field: SerializeField]
        public float FoamWidth { get; private set; } = 0.12f;
    }
}
