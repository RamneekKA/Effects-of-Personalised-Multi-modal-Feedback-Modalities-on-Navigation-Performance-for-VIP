using UnityEngine;

namespace Gley.PedestrianSystem
{
    /// <summary>
    /// Basic methods to control the pedestrian animator.
    /// </summary>
    public class AnimatorMethods
    {
        private readonly Animator _characterAnimator;
        private readonly int _angleId;
        private readonly int _speedId;

        private float _currentWalkSpeed;

        private float AnimatorSpeed
        {
            get
            {
                return _characterAnimator.GetFloat(_speedId);
            }
            set
            {
                _characterAnimator.SetFloat(_speedId, value);
            }
        }

        private float AnimatorAngle
        {
            get
            {
                return _characterAnimator.GetFloat(_angleId);
            }
            set
            {
                _characterAnimator.SetFloat(_angleId, value);
            }
        }

        public AnimatorMethods(Animator characterAnimator)
        {
            _characterAnimator = characterAnimator;
            _angleId = Animator.StringToHash(Internal.PedestrianSystemConstants.AnimatorAngleID);
            _speedId = Animator.StringToHash(Internal.PedestrianSystemConstants.AnimatorSpeedID);
        }


        /// <summary>
        /// Perform walking.
        /// </summary>
        /// <param name="turnAngle">The angle of to turn to.</param>
        /// <param name="moveSpeed">The walking speed.</param>
        public virtual void FreeWalk(float turnAngle, float moveSpeed)
        {
            _currentWalkSpeed = Mathf.MoveTowards(AnimatorSpeed, moveSpeed, Time.deltaTime);
            if (AnimatorSpeed < _currentWalkSpeed)
            {
                AnimatorSpeed = Mathf.MoveTowards(AnimatorSpeed, _currentWalkSpeed, Time.deltaTime);
            }
            else
            {
                AnimatorSpeed = _currentWalkSpeed;
            }
            AnimatorAngle = Mathf.MoveTowards(AnimatorAngle, turnAngle, 20);
        }


        /// <summary>
        /// Rotate the pedestrian towards a target
        /// </summary>
        /// <param name="forward">The forward of the pedestrian</param>
        /// <param name="targetDirection">The target point to turn at</param>
        public virtual void LookAtTarget(Vector3 forward, Vector3 targetDirection)
        {
            AnimatorAngle = Vector3.SignedAngle(forward, targetDirection, Vector3.up);
        }


        /// <summary>
        /// Stop pedestrian movement. Rotation is allowed
        /// </summary>
        public virtual void StopMoving()
        {
            if (AnimatorSpeed > 0)
            {
                AnimatorSpeed = 0;
            }
        }


        /// <summary>
        /// Stop movement and rotation
        /// </summary>
        public virtual void Stop()
        {
            AnimatorSpeed = 0;
            AnimatorAngle = 0;
        }
    }
}