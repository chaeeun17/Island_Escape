using UnityEngine;
using Fusion;

public class NetworkMouthZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 충돌한 오브젝트 또는 그 부모가 'Food' 태그를 가지고 있는지 확인
        if (other.CompareTag("Food") || (other.transform.parent != null && other.transform.parent.CompareTag("Food")))
        {
            // 음식이 가진 포톤 네트워크 오브젝트(NetworkObject)를 가져옵니다.
            NetworkObject foodNetworkObj = other.GetComponentInParent<NetworkObject>();
            if (foodNetworkObj == null) return;

            // 내가 손으로 집어서 소유권(StateAuthority)을 가지고 있는 음식만 먹음
            // (상대방이 들고 있는 음식이 내 입에 슬쩍 닿아 파괴되는 현상 방지)
            if (foodNetworkObj.HasStateAuthority)
            {
                Eat(foodNetworkObj, other.gameObject.name);
            }
        }
    }

    // 진짜 음식을 먹고 처리하는 함수
    private void Eat(NetworkObject foodNetworkObj, string foodName)
    {
        /*
        // 내 플레이어 배고픔/허기 회복 
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.RestoreHunger(25);
        }*/

        Debug.LogWarning($"🍎 [{foodName}]을(를) 맛있게 먹었습니다! 배고픔 +25 회복");

        // Fusion의 Despawn을 호출해야 방에 있는 모든 플레이어 화면에서 음식이 깔끔하게 삭제
        if (foodNetworkObj.Runner != null && foodNetworkObj.Runner.IsRunning)
        {
            foodNetworkObj.Runner.Despawn(foodNetworkObj);
        }
    }
}
