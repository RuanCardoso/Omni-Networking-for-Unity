#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

#pragma warning disable CS0162

namespace Omni.Editor
{
    internal static class EditorHelper
    {
        internal static NamedBuildTarget GetCurrentNamedBuildTarget(BuildTarget? buildTarget = null)
        {
            if (IsServer())
            {
                return NamedBuildTarget.Server;
            }
            else
            {
                buildTarget ??= EditorUserBuildSettings.activeBuildTarget;
                BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget.Value);
                return NamedBuildTarget.FromBuildTargetGroup(targetGroup);
            }
        }

        private static List<string> GetDefines()
        {
            return PlayerSettings
                .GetScriptingDefineSymbols(GetCurrentNamedBuildTarget())
                .Split(";")
                .ToList();
        }

        private static void SetDefines(params string[] defines)
        {
            PlayerSettings.SetScriptingDefineSymbols(GetCurrentNamedBuildTarget(), defines);
        }

        internal static bool SetDefines(bool releaseMode)
        {
            List<string> currentDefines = GetDefines();
            if (IsServer())
            {
                if (!currentDefines.Contains("OMNI_SERVER"))
                {
                    currentDefines.Add("OMNI_SERVER");
                }
            }
            else
            {
                if (currentDefines.Contains("OMNI_SERVER"))
                {
                    currentDefines.Remove("OMNI_SERVER");
                }
            }

            if (!releaseMode)
            {
                if (!currentDefines.Contains("OMNI_DEBUG"))
                {
                    currentDefines.Add("OMNI_DEBUG");
                }

                if (currentDefines.Contains("OMNI_RELEASE"))
                {
                    currentDefines.Remove("OMNI_RELEASE");
                }
            }
            else
            {
                if (!currentDefines.Contains("OMNI_RELEASE"))
                {
                    currentDefines.Add("OMNI_RELEASE");
                }

                if (currentDefines.Contains("OMNI_DEBUG"))
                {
                    currentDefines.Remove("OMNI_DEBUG");
                }
            }

            string[] newDefines = currentDefines.ToArray();
            List<string> originalDefines = GetDefines();
            if (!newDefines.SequenceEqual(originalDefines))
            {
                SetDefines(newDefines);
                return true;
            }

            return false;
        }

        private static bool IsServer()
        {
#if !UNITY_STANDALONE && !UNITY_SERVER
            return false;
#endif
            return EditorUserBuildSettings.standaloneBuildSubtarget
                == StandaloneBuildSubtarget.Server;
        }
    }
}
#endif
