#region

using System;
using System.IO;
using System.Threading.Tasks;
using MagicLeap.SetupTool.Editor.Interfaces;
using MagicLeap.SetupTool.Editor.Utilities;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

#endregion

namespace MagicLeap.SetupTool.Editor.Setup
{
    /// <summary>
    /// Imports the Magic Leap SDK
    /// </summary>
    public class ImportMagicLeapSdkStep : ISetupStep
    {
        //Localization
        private const string IMPORT_MAGIC_LEAP_SDK = "Import the Magic Leap SDK";
        private const string IMPORT_MAGIC_LEAP_SDK_BUTTON = "Import package";
        private const string CHANGE_MAGIC_LEAP_SDK_BUTTON = "Change";
        private const string UPDATE_MAGIC_LEAP_SDK_BUTTON = "Update";
        private const string CONDITION_MET_LABEL = "Done";
        private const string UPDATE_PACKAGE_TOOLTIP = "Current Version: [v{0}]. Update to [{1}]";
        private const string CURRENT_PACKAGE_VERSION_TOOLTIP = "Current Version: v{0}";
        private const string IMPORTING_PACKAGE_PROGRESS_HEADER = "Importing Package";
        private const string DELETING_PACKAGE_PROGRESS_HEADER = "Deleting Package";
        private const string IMPORTING_PACKAGE_PROGRESS_BODY = "Importing: [{0}]";
        private const string CURRENT_VERSION_HINT_FORMAT = "Current Version [{0}]";
        private const string PACKAGE_NOT_INSTALLED_TOOLTIP = "Not installed";
        private const string DELETING_EMBEDDED_PACKAGE_DIALOG_HEADER = "Update Magic Leap SDK package";
        private const string DELETING_EMBEDDED_PACKAGE_DIALOG_BODY = "This will delete your embedded package. This action cannot be undone";
        private const string DELETING_EMBEDDED_PACKAGE_DIALOG_OK = "Continue";
        private const string DELETING_EMBEDDED_PACKAGE_DIALOG_CANCEL = "Cancel";
        private const string SELECT_PACKAGE_DIALOG_HEADER = "Please select Unity Package";
        private const string SELECT_PACKAGE_DIALOG_BODY = "Please select the unity package that you would like to import into your project.";
        private const string SELECT_PACKAGE_DIALOG_OK = "Continue";
        private const string SELECT_PACKAGE_DIALOG_CANCEL = "Cancel";
        private const string SDK_PACKAGE_FILE_BROWSER_TITLE = "Select the Unity MagicLeap SDK package"; //Title text of SDK path browser
        private const string MAGIC_LEAP_PACKAGE_ID = "com.magicleap.unitysdk"; // Used to check if the build platform is installed
        private const string REGISTRY_PACKAGE_OPTION_TITLE = "Add Magic Leap Registry";
        private const string REGISTRY_PACKAGE_OPTION_BODY = "Would you like to install remote version of the Magic Leap SDK via Magic Leap's Registry?";
        private const string REGISTRY_PACKAGE_OPTION_OK = "Use Magic Leap Registry";
        private const string REGISTRY_PACKAGE_OPTION_CANCEL = "Use Local Copy";
        
        public static bool HasMagicLeapSdkInPackageManager;
        private static int _busyCounter;
        private static bool _checkingForPackage;
        public static bool Running;
        /// <inheritdoc />
        public Action OnExecuteFinished { get; set; }
        public bool Block => true;
        private static int BusyCounter
        {
            get => _busyCounter;
            set => _busyCounter = Mathf.Clamp(value, 0, 100);
        }
 
        /// <inheritdoc />
        public bool Busy => BusyCounter > 0 ||  Running || AssetDatabase.IsAssetImportWorkerProcess() ||EditorApplication.isUpdating ||  EditorApplication.isCompiling;
        
        /// <inheritdoc />
        public bool Required => true;
        
