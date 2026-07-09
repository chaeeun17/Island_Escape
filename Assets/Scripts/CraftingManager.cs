using UnityEngine;
using System.Collections.Generic;

public class CraftingManager : MonoBehaviour
{
    public SLot[] inputSlots;
    public SLot resultSlot;

    [Header("등록된 조합법 목록")]
    public List<CraftingRecipe> availableRecipes;

    public void OnCraftButtonClick()
    {
        CraftingRecipe matchedRecipe = GetMatchingRecipe();

        if (matchedRecipe != null)
        {
            if (resultSlot.item != null)
            {
                if (resultSlot.item.getItemName() != matchedRecipe.resultPrefab.getItemName())
                {
                    Debug.Log("결과 슬롯에 다른 아이템이 있습니다. 아이템을 먼저 수령하세요.");
                    return;
                }
            }

            ConsumeIngredients(matchedRecipe);

            if (resultSlot.item == null)
            {
                // ScriptableObject 원본 에셋을 복제(Instance)하여 사용
                Item craftedItem = Instantiate(matchedRecipe.resultPrefab);
                craftedItem.set_Item_Count(matchedRecipe.resultAmount);
                resultSlot.SetSlot(craftedItem, false);
            }
            else
            {
                resultSlot.item.add_Item_Count(matchedRecipe.resultAmount);
                resultSlot.Update_Count_text();
            }

            Debug.Log("조합 성공: " + matchedRecipe.resultPrefab.getItemName() + " (현재 누적 개수: " + resultSlot.item.get_Item_Count() + ")");
        }
        else
        {
            Debug.Log("일치하는 조합법이 없거나 재료가 부족합니다.");
        }
    }

    private CraftingRecipe GetMatchingRecipe()
    {
        foreach (CraftingRecipe recipe in availableRecipes)
        {
            if (IsRecipeMatch(recipe))
                return recipe;
        }
        return null;
    }

    private bool IsRecipeMatch(CraftingRecipe recipe)
    {
        Dictionary<string, int> currentIngredients = new Dictionary<string, int>();
        foreach (var slot in inputSlots)
        {
            if (slot.item != null)
            {
                string name = slot.item.getItemName();
                if (currentIngredients.ContainsKey(name))
                    currentIngredients[name] += slot.item.get_Item_Count();
                else
                    currentIngredients[name] = slot.item.get_Item_Count();
            }
        }

        if (currentIngredients.Count != recipe.requiredIngredients.Count)
        {
            return false;
        }

        foreach (var req in recipe.requiredIngredients)
        {
            if (!currentIngredients.ContainsKey(req.itemName) || currentIngredients[req.itemName] < req.amount)
            {
                return false;
            }
        }

        return true;
    }

    private void ConsumeIngredients(CraftingRecipe recipe)
    {
        Dictionary<string, int> remainingToConsume = new Dictionary<string, int>();
        foreach (var req in recipe.requiredIngredients)
        {
            remainingToConsume[req.itemName] = req.amount;
        }

        foreach (var slot in inputSlots)
        {
            if (slot.item != null)
            {
                string itemName = slot.item.getItemName();
                if (remainingToConsume.ContainsKey(itemName) && remainingToConsume[itemName] > 0)
                {
                    int consumeAmount = Mathf.Min(slot.item.get_Item_Count(), remainingToConsume[itemName]);

                    slot.item.add_Item_Count(-consumeAmount);
                    remainingToConsume[itemName] -= consumeAmount;

                    if (slot.item.get_Item_Count() <= 0)
                    {
                        Destroy(slot.item);
                        slot.Clear_Slot();
                    }
                    else
                    {
                        slot.Update_Count_text();
                    }
                }
            }
        }
    }
}