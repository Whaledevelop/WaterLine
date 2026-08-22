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
        private MaterialPropertyBlock _propertyBlock;
        private ShipVisualProfile _profile;
        private float _intensity;
        private int _directionIndex = -1;

        public void Initialize(ShipVisualProfile profile, ShipVisualPose pose)
        {
            _propertyBlock = new MaterialPropertyBlock();
            _profile = profile;
            ApplyPose(pose);
        }

        public void Tick(ShipVisualPose pose, float normalizedSpeed, float deltaTime)
        {
            _intensity = Mathf.Lerp(_intensity, normalizedSpeed, 1f - Mathf.Exp(-deltaTime * 7f));
            ApplyPose(pose);
        }

        private void ApplyPose(ShipVisualPose pose)
        {
            if (_directionIndex != pose.DirectionIndex)
            {
                _directionIndex = pose.DirectionIndex;
                ApplyVisual(_primaryRenderer, _profile.GetVisual(_directionIndex));
            }

            SetIntensity(_primaryRenderer, _intensity);
            SetAlpha(_primaryRenderer, 1f);
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