        private bool _subscribedToEditorChangeEvent;
        /// <inheritdoc />
        public bool IsComplete => HasMagicLeapSdkInPackageManager;
        private static string _sdkPackageVersion;
        private static bool _packageNotInstalled;
        private static bool _embedded;
        private static bool _isCurrent;
        private static bool _installedFromRegistry;
        private static string _currentVersion;
        private static bool _checkingPackage;
        private static bool _dontTryImportAgain;
        public bool CanExecute => EnableGUI();
        
        private bool _loading
        {
            get
            {
                return AssetDatabase.IsAssetImportWorkerProcess() ||
                       EditorApplication.isUpdating ||
                       EditorApplication.isCompiling || Busy;
            }
        }
        /// <inheritdoc />
        public void Refresh()
        {

            CheckUnityMagicLeapPackage();
            CheckForMagicLeapSdkPackage(CheckVersion);
   
          
        }
#if USE_MLSDK && UNITY_MAGICLEAP
        [MenuItem("Magic Leap/Upgrade To OpenXR")]
#endif
        public static async void UpgradeToOpenXR()
        {
            BusyCounter++;
            var success = await PackageUtility.RemovePackageAsync("com.unity.xr.magicleap");
            if (success)
            {
                Debug.Log("Removed com.unity.xr.magicleap package");
                DefineSymbolUtility.RemoveDefineSymbol("USE_MLSDK");
                BusyCounter++;
                await EditorHelpers.WaitUntilNotBusy();
                DefineSymbolUtility.AddDefineSymbol("USE_ML_OPENXR");
                AssetDatabase.SaveAssets();
                AssetDatabase.RefreshSettings();
                AssetDatabase.Refresh(ImportAssetOptions.Default);
                BusyCounter--;
            }
            else
            {
                Debug.LogError("Failed to remove package.");
            }
            BusyCounter--;
         
        }


    

        void CheckUnityMagicLeapPackage()
        {
            if (_loading || _dontTryImportAgain) return;
              if (!DefineSymbolUtility.ContainsDefineSymbolInAllBuildTargets("USE_MLSDK"))
              {
#if UNITY_MAGICLEAP
                      UpgradeToOpenXR();
#else
                  DefineSymbolUtility.AddDefineSymbol("USE_ML_OPENXR");
#endif
              }
              
              
        }


        

        public async void CheckVersion()
        {
            try
            {
                var getPackageInfoResult = await PackageUtility.GetPackageInfoAsync(MAGIC_LEAP_PACKAGE_ID);

                if (getPackageInfoResult == null)
                {

                    _packageNotInstalled = true;
                    _sdkPackageVersion = PACKAGE_NOT_INSTALLED_TOOLTIP;
                    return;
                }

                _currentVersion = getPackageInfoResult.version;
                _packageNotInstalled = false;

                _embedded = getPackageInfoResult.source == PackageSource.Embedded;
                _installedFromRegistry = getPackageInfoResult.source == PackageSource.Registry;
                if (_installedFromRegistry)
                {
                    var versionComparer = new MagicLeapSdkVersionComparer();
                    var latestVersion = getPackageInfoResult.versions.latest;
                    var isCurrentVersion =
                        versionComparer.Compare(getPackageInfoResult.versions.latest, getPackageInfoResult.version) <=
                        0;
                    _isCurrent = isCurrentVersion;
                    if ((!isCurrentVersion))
                    {
                        _sdkPackageVersion = string.Format(UPDATE_PACKAGE_TOOLTIP, getPackageInfoResult.version,
                            latestVersion);
                        return;
                    }
                }
                else
                {
                    var latestSDKPath = PathHelper.GetLatestUnityPackagePath();
                    var directoryInfo = new DirectoryInfo(latestSDKPath).Parent;

                    if (directoryInfo != null)
                    {
                        var versionComparer = new MagicLeapSdkVersionComparer();
                        var isCurrentVersion = versionComparer.Compare(directoryInfo.Name, getPackageInfoResult.version) <= 0;
                        _isCurrent = isCurrentVersion;
                        if ((!isCurrentVersion))
                        {
                            _sdkPackageVersion = string.Format(UPDATE_PACKAGE_TOOLTIP, getPackageInfoResult.version,
                                directoryInfo.Name);
                            return;
                        }
                    }
                }

                _sdkPackageVersion = string.Format(CURRENT_PACKAGE_VERSION_TOOLTIP, getPackageInfoResult.version);
                
            }// ReSharper disable once EmptyGeneralCatchClause
#pragma warning disable CS0168
            catch (Exception e)
#pragma warning restore CS0168
            {

#if ML_SETUP_DEBUG
                Debug.LogError($"{this.GetType().Name}: {e}");
#endif
            }

        }

