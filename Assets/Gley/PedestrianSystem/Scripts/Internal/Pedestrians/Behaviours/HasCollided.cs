using Gley.PedestrianSystem.Internal;
using UnityEngine;

namespace Gley.PedestrianSystem
{
    /// <summary>
    /// React to collision events.
    /// </summary>
    public class HasCollided : PedestrianBehaviour
    {
        private Transform _pedestrianTransform;
        private float _time = 1;


        /// <summary>
        /// Constructor
        /// Add collision event listener.
        /// </summary>
        public HasCollided()
        {
            Events.OnCollisionEnter += CollidedWithSomething;
        }


        /// <summary>
        /// Internally called by the system to initialize the behaviour.
        /// </summary>
        /// <param name="characterAnimator">The animator from the character<./param>
        /// <param name="pedestrianIndex">The index of the pedestrian associated with the behaviour.</param>
        /// <param name="priority">The priority of the behaviour.</param>
        internal override void Initialize(Animator characterAnimator, int pedestrianIndex, int priority)
        {
            base.Initialize(characterAnimator, pedestrianIndex, priority);

            // Cache the pedestrian transform for performance improvements.
            _pedestrianTransform = characterAnimator.transform;
        }


        /// <summary>
        /// Reset the time when behaviour becomes active.
        /// </summary>
        protected override void OnBecomeActive()
        {
            base.OnBecomeActive();
            _time = 1;
        }


        /// <summary>
        /// Executes on update when the behaviour is active.
        /// </summary>
        /// <param name="turnAngle">The angle of to turn to.</param>
        /// <param name="moveSpeed">The walking speed.</param>
        internal override void Execute(float turnAngle, float moveSpeed)
        {
            // When pedestrian collides with something it stops and looks to destination.
            _time -= Time.deltaTime;
            if (_time < 0)
            {
                // If pedestrian faces the destination it will start moving again.
                if (Vector3.Angle(_pedestrianTransform.forward, API.GetCurrentWaypointPosition(PedestrianIndex) - _pedestrianTransform.position) < 20)
                {
                    Stop();
                }
            }
            AnimatorMethods.StopMoving();
            AnimatorMethods.LookAtTarget(_pedestrianTransform.forward, API.GetCurrentWaypointPosition(PedestrianIndex) - _pedestrianTransform.position);
        }


        /// <summary>
        /// Listener triggered when the pedestrian collided with something.
        /// </summary>
        /// <param name="pedestrianIndex">The index of the pedestrian that triggered the event.</param>
        /// <param name="collision">The collision object.</param>
        /// <param name="obstacleType">The type of the obstacle.</param>
        private void CollidedWithSomething(int pedestrianIndex, Collision collision, ObstacleTypes obstacleType)
        {
            if (PedestrianIndex == pedestrianIndex)
            {
                if (obstacleType == ObstacleTypes.Pedestrian)
                {
                    // If collided with other pedestrian, slow down.
                    var pedestrian = collision.collider.attachedRigidbody.GetComponent<Pedestrian>();
                    if (pedestrian != null)
                    {
                        if (pedestrian.CurrentBehaviour.Priority > Priority)
                        {
                            if (Vector3.Angle(_pedestrianTransform.forward, collision.collider.transform.forward) < 30)
                            {
                                if (Vector3.Angle(_pedestrianTransform.forward, (collision.collider.transform.position - _pedestrianTransform.position)) < 90)
                                {
                                    API.GetPedestrian(PedestrianIndex).SloWDown(pedestrian.WalkSpeed * 0.8f, Random.Range(2, 5));
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning(collision.gameObject.name + " is on the wrong layer. Only walking pedestrians should be on this layer", collision.gameObject);
                    }
                }
                else
                {
                    float impactForce = CalculateImpactForce(collision);
                    if (impactForce < 6000000)
                    {
                        // If the impact force is low, just slightly move the pedestrian to avoid pushing objects.
                        _pedestrianTransform.GetComponent<Rigidbody>().MovePosition(collision.contacts[0].normal * 0.1f + _pedestrianTransform.position);
#if UNITY_6000_0_OR_NEWER
                        _pedestrianTransform.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
#else
                        _pedestrianTransform.GetComponent<Rigidbody>().velocity = Vector3.zero;
#endif
                        Start();
                    }
                    else
                    {
                        if (impactForce < 9000000)
                        {
                            API.StartBehavior<Hit>(PedestrianIndex);
                        }
                        else
                        {
                            API.StartBehavior<Dead>(PedestrianIndex);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Compute the impact force in Newtons between 2 moving objects.
        /// </summary>
        /// <param name="collision">The collision object.</param>
        /// <returns>The impact force.</returns>
        private float CalculateImpactForce(Collision collision)
        {
            if (collision.collider.attachedRigidbody == null)
            {
                return 0;
            }
            // Get relative velocity at the point of collision.
            float relativeVelocity = collision.relativeVelocity.magnitude;

            // Get masses of the colliding objects.
            float mass1 = collision.rigidbody.mass;

            float mass2 = collision.collider.attachedRigidbody.mass;

            // Calculate impact force using Newton's second law.
            float impactForce = relativeVelocity * (mass1 * mass2);

            return impactForce;
        }


        /// <summary>
        /// Remove event listeners.
        /// </summary>
        internal override void OnDestroy()
        {
            Events.OnCollisionEnter -= CollidedWithSomething;
        }
    }
}