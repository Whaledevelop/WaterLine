using UnityEngine;

namespace Game.Water
{
    public sealed class WaterView : MonoBehaviour
    {
        private static readonly int LayerOffsetId = Shader.PropertyToID("_LayerOffset");
        private static readonly int DetailOffsetId = Shader.PropertyToID("_DetailOffset");

        [SerializeField]
        private Renderer _renderer;

        [SerializeField]
        private Vector2 _layerSpeed = new(0.01f, 0.004f);

        [SerializeField]
        private Vector2 _detailSpeed = new(-0.007f, 0.012f);

        private MaterialPropertyBlock _propertyBlock;

        public void Tick(float time)
        {
            _propertyBlock ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetVector(LayerOffsetId, _layerSpeed * time);
            _propertyBlock.SetVector(DetailOffsetId, _detailSpeed * time);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
