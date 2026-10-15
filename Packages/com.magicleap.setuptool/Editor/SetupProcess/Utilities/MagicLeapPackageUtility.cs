
#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ThirdParty.SimpleJson;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
#if MAGICLEAP
using UnityEditor.XR.Management;

#if UNITY_MAGICLEAP
using UnityEngine.XR.MagicLeap;
#endif
using UnityEngine.XR.Management;


#endif

#endregion

namespace MagicLeap.SetupTool.Editor.Utilities
{
	/// <summary>
	/// Script responsible for giving access to the sdk calls using reflections.
	/// </summary>
	public static class MagicLeapPackageUtility
	{


		private const string MAGIC_LEAP_DEFINES_SYMBOL = "MAGICLEAP";
		private const string MAGIC_LEAP_PACKAGE_ID = "com.magicleap.unitysdk";                                                   // Used to check if the build platform is installed
		private const string MAGIC_LEAP_LOADER_ID = "MagicLeapLoader";                                                           // Used to test if the loader is installed and active




		/// <summary>
		/// Refreshes the BuildTargetGroup XR Loader
		/// </summary>
		/// <param name="buildTargetGroup"> </param>
		private static void UpdateLoader(BuildTargetGroup buildTargetGroup)
		{
#if MAGICLEAP && UNITY_ANDROID

		
				if (_currentSettings == null)
				{
					Debug.LogError(XR_CANNOT_BE_FOUND);
					return;
				}
				var settings = _currentSettings.SettingsForBuildTarget(buildTargetGroup);

				if (settings == null)
				{
					settings = ScriptableObject.CreateInstance<XRGeneralSettings>();
					_currentSettings.SetSettingsForBuildTarget(buildTargetGroup, settings);
					settings.name = $"{buildTargetGroup.ToString()} Settings";
					AssetDatabase.AddObjectToAsset(settings, AssetDatabase.GetAssetOrScenePath(_currentSettings));
				}

				var serializedSettingsObject = new SerializedObject(settings);
				serializedSettingsObject.Update();
				AssetDatabase.Refresh();

				var loaderProp = serializedSettingsObject.FindProperty("m_LoaderManagerInstance");
				if (loaderProp == null)
				{
					Debug.LogError(LOADER_PROP_CANNOT_BE_FOUND);
					return;
				}
				if (loaderProp.objectReferenceValue == null)
				{
					var xrManagerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
					xrManagerSettings.name = $"{buildTargetGroup.ToString()} Providers";
					AssetDatabase.AddObjectToAsset(xrManagerSettings,AssetDatabase.GetAssetOrScenePath(_currentSettings));
					loaderProp.objectReferenceValue = xrManagerSettings;
					serializedSettingsObject.ApplyModifiedProperties();
					var serializedManagerSettingsObject = new SerializedObject(xrManagerSettings);
					xrManagerSettings.InitializeLoaderSync();
					serializedManagerSettingsObject.ApplyModifiedProperties();
					serializedManagerSettingsObject.Update();
					AssetDatabase.Refresh();
				}



				serializedSettingsObject.ApplyModifiedProperties();
				serializedSettingsObject.Update();
				UnityProjectSettingsUtility.OpenXRManagementWindow();
				EditorApplication.delayCall += () =>
												{
													var obj = loaderProp.objectReferenceValue;

													if (obj != null)
													{
														loaderProp.objectReferenceValue = obj;

														var e = UnityEditor.Editor.CreateEditor(obj);


														if (e == null)
														{
															Debug.LogError(ERROR_FAILED_TO_CREATE_WINDOW);
														}
														else
														{
															InternalEditorUtility.RepaintAllViews();
															AssetDatabase.Refresh();
															e.serializedObject.Update();
															try {
																var updateBuild = e.GetType().GetProperty("BuildTarget", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
																updateBuild.SetValue(e, (object)buildTargetGroup, null);
															}
															catch (Exception exception)
															{
																Debug.LogException(exception);
															}
											

														}
													}
													else if (obj == null)
													{
														settings.AssignedSettings = null;
														loaderProp.objectReferenceValue = null;
													}
												};


#endif
		}



		/// <summary>
		/// Checks if Magic Leap XR is enabled
		/// </summary>
		/// <returns> </returns>
		public static bool IsMagicLeapXREnabled()
		{
#if MAGICLEAP && UNITY_ANDROID
			EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,out XRGeneralSettingsPerBuildTarget androidBuildSetting);
			var hasMagicLeapLoader = false;
			if (androidBuildSetting == null)
			{
				return false;
			}


			if (androidBuildSetting != null)
			{
				var androidSettings = androidBuildSetting.SettingsForBuildTarget(BuildTargetGroup.Android);
				if (androidSettings != null && androidSettings.Manager != null)
				{
					hasMagicLeapLoader = androidSettings.Manager.activeLoaders.Any(e =>
																			{
																				var fullName = e.GetType().FullName;
																				return !string.IsNullOrEmpty(fullName) && fullName.Contains(MAGIC_LEAP_LOADER_ID);
																			});
				}
			}
			return hasMagicLeapLoader;
#else
			return false;
#endif

		}



		

	



	#region LOG MESSAGES

		private const string ERROR_FAILED_TO_CREATE_WINDOW = "Failed to create a view for XR Manager Settings Instance";
		private const string PROBLEM_FINDING_ML_PERMISSIONS = "Problem finding Magic Leap Permissions at [{0}]";
		private const string XR_CANNOT_BE_FOUND = "Current XR Settings Cannot be found";
		private const string LOADER_PROP_CANNOT_BE_FOUND = "Loader Prop [m_LoaderManagerInstance] Cannot be found";
		private const string SETTINGS_NOT_FOUND = "Settings not Found";

	#endregion

#if MAGICLEAP && UNITY_ANDROID


		private static readonly Type _cachedXRSettingsManagerType =
			Type.GetType("UnityEditor.XR.Management.XRSettingsManager,Unity.XR.Management.Editor");

		private static readonly PropertyInfo _cachedXRSettingsProperty =
			_cachedXRSettingsManagerType?.GetProperty("currentSettings",
													BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

		private static readonly MethodInfo _cachedCreateXRSettingsMethod =
			_cachedXRSettingsManagerType?.GetMethod("Create",
													BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

		private static readonly MethodInfo _cachedCreateAllChildSettingsProvidersMethod =
			_cachedXRSettingsManagerType?.GetMethod("CreateAllChildSettingsProviders",
													BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

		private static XRGeneralSettingsPerBuildTarget _currentSettings
		{
			get
			{
				var settings = (XRGeneralSettingsPerBuildTarget)_cachedXRSettingsProperty?.GetValue(null);

				return settings;
			}
		}
#endif

	
	

	}
}
