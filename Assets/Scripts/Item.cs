using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class Item : MonoBehaviour
{
    [SerializeField] protected int ItemNumber;
    [SerializeField]protected string ItemName;
    [SerializeField] protected Image ItemImage;
    [SerializeField] protected Sprite ItemSprite;
    protected int get_Count = 0;
    protected bool isget = false;

    public int getItemNumber()
    {
        return this.ItemNumber;
    }
    public string getItemName()
    {
        return this.ItemName;
    }
    public int get_Item_Count()
    {
        return this.get_Count;
    }
    public  void set_Item_Count(int get_Count) 
    {
        this.get_Count = get_Count;
    }
    public void add_Item_Count(int add_count)
    {
        this.get_Count += add_count;
    }
    public bool isSameItem(Item otherItem)
    {
        return this.ItemNumber == otherItem.getItemNumber();
    }

    public void set_isGet(bool isget) { this.isget = isget; }
    public bool get_isGet() { return this.isget; }
    public Sprite get_Item_Sprite() { return this.ItemSprite; }
    public Image getImage() { return this.ItemImage; }
    public void setSprite(Sprite itemSprite) { this.ItemSprite = itemSprite; }
}
