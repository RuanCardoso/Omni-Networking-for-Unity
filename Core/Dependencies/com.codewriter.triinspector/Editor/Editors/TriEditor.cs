using UnityEditor;
using UnityEngine.UIElements;

namespace Omni.Inspector.Editors
{
    public abstract class TriEditor : Editor
    {
        private TriEditorCore _core;

        protected virtual void OnEnable()
        {
            _core = new TriEditorCore(this);
        }

        protected virtual void OnDisable()
        {
            _core.Dispose();
        }

        public override void OnInspectorGUI()
        {
            _core.OnInspectorGUI(forceRepaint: InspectorBridge.ForceRepaint);
        }

        public override VisualElement CreateInspectorGUI()
        {
            return _core.CreateVisualElement(forceRepaint: InspectorBridge.ForceRepaint);
        }
    }
}