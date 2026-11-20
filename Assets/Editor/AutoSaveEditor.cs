using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
/// <summary>
/// Unity Editor Auto Save Script
/// Put this script in a folder named "Editor"
/// </summary>
[InitializeOnLoad]
public class AutoSaveEditor
{
    // Auto save interval (seconds)
    private const float SaveInterval = 120f;
    private static double _lastSaveTime;

    static AutoSaveEditor()
    {
        EditorApplication.update += OnEditorUpdate;
        _lastSaveTime = EditorApplication.timeSinceStartup;
    }

    private static void OnEditorUpdate()
    {
        if (EditorApplication.isPlaying) return;

        double now = EditorApplication.timeSinceStartup;
        if (now - _lastSaveTime >= SaveInterval)
        {
            AutoSave();
            _lastSaveTime = now;
        }
    }

    private static void AutoSave()
    {
        Scene currentScene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(currentScene.path))
        {
            Debug.LogWarning("[AutoSave] Current scene is not saved, skip auto save");
            return;
        }

        try
        {
            EditorSceneManager.SaveScene(currentScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00ff00>[AutoSave]</color> Saved scene: {currentScene.name}");

            // Uncomment below to enable scene backup
            // BackupScene(currentScene);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AutoSave] Save failed: {e.Message}");
        }
    }

    private static void BackupScene(Scene scene)
    {
        string scenePath = scene.path;
        string dir = Path.GetDirectoryName(scenePath);
        string fileName = Path.GetFileNameWithoutExtension(scenePath);
        string ext = Path.GetExtension(scenePath);
        string timeStamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupName = $"{fileName}_bak_{timeStamp}{ext}";
        string backupPath = Path.Combine(dir, backupName);

        File.Copy(scenePath, backupPath, true);
        Debug.Log($"<color=#99ccff>[AutoSave]</color> Backup created: {backupName}");
    }

    [MenuItem("Tools/AutoSave/Save Scene Now")]
    public static void ForceSave()
    {
        AutoSave();
    }
}
#endif
