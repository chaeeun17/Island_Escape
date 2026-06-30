using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Crafting Recipe", menuName = "Inventory/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [System.Serializable]
    public struct Ingredient
    {
        public string itemName; // 필요 재료의 이름 
        public int amount;      // 필요 개수
    }

    [Header("필요한 재료들")]
    public List<Ingredient> requiredIngredients;

    [Header("조합 결과물")]
    public Item resultPrefab;   // 완성되어 나올 아이템 프리팹
    public int resultAmount = 1; // 완성될 개수
}