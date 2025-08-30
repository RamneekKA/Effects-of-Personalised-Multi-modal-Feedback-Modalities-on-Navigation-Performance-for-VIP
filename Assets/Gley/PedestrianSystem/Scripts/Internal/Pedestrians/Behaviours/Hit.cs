using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Make a pedestrian fall if hit by an object.
    /// </summary>
    public class Hit : PedestrianBehaviour
    {
        private readonly string _hitState = "Hit";
        private readonly int _moveStateHash = Animator.StringToHash("MoveBlendTree");
        private readonly int _hitStateHash = Animator.StringToHash("Hit");

        private bool _stateChanged;


        /// <summary>
        /// Stop moving and make pedestrian fall every time this behaviour becomes active.
        /// </summary>
        protected override void OnBecomeActive()
        {
            base.OnBecomeActive();
            AnimatorMethods.StopMoving();
            CharacterAnimator.SetTrigger(_hitState);
        }


        /// <summary>
        /// Executes on update when the behaviour is active.
        /// </summary>
        /// <param name="turnAngle">The angle of to turn to.</param>
        /// <param name="moveSpeed">The walking speed.</param>
        internal override void Execute(float turnAngle, float moveSpeed)
        {
            // Check if state has change and only after that check if animator is back in walking state.
            if (_stateChanged == false)
            {
                if (CharacterAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash == _hitStateHash)
                {
                    _stateChanged = true;
                }
            }
            else
            {
                if (CharacterAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash == _moveStateHash)
                {
                    Stop();
                }
            }
        }


        /// <summary>
        /// Reset state every time this behaviour becomes inactive.
        /// </summary>
        protected override void OnBecameInactive()
        {
            base.OnBecameInactive();
            _stateChanged = false;
        }


        /// <summary>
        /// Executes when behaviour is destroyed.
        /// </summary>
        internal override void OnDestroy()
        {

        }
    }
}