#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class TilemapColliderRebaker
{
    private const string SYSTEM_SCENE_CORE = "Core_Persistent";
    private const string SYSTEM_SCENE_MENU = "MainMenu";

    [MenuItem("Tools/Lumenfall/Level/Rebake All Level Scenes in Project", false, 10)]
    public static void RebakeAllLevelScenes()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Cannot Rebake in Play Mode", "Please exit Play Mode before rebaking colliders.", "OK");
            return;
        }

        List<string> scenePaths = FindAllLevelScenes();
        if (scenePaths.Count == 0)
        {
            EditorUtility.DisplayDialog("No Scenes Found", "No level scenes were found in Assets/Scenes.", "OK");
            return;
        }

        RebakeScenesBatch(scenePaths, "All Level Scenes");
    }

    [MenuItem("CONTEXT/CompositeCollider2D/Force Rebake Tilemap Geometry")]
    private static void ForceRebakeCompositeContext(MenuCommand command)
    {
        if (command.context is CompositeCollider2D composite)
        {
            RebakeSingleComposite(composite);
            EditorUtility.DisplayDialog("Rebake Complete", $"Rebaked geometry for '{composite.gameObject.name}'.", "OK");
        }
    }

    [MenuItem("CONTEXT/TilemapCollider2D/Force Rebake Tilemap Geometry")]
    private static void ForceRebakeTilemapContext(MenuCommand command)
    {
        if (command.context is TilemapCollider2D tilemapCollider)
        {
            if (tilemapCollider.TryGetComponent<CompositeCollider2D>(out var composite))
            {
                RebakeSingleComposite(composite);
                EditorUtility.DisplayDialog("Rebake Complete", $"Rebaked geometry for '{tilemapCollider.gameObject.name}'.", "OK");
            }
            else
            {
                tilemapCollider.ProcessTilemapChanges();
                EditorUtility.SetDirty(tilemapCollider);
                EditorUtility.DisplayDialog("Process Complete", $"Processed tilemap changes for '{tilemapCollider.gameObject.name}'.", "OK");
            }
        }
    }

    public static int RebakeSceneColliders(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return 0;

        GameObject[] rootObjects = scene.GetRootGameObjects();
        var composites = new List<CompositeCollider2D>();
        foreach (GameObject root in rootObjects)
        {
            composites.AddRange(root.GetComponentsInChildren<CompositeCollider2D>(true));
        }

        int count = 0;
        foreach (CompositeCollider2D composite in composites)
        {
            if (composite == null) continue;
            RebakeSingleComposite(composite);
            count++;
        }

        return count;
    }

    public static void RebakeSingleComposite(CompositeCollider2D composite)
    {
        if (composite == null) return;

        GameObject go = composite.gameObject;

        Tilemap[] tilemaps = go.GetComponentsInChildren<Tilemap>(true);
        foreach (Tilemap tm in tilemaps)
        {
            if (tm != null)
            {
                tm.RefreshAllTiles();
                EditorUtility.SetDirty(tm);
            }
        }

        TilemapCollider2D[] tilemapColliders = go.GetComponentsInChildren<TilemapCollider2D>(true);
        foreach (TilemapCollider2D tmc in tilemapColliders)
        {
            if (tmc != null)
            {
                tmc.enabled = false;
                tmc.enabled = true;
                tmc.compositeOperation = Collider2D.CompositeOperation.None;
                tmc.ProcessTilemapChanges();
                tmc.compositeOperation = Collider2D.CompositeOperation.Merge;
                tmc.ProcessTilemapChanges();
                EditorUtility.SetDirty(tmc);
            }
        }

        CompositeCollider2D.GeometryType originalGeom = composite.geometryType;
        CompositeCollider2D.GenerationType originalGen = composite.generationType;

        composite.enabled = false;
        composite.enabled = true;
        composite.generationType = CompositeCollider2D.GenerationType.Manual;

        composite.geometryType = (originalGeom == CompositeCollider2D.GeometryType.Polygons)
            ? CompositeCollider2D.GeometryType.Outlines
            : CompositeCollider2D.GeometryType.Polygons;
        composite.GenerateGeometry();

        composite.geometryType = originalGeom;
        composite.GenerateGeometry();

        composite.generationType = originalGen;

        EditorUtility.SetDirty(composite);
    }

    private static void RebakeScenesBatch(List<string> scenePaths, string title)
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Cannot Rebake in Play Mode", "Please exit Play Mode before rebaking colliders.", "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string originalScenePath = SceneManager.GetActiveScene().path;
        int totalRebakedColliders = 0;
        int totalScenesModified = 0;

        try
        {
            for (int i = 0; i < scenePaths.Count; i++)
            {
                string path = scenePaths[i];
                string sceneName = Path.GetFileNameWithoutExtension(path);

                EditorUtility.DisplayProgressBar(
                    $"Rebaking Level Colliders ({title})",
                    $"Processing [{sceneName}] ({i + 1}/{scenePaths.Count})...",
                    (float)i / scenePaths.Count
                );

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int rebakedInScene = RebakeSceneColliders(scene);

                if (rebakedInScene > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    totalRebakedColliders += rebakedInScene;
                    totalScenesModified++;
                    Debug.Log($"[TilemapColliderRebaker] Saved {rebakedInScene} rebaked collider(s) in '{sceneName}'.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TilemapColliderRebaker] Error during batch rebake: {ex.Message}\n{ex.StackTrace}");
            EditorUtility.DisplayDialog("Error", $"Rebaking encountered an error: {ex.Message}", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();

            if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }
        }

        string summary = $"Completed {title} Rebake!\n\n" +
                         $"- Scenes processed: {scenePaths.Count}\n" +
                         $"- Scenes updated & saved: {totalScenesModified}\n" +
                         $"- Total colliders rebaked: {totalRebakedColliders}";

        Debug.Log($"[TilemapColliderRebaker] {summary.Replace("\n\n", " ").Replace("\n", " ")}");
        EditorUtility.DisplayDialog("Rebake Summary", summary, "OK");
    }

    private static List<string> FindAllLevelScenes()
    {
        var result = new List<string>();
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string sceneName = Path.GetFileNameWithoutExtension(path);

            if (!IsSystemScene(sceneName))
            {
                result.Add(path);
            }
        }

        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }

    private static bool IsSystemScene(string sceneName)
    {
        return sceneName.Equals(SYSTEM_SCENE_CORE, StringComparison.OrdinalIgnoreCase) ||
               sceneName.Equals(SYSTEM_SCENE_MENU, StringComparison.OrdinalIgnoreCase);
    }
}
#endif
