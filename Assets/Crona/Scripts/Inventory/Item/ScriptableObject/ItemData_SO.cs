using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum ItemType { Atlas, File, Useable}
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]

public class ItemData_SO : ScriptableObject
{
    public ItemType itemType;

    [SerializeField] private string itemId;
    public string Id { get { return itemId; } }
    public string itemName;

    public Sprite itemIcon_1100_700;

    public Sprite iconInDoor_135_135;

    public Sprite filePreviewSprite_1920_1080;

    public int relatedCheckWordNumber = -1;

    [TextArea(10, 200)]
    public string description = "";

    public Transform modelPrefab;

    public static ItemData_SO LoadItemDataByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;
        // Accept old display-name saves, including Communication Code's spaced name.
        foreach (ItemData_SO item in Resources.LoadAll<ItemData_SO>("Items"))
            if (item.Id == itemName || item.name == itemName || item.itemName == itemName)
                return item;
        Debug.LogError("Unknown saved inventory item: " + itemName);
        return null;
    }

}
