using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Active when a pedestrian is killed.
    /// </summary>
    public class Dead : PedestrianBehaviour
    {
        /// <summary>
        /// Internally called by the system to initialize the behaviour.
        /// </summary>
        /// <param name="characterAnimator">The animator from the character.</param>
        /// <param name="pedestrianIndex">The index of the pedestrian associated with the behaviour.</param>
        /// <param name="priority">The priority of the behaviour</param>
        internal override void Initialize(Animator characterAnimator, int pedestrianIndex, int priority)
        {
            base.Initialize(characterAnimator, pedestrianIndex, priority);
        }


        /// <summary>
        /// Instantiate the associated ragdoll.
        /// </summary>
        protected override void OnBecomeActive()
        {
            base.OnBecomeActive();
            API.GetPedestrian(PedestrianIndex).InstantiateRagDoll();
            
        }


        /// <summary>
        /// Executes on update when the behaviour is active.
        /// </summary>
        /// <param name="turnAngle">The angle of to turn to.</param>
        /// <param name="moveSpeed">The walking speed.</param>
        internal override void Execute(float turnAngle, float moveSpeed)
        {
            AnimatorMethods.StopMoving();
        }


        /// <summary>
        /// Executes when behaviour is destroyed.
        /// </summary>
        internal override void OnDestroy()
        {

        }
    }
}