using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Ships
{
    [CreateAssetMenu(menuName = "Game/Ships/Visual Profile")]
    public sealed class ShipVisualProfile : ScriptableObject
    {
        [field: SerializeField]
        [field: BoxGroup("Visual")]
        public float VisualScale { get; private set; } = 0.36f;

        [field: SerializeField]
        [field: BoxGroup("Visual")]
        public float CrossfadeDuration { get; private set; } = 0.1f;

        [field: SerializeField]
        [field: BoxGroup("Directions")]
        [field: ListDrawerSettings(ShowFoldout = true)]
        public ShipDirectionVisual[] Directions { get; private set; }

        public ShipDirectionVisual GetVisual(int index)
        {
            return Directions[index];
        }
    }
}
