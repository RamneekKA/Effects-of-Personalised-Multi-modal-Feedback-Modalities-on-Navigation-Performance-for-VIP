namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// make a pedestrian run until the stop method is called.
    /// </summary>
    public class RunForever : PedestrianBehaviour
    {
        private float _runSpeed;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="runSpeed">The run speed.</param>
        public RunForever(float runSpeed)
        {
            SetParams(runSpeed);
        }


        /// <summary>
        /// Executes on update when the behaviour is active.
        /// </summary>
        /// <param name="turnAngle">The angle of to turn to.</param>
        /// <param name="moveSpeed">The walking speed.</param>
        internal override void Execute(float turnAngle, float moveSpeed)
        {
            //instead of the walk speed use the run speed 
            AnimatorMethods.FreeWalk(turnAngle, _runSpeed);
        }


        /// <summary>
        /// Changes the running parameters at runtime.
        /// </summary>
        /// <param name="runSpeed">The run speed.</param>
        public void SetParams(float runSpeed)
        {
            this._runSpeed = runSpeed;
        }


        /// <summary>
        /// Executes when behaviour is destroyed.
        /// </summary>
        internal override void OnDestroy()
        {
            
        }
    }
}