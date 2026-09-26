using UnityEngine;

namespace AMG.Utilities
{
    public class PlayerMotionTracker(PlayerControl target)
    {
        public enum FacingSide { Left, Right }

        public PlayerControl Target { get; } = target;

        public float Speed { get; private set; }

        public Vector2 Velocity { get; private set; }

        public Vector2 Direction { get; private set; }

        public FacingSide Facing { get; private set; } = FacingSide.Right;

        private Vector2 _lastPosition = target.transform.position;
        private bool _hasSample;

        private const float MinSpeedForDirection = 0.05f;

        public void Tick(float deltaTime)
        {
            if (Target == null || deltaTime <= 0f) return;

            Vector2 currentPosition = Target.transform.position;

            if (!_hasSample)
            {
                _lastPosition = currentPosition;
                _hasSample = true;
                return;
            }

            Velocity = (currentPosition - _lastPosition) / deltaTime;
            Speed = Velocity.magnitude;

            if (Speed > MinSpeedForDirection)
            {
                Direction = Velocity.normalized;
                Facing = Direction.x >= 0f ? FacingSide.Right : FacingSide.Left;
            }
            else
            {
                Direction = Vector2.zero;
                TryRefreshFacingFromSprite();
            }

            _lastPosition = currentPosition;
        }

        private void TryRefreshFacingFromSprite()
        {
            try
            {
                var bodySprite = Target?.cosmetics?.currentBodySprite?.BodySprite;
                if (bodySprite != null)
                {
                    Facing = bodySprite.flipX ? FacingSide.Left : FacingSide.Right;
                }
            }
            catch
            {
                
            }
        }
    }
}