using UnityEngine;

namespace Game.Ships
{
    public sealed class ShipMovement
    {
        private readonly ShipModel _model;
        private readonly ShipMovementSettings _settings;

        public ShipMovement(ShipModel model, ShipMovementSettings settings)
        {
            _model = model;
            _settings = settings;
        }

        public void Tick(float deltaTime)
        {
            if (!_model.HasTarget)
            {
                return;
            }

            var toTarget = _model.TargetPosition - _model.Position;
            var distance = toTarget.magnitude;
            if (distance <= _settings.ArrivalRadius)
            {
                _model.Position = _model.TargetPosition;
                _model.CompleteMovement();

                return;
            }

            var targetHeading = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            _model.Heading = Mathf.MoveTowardsAngle(_model.Heading, targetHeading, _settings.TurnSpeed * deltaTime);
            var headingError = Mathf.Abs(Mathf.DeltaAngle(_model.Heading, targetHeading));
            var speedFactor = Mathf.Clamp01(1f - headingError / 90f);
            var targetSpeed = _settings.MaximumSpeed * speedFactor;
            if (distance < _settings.SlowdownDistance)
            {
                targetSpeed *= distance / _settings.SlowdownDistance;
                _model.State = ShipMovementState.Arriving;
            }
            else if (headingError > 15f)
            {
                _model.State = ShipMovementState.Turning;
            }
            else
            {
                _model.State = ShipMovementState.Moving;
            }

            var rate = targetSpeed < _model.Speed ? _settings.Braking : _settings.Acceleration;
            _model.Speed = Mathf.MoveTowards(_model.Speed, targetSpeed, rate * deltaTime);
            var radians = _model.Heading * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            _model.Position += direction * (_model.Speed * deltaTime);
        }
    }
}
