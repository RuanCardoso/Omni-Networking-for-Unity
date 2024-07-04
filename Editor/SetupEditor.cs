#if UNITY_EDITOR
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Omni.Editor
{
    enum ScriptingBackend
    {
        IL2CPP,
        Mono
    }

    internal class SetupEditor
        : AssetPostprocessor,
            IActiveBuildTargetChanged,
            IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
#if OMNI_RELEASE
            Object selectedObject = Selection.activeObject;
            if (selectedObject == null || selectedObject.name != "Network Manager")
            {
                throw new System.NullReferenceException(
                    "Please select the 'Network Manager' in the current scene and try again. An error occurred during the build process. If the problem persists, refer to the documentation or contact technical support."
                );
            }
#endif
            SetScriptingBackend();
        }

        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            SetMacros();
            SetScriptingBackend();
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            SetMacros();
            SetScriptingBackend();
        }

        private void OnPreprocessAsset()
        {
            SetMacros();
            SetScriptingBackend();
        }

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            SetMacros(newTarget);
            SetScriptingBackend(newTarget);
        }

#if OMNI_RELEASE
        [MenuItem("Omni Networking/Change to Debug")]
#endif
        private static void ChangeToDebug()
        {
            if (EditorHelper.SetDefines(false))
            {
                ShowDialog();
            }
        }

#if OMNI_DEBUG
        [MenuItem("Omni Networking/Change to Release")]
#endif
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

        private static void SetScriptingBackend(BuildTarget? buildTarget = null)
        {
            if (File.Exists("ScriptingBackend.txt"))
            {
                using StreamReader reader = new("ScriptingBackend.txt");
                string json = reader.ReadToEnd();

                ScriptingBackend[] scriptingBackends =
                    JsonConvert.DeserializeObject<ScriptingBackend[]>(json);

                var namedBuildTarget = EditorHelper.GetCurrentNamedBuildTarget(buildTarget);
                ScriptingBackend scriptingBackend =
                    namedBuildTarget == NamedBuildTarget.Server
                        ? scriptingBackends[0]
                        : scriptingBackends[1];

                PlayerSettings.SetScriptingBackend(
                    namedBuildTarget,
                    scriptingBackend == ScriptingBackend.IL2CPP
                        ? ScriptingImplementation.IL2CPP
                        : ScriptingImplementation.Mono2x
                );
            }
            else
            {
                var namedBuildTarget = EditorHelper.GetCurrentNamedBuildTarget(buildTarget);
                PlayerSettings.SetScriptingBackend(
                    namedBuildTarget,
                    ScriptingImplementation.Mono2x
                );
            }
        }
    }
}
#endif
