#region

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#endregion

namespace UnitySceneTools.Editor
{
    [InitializeOnLoad]
    public static class PlaySceneDropdown
    {
        private static string[] scenePaths;
        private static string[] sceneNames;
        private static int selectedSceneIndex;
        private static string[] previousScenes; // To detect changes

        static PlaySceneDropdown()
        {
            LoadScenesFromBuildSettings();
            selectedSceneIndex = EditorPrefs.GetInt("DefaultPlaySceneIndex", 0);
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += CheckForBuildSettingsChanges; // NEW: Detect scene changes
            EditorApplication.update += AddToolbarDropdown;
        }

        private static void LoadScenesFromBuildSettings()
        {
            scenePaths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            sceneNames = scenePaths.Select(Path.GetFileNameWithoutExtension).ToArray();
            previousScenes = scenePaths; // Store current scenes
        }

        private static void CheckForBuildSettingsChanges()
        {
            string[] currentScenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

            if (!currentScenes.SequenceEqual(previousScenes)) // Detect changes
            {
                LoadScenesFromBuildSettings(); // Refresh scene list
                SceneSelectorToolbar.RefreshDropdown(); // Update dropdown UI
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && scenePaths.Length > 0)
            {
                string selectedScenePath = scenePaths[selectedSceneIndex];

                if (EditorSceneManager.GetActiveScene().path != selectedScenePath)
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (EditorApplication.isPlaying)
                        {
                            SceneManager.LoadScene(Path.GetFileNameWithoutExtension(selectedScenePath));
                        }
                    };
                }
            }
        }

        private static void AddToolbarDropdown()
        {
            EditorApplication.update -= AddToolbarDropdown; // Run once only
            SceneSelectorToolbar.Init();
        }
    }

    public class SceneSelectorToolbar : EditorWindow
    {
        private static int selectedSceneIndex;
        private static string[] sceneNames;
        private static string[] scenePaths;

        public static void Init()
        {
            LoadScenes();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void LoadScenes()
        {
            scenePaths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            sceneNames = scenePaths.Select(Path.GetFileNameWithoutExtension).ToArray();
            selectedSceneIndex = EditorPrefs.GetInt("DefaultPlaySceneIndex", 0);
        }

        public static void RefreshDropdown()
        {
            LoadScenes();
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            Handles.BeginGUI();

            // Get the current window width and calculate center position
            float windowWidth = sceneView.position.width;
            float dropdownWidth = 200; // Adjust as needed
            float xPos = (windowWidth - dropdownWidth) / 2; // Center horizontally

            GUILayout.BeginArea(new Rect(xPos, 10, dropdownWidth, 40)); // 10px from top

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Start Scene:", GUILayout.Width(70));
            int newSelectedSceneIndex = EditorGUILayout.Popup(selectedSceneIndex, sceneNames, GUILayout.Width(100));

            if (newSelectedSceneIndex != selectedSceneIndex)
            {
                selectedSceneIndex = newSelectedSceneIndex;
                EditorPrefs.SetInt("DefaultPlaySceneIndex", selectedSceneIndex);
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}