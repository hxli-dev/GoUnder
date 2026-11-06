using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TVInteract : MonoBehaviour
{
    public GameObject TVCam;
    public GameObject EPanel;
    public GameObject crosshairCanvas;

    private bool TVOn = false;
    private bool busy;
    private bool previousPause;
    private bool previousInventory;


    private void Update()
    {
        if (TVCam != null && TVOn)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                TVOn = false;
                busy = true;
                EPanel.SetActive(false);
                TVCam.SetActive(false);
                StartCoroutine(WaitCameraOut());
            }
        }
    }


    public void SwitchTV()
    {
        if (TVCam != null)
        {
            if (!TVOn && !busy)
            {
                busy = true;
                previousPause = GameManager.Instance.pauseMenuEnabled;
                previousInventory = GameManager.Instance.inventoryEnabled;
                GameManager.Instance.pauseMenuEnabled = false;
                GameManager.Instance.inventoryEnabled = false;
                TVCam.SetActive(true);
                StartCoroutine(WaitCameraIn());
                GameManager.Instance.LockPlayerCam();
                PlayerInteraction.Instance.interactAllowed = false;

                if (!PlayerMovement.Instance.moveDisabled)
                {
                    PlayerMovement.Instance.moveDisabled = true;
                }

                if (crosshairCanvas.activeInHierarchy)
                {
                    crosshairCanvas.SetActive(false);
                }
            }
        }
    }


    IEnumerator WaitCameraIn()
    {
        yield return new WaitForSeconds(1f);
        EPanel.SetActive(true);
        TVOn = true;
        busy = false;
    }

    IEnumerator WaitCameraOut()
    {
        yield return new WaitForSeconds(1f);

        PlayerMovement.Instance.moveDisabled = false;
        crosshairCanvas.SetActive(true);
        GameManager.Instance.UnlockPlayerCam();
        PlayerInteraction.Instance.interactAllowed = true;
        GameManager.Instance.pauseMenuEnabled = previousPause;
        GameManager.Instance.inventoryEnabled = previousInventory;
        busy = false;
    }
}
