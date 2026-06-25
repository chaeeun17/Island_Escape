using UnityEngine;
using System.Collections.Generic;

public class CraftingManager : MonoBehaviour
{
    public SLot[] inputSlots;
    public SLot resultSlot;

    [Header("등록된 조합법 목록")]
    public List<CraftingRecipe> availableRecipes;

    /// <summary>
    /// UI 버튼의 OnClick 이벤트에 연결할 메서드입니다.
    /// </summary>
    public void OnCraftButtonClick()
    {
        // 1. 결과 슬롯이 이미 차있다면 조합을 진행하지 않음
        if (resultSlot.item != null)
        {
            Debug.Log("결과 슬롯이 비어있지 않습니다. 아이템을 먼저 수령하세요.");
            return;
        }

        // 2. 현재 올려둔 재료와 일치하는 조합법이 있는지 확인
        CraftingRecipe matchedRecipe = GetMatchingRecipe();

        if (matchedRecipe != null)
        {
            // 3. 일치하는 조합법이 있다면 재료를 먼저 소모
            ConsumeIngredients(matchedRecipe);

            // 4. 결과 슬롯에 아이템 생성 및 배치
            Item craftedItem = Instantiate(matchedRecipe.resultPrefab);
            craftedItem.set_Item_Count(matchedRecipe.resultAmount);
            resultSlot.SetSlot(craftedItem, true);

            Debug.Log("조합 성공: " + matchedRecipe.resultPrefab.getItemName());
        }
        else
        {
            Debug.Log("일치하는 조합법이 없거나 재료가 부족합니다.");
        }
    }

    // 등록된 레시피 중 현재 올려둔 재료와 일치하는 레시피를 찾습니다.
    private CraftingRecipe GetMatchingRecipe()
    {
        foreach (CraftingRecipe recipe in availableRecipes)
        {
            if (IsRecipeMatch(recipe))
                return recipe;
        }
        return null;
    }

    // 슬롯에 올라간 아이템들이 특정 레시피의 조건을 만족하는지 검사합니다.
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

        foreach (var req in recipe.requiredIngredients)
        {
            if (!currentIngredients.ContainsKey(req.itemName) || currentIngredients[req.itemName] < req.amount)
            {
                return false; // 재료 부족 혹은 없음
            }
        }

        return true;
    }

    // 조합법에 맞게 재료 슬롯의 아이템 개수를 차감합니다.
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
                        Destroy(slot.item.gameObject);
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