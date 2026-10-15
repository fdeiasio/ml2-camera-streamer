#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

#endregion

namespace MagicLeap.SetupTool.Editor.Utilities
{
    /// <summary>
    /// Utility that controls internal Unity Project Settings
    /// </summary>
    public static class UnityProjectSettingsUtility
    {

         public enum InputSystemTypes { Old, New, Both}
        private static readonly Dictionary<BuildTarget, bool> _supportedPlatformByBuildTarget = new Dictionary<BuildTarget, bool>(); //memo to avoid requesting the same value multiple times
        private static readonly Type _playerSettingsType = Type.GetType("UnityEditor.PlayerSettings, UnityEditor.CoreModule");
        public static InputSystemTypes InputSystemType
        {
            get
            {
                bool newInputEnabled = false;
                bool oldInputEnabled = false;
                #if ENABLE_INPUT_SYSTEM
                    newInputEnabled= true;
                #endif

                #if ENABLE_LEGACY_INPUT_MANAGER
                    oldInputEnabled = true;
                #endif
                
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (newInputEnabled && oldInputEnabled) 
                    return InputSystemTypes.Both;

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                return newInputEnabled ? InputSystemTypes.New : InputSystemTypes.Old;
            }
        }


        /// <summary>
        /// Checks if the current Unity editor supports the given build platform
        /// </summary>
        /// <param name="buildTargetToTest"></param>
        /// <returns></returns>
        private static bool IsPlatformSupported(BuildTarget buildTargetToTest)
        {
            if (_supportedPlatformByBuildTarget.TryGetValue(buildTargetToTest, out var supported)) return supported;
            var buildTargetSupported = false;
            try {
                var moduleManager = Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor.CoreModule");
                var isPlatformSupportLoaded = moduleManager.GetMethod("IsPlatformSupportLoaded", BindingFlags.Static | BindingFlags.NonPublic);
                var getTargetStringFromBuildTarget = moduleManager.GetMethod("GetTargetStringFromBuildTarget",
                                                                             BindingFlags.Static | BindingFlags.NonPublic);
                buildTargetSupported = (bool)isPlatformSupportLoaded.Invoke(null,
                                                                                new object[] { (string)getTargetStringFromBuildTarget.Invoke(null, new object[] { buildTargetToTest }) });
                _supportedPlatformByBuildTarget.Add(buildTargetToTest, buildTargetSupported);
            }
            catch
            {
                Debug.LogWarningFormat(CANNOT_CALL, "UnityEditor.Modules.ModuleManager.GetTargetStringFromBuildTarget(BuildTarget)");
                Debug.LogWarningFormat(CANNOT_CALL, "UnityEditor.Modules.ModuleManager.IsPlatformSupportLoaded(TargetStringFromBuildTarget)");
            }
           


            return buildTargetSupported;
        }

        
        
       
        
        /// <summary>
        /// Returns an input handler label based on index.
        /// 0 - Input Manager (Old)<br/>
        /// 1 - Input System Package (New)<br/>
        /// 2 - Both
        /// </summary>
        /// <param name="index">index</param>
        /// <returns></returns>
        public static string IndexToInputHandler(int index)
        {
            switch (index)
            {
                case 0:
                    return "Input Manager (Old)";
                case 1:
                    return "Input System Package (New)";
                case 2:
                    return "Both";
            }

            return "Invalid";
        }
        
        
        
        /// <summary>
        /// Gets the activeInputHandler in the Player Settings.
        /// </summary>
        /// <returns>
        /// -1 - Failed (Old)<br/>
        /// 0 - Input Manager (Old)<br/>
        /// 1 - Input System Package (New)<br/>
        /// 2 - Both
        /// </returns>
    