        private bool EnableGUI()
        {
            var correctBuildTarget = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
            return correctBuildTarget;
        }
        /// <inheritdoc />
        public bool Draw()
        {
            GUI.enabled = EnableGUI();


            if (_packageNotInstalled)
            {
                if (CustomGuiContent.CustomButtons.DrawConditionButton(new GUIContent(IMPORT_MAGIC_LEAP_SDK),
                                                                       HasMagicLeapSdkInPackageManager, new GUIContent(CONDITION_MET_LABEL, _sdkPackageVersion),
                                                                       new GUIContent(IMPORT_MAGIC_LEAP_SDK_BUTTON, _sdkPackageVersion), Styles.FixButtonStyle, _installedFromRegistry))
                {

                    Execute();
                    return true;
                }
            }
            else
            {
             
                if (_installedFromRegistry)
                {
                    if (CustomGuiContent.CustomButtons.DrawConditionButton(new GUIContent(IMPORT_MAGIC_LEAP_SDK),
                            _isCurrent, new GUIContent(CONDITION_MET_LABEL, _sdkPackageVersion),
                            new GUIContent(UPDATE_MAGIC_LEAP_SDK_BUTTON, _sdkPackageVersion), Styles.FixButtonStyle, true,null, Color.green,Color.green))
                    {
                
                        DeleteAndExecute();
                        return true;
                    }
                }
                else
                {
                    if (CustomGuiContent.CustomButtons.DrawButton(new GUIContent(IMPORT_MAGIC_LEAP_SDK), new GUIContent(CHANGE_MAGIC_LEAP_SDK_BUTTON, string.Format(CURRENT_VERSION_HINT_FORMAT, _currentVersion)), Styles.FixButtonStyle))
                    {

                        DeleteAndExecute();
                        return true;
                    }
                }
            }

            return false;
        }


