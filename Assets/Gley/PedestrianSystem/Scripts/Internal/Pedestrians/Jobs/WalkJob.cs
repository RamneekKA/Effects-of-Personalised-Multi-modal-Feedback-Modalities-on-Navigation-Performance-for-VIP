using Unity.Collections;
using Unity.Jobs;
#if GLEY_PEDESTRIAN_SYSTEM
using Unity.Mathematics;
#endif

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Perform computations required for movement.
    /// </summary>
    internal struct WalkJob : IJobParallelFor
    {
#if GLEY_PEDESTRIAN_SYSTEM
        private const float _minWaypointDistance = 1;

        private float3 _waypointDirection;
        private float _waypointDistance;
        private float _dotProduct;

        [ReadOnly] public NativeArray<float3> TargetWaypointPosition;
        [ReadOnly] public NativeArray<float3> AllBotsPosition;
        [ReadOnly] public NativeArray<float3> ForwardDirection;
        [ReadOnly] public NativeArray<float3> CameraPositions;
        [ReadOnly] public float3 WorldUp;
        [ReadOnly] public float FixedDeltaTime;
        [ReadOnly] public float DistanceToRemove;

        public NativeArray<float> CurrentAngle;
        public NativeArray<bool> NeedsWaypoint;
        public NativeArray<bool> ReadyToRemove;

#endif
        public void Execute(int index)
        {
#if GLEY_PEDESTRIAN_SYSTEM
            _waypointDirection = TargetWaypointPosition[index] - AllBotsPosition[index];
            _waypointDistance = math.distance(TargetWaypointPosition[index], AllBotsPosition[index]);
            _dotProduct = math.dot(_waypointDirection, ForwardDirection[index]);

            ChangeWaypoint(index);

            LookAtTargetFunction(index, TargetWaypointPosition[index]);

            RemovePedestrian(index);
#endif
        }

#if GLEY_PEDESTRIAN_SYSTEM
        /// <summary>
        /// Checks if the pedestrian can be removed from scene.
        /// </summary>
        /// <param name="index">The list index of the pedestrian</param>
        private void RemovePedestrian(int index)
        {
            bool remove = true;
            for (int i = 0; i < CameraPositions.Length; i++)
            {
                if (math.distancesq(AllBotsPosition[index], CameraPositions[i]) < DistanceToRemove)
                {
                    remove = false;
                    break;
                }
            }
            ReadyToRemove[index] = remove;
        }


        /// <summary>
        /// Calculate rotation angle to target.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="target"></param>
        private void LookAtTargetFunction(int index, float3 target)
        {
            float3 forward = ForwardDirection[index];
            forward.y = 0;
            float3 directionMan = new float3(target.x - AllBotsPosition[index].x, 0f, target.z - AllBotsPosition[index].z);

            float targetAngle = SignedAngle(forward, directionMan, WorldUp);
            if (math.abs(CurrentAngle[index] - targetAngle) < 5)
            {
                CurrentAngle[index] = targetAngle;
            }
            else
            {
                //if the angle difference is greater than 90 degrees rotate with 5 degrees.
                //if is less than 18 rotate with 1
                //in the middle rotate based on the angle difference
                CurrentAngle[index] -= math.sign(CurrentAngle[index] - targetAngle) * math.clamp(math.abs(CurrentAngle[index] - targetAngle) / 18, 1, 5);
            }
        }


        /// <summary>
        /// Compute sign angle between 2 directions.
        /// </summary>
        /// <param name="dir1"></param>
        /// <param name="dir2"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        private float SignedAngle(float3 dir1, float3 dir2, float3 normal)
        {
            if (dir1.Equals(float3.zero))
            {
                return 0;
            }
            dir1 = math.normalize(dir1);
            return math.degrees(math.atan2(math.dot(math.cross(dir1, dir2), normal), math.dot(dir1, dir2)));
        }


        /// <summary>
        /// Check if waypoint needs to be changed.
        /// </summary>
        /// <param name="index"></param>
        private void ChangeWaypoint(int index)
        {
            if (_waypointDistance < _minWaypointDistance || (_dotProduct < 0 && _waypointDistance < _minWaypointDistance * 2))
            {
                NeedsWaypoint[index] = true;
            }
        }
#endif
    }
}
