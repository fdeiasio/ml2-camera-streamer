using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace MagicLeap.SetupTool.Editor.Utilities
{
    public static class EditorHelpers
    {
        // A set to track queued actions to prevent stacking
        private static readonly HashSet<Action> NotBusyQueuedActions = new();
        private static readonly HashSet<Action> DelayedQueuedActions = new();
        public static void CallWhenNotBusy(Action action, bool preventStacking = false)
        {
            if (preventStacking && NotBusyQueuedActions.Contains(action))
                // If preventStacking is enabled and the action is already queued, ignore this call
                return;

            if (preventStacking) NotBusyQueuedActions.Add(action);

            EditorApplication.delayCall += () =>
            {
                EditorApplication.update += UpdateEditor;

                void UpdateEditor()
                {
                    if (AssetDatabase.IsAssetImportWorkerProcess() ||
                        EditorApplication.isUpdating ||
                        EditorApplication.isCompiling)
                        return;

                    EditorApplication.update -= UpdateEditor;

                    // Remove from the queued actions set before invoking
                    if (preventStacking) NotBusyQueuedActions.Remove(action);

                    action?.Invoke();
                }
            };
        }
        public static void CallWhenNotBusyAndAfterDelay(Action action, float delay, bool preventStacking = false)
        {
           CallWhenNotBusy(() =>
           {
               CallAfterDelay(action,delay);
           },preventStacking);
        }
        public static void CallAfterDelay(Action action, float delay, bool preventStacking = false)
        {
            if (preventStacking && DelayedQueuedActions.Contains(action))
                // If preventStacking is enabled and the action is already queued, ignore this call
                return;

            if (preventStacking) DelayedQueuedActions.Add(action);
            
            EditorApplication.delayCall += () =>
            {
                var currentTime = EditorApplication.timeSinceStartup;
                EditorApplication.update += UpdateEditor;

                void UpdateEditor()
                {
                    if (EditorApplication.timeSinceStartup-currentTime>delay)
                        return;

                    EditorApplication.update -= UpdateEditor;

                    // Remove from the queued actions set before invoking
                    if (preventStacking) DelayedQueuedActions.Remove(action);

                    action?.Invoke();
                }
            };
        }
        public static async Task WaitUntilNotBusy()
        {
            var tcs = new TaskCompletionSource<bool>();
            try
            {
                EditorApplication.delayCall += () =>
                {
                    EditorApplication.update += UpdateEditor;

                    void UpdateEditor()
                    {
                        if (AssetDatabase.IsAssetImportWorkerProcess() ||
                            EditorApplication.isUpdating ||
                            EditorApplication.isCompiling)
                            return;


                        EditorApplication.update -= UpdateEditor;
                        tcs.SetResult(true);
                    }
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"Error Waiting For Not Busy Editor: {e}");
            }

            await tcs.Task;
        }
    }
}