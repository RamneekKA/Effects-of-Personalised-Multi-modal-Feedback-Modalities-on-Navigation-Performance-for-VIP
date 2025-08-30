using Gley.UrbanSystem.Editor;

namespace Gley.PedestrianSystem.Editor
{
    internal static class AllWindowsData
    {
        const string urbanNamespace = "Gley.UrbanSystem";

        static readonly WindowProperties[] _allWindows =
        {
            //Main Menu
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(MainMenuWindow),"Pedestrian Settings",false,true,false,true,true,false,"https://youtu.be/V4RDuL2OspY"),
            new WindowProperties(urbanNamespace,nameof(ImportPackagesWindow),"Import Packages",true,true,true,false,true,false,"https://youtu.be/V4RDuL2OspY?feature=shared&t=23"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(PathSetupWindow),"Path Setup",true,true,true,false,false,false,"https://youtu.be/E-LCRPA-ShI?feature=shared&t=731"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(PedestrianWaypointSetupWindow),"Waypoint Setup",true,true,false,true,true,false,"https://gley.gitbook.io/mobile-pedestrian-system/setup-guide/waypoint-setup-window"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(SceneSetupWindow), "Scene Setup",true,true,false,true,false,false,"https://youtu.be/E-LCRPA-ShI?feature=shared&t=464"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(ExternalToolsWindow), "External Tools",true,true,true,false,false,false,""),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(PedestrianDebugWindow), "Debug",true,true,true,false,false,false,"https://gley.gitbook.io/mobile-pedestrian-system/setup-guide/debug-window"),

            //Path Setup
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(CreatePathWindow), "Create Path",true,true,true,true,true,true,"https://youtu.be/E-LCRPA-ShI?feature=shared&t=731"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(ConnectPathsWindow), "Connect Paths",true,true,true,true,true,true,"https://youtu.be/E-LCRPA-ShI?feature=shared&t=1157"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(ViewPathsWindow), "View Paths",true,true,true,true,true,true,"https://youtu.be/E-LCRPA-ShI?feature=shared&t=731"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(EditPathWindow), "Edit Path",true,true,true,true,true,true,"https://youtu.be/E-LCRPA-ShI?feature=shared&t=731"),

            //Waypoint Setup
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(ShowAllWaypoints), "All Waypoints",true,true,true,false,true,true,"https://youtu.be/E-LCRPA-ShI?feature=shared&t=1610"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(ShowDisconnectedWaypoints), "Disconnected Waypoints",true,true,true,true,true,true,"https://gley.gitbook.io/mobile-pedestrian-system/setup-guide/waypoint-setup-window"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(ShowPedestrianTypeEditedWaypoints), "Pedestrian Type Edited Waypoints",true,true,true,true,true,true,"https://gley.gitbook.io/mobile-pedestrian-system/setup-guide/waypoint-setup-window"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(ShowPriorityEditedWaypoints), "Priority Edited Waypoints",true,true,true,true,true,true,"https://gley.gitbook.io/mobile-pedestrian-system/setup-guide/waypoint-setup-window"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(ShowEventWaypoints), "Show Event Waypoints",true,true,true,true,true,true,"https://gley.gitbook.io/mobile-pedestrian-system/setup-guide/waypoint-setup-window"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(ShowPedestrianPathProblems), "Path Problems",true,true,true,true,true,true,"https://gley.gitbook.io/mobile-pedestrian-system/setup-guide/waypoint-setup-window"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(EditPedestrianWaypointWindow), "Edit Waypoint",true,true,true,true,true,true,"https://youtu.be/E-LCRPA-ShI?feature=shared&t=1552"),

            //Scene Setup
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(GridSetupWindow), "Grid Setup",true,true,true,true,true,true,"https://youtu.be/E-LCRPA-ShI?feature=shared&t=464"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(PedestrianRoutesSetupWindow), "Pedestrian Routes",true,true,false,true,true,true,"https://youtu.be/E-LCRPA-ShI?feature=shared&t=1590"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(LayerSetupWindow), "Layer Setup",true,true,true,false,true,false,"https://youtu.be/E-LCRPA-ShI?feature=shared&t=464"),

            //Pedestrian setup
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(PedestrianTypesWindow), "Pedestrian Types",true,true,true,true,true,false,"https://youtu.be/E-LCRPA-ShI?feature=shared&t=1590"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(PathFindingWindow), "Path Finding",true,true,true,true,true,true,"https://gley.gitbook.io/mobile-pedestrian-system/setup-guide/path-finding-setup"),
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(PedestrianWaypointPriorityWindow), "Waypoint Priority",true,true,false,true,true,true,"https://gley.gitbook.io/mobile-pedestrian-system/setup-guide/waypoint-priority-setup"),
        
            //External Tools
            new WindowProperties(Gley.PedestrianSystem.Internal.PedestrianSystemConstants.PedestrianNamespaceEditor,nameof(RoadConstructorSetup), "Road Constructor Setup",true,true,true,true,false,false,""),
        };

        internal static WindowProperties[] GetWindowsData()
        {
            return _allWindows;
        }
    }
}
