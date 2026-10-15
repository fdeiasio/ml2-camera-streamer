using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MagicLeap.SetupTool.Editor.Utilities
{
    public static class PathHelper
    {   
        //used to set and check the sdk path [key is an internal variable set by Unity]
        private const string SDK_PACKAGE_MANAGER_PATH_RELATIVE_TO_SDK_ROOT = "../../tools/unity/{0}/com.magicleap.unitysdk.tgz"; //The path to the Package Manager folder relative to the SDK Root | {0} is the sdk version
        private const string SDK_LOCATION_PREF_KEY = "MAGICLEAP_SDK_LOCATION";
        public static string DefaultUnityPackagePath => Path.GetFullPath(Path.Combine(EditorPrefs.GetString(SDK_PATH_EDITOR_PREF_KEY), string.Format(SDK_PACKAGE_MANAGER_PATH_RELATIVE_TO_SDK_ROOT, GetSdkFolderName())));
        private const string MINIMUM_API_LEVEL_EDITOR_PREF_KEY = "MagicLeap.Permissions.MinimumAPILevelDropdownValue_{0}";       //used to set and check the api level [key is an internal variable set by Unity]
        private const string SDK_PATH_EDITOR_PREF_KEY = "MagicLeapSDKRoot";  
        public static string SdkRoot => EditorPrefs.GetString(SDK_PATH_EDITOR_PREF_KEY, null);
        public static string GetLatestUnityPackagePath()
        {


            var root = Environment.GetEnvironmentVariable("USERPROFILE") ?? Environment.GetEnvironmentVariable("HOME");


            if (!string.IsNullOrEmpty(root))
            {
                var sdkRoot = Path.Combine(root, "MagicLeap/tools/unity");
                if (string.IsNullOrEmpty(sdkRoot) || !Directory.Exists(sdkRoot))
                {
                    var prefsRoot = EditorPrefs.GetString(SDK_LOCATION_PREF_KEY, null);
                    if (!string.IsNullOrEmpty(prefsRoot))
                    {
                        sdkRoot = Path.Combine(prefsRoot, "tools/unity");
                    }
                }
				
                if (!string.IsNullOrEmpty(sdkRoot)  && Directory.Exists(sdkRoot))
                {
                    var getVersionDirectories = Directory.EnumerateDirectories(sdkRoot, "v*").ToList();

                    getVersionDirectories.RemoveAll((e) => !File.Exists(Path.Combine(e, "com.magicleap.unitysdk.tgz")));

                    getVersionDirectories.Sort(new MagicLeapSdkVersionComparer());
					
                    if (getVersionDirectories.Count == 0)
                        return null;

                    return Path.Combine(getVersionDirectories[getVersionDirectories.Count-1], "com.magicleap.unitysdk.tgz");
                }
            }


            return null;
        }
        
        public static string GetLatestSDKPath()
        {


            var root = Environment.GetEnvironmentVariable("USERPROFILE") ?? Environment.GetEnvironmentVariable("HOME");


            if (!string.IsNullOrEmpty(root))
            {
                var sdkRoot = Path.Combine(root, "MagicLeap/mlsdk/");

                if (string.IsNullOrEmpty(sdkRoot) || !Directory.Exists(sdkRoot))
                {
                    var prefsRoot = EditorPrefs.GetString(SDK_LOCATION_PREF_KEY, null);
                    if (!string.IsNullOrEmpty(prefsRoot))
                    {
                        sdkRoot = Path.Combine(prefsRoot, "mlsdk");
                    }
                }
			
				
                if (!string.IsNullOrEmpty(sdkRoot) && Directory.Exists(sdkRoot.Replace("\\","/")))
                {
                    var getVersionDirectories = Directory.EnumerateDirectories(sdkRoot, "v*").ToList();
 
                    getVersionDirectories.RemoveAll((e) => !File.Exists(Path.Combine(e, ".metadata", "sdk.manifest")));
				
                    getVersionDirectories.Sort(new MagicLeapSdkVersionComparer());

                    return getVersionDirectories.Count == 0 ? sdkRoot : getVersionDirectories[getVersionDirectories.Count - 1];
                }
            }


            return null;
        }
        
        public static string GetUnityPackageDirectory()
        {
            var root = Environment.GetEnvironmentVariable("USERPROFILE") ?? Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(root))
            {
                var sdkRoot = Path.Combine(root, "MagicLeap/tools/unity");
                if (string.IsNullOrEmpty(sdkRoot) || !Directory.Exists(sdkRoot))
                {
                    var prefsRoot = EditorPrefs.GetString(SDK_LOCATION_PREF_KEY, null);
                    if (!string.IsNullOrEmpty(prefsRoot))
                    {
                        sdkRoot = Path.Combine(prefsRoot, "tools/unity");
                        if (Directory.Exists(sdkRoot))
                        {
                            return sdkRoot.Replace("\\", "/");
                        }
                    }
                }
                else
                {
                    return sdkRoot.Replace("\\", "/");
                }
            }
            else
            {
                var prefsRoot = EditorPrefs.GetString(SDK_LOCATION_PREF_KEY, null);
                if (!string.IsNullOrEmpty(prefsRoot))
                {
                    var sdkRoot = Path.Combine(prefsRoot, "tools/unity");
                    if (Directory.Exists(sdkRoot))
                    {
                        return sdkRoot.Replace("\\", "/");
                    }
                }
            }

            return null;
        }
        
        /// <summary>
        /// Returns the SDK folder name
        /// </summary>
        /// <returns> </returns>
        public static string GetSdkFolderName()
        {
            var sdkRoot = EditorPrefs.GetString(SDK_PATH_EDITOR_PREF_KEY, null);


            if (!string.IsNullOrEmpty(sdkRoot) && Directory.Exists(sdkRoot))
            {
                return new DirectoryInfo(sdkRoot).Name;
            }

            return "0.0.0";
        }
        
        public static string GetSDKDirectory()
        {
            var root = Environment.GetEnvironmentVariable("USERPROFILE") ?? Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(root))
            {
                var sdkRoot = Path.Combine(root, "MagicLeap/mlsdk");
                if (string.IsNullOrEmpty(sdkRoot) || !Directory.Exists(sdkRoot))
                {
                    var prefsRoot = EditorPrefs.GetString(SDK_LOCATION_PREF_KEY, null);
                    if (!string.IsNullOrEmpty(prefsRoot))
                    {
                        sdkRoot = Path.Combine(prefsRoot, "mlsdk");
                        if (Directory.Exists(sdkRoot))
                        {
                            return sdkRoot.Replace("\\", "/");
                        }
                    }
                }
                else
                {
                    return sdkRoot.Replace("\\", "/");
                }
            }
            else
            {
                var prefsRoot = EditorPrefs.GetString(SDK_LOCATION_PREF_KEY, null);
                if (!string.IsNullOrEmpty(prefsRoot))
                {
                    var sdkRoot = Path.Combine(prefsRoot, "mlsdk");
                    if (Directory.Exists(sdkRoot))
                    {
                        return sdkRoot.Replace("\\", "/");
                    }
                }
            }

            return null;
        }
        
        public static string GetPackageDirectory()
        {
            string directoryToUse = Environment.GetEnvironmentVariable("USERPROFILE") ?? Environment.GetEnvironmentVariable("HOME");


            var sdkRoot = PathHelper.GetUnityPackageDirectory();
            if (!string.IsNullOrEmpty(sdkRoot))
            {
                directoryToUse = sdkRoot;
            }

            if (File.Exists(PathHelper.DefaultUnityPackagePath))
            {
                var directoryInfo = new DirectoryInfo(PathHelper.DefaultUnityPackagePath).Parent;
                if (directoryInfo != null)
                {
                    directoryToUse = directoryInfo.FullName;
                }
            }
            else
            {
                var latestUnityPackageFolder = PathHelper.GetLatestUnityPackagePath();

                if (File.Exists(latestUnityPackageFolder))
                {
                    var directoryInfo = new DirectoryInfo(latestUnityPackageFolder).Parent;
                    if (directoryInfo != null)
                    {
                        directoryToUse = directoryInfo.FullName;
                    }
             
                }
            }

            return directoryToUse;
        }

        public static string GetPackageInProject()
        {
            var packageZip = Path.GetFullPath(Application.dataPath + "/../Packages/com.magicleap.unitysdk.tgz");
            var packageFolder = Path.GetFullPath(Application.dataPath + "/../Packages/com.magicleap.unitysdk");
            if (File.Exists(packageZip))
            {
                return packageZip;
            }

            if (Directory.Exists(packageFolder))
            {
                return packageFolder;
            }

            return null;
        }
    }
}