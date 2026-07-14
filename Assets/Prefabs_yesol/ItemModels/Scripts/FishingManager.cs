using UnityEngine;
using System.Collections.Generic;

public class FishingManager : MonoBehaviour
{
    /// 동시에 여러 명이 낚시 가능
    /// 낚시꾼들을 Observer로 추가?
    /// 일정 틱 지난 이후에 낚시 성공 알림 보내고, 
    /// 랜덤한 종류 물고기 보내기
    [SerializeField] private float maxFishingTime = 20f;
    [SerializeField] private float minFishingTime = 5f;
    [SerializeField] private float catchFishTime = 5f;
    private List<FishingCaster> observer = new List<FishingCaster>();

    private void Update()
    {
        foreach(FishingCaster caster in observer)
        {
            if (caster.waitingTime > Random.Range(minFishingTime, maxFishingTime))
            {
                if (caster.catchTime > catchFishTime)
                {
                    caster.isCatch = false;
                    // 햅틱 종료
                    caster.catchTime = 0;
                }
                else if (!caster.isCatch)
                {
                    caster.isCatch = true;
                    caster.waitingTime = 0;
                    caster.fishType = (Fish)Random.Range(1f, (float)Fish.END);
                    // 햅틱 피드백
                    caster.TriggerHaptic();
                }
                else
                {
                    caster.catchTime += Time.deltaTime;
                }
            }
            else
            {
                caster.waitingTime += Time.deltaTime;
            }
        }
    }

    public void ObserverRegister(FishingCaster caster)
    {
        if (!observer.Contains(caster))
        {
            observer.Add(caster);
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