        public async void DeleteAndExecute()
        {

        
            var useRegistry = EditorUtility.DisplayDialog(REGISTRY_PACKAGE_OPTION_TITLE, REGISTRY_PACKAGE_OPTION_BODY,
                REGISTRY_PACKAGE_OPTION_OK, REGISTRY_PACKAGE_OPTION_CANCEL);
            Client.Resolve();
            if (_installedFromRegistry && useRegistry)
            {
                UpdateRegistryPackage();
            }
            else if (_installedFromRegistry && !useRegistry)
            {
                Running = true;
                BusyCounter++;


                EditorUtility.DisplayProgressBar(DELETING_PACKAGE_PROGRESS_HEADER, DELETING_PACKAGE_PROGRESS_HEADER, .3f);
#if ML_SETUP_DEBUG
                Debug.Log($"{this.GetType().Name} Removing package...");;
#endif
                var removePackageResult = await PackageUtility.RemovePackageAsync(MAGIC_LEAP_PACKAGE_ID);
#if ML_SETUP_DEBUG
                Debug.Log($"{this.GetType().Name} Removed package: {removePackageResult}");;
#endif

                EditorUtility.ClearProgressBar();
                Refresh();
                if (removePackageResult)
                {
                    OnPackageDelete(false);
                }
                else
                {
                    BusyCounter--;     
                    Running = false;
                }
              
            }
            else if (_embedded)
            {
                Running = true;
                BusyCounter++;
                var deletePackage = EditorUtility.DisplayDialog(DELETING_EMBEDDED_PACKAGE_DIALOG_HEADER, DELETING_EMBEDDED_PACKAGE_DIALOG_BODY, DELETING_EMBEDDED_PACKAGE_DIALOG_OK, DELETING_EMBEDDED_PACKAGE_DIALOG_CANCEL);
                if (!deletePackage)
                {
                    EditorUtility.ClearProgressBar();
                    Running = false;
                    BusyCounter--;
                    return;
                }

              
                EditorUtility.DisplayProgressBar(DELETING_PACKAGE_PROGRESS_HEADER, DELETING_PACKAGE_PROGRESS_HEADER, .3f);

             
                var pathToPackagesFolder = PathHelper.GetPackageInProject();
                if (!string.IsNullOrWhiteSpace(pathToPackagesFolder))
                {
                    FileUtil.DeleteFileOrDirectory(pathToPackagesFolder);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    Client.Resolve();
                }
#if ML_SETUP_DEBUG
                Debug.Log($"{this.GetType().Name} Waiting till editor is not busy...");;
#endif
                await EditorHelpers.WaitUntilNotBusy();
#if ML_SETUP_DEBUG
                Debug.Log($"{this.GetType().Name} Finished waiting. Start Refresh");;
#endif
                Refresh();
                OnPackageDelete(true);
                Running = false;
                EditorUtility.ClearProgressBar();
                BusyCounter--;
               
            }
            else
            {
               
                var pathToPackageTarball = PathHelper.GetPackageInProject();

                if (!string.IsNullOrWhiteSpace(pathToPackageTarball))
                {
                    FileUtil.DeleteFileOrDirectory(pathToPackageTarball);
#if ML_SETUP_DEBUG
                    Debug.Log($"{this.GetType().Name} DeleteFileOrDirectory: {pathToPackageTarball}");;
#endif
                }
             
                
                Running = true;
                BusyCounter++;
                EditorUtility.DisplayProgressBar(DELETING_PACKAGE_PROGRESS_HEADER, DELETING_PACKAGE_PROGRESS_HEADER, .3f);
#if ML_SETUP_DEBUG
                Debug.Log($"{this.GetType().Name} Removing package...");;
#endif
                var removePackageResult = await PackageUtility.RemovePackageAsync(MAGIC_LEAP_PACKAGE_ID);
#if ML_SETUP_DEBUG
                Debug.Log($"{this.GetType().Name} Removed package: {removePackageResult}");;
#endif
                if (removePackageResult)
                {
                    OnPackageDelete(useRegistry);
                }
                else
                {
                    EditorUtility.ClearProgressBar();
                    BusyCounter--;
                    Running = false;
                }
             
            }
        }

        void OnPackageDelete(bool useRegistry)
        {
            Running = false;
            BusyCounter--;
            EditorUtility.ClearProgressBar();
#if ML_SETUP_DEBUG
            Debug.Log($"{this.GetType().Name} OnPackageDelete -useRegistry: {useRegistry}");;
#endif
        
      

            if (useRegistry)
            {
                AddRegistryAndImport();
            }
            else
            {
                AddCopyPastePackageRefresh();
            }

          
  
        }
        
        

        /// <inheritdoc />
        public void Execute()
        {
            if (IsComplete || Busy)
            {
                if(IsComplete)
                    Debug.LogWarning($"Cannot execute step because the step is {(Busy?"busy":"complete")}");
                return;
            }
            



            BusyCounter++;
            var useRegistry = EditorUtility.DisplayDialog(REGISTRY_PACKAGE_OPTION_TITLE, REGISTRY_PACKAGE_OPTION_BODY,
                                                          REGISTRY_PACKAGE_OPTION_OK, REGISTRY_PACKAGE_OPTION_CANCEL);
                                                          
            if (useRegistry)
            { 
               
                AddRegistryAndImport();
            }
            else
            {
                var selectPackage = EditorUtility.DisplayDialog(SELECT_PACKAGE_DIALOG_HEADER, SELECT_PACKAGE_DIALOG_BODY, SELECT_PACKAGE_DIALOG_OK, SELECT_PACKAGE_DIALOG_CANCEL);
                if (!selectPackage)
                {
                    BusyCounter = 0;
                    Running = false;
                    return;
                }
                AddCopyPastePackageRefresh();

            }

            BusyCounter--;
        
        }

