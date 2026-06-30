using UnityEngine;

public class TestAddItem: MonoBehaviour
{
    public Item itemToGive;
    public void GiveItem()
    {
        if (Inventory.Instance != null && itemToGive != null)
        {
            Inventory.Instance.AddItem(itemToGive);
        }
    }
}
