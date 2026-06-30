using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Inventory: MonoBehaviour 
{
    public SLot[] itemSlots;
    public static Inventory Instance;

    public void AddItem(Item item)
    { 
        if(itemSlots != null) 
        {
            //1. check for an existing item of the same type
            for(int i = 0; i<itemSlots.Length; i++)
            {
                if (itemSlots[i].isin() && itemSlots[i].getItem().getItemName()==item.getItemName())
                {
                    itemSlots[i].item.add_Item_Count(1); //increase the count of the existing item
                    itemSlots[i].Update_Count_text();//update the count display
                    Debug.Log("Increased count of" + item.getItemName() + "in slot" + i);
                    return; //item added, exit the method
                }
            }
            //2. if no existing item found, add to the first empty slot
            for(int i =0; i< itemSlots.Length; i++)
            {
                if (!itemSlots[i].isin())
                {
                    Item newItem = Instantiate(item);

                    //Add the new item instance to the inventory slot
                    itemSlots[i].SetSlot(newItem, true);

                    Debug.Log("New item added to slot" + i);
                    return; //exit the method after adding the new item
                }
            }


            //if no empty slot found
            Debug.Log("Inventory is full");
        }
    }

    private void Awake()
    {
        Instance = this;
    }
}