        private async void UpdateRegistryPackage()
        {
            Running = true;
            BusyCounter++;
#if ML_SETUP_DEBUG
            Debug.Log($"{this.GetType().Name} Adding Package...");;
#endif

            var shouldRunUpdater = (SemanticVersion.Parse(_currentVersion).CompareTo(SemanticVersion.Parse("2.0.0")) < 0);

            var success = await PackageUtility.AddPackageAsync("com.magicleap.unitysdk");
            Running = true;
            
#if ML_SETUP_DEBUG
            Debug.Log($"{this.GetType().Name} Added Package: {success}");;
#endif
            if (!success)
            {
                Debug.LogError("Failed to import com.magicleap.unitysdk.");
            }
            if (shouldRunUpdater)
            {
                NamespaceUpdater.PromptAndUpdateNamespaces();
            }
            BusyCounter--;
            await EditorHelpers.WaitUntilNotBusy();
            Running = false;

        }

        private void ImportPackageFromRegistryV2()
        {
           Running = true;
        

            Debug.Log("Installing Package...");
            MagicLeapRegistryPackageImporter.InstallSdkPackage((packageSuccess) =>
            {
                Debug.Log($"{this.GetType().Name} Added Package: {packageSuccess}");
                if (packageSuccess)
                {
                    EditorHelpers.CallWhenNotBusyAndAfterDelay(() =>
                    {
                        Running = true;
                        UnityProjectSettingsUtility.ForceCloseProjectSettings();
                        SettingsService.RepaintAllSettingsWindow();
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        Client.Resolve();
                        EditorHelpers.CallWhenNotBusy(CheckForPackage);
#if ML_SETUP_DEBUG
                    Debug.Log($"{this.GetType().Name} finished.");
#endif
                    },2);
                

                }
                else
                {
                    Debug.LogError("Failed to import com.magicleap.unitysdk.");
                    EditorHelpers.CallWhenNotBusy(CheckForPackage);
                }
            
      
            });
          



            
        }

        private void CheckForPackage()
        {
            Running = true;
            _checkingForPackage = false;
            CheckForMagicLeapSdkPackage(() =>
            {
                EditorHelpers.CallWhenNotBusy(() =>
                {
                    BusyCounter--;
                    OnExecuteFinished?.Invoke();
                    Running = false;
                    Debug.Log("Finished!");
                });
           
            });
        }

        private void AddRegistryAndImport()
        {
        
#if ML_SETUP_DEBUG
            Debug.Log($"{this.GetType().Name} Adding Registry And Importing Package...");
#endif
            Running = true;
 
            MagicLeapRegistryPackageImporter.AddRegistry(() =>
            {
                Debug.Log("Added Magic Leap Registry");
                Running = true;
                EditorHelpers.CallWhenNotBusy(()=>
                {
                    Running = true;
                    EditorHelpers.CallWhenNotBusyAndAfterDelay(() =>
                    {
                        Running = true;
                        SettingsService.RepaintAllSettingsWindow();
                        EditorApplication.RepaintProjectWindow();
                        Debug.Log("Request Package Install");
                        EditorHelpers.CallWhenNotBusyAndAfterDelay(ImportPackageFromRegistryV2,2);
                    },15);
                    
                });
            });

        }
        /// <summary>
        /// Updates the variables based on if the Magic Leap SDK are installed
        /// </summary>
        private void CheckForMagicLeapSdkPackage(Action onFinished = null)
        {
            if (!_checkingForPackage)
            {
                _checkingForPackage = true;
                PackageUtility.HasPackageInstalled(MAGIC_LEAP_PACKAGE_ID, OnFinishedCheck, true);
 
           
            }

            void OnFinishedCheck(bool success, bool hasPackage)
            {
                HasMagicLeapSdkInPackageManager = hasPackage;
                onFinished?.Invoke();
                _checkingForPackage = false;
                Running = false;
            }
        }

