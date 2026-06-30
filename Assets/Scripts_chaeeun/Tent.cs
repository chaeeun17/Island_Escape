using UnityEngine;

public class Tent : MonoBehaviour, IInteractable
{
    // 현재 플레이어가 잠든 상태인지를 기억하는 변수
    private bool isPlayerSleeping = false;

    public string GetInteractText()
    {
        // 현재 상태에 따라 X버튼 UI에 뜰 텍스트를 다르게 넘겨줍니다.
        if (isPlayerSleeping)
        {
            return "잠에서 깨기";
        }
        else
        {
            return "잠자기";
        }
    }

    public void Interact()
    {
        // X 버튼을 누를 때마다 상태를 반대로 뒤집고 로그를 찍습니다.
        if (!isPlayerSleeping)
        {
            // 깨어있는 상태에서 X를 누름 ➔ 잠들기
            isPlayerSleeping = true;
            
            Debug.Log("[텐트 상호작용] 플레이어가 잠을 자러 들어갔습니다.");
            // 💡 추후 여기에 시간 경과 로직이나 플레이어 이동 정지 코드를 넣으시면 됩니다.
        }
        else
        {
            // 자고 있는 상태에서 X를 누름 ➔ 깨어나기
            isPlayerSleeping = false;

            Debug.Log("[텐트 상호작용] 잠에서 깨어났습니다!");
            // 💡 추후 여기에 체력 회복 로직이나 플레이어 이동 재개 코드를 넣으시면 됩니다.
        }
    }
}