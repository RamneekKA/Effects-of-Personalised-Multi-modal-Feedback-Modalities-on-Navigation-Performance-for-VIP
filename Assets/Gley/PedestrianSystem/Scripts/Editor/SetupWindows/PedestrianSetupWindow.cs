using Gley.UrbanSystem.Editor;
using UnityEditor;

namespace Gley.PedestrianSystem.Editor
{
    public class PedestrianSetupWindow : SetupWindowBase
    {
        protected PedestrianSettingsWindowData _editorSave;

        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);
            _editorSave = new SettingsLoader(Internal.PedestrianSystemConstants.WindowSettingsPath).LoadSettingsAsset<PedestrianSettingsWindowData>();
            return this;
        }


        public override void DestroyWindow()
        {
            EditorUtility.SetDirty(_editorSave);
            base.DestroyWindow();
        }
    }
}