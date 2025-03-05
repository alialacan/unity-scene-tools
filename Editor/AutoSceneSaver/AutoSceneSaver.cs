#region

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#endregion

namespace UnitySceneTools.unity_scene_tools.Editor.AutoSceneSaver
{
    public class AutoSceneSaver : EditorWindow
    {
        private bool enableAutoSave = false;
        private int saveInterval = 10; // Default interval in minutes
        private bool enableBackup = true;
        private bool saveAllScenes = false;
        private bool enableLogs = true;
        private string backupFolder = "AutoSaveBackups";
        private List<string> autoSaveHistory = new List<string>();
        private const int maxHistory = 5;
        private double lastSaveTime;

        [MenuItem("Unity Scene Tools/Auto Scene Saver")]
        public static void ShowWindow()
        {
            GetWindow<AutoSceneSaver>("Auto Scene Saver");
        }

        private void OnGUI()
        {
            GUILayout.Label("Auto Scene Saver Settings", EditorStyles.boldLabel);

            enableAutoSave = EditorGUILayout.Toggle("Enable Auto Save", enableAutoSave);
            saveInterval = EditorGUILayout.IntPopup("Save Interval (mins)", saveInterval,
                new string[] { "1", "3", "5", "10", "30" }, new int[] { 1, 3, 5, 10, 30 });
            enableBackup = EditorGUILayout.Toggle("Enable Backup", enableBackup);
            saveAllScenes = EditorGUILayout.Toggle("Save All Open Scenes", saveAllScenes);
            enableLogs = EditorGUILayout.Toggle("Enable Logs", enableLogs);

            EditorGUILayout.Space();
            GUILayout.Label("Backup Folder", EditorStyles.label);
            backupFolder = EditorGUILayout.TextField(backupFolder);

            if (GUILayout.Button("Save Settings"))
            {
                SavePreferences();
            }
        }

        private void OnEnable()
        {
            LoadPreferences();
            lastSaveTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Update;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Update;
        }

        private void Update()
        {
            if (!enableAutoSave) return;
            if (EditorApplication.timeSinceStartup - lastSaveTime >= saveInterval * 60)
            {
                PerformAutoSave();
                lastSaveTime = EditorApplication.timeSinceStartup;
            }
        }

        private void PerformAutoSave()
        {
            try
            {
                Scene activeScene = EditorSceneManager.GetActiveScene();

                // Check if the scene has unsaved changes before saving
                if (!activeScene.isDirty)
                {
                    if (enableLogs)
                        Debug.Log("[Auto Scene Saver] No changes detected. Skipping auto-save.");
                    return;
                }

                if (enableBackup)
                {
                    BackupScene();
                }

                if (saveAllScenes)
                {
                    EditorSceneManager.SaveOpenScenes();
                }
                else
                {
                    EditorSceneManager.SaveScene(activeScene);
                }

                autoSaveHistory.Add(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                if (autoSaveHistory.Count > maxHistory)
                    autoSaveHistory.RemoveAt(0);

                if (enableLogs)
                    Debug.Log("[Auto Scene Saver] Scene Auto-Saved at " + DateTime.Now);
            }
            catch (Exception e)
            {
                Debug.LogError("[Auto Scene Saver] Auto-save failed: " + e.Message);
            }
        }


        private void BackupScene()
        {
            string path = Path.Combine(Application.dataPath, backupFolder);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string scenePath = EditorSceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(scenePath)) return;

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFile = Path.Combine(path, $"Backup_{timestamp}.unity");

            File.Copy(scenePath, backupFile, true);
            if (enableLogs)
                Debug.Log($"[Auto Scene Saver] Backup created: {backupFile}");

            autoSaveHistory.Add(backupFile);

            // Clean up old backups
            while (autoSaveHistory.Count > maxHistory)
            {
                string oldBackup = autoSaveHistory[0];
                string oldMetaFile = oldBackup + ".meta"; // Corresponding .meta file

                if (File.Exists(oldBackup))
                {
                    File.Delete(oldBackup);
                    if (enableLogs)
                        Debug.Log($"[Auto Scene Saver] Deleted old backup: {oldBackup}");
                }
                else
                {
                    if (enableLogs)
                        Debug.LogWarning(
                            $"[Auto Scene Saver] Missing backup file detected and removed from history: {oldBackup}");
                }

                // Ensure .meta file is also removed
                if (File.Exists(oldMetaFile))
                {
                    File.Delete(oldMetaFile);
                    if (enableLogs)
                        Debug.Log($"[Auto Scene Saver] Deleted meta file: {oldMetaFile}");
                }

                autoSaveHistory.RemoveAt(0);
            }
        }

        private void SavePreferences()
        {
            EditorPrefs.SetBool("AutoSaveEnabled", enableAutoSave);
            EditorPrefs.SetInt("AutoSaveInterval", saveInterval);
            EditorPrefs.SetBool("AutoSaveLogs", enableLogs);
            EditorPrefs.SetBool("AutoSaveBackup", enableBackup);
            EditorPrefs.SetBool("AutoSaveAllScenes", saveAllScenes);
            EditorPrefs.SetString("AutoSaveBackupFolder", backupFolder);
        }

        private void LoadPreferences()
        {
            enableAutoSave = EditorPrefs.GetBool("AutoSaveEnabled", false);
            saveInterval = EditorPrefs.GetInt("AutoSaveInterval", 10);
            enableLogs = EditorPrefs.GetBool("AutoSaveLogs", true);
            enableBackup = EditorPrefs.GetBool("AutoSaveBackup", true);
            saveAllScenes = EditorPrefs.GetBool("AutoSaveAllScenes", false);
            backupFolder = EditorPrefs.GetString("AutoSaveBackupFolder", "AutoSaveBackups");
        }
    }
}