using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item", order = 1)]
public class Item : ScriptableObject
{
    [SerializeField] protected int ItemNumber;
    [SerializeField] protected string ItemName;
    [SerializeField] protected Sprite ItemSprite;

    [System.NonSerialized] protected int get_Count = 0;
    [System.NonSerialized] protected bool isget = false;

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

    public void set_Item_Count(int get_Count)
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
    public void setSprite(Sprite itemSprite) { this.ItemSprite = itemSprite; }
}