using UnityEngine;

namespace Game.Ships
{
    public sealed class ShipFoamView : MonoBehaviour
    {
        private static readonly int WaterlineMaskId = Shader.PropertyToID("_WaterlineMask");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        [SerializeField]
        private SpriteRenderer _primaryRenderer;

        [SerializeField]
        private SpriteRenderer _secondaryRenderer;

        private MaterialPropertyBlock _propertyBlock;
        private SpriteRenderer _activeRenderer;
        private SpriteRenderer _fadingRenderer;
        private ShipVisualProfile _profile;
        private float _fadeProgress;
        private float _intensity;

        public void Initialize(ShipVisualProfile profile, int directionIndex)
        {
            _propertyBlock = new MaterialPropertyBlock();
            _profile = profile;
            _activeRenderer = _primaryRenderer;
            _fadingRenderer = _secondaryRenderer;
            ApplyVisual(_activeRenderer, profile.GetVisual(directionIndex));
            SetAlpha(_activeRenderer, 1f);
            SetAlpha(_fadingRenderer, 0f);
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

        public void Tick(float normalizedSpeed, float deltaTime)
        {
            _intensity = Mathf.Lerp(_intensity, normalizedSpeed, 1f - Mathf.Exp(-deltaTime * 7f));
            SetIntensity(_activeRenderer, _intensity);
            SetIntensity(_fadingRenderer, _intensity);
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
            _propertyBlock.SetTexture(WaterlineMaskId, visual.WaterlineMask);
            _propertyBlock.SetFloat(IntensityId, _intensity);
            spriteRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void SetIntensity(SpriteRenderer spriteRenderer, float intensity)
        {
            spriteRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(IntensityId, intensity);
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
