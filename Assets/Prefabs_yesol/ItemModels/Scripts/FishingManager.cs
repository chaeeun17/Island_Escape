using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class FishingManager : NetworkBehaviour
{
    /// 동시에 여러 명이 낚시 가능
    /// 일정 틱 지난 이후에 낚시 성공 알림 보내고, 
    /// 랜덤한 종류 물고기 보내기
    [SerializeField] private float maxFishingTime = 20f;
    [SerializeField] private float minFishingTime = 5f;
    [SerializeField] private float catchFishWindow = 5f; // 입질 후 기회 시간
    [SerializeField] private float hapticInterval = 0.15f; // 햅틱 진동 간격 (초)

    private List<FishingCaster> observer = new List<FishingCaster>();

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        for (int i = observer.Count - 1; i >= 0; i--)
        {
            FishingCaster caster = observer[i];

            if (caster == null || caster.state != FloatState.Floating)
            {
                continue;
            }

            caster.waitingTime += Runner.DeltaTime;

            if (caster.waitingTime >= caster.targetWaitTime)
            {
                // 1. 입질 시작
                if (!caster.isCatch)
                {
                    caster.isCatch = true;
                    caster.catchTime = 0f;
                    caster.hapticTimer = 0f; // 타이머 초기화

                    caster.fishType = (Fish)UnityEngine.Random.Range(1, (int)Fish.END);
                }

                // 2. 입질 유지 중 (catchTime 동안 지속적 진동 연사)
                caster.catchTime += Runner.DeltaTime;
                caster.hapticTimer += Runner.DeltaTime;

                // 재시뮬레이션(Resimulation) 틱이 아닐 때만 RPC 발사
                if (Runner.IsForward && caster.hapticTimer >= hapticInterval)
                {
                    caster.hapticTimer -= hapticInterval;

                    // 해당 낚싯대의 주인(InputAuthority)에게 Unreliable RPC 전송
                    caster.RPC_TriggerHaptic();
                }

                // 3. 기회 시간 초과시 놓침
                if (caster.catchTime > catchFishWindow)
                {
                    Debug.Log("[Fishing Debug] 낚시 실패 : 기회 시간을 초과하여 물고기가 도망갔습니다!");
                    caster.isCatch = false;
                    caster.waitingTime = 0f;
                    caster.targetWaitTime = UnityEngine.Random.Range(minFishingTime, maxFishingTime);
                }
            }
        }
    }

    public void ObserverRegister(FishingCaster caster)
    {
        if (!HasStateAuthority) return;

        if (!observer.Contains(caster))
        {
            observer.Add(caster);
            // 등록 시 최초 targetWaitTime 생성
            caster.targetWaitTime = UnityEngine.Random.Range(minFishingTime, maxFishingTime);
            caster.waitingTime = 0f;
            caster.catchTime = 0f;
            caster.hapticTimer = 0f;
            caster.isCatch = false;
        }
    }

    public void ObserverDeregister(FishingCaster caster)
    {
        if (observer.Contains(caster))
        {
            observer.Remove(caster);
        }
    }
}
