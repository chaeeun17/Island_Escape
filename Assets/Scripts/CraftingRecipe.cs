using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Crafting Recipe", menuName = "Inventory/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("제작 결과물")]
    public Item resultItem;         // 결과물 Item SO
    public int resultAmount = 1;    // 제작 시 결과물 수량

    [System.Serializable]
    public struct Ingredient
    {
        public Item item;           
        public int amount;          // 필요 수량
    }

    [Header("필요 재료 목록")]
    public List<Ingredient> requiredIngredients;
}