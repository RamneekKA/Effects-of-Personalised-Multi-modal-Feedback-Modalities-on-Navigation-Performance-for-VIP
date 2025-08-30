using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Make pedestrian run for a specified amount of time.
    /// </summary>
    public class TimeRun : PedestrianBehaviour
    {
        private float _runSpeed;
        private float _runTime;
        private float _currentRunTime;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="runSpeed">The run speed.</param>
        /// <param name="runTime">How much time this behaviour will be active.</param>
        public TimeRun(float runSpeed, float runTime)
        {
            SetParams(runSpeed, runTime);
        }


        /// <summary>
        /// Reset the time every time this behaviour becomes active.
        /// </summary>
        protected override void OnBecomeActive()
        {
            base.OnBecomeActive();
            _currentRunTime = 0;
        }


        /// <summary>
        /// Executes on update when the behaviour is active.
        /// </summary>
        /// <param name="turnAngle">The angle of to turn to.</param>
        /// <param name="moveSpeed">The walking speed.</param>
        internal override void Execute(float turnAngle, float moveSpeed)
        {
            // Make pedestrian run until time runs out then stop
            _currentRunTime += Time.deltaTime;
            AnimatorMethods.FreeWalk(turnAngle, _runSpeed);
            if (_currentRunTime > _runTime)
            {
                Stop();       
            }
        }


        /// <summary>
        /// Changes the running parameters at runtime.
        /// </summary>
        /// <param name="runSpeed">The run speed.</param>
        /// <param name="runTime">How much time this behaviour will be active.</param>
        public void SetParams(float runSpeed, float runTime)
        {
            _runTime = runTime;
            _runSpeed = runSpeed;
        }


        /// <summary>
        /// Executes when behaviour is destroyed.
        /// </summary>
        internal override void OnDestroy()
        {
            
        }
    }
}
