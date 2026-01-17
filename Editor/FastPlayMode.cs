#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;

namespace Omni.Editor
{
    [InitializeOnLoad]
    public class FastPlayMode
    {
        private static bool originalEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions originalEnterPlayModeOptions;
        private static bool wasFastPlay;

        static FastPlayMode()
        {
            ToolbarExtender.RightToolbarGUI.Remove(OnToolbarGUI);
            ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnToolbarGUI()
        {
            bool isPlaying = EditorApplication.isPlaying;

            GUIContent fastPlayContent = isPlaying
                ? new GUIContent("■ Stop", "Stop Fast Play")
                : new GUIContent("▶ Fast (Experimental)",
                    "Fast Play (Disable Domain Reload)\n\n" +
                    "⚠ Original Enter Play Mode settings will be restored automatically after exiting Play Mode.");

            GUIStyle style = new GUIStyle(GUI.skin.button);
            if (isPlaying)
            {
                style.normal.textColor = Color.red;
                style.fontStyle = FontStyle.Bold;
            }

            if (GUILayout.Button(fastPlayContent, style, GUILayout.Width(150)))
            {
                if (!isPlaying)
                {
                    originalEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
                    originalEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;

                    EditorSettings.enterPlayModeOptionsEnabled = true;
                    EditorSettings.enterPlayModeOptions =
                        EnterPlayModeOptions.DisableDomainReload; // | EnterPlayModeOptions.DisableSceneReload;

                    wasFastPlay = true;
                    EditorApplication.isPlaying = true;
                }
                else
                {
                    EditorApplication.isPlaying = false;
                }
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode && wasFastPlay)
            {
                EditorSettings.enterPlayModeOptionsEnabled = originalEnterPlayModeOptionsEnabled;
                EditorSettings.enterPlayModeOptions = originalEnterPlayModeOptions;

                wasFastPlay = false;
            }
        }
    }
}
#endif