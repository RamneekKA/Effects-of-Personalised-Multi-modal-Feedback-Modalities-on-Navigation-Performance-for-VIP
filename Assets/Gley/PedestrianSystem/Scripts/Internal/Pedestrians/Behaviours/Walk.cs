namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Make a pedestrian walk -> the default behaviour
    /// </summary>
    public class Walk : PedestrianBehaviour
    {
        /// <summary>
        /// Executes on update when the behaviour is active.
        /// </summary>
        /// <param name="turnAngle">The angle of to turn to.</param>
        /// <param name="moveSpeed">The walking speed.</param>
        internal override void Execute(float turnAngle, float moveSpeed)
        {
            AnimatorMethods.FreeWalk(turnAngle, moveSpeed);
        }


        /// <summary>
        /// Called when a pedestrian is disabled to reset everything to initial state.
        /// </summary>
        protected override void OnReset()
        {
            base.OnReset();
            // Start again the behaviour since this is the default one.
            Start();
        }


        /// <summary>
        /// Executes when behaviour is destroyed.
        /// </summary>
        internal override void OnDestroy()
        {
        }
    }
}