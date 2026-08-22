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
        private MaterialPropertyBlock _propertyBlock;
        private ShipVisualProfile _profile;
        private int _directionIndex = -1;

        public void Initialize(ShipVisualProfile profile, ShipVisualPose pose)
        {
            _propertyBlock = new MaterialPropertyBlock();
            _profile = profile;
            transform.localScale = Vector3.one * profile.VisualScale;
            ApplyPose(pose);
        }

        public void Tick(ShipVisualPose pose)
        {
            transform.position = new Vector3(pose.Center.x, pose.Center.y, transform.position.z);
            ApplyPose(pose);
        }

        private void ApplyPose(ShipVisualPose pose)
        {
            if (_directionIndex == pose.DirectionIndex)
            {
                return;
            }

            _directionIndex = pose.DirectionIndex;
            ApplyVisual(_primaryRenderer, _profile.GetVisual(_directionIndex));
            SetAlpha(_primaryRenderer, 1f);
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
