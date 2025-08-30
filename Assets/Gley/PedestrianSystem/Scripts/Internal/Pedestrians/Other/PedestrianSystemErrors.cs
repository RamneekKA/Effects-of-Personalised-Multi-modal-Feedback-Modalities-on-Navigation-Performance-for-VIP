namespace Gley.PedestrianSystem.Internal
{
    public static class PedestrianSystemErrors
    {
        public static string FatalError => "Pedestrian System will not work";
        public static string NullWaypointData => "Waypoints data is null";
        public static string NoWaypointsFound => "No waypoints found";
        public static string LayersNotConfigured => "Layers are not configured. Go to Tools->Gley->Pedestrian System->Scene Setup->Layer Setup";
        public static string NoPedestrians => "Nr of pedestrians needs to be greater than 1";
        public static string NoPathFindingWaypoints => "PathFindng not enabled.";
        public static string NullPathFindingData => "Path Finding data is null.";
    }
}