        private string GetPackageTgz()
        {
            var path = EditorUtility.OpenFilePanel(SDK_PACKAGE_FILE_BROWSER_TITLE, PathHelper.GetPackageDirectory(), "tgz");
            return path;
        }

        private void AddCopyPastePackageRefresh(string packagePath = null)
        {

            if (string.IsNullOrWhiteSpace(packagePath))
            {
                packagePath = GetPackageTgz();
            }
       
            if (string.IsNullOrWhiteSpace(packagePath))
            {
                ApplyAllRunner.Stop();
                return;
            }
      
            Running = true;
            BusyCounter++;
            EditorUtility.DisplayProgressBar(IMPORTING_PACKAGE_PROGRESS_HEADER, string.Format(IMPORTING_PACKAGE_PROGRESS_BODY, packagePath), .3f);
            var packageName = Path.GetFileName(packagePath);
            var pathToPackagesFolder = Path.GetFullPath(Application.dataPath+ "/../Packages/"+ packageName);
#if ML_SETUP_DEBUG
            Debug.Log($"{this.GetType().Name} package name: {packageName}. Path: {pathToPackagesFolder}");
#endif
            if(!File.Exists(pathToPackagesFolder))
            {
                 FileUtil.CopyFileOrDirectory(packagePath, pathToPackagesFolder);
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Refresh();
            Running = false;
            BusyCounter--;
            AddPackageManagerAndRefresh(packageName);
        }
        /// <summary>
        /// Adds the Magic Leap SDK and refreshes setup variables
        /// </summary>
        private async void AddPackageManagerAndRefresh(string packageName)
        {
            Running = true;
            BusyCounter++;
            var startImportTime = EditorApplication.timeSinceStartup;
#if ML_SETUP_DEBUG
            Debug.Log($"{this.GetType().Name} waiting for package to install.");
#endif
            Running = true;
            var addPackageResult = await PackageUtility.AddPackageAsync("file:"+packageName);

#if ML_SETUP_DEBUG
            Debug.Log($"{this.GetType().Name} package installed: {addPackageResult}");
#endif
            if (addPackageResult)
            {
#if ML_SETUP_DEBUG
                Debug.Log($"{this.GetType().Name} waiting for editor to not be busy");
#endif
                EditorUtility.DisplayProgressBar(IMPORTING_PACKAGE_PROGRESS_HEADER, string.Format(IMPORTING_PACKAGE_PROGRESS_BODY, packageName), .4f);
                Running = true;
                _checkingForPackage = false;
                CheckForMagicLeapSdkPackage(() =>
                {
 
                    BusyCounter--;
                    OnExecuteFinished?.Invoke();
                    Running = false;
                });
            }
          
#if ML_SETUP_DEBUG
                Debug.Log($"{this.GetType().Name} finished.");
#endif
            
           
            
        }
        
        /// <inheritdoc cref="ISetupStep.ToString"/>
        public override string ToString()
        {
            var info =$"Step: {this.GetType().Name}, CanExecute: {CanExecute}, Busy: {Busy}, IsComplete: {IsComplete}";
            
            if (!EnableGUI())
            {
                var correctBuildTarget = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;
                info += "\nDisabling GUI: ";
                if (!correctBuildTarget)
                {
                    info += "[not the correct build target]";
                }
            }
            
            info += $"\nMore Info: BusyCounter: {BusyCounter}, HasMagicLeapSdkInPackageManager: {HasMagicLeapSdkInPackageManager}," +
                    $" _embedded: {_checkingPackage}, CheckingPackage: {_checkingPackage}, CurrentVersion:{_currentVersion}, IsCurrent: {_isCurrent},"
            + $" DontTryImportAgain: {_dontTryImportAgain}, PackageNotInstalled: {_packageNotInstalled}, SdkPackageVersion: {_sdkPackageVersion},"
            +$" _installedFromRegistry: {_installedFromRegistry}, _subscribedToEditorChangeEvent: {_subscribedToEditorChangeEvent}";

            return info;
        }
    }
}