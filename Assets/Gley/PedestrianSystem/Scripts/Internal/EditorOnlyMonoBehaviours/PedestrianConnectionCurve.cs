using Gley.UrbanSystem.Internal;
using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    /// <summary>
    /// Store pedestrian connection parameters for editor.
    /// </summary>
    [System.Serializable]
    public class PedestrianConnectionCurve : ConnectionCurveBase
    {
        [SerializeField][HideInInspector] private string _name;

        [SerializeField] private Transform _holder;
        [SerializeField] private Path _curve;
        [SerializeField] private PedestrianPath _fromPath;
        [SerializeField] private PedestrianPath _toPath;
        [SerializeField] private PedestrianWaypointSettings _fromWaypoint;
        [SerializeField] private PedestrianWaypointSettings _toWaypoint;

#if UNITY_EDITOR
        public bool Draw;
        public bool DrawWaypoints;
        public bool DrawWidth;
        public Vector3 InPosition;
        public Vector3 OutPosition;
        public bool InView;
#endif

        public string Name => _name;
        public Transform Holder => _holder;
        public Path Curve => _curve;
        public PedestrianWaypointSettings FromWaypoint => _fromWaypoint;
        public PedestrianWaypointSettings ToWaypoint => _toWaypoint;


        public PedestrianConnectionCurve(Path curve, PedestrianPath fromRoad, PedestrianPath toRoad, Transform holder, PedestrianWaypointSettings fromWaypoint, PedestrianWaypointSettings toWaypoint)
        {
            _name = holder.name;
            _fromWaypoint = fromWaypoint;
            _toWaypoint = toWaypoint;
            _curve = curve;
            _fromPath = fromRoad;
            _toPath = toRoad;
            _holder = holder;
#if UNITY_EDITOR
            Draw = true;
#endif
        }


        public bool VerifyAssignments()
        {
            if (_holder == null)
                return false;

            if (_fromWaypoint == null)
                return false;

            if (_toWaypoint == null)
                return false;

            if (_fromPath == null)
                return false;

            if (_toPath == null)
                return false;
            return true;
        }


        public Transform GetOriginRoad()
        {
            return _fromPath.transform;
        }


        public Vector3 GetOffset()
        {
            return _fromPath.positionOffset;
        }


        public bool ContainsRoad(PedestrianPath road)
        {
            if (_toPath == road || _fromPath == road)
            {
                return true;
            }
            return false;
        }
    }
}