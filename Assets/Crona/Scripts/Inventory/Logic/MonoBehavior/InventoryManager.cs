using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Inventory Data")]

    public List<InventoryItem> AtlasItems = new List<InventoryItem>();
    public List<InventoryItem> FileItems = new List<InventoryItem>();
    public List<InventoryItem> UseableItems = new List<InventoryItem>();

    [Header("Inventory UI")]
    public GameObject AtlasContainer;
    public GameObject FileContainer;
    public GameObject UseableContainer;

    public GameObject itemPanel;
    public GameObject filePanel;
    public Image fileImage;
    public Transform itemPrefab;
    public GameObject previewModel;

    [Header("Door UI")]
    public GameObject doorItemPages;

    [Header("Reminder UI")]
    public GameObject PopupPanel;
    public Image ReminderInfo;
    public TMP_Text ReminderName;
    public TMP_Text ReminderType;
    public int timer = 0;
    private float t = 1f;
    private float posT = 1f;

    [Header("Player Info")]
    public GameObject crosshairCanvas;
    public GameObject mainCam;

    [Header("Pause Menu")]
    public bool pauseMenuEnabled = true;

    //Alexis added
    [Header("Item Pickup")]
    private List<GameObject> itemPickup;

    [Header("Editor Control")]
    public bool isInEditor;

    //[Header("Inventory Metrics")]
    //public int itemCollected;

    private bool loaded;
    private bool saveValid = true;
    private bool UsesEditorInventory { get { return Application.isEditor && isInEditor; } }

    private void Awake() { Instance = this; }
    private void OnEnable() { if (GameManager.Instance != null) EnsureLoaded(); }
    private void OnDisable() { if (loaded && !UsesEditorInventory) SaveInventory(); }
    private void OnApplicationPause(bool paused) { if (paused && loaded) SaveInventory(); }
    private void OnApplicationQuit() { if (loaded) SaveInventory(); }

    public void EnsureLoaded()
    {
        Instance = this;
        if (loaded) return;
        loaded = true;
        if (!UsesEditorInventory) LoadInventory();
    }

    void Update()
    {
        ReminderSlide();
        ExitPreviewPanel();
    }

    public void AddItem(ItemData_SO data) { TryAddItem(data); }

    public bool TryAddItem(ItemData_SO data)
    {
        EnsureLoaded();
        if (!AddCore(data)) return false;
        SaveInventory();
        string type = data.itemType == ItemType.Atlas ? "Info Added" :
            data.itemType == ItemType.File ? "File Added" : "Item Added";
        ShowReminder(data.itemName, type);
        if (data.itemType == ItemType.File && data.filePreviewSprite_1920_1080 != null)
            StartCoroutine(ShowFilePreview(data.filePreviewSprite_1920_1080));
        if (data.itemType == ItemType.Useable && data.modelPrefab != null)
            StartCoroutine(ShowItemPreview(data.modelPrefab));
        return true;
    }

    public void AddItemWithoutReminder(ItemData_SO data)
    {
        EnsureLoaded();
        AddCore(data);
        RemoveCollectedPickups(data);
    }

    private bool AddCore(ItemData_SO data)
    {
        if (data == null) return false;
        List<InventoryItem> items = data.itemType == ItemType.Atlas ? AtlasItems :
            data.itemType == ItemType.File ? FileItems : UseableItems;
        foreach (InventoryItem item in items)
            if (item != null && item.itemData == data) return false;
        InventoryItem slot = items.Find(item => item != null && item.itemData == null);
        if (slot == null)
        {
            Debug.LogError("Inventory is full; pickup was not consumed: " + data.name, this);
            return false;
        }
        slot.itemData = data;
        if (data.itemType == ItemType.Atlas) AtlasContainer.GetComponent<AtlasContainerUI>().AddItem(data);
        else if (data.itemType == ItemType.File) FileContainer.GetComponent<FileContainerUI>().AddItem(data);
        else UseableContainer.GetComponent<UseableContainerUI>().AddItem(data);
        if (data.iconInDoor_135_135 != null)
            doorItemPages.GetComponent<PageList>().AddItemToDoor(data.iconInDoor_135_135, data.itemName, data.relatedCheckWordNumber);
        return true;
    }

    private void RemoveCollectedPickups(ItemData_SO data)
    {
        if (data == null) return;
        // Compare the serialized asset, never a potentially renamed GameObject.
        foreach (ItemPickup pickup in FindObjectsOfType<ItemPickup>(true))
            if (pickup.itemData == data) Destroy(pickup.gameObject);
    }

    [Serializable]
    private class SavedInventory
    {
        public List<string> ids = new List<string>();
    }

    public void SaveInventory()
    {
        if (!loaded || !saveValid || UsesEditorInventory) return;
        SavedInventory saved = new SavedInventory();
        foreach (List<InventoryItem> items in new[] { AtlasItems, FileItems, UseableItems })
            foreach (InventoryItem item in items)
                if (item != null && item.itemData != null) saved.ids.Add(item.itemData.Id);
        PlayerPrefs.SetString(GameProgress.InventoryKey, JsonUtility.ToJson(saved));
        PlayerPrefs.Save();
    }

    private void LoadInventory()
    {
        if (PlayerPrefs.HasKey(GameProgress.InventoryKey))
        {
            string json = PlayerPrefs.GetString(GameProgress.InventoryKey, "");
            if (string.IsNullOrEmpty(json)) return;
            SavedInventory saved;
            try { saved = JsonUtility.FromJson<SavedInventory>(json); }
            catch (ArgumentException error)
            {
                saveValid = false;
                Debug.LogError("Invalid inventory save; retained for recovery: " + error.Message, this);
                return;
            }
            if (saved != null && saved.ids != null)
                foreach (string id in saved.ids) AddItemWithoutReminder(ItemData_SO.LoadItemDataByName(id));
        }
        else
        {
            foreach (string category in new[] { "AtlasItem_", "FileItem_", "UseableItem_" })
                for (int i = 0; i < 20; i++)
                {
                    string legacy = PlayerPrefs.GetString(category + i, "");
                    if (!string.IsNullOrEmpty(legacy)) AddItemWithoutReminder(ItemData_SO.LoadItemDataByName(legacy));
                }
        }
    }

    public void ShowReminder(string title, string type)
    {
        ReminderName.text = title;
        ReminderType.text = type;

        timer = 420;
        t = 0f;
        posT = 0f;
        
    }

    public void ReminderSlide()
    {
        if (timer > 0)
        {
            timer--;
        }

        if (timer > 60)
        {
            if (posT < 1f)
            {
                PopupPanel.GetComponent<RectTransform>().position = Vector3.Lerp(new Vector3(2300, 520, 0), new Vector3(1920, 520, 0), posT);

                posT += 4 * Time.deltaTime;
            }
            else
            {
                PopupPanel.GetComponent<RectTransform>().position = new Vector3(1920, 520, 0);
            }


            if (t < 1f)
            {
                ReminderInfo.color = Color.Lerp(new Color(ReminderInfo.color.r, ReminderInfo.color.g, ReminderInfo.color.b, 0f), new Color(ReminderInfo.color.r, ReminderInfo.color.g, ReminderInfo.color.b, 1f), t);
                ReminderName.color = Color.Lerp(new Color(ReminderName.color.r, ReminderName.color.g, ReminderName.color.b, 0f), new Color(ReminderName.color.r, ReminderName.color.g, ReminderName.color.b, 1f), t);
                ReminderType.color = Color.Lerp(new Color(ReminderType.color.r, ReminderType.color.g, ReminderType.color.b, 0f), new Color(ReminderType.color.r, ReminderType.color.g, ReminderType.color.b, 1f), t);

                t += 3f * Time.deltaTime;
            }
            else
            {
                ReminderInfo.color = new Color(ReminderInfo.color.r, ReminderInfo.color.g, ReminderInfo.color.b, 1f);
                ReminderName.color = new Color(ReminderName.color.r, ReminderName.color.g, ReminderName.color.b, 1f);
                ReminderType.color = new Color(ReminderType.color.r, ReminderType.color.g, ReminderType.color.b, 1f);
            }
        }

        else if (timer == 60)
        {
            t = 0f;
            posT = 0f;
        }

        else if (timer < 60)
        {

            if (t < 1f)
            {
                ReminderInfo.color = Color.Lerp(new Color(ReminderInfo.color.r, ReminderInfo.color.g, ReminderInfo.color.b, 1f), new Color(ReminderInfo.color.r, ReminderInfo.color.g, ReminderInfo.color.b, 0f), t);
                ReminderName.color = Color.Lerp(new Color(ReminderName.color.r, ReminderName.color.g, ReminderName.color.b, 1f), new Color(ReminderName.color.r, ReminderName.color.g, ReminderName.color.b, 0f), t);
                ReminderType.color = Color.Lerp(new Color(ReminderType.color.r, ReminderType.color.g, ReminderType.color.b, 1f), new Color(ReminderType.color.r, ReminderType.color.g, ReminderType.color.b, 0f), t);

                t += 4f * Time.deltaTime;
            }
            else
            {
                ReminderInfo.color = new Color(ReminderInfo.color.r, ReminderInfo.color.g, ReminderInfo.color.b, 0f);
                ReminderName.color = new Color(ReminderName.color.r, ReminderName.color.g, ReminderName.color.b, 0f);
                ReminderType.color = new Color(ReminderType.color.r, ReminderType.color.g, ReminderType.color.b, 0f);
            }

            if (posT < 1f)
            {
                PopupPanel.GetComponent<RectTransform>().position = Vector3.Lerp(new Vector3(1920, 520, 0), new Vector3(2300, 520, 0), posT);

                posT += 4 * Time.deltaTime;
            }
            else
            {
                PopupPanel.GetComponent<RectTransform>().position = new Vector3(2300, 520, 0);
            }
        }
    }

    private void ExitPreviewPanel()
    {
        if (itemPanel.activeInHierarchy && Input.GetKey(KeyCode.Escape))
        {
            itemPanel.SetActive(false);
            itemPrefab.gameObject.transform.parent = previewModel.transform;
            itemPrefab.gameObject.transform.position = new Vector3(10000, 10000, 10000);
            Destroy(itemPrefab.gameObject);
            //itemPrefab = null;
            itemPanel.GetComponent<ItemViewer>().itemModel = null;
           

            crosshairCanvas.SetActive(true);

            GameManager.Instance.inventoryEnabled = true;

            PlayerMovement.Instance.moveDisabled = false;
            mainCam.GetComponent<Cinemachine.CinemachineBrain>().enabled = true;
            Cursor.visible = false;
            StartCoroutine(WaitForPauseMenu());
        }

        if (filePanel.activeInHierarchy && Input.GetKey(KeyCode.Escape))
        {
            filePanel.SetActive(false);
            fileImage.sprite = null;

            crosshairCanvas.SetActive(true);
            if (SceneManager.GetActiveScene().buildIndex != 3)
            {
                GameManager.Instance.inventoryEnabled = true;
            }
            
            PlayerMovement.Instance.moveDisabled = false;
            mainCam.GetComponent<Cinemachine.CinemachineBrain>().enabled = true;
            Cursor.visible = false;
            StartCoroutine(WaitForPauseMenu());
        }
    }


    IEnumerator ShowItemPreview(Transform modelPrefab)
    {
        pauseMenuEnabled = false;
        GameManager.Instance.inventoryEnabled = false;
        PlayerMovement.Instance.moveDisabled = true;
        mainCam.GetComponent<Cinemachine.CinemachineBrain>().enabled = false;
        yield return new WaitForSeconds(0.5f);
        itemPanel.SetActive(true);
        crosshairCanvas.SetActive(false);


        Cursor.visible = true;

        if (itemPrefab != null)
        {
            Destroy(itemPrefab.gameObject);
        }

        if (previewModel.transform.childCount > 0)
        {
            Destroy(previewModel.transform.GetChild(0).gameObject);
            itemPanel.GetComponent<ItemViewer>().itemModel = null;
            itemPrefab = null;
        }
        
        itemPrefab = Instantiate(modelPrefab, new Vector3(1000, 2000, 1000), Quaternion.identity);
        itemPrefab.gameObject.transform.parent = previewModel.transform;
        itemPanel.GetComponent<ItemViewer>().itemModel = itemPrefab;
    }


    IEnumerator ShowFilePreview(Sprite fileSprite)
    {
        pauseMenuEnabled = false;
        GameManager.Instance.inventoryEnabled = false;
        PlayerMovement.Instance.moveDisabled = true;
        mainCam.GetComponent<Cinemachine.CinemachineBrain>().enabled = false;
        yield return new WaitForSeconds(0.5f);
        filePanel.SetActive(true);
        crosshairCanvas.SetActive(false);


        Cursor.visible = true;

        fileImage.sprite = fileSprite;
    }


    IEnumerator WaitForPauseMenu()
    {
        yield return new WaitForSeconds(1f);
        pauseMenuEnabled = true;
    }

}



[System.Serializable]

public class InventoryItem
{
    public ItemData_SO itemData;
}
