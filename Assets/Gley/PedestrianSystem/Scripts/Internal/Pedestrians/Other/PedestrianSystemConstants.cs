using UnityEngine;

namespace Gley.PedestrianSystem.Internal
{
    public static class PedestrianSystemConstants
    {
        public const string PACKAGE_NAME = "PedestrianSystem";

        public const string LayerSetupData = "PedestrianLayerSetupData";
        public const string PedestriansHolderName = "PedestriansHolder";
        public const string LayerPath = "Assets/Gley/PedestrianSystem/Resources/PedestrianLayerSetupData.asset";
        public const string PedestrianNamespaceEditor = "Gley.PedestrianSystem.Editor";
        public const string PedestrianNamespace = "Gley.PedestrianSystem";
        public const string WindowSettingsPath = "Assets/Gley/PedestrianSystem/EditorSave/SettingsWindowData.asset";
        public const string AgentTypesPath = "/Gley/PedestrianSystem/Scripts/Public";
        public const string PathName = "Path";
        public const string AnimatorAngleID = "Angle";
        public const string AnimatorSpeedID = "Speed";
        public const string GLEY_PEDESTRIAN_SYSTEM = "GLEY_PEDESTRIAN_SYSTEM";
        public const int PATH_ID = 1029384857;
        public const int INVALID_WAYPOINT_INDEX = int.MinValue;
        public const int INVALID_PEDESTRIAN_INDEX = int.MinValue;

        public static readonly Vector3 INVALID_WAYPOINT_POSITION = Vector3.positiveInfinity;

        public static string PlayHolder
        {
            get
            {
                return $"{PACKAGE_NAME}/{UrbanSystem.Internal.UrbanSystemConstants.PLAY_HOLDER}";
            }
        }

        public static string EditorWaypointsHolder
        {
            get
            {
                return $"{PACKAGE_NAME}/{UrbanSystem.Internal.UrbanSystemConstants.EDITOR_HOLDER}/EditorWaypoints";
            }
        }

        public static string EditorConnectionsHolder
        {
            get
            {
                return $"{PACKAGE_NAME}/{UrbanSystem.Internal.UrbanSystemConstants.EDITOR_HOLDER}/EditorConnections";
            }
        }
    }
}
