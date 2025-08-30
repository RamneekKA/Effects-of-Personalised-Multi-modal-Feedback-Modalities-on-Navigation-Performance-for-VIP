using Gley.UrbanSystem.Editor;

namespace Gley.PedestrianSystem.Editor
{
    public class GridSetupWindow : GridSetupWindowBase
    {
        public override void DrawInScene()
        {
            if (_viewGrid)
            {
                _gridDrawer.DrawGrid(false);
            }
            base.DrawInScene();
        }
    }
}
