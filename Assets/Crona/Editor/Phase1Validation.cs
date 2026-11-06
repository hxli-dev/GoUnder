using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Run only with the pinned editor. These checks do not save or rewrite scenes.
public static class Phase1Validation
{
    [Serializable] private class Result
    {
        public string unityVersion;
        public List<string> checkedScenes = new List<string>();
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();
    }

    private static readonly Dictionary<string, string[]> Required = new Dictionary<string, string[]>
    {
        { "MouseLook", new[] { "slider_x", "slider_y", "cinemachineVirtualCamera" } },
        { "Brightness", new[] { "slider", "globalVolume" } },
        { "MusicSound", new[] { "slider_music", "slider_sound", "backgroundAudio", "soundEffectsAudio" } },
        { "InventoryManager", new[] { "AtlasContainer", "FileContainer", "UseableContainer", "itemPanel", "filePanel", "fileImage", "previewModel", "doorItemPages", "crosshairCanvas", "mainCam" } },
        { "GameManager", new[] { "player", "InventoryCanvas", "CrosshairCanvas", "mainCam", "vCam", "blackPanel", "whitePanel", "playerAudio", "ambience", "BGM", "forwardRendererData" } },
        { "PlayerMovement", new[] { "myController", "cam", "groundCheck", "vCam" } },
        { "PlayerInteraction", new[] { "cam", "crosshairCanvas", "onTarget", "targetDisabled", "pickupSFX" } },
        { "SubtitleManager", new[] { "subtitleText", "playerAudio", "callReminder", "callerImage", "callerName", "inCallPanel", "blackPanel", "subtitlePanel" } },
        { "ScreenFade", new[] { "button", "fadeImage" } },
        { "ScreenFadeForContinue", new[] { "button", "fadeImage" } }
    };

    [MenuItem("Tools/Phase 1/Validate build scenes")]
    public static void Validate()
    {
        Result result = new Result { unityVersion = Application.unityVersion };
        if (Application.unityVersion != "2020.3.26f1")
            throw new InvalidOperationException("Use Unity 2020.3.26f1; no automatic upgrade is authorized.");
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            {
                if (!entry.enabled) continue;
                Scene scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                result.checkedScenes.Add(entry.path);
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    {
                        GameObject go = transform.gameObject;
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
                            result.errors.Add(entry.path + ": missing script on " + go.name);
                        foreach (Component component in go.GetComponents<Component>())
                        {
                            if (component == null) continue;
                            SerializedObject serialized = new SerializedObject(component);
                            SerializedProperty property = serialized.GetIterator();
                            while (property.NextVisible(true))
                                if (property.propertyType == SerializedPropertyType.ObjectReference &&
                                    property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                                    result.errors.Add(entry.path + ": broken reference " + go.name + "." + property.propertyPath);
                            string[] required;
                            if (Required.TryGetValue(component.GetType().Name, out required))
                                foreach (string field in required)
                                {
                                    SerializedProperty value = serialized.FindProperty(field);
                                    if (value == null) { result.errors.Add(go.name + ": absent field " + field); continue; }
                                    if (value.isArray && value.propertyType != SerializedPropertyType.String)
                                    {
                                        if (value.arraySize == 0) result.errors.Add(go.name + ": empty " + field);
                                        for (int i = 0; i < value.arraySize; i++)
                                            if (value.GetArrayElementAtIndex(i).objectReferenceValue == null)
                                                result.errors.Add(go.name + ": null " + field + "[" + i + "]");
                                    }
                                    else if (value.objectReferenceValue == null)
                                        result.errors.Add(entry.path + ": unassigned " + go.name + "." + field);
                                }
                            StartDataBase database = component as StartDataBase;
                            if (database != null && database.usesLegacyFloorDoor && database.F1DoorAnimator == null)
                                result.errors.Add(entry.path + ": legacy database gate has no Animator.");
                        }
                    }
                foreach (string dependency in AssetDatabase.GetDependencies(entry.path, true))
                    if (!File.Exists(dependency) && !Directory.Exists(dependency))
                        result.warnings.Add(entry.path + ": dependency path requires verification: " + dependency);
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/phase1-scene-validation.json", JsonUtility.ToJson(result, true));
        }
        if (result.errors.Count > 0) throw new InvalidOperationException("Scene validation failed; see Logs/phase1-scene-validation.json");
        Debug.Log("Phase 1 scene reference validation passed. This is not a gameplay test.");
    }

    public static void BuildWindows()
    {
        Validate();
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes) if (scene.enabled) scenes.Add(scene.path);
        Directory.CreateDirectory("Builds/Windows");
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = "Builds/Windows/Deep Blue Requiem.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        File.WriteAllText("Logs/phase1-build-result.txt", report.summary.result + "\n" + report.summary.totalErrors + " errors");
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows development build failed.");
    }
}
