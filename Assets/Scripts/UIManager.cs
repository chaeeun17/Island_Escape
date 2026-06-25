using System.Runtime.CompilerServices;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject inventoryPanel;
    public GameObject craftingPanel;

    [Header("Shortcut Keys")]
    public KeyCode inventoryKey = KeyCode.I;
    public KeyCode craftingKey = KeyCode.C;

    private void Start()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        if (craftingPanel != null)
        {
            craftingPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(inventoryKey))
        {
            ToggleInventory();
        }
        if (Input.GetKeyDown(craftingKey))
        {
            ToggleCrafting();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAllPanels();
        }
    }

    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        }
    }

    public void ToggleCrafting()
    {
        if(craftingPanel != null)
        {
            craftingPanel.SetActive(!craftingPanel.activeSelf);
        }
    }
    public void CloseAllPanels()
    {
        if(inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        if (craftingPanel != null)
        {
            craftingPanel.SetActive(false);
        }
    }

}