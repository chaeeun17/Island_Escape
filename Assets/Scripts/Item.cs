using UnityEngine;


public enum ItemType
{
    General, // 일반 아이템
    Material, //제작 아이템
    Tool,
    RawFood, //날고기, 조리용 아이템
    CookedFood, // 조리된 음식
    Water
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item", order = 1)]
public class Item : ScriptableObject
{
    [SerializeField] protected int ItemNumber;
    [SerializeField] protected string ItemName;
    [SerializeField] protected Sprite ItemSprite;

    [TextArea(3, 5)]
    [SerializeField] protected string ItemDescription;


    [Header("아이템 세부 특성")]
    [SerializeField] protected ItemType itemType;      // 아이템 특성 (RawFood 등 선택)
    [SerializeField] protected Item cookedVersion;     // 요리 완료 시 변환될 결과물 (Item SO 연결)
    [SerializeField] protected GameObject prefab3D;    // 월드에 드롭/생성될 3D 모델 프리팹

    [Header("음식 회복 스탯 설정")]
    [SerializeField] protected int restoreHunger = 0;
    [SerializeField] protected int restoreThirst = 0;
    [SerializeField] protected int restoreStamina = 0;

    public int GetRestoreHunger() { return this.restoreHunger; }
    public int GetRestoreThirst() { return this.restoreThirst; }
    public int GetRestoreStamina() { return this.restoreStamina; }


    [System.NonSerialized] protected int get_Count = 0;
    [System.NonSerialized] protected bool isget = false;

    public int getItemNumber() { return this.ItemNumber; }
    public string getItemName() { return this.ItemName; }


    public string getItemDescription() { return this.ItemDescription; }

    public int get_Item_Count() { return this.get_Count; }
    public void set_Item_Count(int get_Count) { this.get_Count = get_Count; }
    public void add_Item_Count(int add_count) { this.get_Count += add_count; }
    public bool isSameItem(Item otherItem) { return this.ItemNumber == otherItem.getItemNumber(); }
    public void set_isGet(bool isget) { this.isget = isget; }
    public bool get_isGet() { return this.isget; }
    public Sprite get_Item_Sprite() { return this.ItemSprite; }
    public void setSprite(Sprite itemSprite) { this.ItemSprite = itemSprite; }


    public ItemType GetItemType() { return this.itemType; }
    public Item GetCookedVersion() { return this.cookedVersion; }
    public GameObject GetPrefab3D() { return this.prefab3D; }
}