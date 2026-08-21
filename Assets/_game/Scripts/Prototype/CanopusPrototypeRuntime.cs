using Game.Ships;
using Game.Water;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using Whaledevelop;

namespace Game.Prototype
{
    public sealed class CanopusPrototypeRuntime : IUpdate
    {
        private readonly Camera _camera;
        private readonly ShipModel _model;
        private readonly ShipMovement _movement;
        private readonly ShipMovementSettings _movementSettings;
        private readonly ShipVisualProfile _visualProfile;
        private readonly ShipView _shipView;
        private readonly ShipFoamView _foamView;
        private readonly ShipWakeView _wakeView;
        private readonly WaterView _waterView;
        private readonly ShipDirectionResolver _directionResolver = new();

        [Inject]
        public CanopusPrototypeRuntime(Camera camera, ShipModel model, ShipMovement movement,
            ShipMovementSettings movementSettings, ShipVisualProfile visualProfile, ShipView shipView,
            ShipFoamView foamView, ShipWakeView wakeView, WaterView waterView)
        {
            _camera = camera;
            _model = model;
            _movement = movement;
            _movementSettings = movementSettings;
            _visualProfile = visualProfile;
            _shipView = shipView;
            _foamView = foamView;
            _wakeView = wakeView;
            _waterView = waterView;
        }

        public void Initialize()
        {
            _model.Position = Vector2.zero;
            _model.Heading = 0f;
            _directionResolver.Initialize(_model.Heading);
            _shipView.Initialize(_visualProfile, _directionResolver.CurrentIndex);
            UpdateViews(0f);
        }

        public void OnUpdate()
        {
            ProcessInput();
            _movement.Tick(Time.deltaTime);
            if (_directionResolver.Resolve(_model.Heading))
            {
                _shipView.SetDirection(_directionResolver.CurrentIndex);
            }

            UpdateViews(Time.deltaTime);
        }

        private void ProcessInput()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var screenPosition = Mouse.current.position.ReadValue();
            var worldPosition = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -_camera.transform.position.z));
            _model.SetTarget(worldPosition);
        }

        private void UpdateViews(float deltaTime)
        {
            _directionResolver.GetInterpolation(_model.Heading, out var fromIndex, out var toIndex, out var factor);
            var anchors = InterpolateAnchors(_visualProfile.GetVisual(fromIndex), _visualProfile.GetVisual(toIndex), factor);
            var normalizedSpeed = _model.Speed / _movementSettings.MaximumSpeed;
            _shipView.Tick(_model.Position, deltaTime);
            _foamView.Tick(_model.Position, anchors, normalizedSpeed);
            _wakeView.Tick(_model.Position + anchors.Stern, normalizedSpeed);
            _waterView.Tick(Time.time);
        }

        private static ShipVisualAnchors InterpolateAnchors(ShipDirectionVisual from, ShipDirectionVisual to, float factor)
        {
            return new ShipVisualAnchors(
                Vector2.Lerp(from.BowAnchor, to.BowAnchor, factor),
                Vector2.Lerp(from.SternAnchor, to.SternAnchor, factor),
                Vector2.Lerp(from.PortAnchor, to.PortAnchor, factor),
                Vector2.Lerp(from.StarboardAnchor, to.StarboardAnchor, factor),
                Mathf.Lerp(from.FoamWidth, to.FoamWidth, factor));
        }
    }
}
