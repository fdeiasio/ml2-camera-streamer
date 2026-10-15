using System;
using UnityEditor;


namespace MagicLeap.SetupTool.Editor.Utilities
{
    public static class DefineSymbolUtility
    {
        private static bool IsObsolete(BuildTargetGroup group)
		{
			var attrs = typeof(BuildTargetGroup).GetField(group.ToString()).GetCustomAttributes(typeof(ObsoleteAttribute), false);
			return attrs.Length > 0;
		}

		public static void RemoveDefineSymbol(string define)
		{
			foreach (BuildTargetGroup targetGroup in Enum.GetValues(typeof(BuildTargetGroup)))
			{
				if (targetGroup == BuildTargetGroup.Unknown || IsObsolete(targetGroup)) continue;

			
				
#if UNITY_2023_1_OR_NEWER
				var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
				var defineSymbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
            	var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#endif

				if (defineSymbols.Contains(define))
				{
					defineSymbols = defineSymbols.Replace($"{define};", "");
					defineSymbols = defineSymbols.Replace(define, "");

#if UNITY_2023_1_OR_NEWER
					PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defineSymbols);
#else
      				PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defineSymbols);
#endif
				}
			}
		}

		public static void AddDefineSymbol(string define)
		{
	
			foreach (BuildTargetGroup targetGroup in Enum.GetValues(typeof(BuildTargetGroup)))
			{
				if (targetGroup == BuildTargetGroup.Unknown || IsObsolete(targetGroup)) continue;

#if UNITY_2023_1_OR_NEWER
				var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
				var defineSymbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
            	var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#endif

				if (!defineSymbols.Contains(define))
				{
					if (defineSymbols.Length < 1)
						defineSymbols = define;
					else if (defineSymbols.EndsWith(";"))
						defineSymbols = $"{defineSymbols}{define}";
					else
						defineSymbols = $"{defineSymbols};{define}";

#if UNITY_2023_1_OR_NEWER
					PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defineSymbols);
#else
      				PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defineSymbols);
#endif
	
				}
			}
		}

		public static bool ContainsDefineSymbolInAllBuildTargets(string symbol)
		{
			
			bool contains = false;
			foreach (BuildTargetGroup targetGroup in Enum.GetValues(typeof(BuildTargetGroup)))
			{
				if (targetGroup== BuildTargetGroup.EmbeddedLinux || targetGroup== BuildTargetGroup.LinuxHeadlessSimulation || targetGroup == BuildTargetGroup.Unknown || IsObsolete(targetGroup)) continue;

#if UNITY_2023_1_OR_NEWER
				var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
				var defineSymbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
            	var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#endif
				contains = defineSymbols.Contains(symbol);
				if (!contains)
				{
			
					break;
				}
			}

			return contains;
		}
		public static bool ContainsDefineSymbolInAnyBuildTarget(string symbol)
		{
			bool contains = false;
			foreach (BuildTargetGroup targetGroup in Enum.GetValues(typeof(BuildTargetGroup)))
			{
				if (targetGroup == BuildTargetGroup.Unknown || IsObsolete(targetGroup)) continue;

#if UNITY_2023_1_OR_NEWER
				var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
				var defineSymbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
            	var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#endif
				contains = defineSymbols.Contains(symbol);
				if (contains)
				{
					break;
				}
			}

			return contains;
		}
    }
}