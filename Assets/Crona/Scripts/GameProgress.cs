using UnityEngine;

// Floor-entry checkpoints deliberately replay the current floor's puzzles on Continue.
// Live inventory and the immutable floor-entry snapshot must never be mixed.
public static class GameProgress
{
    public const string InventoryKey = "GoUnder.Inventory.v1";
    private const string SceneKey = "GoUnder.Checkpoint.Scene";
    private const string SnapshotKey = "GoUnder.Checkpoint.Inventory";

    public static bool IsFloor(string scene)
    {
        return scene == "MainSceneF3" || scene == "MainSceneF2" || scene == "MainSceneF1";
    }

    public static void EnterFloor(string scene)
    {
        if (!IsFloor(scene)) return;
        PlayerPrefs.SetString(SceneKey, scene);
        PlayerPrefs.SetString(SnapshotKey, PlayerPrefs.GetString(InventoryKey, ""));
        PlayerPrefs.Save();
    }

    public static string PrepareContinue()
    {
        string scene = PlayerPrefs.GetString(SceneKey, "");
        if (!IsFloor(scene))
        {
            // Older saves have no floor-entry snapshot. Retain their inventory as a migration.
            return PlayerPrefs.GetInt("isInFloor2", 0) == 1 ? "MainSceneF2" : "MainSceneF3";
        }
        PlayerPrefs.SetString(InventoryKey, PlayerPrefs.GetString(SnapshotKey, ""));
        PlayerPrefs.Save();
        return scene;
    }

    public static void NewGame()
    {
        PlayerPrefs.DeleteKey(InventoryKey);
        PlayerPrefs.DeleteKey(SceneKey);
        PlayerPrefs.DeleteKey(SnapshotKey);
        PlayerPrefs.DeleteKey("isInFloor2");
        // All legacy scene inventories have twenty slots in each category.
        foreach (string category in new[] { "AtlasItem_", "FileItem_", "UseableItem_" })
            for (int i = 0; i < 20; i++) PlayerPrefs.DeleteKey(category + i);
        PlayerPrefs.Save();
    }
}
