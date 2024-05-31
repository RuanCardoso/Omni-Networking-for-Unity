#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Omni.Editor
{
    internal class ImportMacros : AssetPostprocessor, IActiveBuildTargetChanged
    {
        public int callbackOrder => 0;

        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            SetMacros();
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            SetMacros();
        }

        private void OnPreprocessAsset()
        {
            SetMacros();
        }

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            SetMacros(newTarget);
        }

        [MenuItem("Omni Networking/Change to Debug")]
        private static void ChangeToDebug()
        {
            if (EditorHelper.SetDefines(false))
            {
                ShowDialog();
            }
        }

        [MenuItem("Omni Networking/Change to Release")]
        private static void ChangeToRelease()
        {
            if (EditorHelper.SetDefines(true))
            {
                ShowDialog();
            }
        }

        private static void ShowDialog()
        {
            EditorUtility.DisplayDialog(
                "Omni Networking",
                "Macros have been imported, please wait for recompilation.",
                "Ok"
            );
        }

        // Set the macros to the current build target the first time.
        private static void SetMacros(BuildTarget? buildTarget = null)
        {
            string fileName = Path.Combine(
                Application.dataPath,
                $".omni_macros_{EditorHelper.GetCurrentNamedBuildTarget(buildTarget).TargetName.ToLower()}.ini"
            );

            if (!File.Exists(fileName))
            {
                if (EditorHelper.SetDefines(false))
                {
                    using FileStream _ = File.Create(fileName);
                    ShowDialog();
                }
            }
        }
    }
}
#endif
