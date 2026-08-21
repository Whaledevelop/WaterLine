using UnityEngine;

namespace Game.Ships
{
    public sealed class ShipView : MonoBehaviour
    {
        private static readonly int SubmersionMaskId = Shader.PropertyToID("_SubmersionMask");
        private static readonly int UseSubmersionMaskId = Shader.PropertyToID("_UseSubmersionMask");

        [SerializeField]
        private SpriteRenderer _primaryRenderer;

        [SerializeField]
        private SpriteRenderer _secondaryRenderer;

        private MaterialPropertyBlock _propertyBlock;
        private SpriteRenderer _activeRenderer;
        private SpriteRenderer _fadingRenderer;
        private ShipVisualProfile _profile;
        private float _fadeProgress;

        public void Initialize(ShipVisualProfile profile, int directionIndex)
        {
            _propertyBlock = new MaterialPropertyBlock();
            _profile = profile;
            _activeRenderer = _primaryRenderer;
            _fadingRenderer = _secondaryRenderer;
            ApplyVisual(_activeRenderer, profile.GetVisual(directionIndex));
            SetAlpha(_activeRenderer, 1f);
            SetAlpha(_fadingRenderer, 0f);
            transform.localScale = Vector3.one * profile.VisualScale;
        }

        public void SetDirection(int directionIndex)
        {
            var previousRenderer = _activeRenderer;
            _activeRenderer = _fadingRenderer;
            _fadingRenderer = previousRenderer;
            ApplyVisual(_activeRenderer, _profile.GetVisual(directionIndex));
            SetAlpha(_activeRenderer, 0f);
            SetAlpha(_fadingRenderer, 1f);
            _fadeProgress = 0f;
        }

        public void Tick(Vector2 position, float deltaTime)
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            if (_fadeProgress >= 1f)
            {
                return;
            }

            _fadeProgress = Mathf.Clamp01(_fadeProgress + deltaTime / _profile.CrossfadeDuration);
            SetAlpha(_activeRenderer, _fadeProgress);
            SetAlpha(_fadingRenderer, 1f - _fadeProgress);
        }

        private void ApplyVisual(SpriteRenderer spriteRenderer, ShipDirectionVisual visual)
        {
            spriteRenderer.sprite = visual.Sprite;
            spriteRenderer.transform.localPosition = visual.VisualOffset;
            spriteRenderer.GetPropertyBlock(_propertyBlock);
            if (visual.SubmersionMask)
            {
                _propertyBlock.SetTexture(SubmersionMaskId, visual.SubmersionMask);
            }

            _propertyBlock.SetFloat(UseSubmersionMaskId, visual.SubmersionMask ? 1f : 0f);
            spriteRenderer.SetPropertyBlock(_propertyBlock);
        }

        private static void SetAlpha(SpriteRenderer spriteRenderer, float alpha)
        {
            var color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
}
