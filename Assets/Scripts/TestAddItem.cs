using UnityEngine;

public class TestAddItem : MonoBehaviour
{

    public Item[] itemsToGive;

    public void GiveItem()
    {
        if (Inventory.Instance != null && itemsToGive != null)
        {
            foreach (Item item in itemsToGive)
            {
                if (item != null)
                {
                    Inventory.Instance.AddItem(item);
                }
            }
        }
    }
}