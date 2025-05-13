#if UNITY_EDITOR
using Newtonsoft.Json;
using Omni.Shared;
using ParrelSync;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;
using System;
using UnityEditor.Compilation;
using Omni.Core;

#pragma warning disable

namespace Omni.Editor
{
    enum ScriptingBackend
    {
        IL2CPP,
        Mono
    }

    internal class BuildInterceptor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private static void SetupBeforeBuild()
        {
            var target = EditorHelper.GetCurrentNamedBuildTarget();
#if OMNI_RELEASE
#if UNITY_SERVER
            if (PlayerSettings.GetScriptingBackend(target) != ScriptingImplementation.Mono2x)
            {
                bool changeToBuildMono = EditorUtility.DisplayDialog(
                    "Server Build Recommendation",
                    "It is recommended to use the Mono scripting backend for server builds.\n\n" +
                    "Would you like to switch to Mono?",
                    "Yes, switch to Mono",
                    "No, keep current configuration"
                );

                if (changeToBuildMono)
                {
                    PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
                }
            }
#else
            if (PlayerSettings.GetScriptingBackend(target) != ScriptingImplementation.IL2CPP)
            {
                bool changeToBuildIL2CPP = EditorUtility.DisplayDialog(
                    "Client Build Recommendation",
                    "It is recommended to use the IL2CPP scripting backend for client builds.\n\n" +
                    "Would you like to switch to IL2CPP?",
                    "Yes, switch to IL2CPP",
                    "No, keep current configuration"
                );

                if (changeToBuildIL2CPP)
                {
                    PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
                }
            }
#endif
            string path = NetworkManager.__Internal__Key_Path__;
            bool isOk = EditorUtility.DisplayDialog(
                "Generate Encryption Keys",
                "Would you like to generate new encryption keys for this project?\n\n" +
                "Important: Encryption keys must match between client and server builds.\n" +
                "• Generating new keys will require rebuilding both client and server with the same keys\n" +
                "• Existing connections will not work with new keys\n" +
                "• Make sure to distribute the same keys to all parts of your application",
                "No, Keep Current Keys",
                "Yes, Generate New Keys"
            );

            if (!isOk)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
            }
            else
            {
                if (!File.Exists(path))
                {
                    CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
                }
            }
#endif
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            SetupBeforeBuild();
        }
    }

    internal class SetupEditor
        : AssetPostprocessor,
            IActiveBuildTargetChanged,
            IPreprocessBuildWithReport
    {
        private const string OMNI_VERSION = NetworkLogger.Version;
        public int callbackOrder => -1;

        public void OnPreprocessBuild(BuildReport report)
        {
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
        }

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            SetMacros(newTarget);
            SetScriptingBackend(newTarget);
        }

#if OMNI_RELEASE
        [MenuItem("Omni Networking/Settings/Set Build Mode: Debug", priority = 0)]
#endif
        private static void ChangeToDebug()
        {
            bool confirmChange = EditorUtility.DisplayDialog(
                "Omni Networking Configuration",
                "Are you sure you want to switch to debug mode?",
                "Yes",
                "No"
            );

            if (confirmChange)
            {
                if (EditorHelper.SetDefines(false))
                {
                    ShowDialog();
                }
            }
        }

#if OMNI_DEBUG
        [MenuItem("Omni Networking/Settings/Set Build Mode: Release", priority = 0)]
#endif
        private static void ChangeToRelease()
        {
            bool confirmChange = EditorUtility.DisplayDialog(
                "Omni Networking Configuration",
                "Are you sure you want to switch to release mode?",
                "Yes",
                "No"
            );

            if (confirmChange)
            {
                if (EditorHelper.SetDefines(true))
                {
                    ShowDialog();
                }
            }
        }

        private static void ShowDialog()
        {
            EditorUtility.DisplayDialog(
                "Omni Networking Configuration",
                "Macros have been successfully imported. The project is now recompiling, please wait for the process to complete.",
                "OK"
            );
        }

        // Set the macros to the current build target the first time.
        private static void SetMacros(BuildTarget? buildTarget = null) // Saved in assets folder(hidden)!
        {
            string fileName = Path.Combine(
                Application.dataPath,
                $".omni_macros_{EditorHelper.GetCurrentNamedBuildTarget(buildTarget).TargetName.ToLower()}_{OMNI_VERSION}.ini"
            );

            if (!File.Exists(fileName))
            {
                using FileStream fileStream = File.Create(fileName);
                if (EditorHelper.SetDefines(false))
                {
                    using StreamWriter writer = new(fileStream);
                    writer.Write(string.Join("\n", EditorHelper.GetDefines()));
                    // Warn the user that the macros have been imported.
                    ShowDialog();
                }
            }
        }

        private static void SetScriptingBackend(BuildTarget? buildTarget = null)
        {
            // WebGl only supports IL2CPP -> Wasm
#if UNITY_WEBGL
			return;
#endif
            if (!ClonesManager.IsClone())
            {
                if (File.Exists("ScriptingBackend.txt"))
                {
                    try
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
                    catch
                    {
                        File.Delete("ScriptingBackend.txt");
                    }
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
}
#endif