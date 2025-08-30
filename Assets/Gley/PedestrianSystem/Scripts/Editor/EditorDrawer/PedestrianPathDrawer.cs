using Gley.PedestrianSystem.Internal;
using Gley.UrbanSystem.Editor;
using Gley.UrbanSystem.Internal;
using UnityEditor;
using UnityEngine;

namespace Gley.PedestrianSystem.Editor
{
    public class PedestrianPathDrawer : RoadDrawer<PedestrianPathEditorData,PedestrianPath>
    {
        public PedestrianPathDrawer (PedestrianPathEditorData data):base(data) 
        {

        }


        public void ToggleClosed(PedestrianPath road)
        {
            Path path = road.path;
            path.IsClosed = !path.IsClosed;
            SceneView.RepaintAll();
        }


        public void CloseLoop(PedestrianPath road)
        {
            if (road.path.IsClosed == false)
            {
                Undo.RecordObject(road, "Close Loop");
                ToggleClosed(road);
            }
        }


        public void AddPathPoint(Vector3 mousePosition, PedestrianPath road, bool clicked)
        {
            if (clicked == true)
            {
                return;
            }
            if (road.selectedSegmentIndex == -1)
            {
                Undo.RecordObject(road, "Add segment");
                road.path.AddSegment(mousePosition);
            }
            else
            {
                Undo.RecordObject(road, "Split segment");
                road.path.SplitSegment(mousePosition, road.selectedSegmentIndex);
            }
        }


        public override Vector3 DrawHandle(Path path, int i, RoadBase road, float handleSize)
        {
#if UNITY_2019 || UNITY_2020 || UNITY_2021
            if (i == 0)
            {
                return Handles.FreeMoveHandle(Internal.PedestrianSystemConstants.PATH_ID, path.GetPoint(i, road.positionOffset), Quaternion.identity, handleSize, Vector2.zero, Handles.SphereHandleCap);
            }
            else
            {
                return base.DrawHandle(path, i, road, handleSize);
            }
#else
            if (i == 0)
            {
                return Handles.FreeMoveHandle(PedestrianSystemConstants.PATH_ID, path.GetPoint(i, road.positionOffset), handleSize, Vector2.zero, Handles.SphereHandleCap);
            }
            else
            {
                return base.DrawHandle(path, i, road, handleSize);
            }
#endif
        }
    }
}