        public static int GetActiveInputHandler()
         {
             var currentInputValue = -1;
             try
             {
                 var playerSettingsEditorType = Type.GetType("UnityEditor.PlayerSettings, UnityEditor.CoreModule");

                 var getSerializedObjectMethodInfo = playerSettingsEditorType.GetMethod("GetSerializedObject", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                 var serializedObjectPropertyInfo = playerSettingsEditorType.GetField("_serializedObject", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                 getSerializedObjectMethodInfo.Invoke(null, null);
                 var playerSettingsEditors = Resources.FindObjectsOfTypeAll(playerSettingsEditorType);

                 var serializedObject = serializedObjectPropertyInfo.GetValue(playerSettingsEditors[0]) as SerializedObject;
                 var serializedProperty = serializedObject.FindProperty("activeInputHandler");
                 currentInputValue = serializedProperty.intValue;


             }
             catch (Exception e)
             {
                 Debug.LogException(e);
             }
             return currentInputValue;
        }

        /// <summary>
        /// Sets the activeInputHandler in the Player Settings. If successful, editor requires restart for input to take effect.
        /// </summary>
        /// <param name="value">0-3<br/>
        /// 0 - Input Manager (Old)<br/>
        /// 1 - Input System Package (New)<br/>
        /// 2 - Both </param>
        /// <returns>true if successful </returns>
        public static bool SetActiveInputHandler(int value)
        {
            bool success = false;
            if (value < 0 || value > 2)
            {
                Debug.LogWarningFormat(INVALID_ACTIVE_INPUT_VALUE, value);
                return success;
            }

            try
            {
                var playerSettingsEditorType = Type.GetType("UnityEditor.PlayerSettings, UnityEditor.CoreModule");
                var getSerializedObjectMethodInfo = playerSettingsEditorType.GetMethod("GetSerializedObject", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                var serializedObjectPropertyInfo = playerSettingsEditorType.GetField("_serializedObject", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                getSerializedObjectMethodInfo.Invoke(null, null);
                var playerSettingsEditors = Resources.FindObjectsOfTypeAll(playerSettingsEditorType);
                var serializedObject = serializedObjectPropertyInfo.GetValue(playerSettingsEditors[0]) as SerializedObject;
                var serializedProperty = serializedObject.FindProperty("activeInputHandler");
                var currentInputValue = serializedProperty.intValue;
                Debug.LogFormat(SWITCHING_INPUT_FROM_TO, IndexToInputHandler(currentInputValue), IndexToInputHandler(value));
                serializedProperty.intValue = value;
                serializedObject.ApplyModifiedProperties();
                success = true;
            }
            catch (Exception e)
            {
               Debug.LogException(e);
            }

            return success;
        }


        
        /// <summary>
        /// Opens the Project Settings window to the XR Management Tab
        /// </summary>
        public static void OpenXRManagementWindow()
        {
            SettingsService.OpenProjectSettings("Project/XR Plug-in Management");
        }
        
#if SETUP_TOOL_TESTING
     [MenuItem("Tools/Force Close Project Settings")]
#endif
        public static void ForceCloseProjectSettings()
        {
            // Use reflection to find the Project Settings window type
            var projectSettingsType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SettingsWindow");
            if (projectSettingsType == null)
            {
                Debug.LogError("Could not find Project Settings window type.");
                return;
            }

            // Get the currently open Project Settings window, if any
            var projectSettingsWindow = EditorWindow.GetWindow(projectSettingsType, false, "Project Settings", false);
            if (projectSettingsWindow != null)
            {
                // Close the window
                projectSettingsWindow.Close();
                Debug.Log("Project Settings window closed.");
            }
            else
            {
                Debug.Log("Project Settings window is not open.");
            }
        }
#if SETUP_TOOL_TESTING
     [MenuItem("Tools/Refresh XR Plug-in Management Settings")]
#endif
        public static void RefreshXRSettings()
        {
            // Open the XR Plug-in Management settings
            SettingsService.OpenProjectSettings("Project/XR Plug-in Management");

            // Repaint the window to ensure UI updates
            EditorApplication.delayCall += RepaintXRSettingsWindow;
        
            Debug.Log("XR Plug-in Management settings refreshed.");
        }
#if SETUP_TOOL_TESTING
     [MenuItem("Tools/Show Build Profile Window")]
#endif
        public static void ShowBuildProfileWindowViaReflection()
        {
            // Fully qualified name of the class including namespace
            var typeName = "UnityEditor.Build.Profile.BuildProfileWindow, UnityEditor";
        
            // Get the Type
            var type = Type.GetType(typeName);
            if (type == null)
            {
                EditorApplication.ExecuteMenuItem("File/Build Profiles");
                return;
            }

            // Find the static method 'ShowBuildProfileWindow'
            var method = type.GetMethod("ShowBuildProfileWindow", BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                EditorApplication.ExecuteMenuItem("File/Build Profiles");
                return;
            }

            // Invoke the method (null for static methods)
            method.Invoke(null, null);
            EditorApplication.ExecuteMenuItem("File/Build Profiles");
        }
        private static void RepaintXRSettingsWindow()
        {
            // Get the Settings window using reflection
            var settingsWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SettingsWindow");
            if (settingsWindowType == null)
            {
                Debug.LogError("Settings window type not found.");
                return;
            }

            // Find the open Settings window
            var settingsWindow = EditorWindow.GetWindow(settingsWindowType, false, "Project Settings");
            if (settingsWindow != null)
            {
                // Force repaint to refresh UI
                settingsWindow.Repaint();
                Debug.Log("Settings window repainted.");
            }
        }
        
#if UNITY_2023_1_OR_NEWER
        /// <summary>
        /// Checks if the current compression is set to a specified value:<br/>
        /// - Unknown<br/>
        /// - ETC<br/>
        /// - ETC2<br/>
        /// - ASTC<br/>
        /// - PCRTC<br/>
        /// - DXTC<br/>
        /// - BPTC<br/>
        /// checked using reflections <a href="https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/PlayerSettings.bindings.cs">See source</a> .
        /// </summary>
        /// <param name="buildTarget"> </param>
        /// <param name="label"></param>
        /// <returns></returns>
        public static bool IsTextureCompressionSet(BuildTarget buildTarget, string label)
        {
          
            var textureCompressionFormat = GetTextureCompressionFormat(label);
            if (textureCompressionFormat == null)
            {
                Debug.LogWarningFormat(CANNOT_FIND, label);
                return false;
            }

            try
            {
                var getDefaultTextureCompressionMethodInfo = _playerSettingsType.GetMethod("GetDefaultTextureCompressionFormat", BindingFlags.Static | BindingFlags.NonPublic);
               
                var enabledStateResult = getDefaultTextureCompressionMethodInfo.Invoke(null, new object[] { buildTarget });
                return Convert.ToInt32(textureCompressionFormat) == Convert.ToInt32(enabledStateResult);
               
            }
            catch 
            {
                Debug.LogWarningFormat(CANNOT_CALL, "UnityEditor.PlayerSettings.GetDefaultTextureCompressionFormat(BuildTarget buildTarget)");
            }

            return false;
        }

#else
        
        /// <summary>
        /// Checks if the current compression is set to a specified value:<br/>
        /// - Unknown<br/>
        /// - ETC<br/>
        /// - ETC2<br/>
        /// - ASTC<br/>
        /// - PCRTC<br/>
        /// - DXTC<br/>
        /// - BPTC<br/>
        /// checked using reflections <a href="https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/PlayerSettings.bindings.cs">See source</a> .
        /// </summary>
        /// <param name="buildTargetGroup"> </param>
        /// <param name="label"></param>
        /// <returns></returns>
        public static bool IsTextureCompressionSet(BuildTargetGroup buildTargetGroup, string label)
        {
          
            var textureCompressionFormat = GetTextureCompressionFormat(label);
            if (textureCompressionFormat == null)
            {
                Debug.LogWarningFormat(CANNOT_FIND, label);
                return false;
            }

            try
            {
                var getDefaultTextureCompressionMethodInfo = _playerSettingsType.GetMethod("GetDefaultTextureCompressionFormat", BindingFlags.Static | BindingFlags.NonPublic);
               
                var enabledStateResult = getDefaultTextureCompressionMethodInfo.Invoke(null, new object[] { buildTargetGroup });
                return Convert.ToInt32(textureCompressionFormat) == Convert.ToInt32(enabledStateResult);
               
            }
            catch 
            {
                Debug.LogWarningFormat(CANNOT_CALL, "UnityEditor.PlayerSettings.GetDefaultTextureCompressionFormat(BuildTargetGroup buildTargetGroup)");
            }

            return false;
        }
#endif


        
        /// <summary>
        /// Checks if the current normal map compression is set to a specified value:<br/>
        /// - XYZ<br/>
        /// - DXT5nm<br/>
        /// checked using reflections <a href="https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/PlayerSettings.bindings.cs">See source</a> .
        /// </summary>
        /// <param name="buildTargetGroup"> </param>
        /// <param name="label"></param>
        /// <returns></returns>
        public static bool IsNormalMapCompressionSet(BuildTargetGroup buildTargetGroup, string label)
        {
          
            var normalMapCompressionFormat =GetNormalMapCompressionFormat(label);
            if (normalMapCompressionFormat == null)
            {
                Debug.LogWarningFormat(CANNOT_FIND, label);
                return false;
            }
            try
            {
                var getDefaultNormalMapCompressionMethodInfo = _playerSettingsType.GetMethod("GetNormalMapEncoding", BindingFlags.Public | BindingFlags.Static, null,  new Type[]{typeof(BuildTargetGroup)}, null);
               
                var enabledStateResult = getDefaultNormalMapCompressionMethodInfo.Invoke(null, new object[] { buildTargetGroup });
                return Convert.ToInt32(normalMapCompressionFormat) == Convert.ToInt32(enabledStateResult);
               
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat(CANNOT_CALL, $"UnityEditor.PlayerSettings.GetNormalMapEncoding(BuildTargetGroup buildTargetGroup): {e}");
            }

            return false;
        }
        
        
        /// <summary>
        /// Sets the default Normal Map Compression Format.
        /// <a href="https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/PlayerSettings.bindings.cs">See source</a> .
        /// </summary>
        /// <param name="buildTargetGroup"> </param>
        /// <param name="label"></param>
        /// <returns></returns>
        public static void SetNormalMapCompression(BuildTargetGroup buildTargetGroup, string label)
        {
       
            var normalMapCompressionFormat = GetNormalMapCompressionFormat(label);
            if (normalMapCompressionFormat == null)
            {
                Debug.LogWarning($"Could not find [{label}]");
                return;
            }
            try
            {
               var parameterTypes = new Type[] { typeof(BuildTargetGroup), typeof(NormalMapEncoding) };
                var setNormalMapCompressionMethodInfo = _playerSettingsType.GetMethod("SetNormalMapEncoding", BindingFlags.Public | BindingFlags.Static, null, parameterTypes, null);
                setNormalMapCompressionMethodInfo.Invoke(null, new object[] { buildTargetGroup, normalMapCompressionFormat});

            }
            catch (Exception e)
            {
                Debug.LogWarningFormat(CANNOT_CALL, $"UnityEditor.PlayerSettings.SetNormalMapEncoding(BuildTargetGroup, TextureCompressionFormat): {e}");
            }

   
        }
#if UNITY_2023_1_OR_NEWER
        /// <summary>
        /// Sets the Default Texture Compression Format.
        /// <a href="https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/PlayerSettings.bindings.cs">See source</a> .
        /// </summary>
        /// <param name="buildTarget"> </param>
        /// <param name="label"></param>
        /// <returns></returns>
        public static void SetTextureCompression(BuildTarget buildTarget, string label)
        {
       
            var textureCompressionFormat = GetTextureCompressionFormat(label);
            if (textureCompressionFormat == null)
            {
                Debug.LogWarning($"Could not find [{label}]");
                return;
            }
            try
            {
                var setDefaultTextureCompressionMethodInfo = _playerSettingsType.GetMethod("SetDefaultTextureCompressionFormat", BindingFlags.Static | BindingFlags.NonPublic);
                
                setDefaultTextureCompressionMethodInfo.Invoke(null, new object[] { buildTarget, textureCompressionFormat});

            }
            catch (Exception)
            {
                Debug.LogWarningFormat(CANNOT_CALL, "UnityEditor.PlayerSettings.SetDefaultTextureCompressionFormat(BuildTarget, TextureCompressionFormat)");
            }

   
        }

#else
        
        /// <summary>
        /// Sets the Default Texture Compression Format.
        /// <a href="https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/PlayerSettings.bindings.cs">See source</a> .
        /// </summary>
        /// <param name="buildTargetGroup"> </param>
        /// <param name="label"></param>
        /// <returns></returns>
        public static void SetTextureCompression(BuildTargetGroup buildTargetGroup, string label)
        {
       
            var textureCompressionFormat = GetTextureCompressionFormat(label);
            if (textureCompressionFormat == null)
            {
                Debug.LogWarning($"Could not find [{label}]");
                return;
            }
            try
            {
                var setDefaultTextureCompressionMethodInfo = _playerSettingsType.GetMethod("SetDefaultTextureCompressionFormat", BindingFlags.Static | BindingFlags.NonPublic);
                
                setDefaultTextureCompressionMethodInfo.Invoke(null, new object[] { buildTargetGroup, textureCompressionFormat});

            }
            catch (Exception)
            {
                Debug.LogWarningFormat(CANNOT_CALL, "UnityEditor.PlayerSettings.SetDefaultTextureCompressionFormat(BuildTargetGroup, TextureCompressionFormat)");
            }

   
        }
#endif


        /// <summary>
        /// Gets the object value of the Texture Compression Format Enum for reflection calls.
        /// <a href="https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/PlayerSettings.bindings.cs">See source</a> .
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        private static object GetTextureCompressionFormat(string label)
        {
            try
            {
                var textureCompressionFormatType = Type.GetType("UnityEditor.TextureCompressionFormat,UnityEditor.CoreModule");
                if (textureCompressionFormatType != null && textureCompressionFormatType.IsEnum)
                {
                    var enumNames = textureCompressionFormatType.GetEnumNames();
                    var enumValues = textureCompressionFormatType.GetEnumValues();
                    for (var i = 0; i < enumValues.Length; ++i)
                    {
                        if (enumNames[i] != label) continue;

                        var  enumValue = enumValues.GetValue(i);
                        return enumValue;
                    }
                }
                else
                {
                    Debug.LogWarningFormat(CANNOT_CALL, "UnityEditor.TextureCompressionFormat,UnityEditor.CoreModule");
                }
            }
            catch (Exception)
            {
                Debug.LogError(FAILED_TO_GET_TEXTURE_COMPRESSION);
            }

            return null;
        }
        
        
        /// <summary>
        /// Gets the object value of the Normal Map Compression Format Enum for reflection calls.
        /// <a href="https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/PlayerSettings.bindings.cs">See source</a> .
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        private static object GetNormalMapCompressionFormat(string label)
        {
            try
            {
                var textureCompressionFormatType = Type.GetType("UnityEditor.NormalMapEncoding,UnityEditor.CoreModule");
                if (textureCompressionFormatType != null && textureCompressionFormatType.IsEnum)
                {
                    var enumNames = textureCompressionFormatType.GetEnumNames();
                    var enumValues = textureCompressionFormatType.GetEnumValues();
                    for (var i = 0; i < enumValues.Length; ++i)
                    {
                        if (enumNames[i] != label) continue;

                        var  enumValue = enumValues.GetValue(i);
                        return enumValue;
                    }
                }
                else
                {
                    Debug.LogWarningFormat(CANNOT_CALL, "UnityEditor.TextureCompressionFormat,UnityEditor.CoreModule");
                }
            }
            catch (Exception)
            {
                Debug.LogError(FAILED_TO_GET_TEXTURE_COMPRESSION);
            }

            return null;
        }
        /// <summary>
        /// Toggles the Auto Graphics Api project setting
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="automatic"></param>
        public static void SetAutoGraphicsApi(BuildTarget buildTarget, bool automatic)
        {
            if (GetAutoGraphicsApi(buildTarget) != automatic)
            {
                try
                {
                    var methodInfo = _playerSettingsType.GetMethod("SetUseDefaultGraphicsAPIs",
                                                                   BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                    methodInfo.Invoke(_playerSettingsType, new object[] { buildTarget, automatic });
                }
                catch (Exception)
                {
                    Debug.LogWarningFormat(CANNOT_CALL, "UnityEditor.PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget, bool)");
                }
            }
        }

        /// <summary>
        /// Gets the current value of the Graphics API setting for the given Build Target
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <returns></returns>
        public static bool GetAutoGraphicsApi(BuildTarget buildTarget)
        {
         
            try
            {
                var isPlatformSupportLoaded = _playerSettingsType?.GetMethod("GetUseDefaultGraphicsAPIs",BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                if (isPlatformSupportLoaded != null)
                {
                   return (bool)isPlatformSupportLoaded?.Invoke(_playerSettingsType, new object[] { buildTarget });
                }
                else
                {
                    Debug.LogWarningFormat(CANNOT_CALL, "UnityEditor.PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget)");
                    return false;
                }

            }
            catch (Exception)
            {
                Debug.LogWarningFormat(CANNOT_CALL, "UnityEditor.PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget)");
            }

            return false;

        }

        /// <summary>
        /// Call to Unity internal method that clears unsaved changes from the scene
        /// </summary>
        /// <param name="scene"></param>
        private static void ClearSceneDirtiness(Scene scene)
        {
            var moduleManager = Type.GetType("UnityEditor.SceneManagement.EditorSceneManager, UnityEditor.CoreModule");
            try
            {
                var methodInfo = moduleManager?.GetMethod("ClearSceneDirtiness", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (methodInfo != null)
                {
                    methodInfo.Invoke(null, new object[] { scene });
                }
            }
            catch
            {
                Debug.LogWarningFormat(CANNOT_CALL, "UnityEditor.SceneManagement.EditorSceneManager.ClearSceneDirtiness()");
               //Failed
            }
         
        }

        /// <summary>
        /// Closes and relaunches editor with the current window and scene
        /// </summary>
        public static void RequestCloseAndRelaunchWithCurrentArguments()
        {
           var editorApplicationType = Type.GetType("UnityEditor.EditorApplication, UnityEditor.CoreModule");
            var requestCloseAndRelaunchMethodInfo = editorApplicationType?.GetMethod("RequestCloseAndRelaunchWithCurrentArguments",
                                                                                            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            if (requestCloseAndRelaunchMethodInfo != null)
            {
                requestCloseAndRelaunchMethodInfo.Invoke(null, null);
            }
            else
            {
                Debug.LogWarningFormat(CANNOT_CALL,"UnityEditor.EditorApplication.RequestCloseAndRelaunchWithCurrentArguments()");
            }
        }


        /// <summary>
        /// Checks the given build target if a graphic device type is available
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="graphicsDeviceType"></param>
        /// <returns></returns>
        public static bool HasGraphicsDeviceType(BuildTarget buildTarget, GraphicsDeviceType graphicsDeviceType)
        {
            if (IsPlatformSupported(BuildTarget.StandaloneWindows))
            {
                var graphics = PlayerSettings.GetGraphicsAPIs(buildTarget).ToList();
                return graphics.Contains(graphicsDeviceType);
            }

            return true;
        }

        /// <summary>
        /// Checks if the requested graphics device type for a given build target is at a certain index
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="graphicsDeviceType"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool HasGraphicsDeviceTypeAtIndex(BuildTarget buildTarget, GraphicsDeviceType graphicsDeviceType,
            int index)
        {
            var graphics = PlayerSettings.GetGraphicsAPIs(buildTarget).ToList();
            return graphics[index] == graphicsDeviceType;
        }

        /// <summary>
        /// Checks if the requested graphics device type for a given build target is the only one used
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="graphicsDeviceType"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool OnlyHasGraphicsDeviceType(BuildTarget buildTarget, GraphicsDeviceType graphicsDeviceType)
        {
            var graphics = PlayerSettings.GetGraphicsAPIs(buildTarget);
            return graphics[0] == graphicsDeviceType && graphics.Length==1;
        }

        /// <summary>
        /// Adds the graphics device to the desired build target at a given index.
        /// </summary>
        /// <param name="buildTarget"></param>
        /// <param name="graphicsDeviceType"></param>
        /// <param name="index"></param>
        /// <returns>returns true of the new graphics device is used by the editor</returns>
        public static bool SetGraphicsApi(BuildTarget buildTarget, GraphicsDeviceType graphicsDeviceType,
            int index = 9999)
        {
            var graphicsDeviceTypes = PlayerSettings.GetGraphicsAPIs(buildTarget).ToList();
            index = Mathf.Clamp(index, 0, graphicsDeviceTypes.Count);
            if (graphicsDeviceTypes.Contains(graphicsDeviceType))
            {
                if (index == graphicsDeviceTypes.Count)
                    //already added and we don't care about the index
                    return false;

                if (graphicsDeviceTypes[index] == graphicsDeviceType)
                    //already setup to the desired index
                    return false;

                graphicsDeviceTypes.Remove(graphicsDeviceType);
            }

            if (index == graphicsDeviceTypes.Count)
            {
                graphicsDeviceTypes.Add(graphicsDeviceType);
            }
            else
            {
                graphicsDeviceTypes.Insert(index, graphicsDeviceType);
            }

            PlayerSettings.SetGraphicsAPIs(buildTarget, graphicsDeviceTypes.ToArray());
            return index == 0 && WillEditorUseFirstGraphicsAPI(buildTarget);
        }

        public static bool UseOnlyThisGraphicsApi(BuildTarget buildTarget, GraphicsDeviceType graphicsDeviceType)
        {
            var graphicsDeviceTypes = PlayerSettings.GetGraphicsAPIs(buildTarget).ToList();
            var isFirstGraphic = (graphicsDeviceTypes[0] == graphicsDeviceType);

            PlayerSettings.SetGraphicsAPIs(buildTarget, new GraphicsDeviceType[1]{graphicsDeviceType});

       
            return !isFirstGraphic && WillEditorUseFirstGraphicsAPI(buildTarget);
        }

        /// <summary>
        /// returns true of the new graphics device is used by the editor
        /// </summary>
        /// <param name="targetPlatform"></param>
        /// <returns></returns>
        private static bool WillEditorUseFirstGraphicsAPI(BuildTarget targetPlatform)
        {
            return
                (Application.platform == RuntimePlatform.WindowsEditor &&
                 targetPlatform == BuildTarget.StandaloneWindows) ||
                (Application.platform == RuntimePlatform.LinuxEditor &&
                 targetPlatform == BuildTarget.StandaloneLinux64) || (Application.platform == RuntimePlatform.OSXEditor &&
                                                                      targetPlatform == BuildTarget.StandaloneOSX);
        }

        /// <summary>
        /// Shows the Update Graphics API window.
        /// </summary>
        /// <param name="needsReset">if the editor graphics device has changed</param>
        public static void UpdateGraphicsApi(bool needsReset)
        {
            if (needsReset)
            {
                // If we have dirty scenes we need to save or discard changes before we restart editor.
                // Otherwise user will get a dialog later on where they can click cancel and put editor in a bad device state.
                var dirtyScenes = new List<Scene>();
                for (var i = 0; i < SceneManager.sceneCount; ++i)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isDirty) dirtyScenes.Add(scene);
                }

                var restart = dirtyScenes.Count != 0
                    ? ShowSaveAndQuitGraphicsApiPrompt(dirtyScenes)
                    : ShowQuitGraphicsApiPrompt();
                if (restart) RequestCloseAndRelaunchWithCurrentArguments();
            }
        }

        private static bool ShowQuitGraphicsApiPrompt()
        {
            var dialogComplex = EditorUtility.DisplayDialog(CHANGE_EDITOR_GRAPHICS_API_TITLE,
                CHANGE_EDITOR_GRAPHICS_API_MESSAGE,
                CHANGE_EDITOR_GRAPHICS_API_OK, CHANGE_EDITOR_GRAPHICS_API_CANCEL);
            return dialogComplex;
        }
        private static bool ShowSaveAndQuitGraphicsApiPrompt(List<Scene> dirtyScenes)
        {
            var doRestart = false;
            var dialogComplex = EditorUtility.DisplayDialogComplex(CHANGE_EDITOR_GRAPHICS_API_TITLE,
                CHANGE_EDITOR_GRAPHICS_API_SAVE_MESSAGE,
                CHANGE_EDITOR_GRAPHICS_API_OK_SAVE, CHANGE_EDITOR_GRAPHICS_API_CANCEL,
                CHANGE_EDITOR_GRAPHICS_API_DONT_SAVE_CANCEL);

            switch (dialogComplex)
            {
                case 0: //Save and Restart
                    doRestart = true;
                    for (var i = 0; i < dirtyScenes.Count; ++i)
                    {
                        var saved = EditorSceneManager.SaveScene(dirtyScenes[i]);
                        if (saved == false) doRestart = false;
                    }

                    break;
                case 1: //Cancel
                    break;
                case 2: //Discard Changes and Restart
                    doRestart = true;
                    for (var i = 0; i < dirtyScenes.Count; ++i) ClearSceneDirtiness(dirtyScenes[i]);

                    break;
            }

            return doRestart;
        }

    #region LOG MESSAGES

        private const string CANNOT_CALL = "Cannot call: [{0}]";
        private const string CANNOT_FIND = "Could not find [{0}]";
        private const string FAILED_TO_GET_TEXTURE_COMPRESSION = "Failed To Get Texture Compression.";
        private const string INVALID_ACTIVE_INPUT_VALUE = "Cannot change current input handler. Value: [{0}] is invalid. Options are:\n0 - Input Manager (Old)\n1 - Input System Package (New)\n2 - Both";
        private const string SWITCHING_INPUT_FROM_TO = "Switching Current Input Handler from [{0}] to [{1}] ";
        private const string CANNOT_FIND_PROJECT_SETTINGS_WINDOW = "Cannot find window of type [UnityEditor.SettingsWindow,UnityEditor.CoreModule]";
        private const string CANNOT_FIND_SELECT_PROVIDER_IN_PROJECT_SETTINGS = "Cannot find method [SelectProviderByName] in  [UnityEditor.SettingsWindow,UnityEditor.CoreModule]";


    #endregion

    #region GUI TEXT

        private const string CHANGE_EDITOR_GRAPHICS_API_TITLE = "Changing editor graphics API";
        private const string CHANGE_EDITOR_GRAPHICS_API_MESSAGE ="You've changed the active graphics API. This requires a restart of the Editor. Do you want to save the Scene when restarting?";
        private const string CHANGE_EDITOR_GRAPHICS_API_SAVE_MESSAGE ="You've changed the active graphics API. This requires a restart of the Editor.";

        private const string CHANGE_EDITOR_GRAPHICS_API_OK = "Restart";
        private const string CHANGE_EDITOR_GRAPHICS_API_OK_SAVE = "Save and Restart";
        private const string CHANGE_EDITOR_GRAPHICS_API_DONT_SAVE_CANCEL = "Discard Changes and Restart";
        private const string CHANGE_EDITOR_GRAPHICS_API_CANCEL = "Not Now";

    #endregion
    }
}