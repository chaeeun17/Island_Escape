using UnityEngine;
using Fusion;

public class FlyingFloat : NetworkBehaviour
{
    public Rigidbody floatRb;
    [HideInInspector] public FishingCaster ownerCaster; // 자신을 던진 낚싯대 참조

    private void Awake()
    {
        floatRb = GetComponent<Rigidbody>();
    }

    // 물 충돌 및 지면 충돌 감지
    private void OnTriggerEnter(Collider other)
    {
        // StateAuthority(서버/호스트)에서만 물리 및 상태 변화 처리
        if (!HasStateAuthority || ownerCaster == null) return;

        if (other.CompareTag("Water"))
        {
            // 물에 닿았을 때 Caster에게 알림
            Debug.Log("물");
            ownerCaster.OnHitWater(transform.position);
            return;
        }

        // 2. 낚싯대, 플레이어 손, 자기 자신과의 충돌은 무시!
        if (other.CompareTag("Player") || other.CompareTag("Fishingrod") || other.transform.IsChildOf(ownerCaster.transform))
        {
            Debug.Log("손/낚싯대");
            return; // 스폰되자마자 낚싯대에 부딪혀 삭제되는 것을 방지
        }
        else
        {
            Debug.Log("그 외");
            // 땅이나 다른 곳에 부딪히면 리셋
            // ownerCaster.ResetFloat();
        }
    }
}