using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fusion; // 🌟 Fusion 네트워크 라이브러리

public class NetworkCampfire : NetworkBehaviour
{
    // 🌟 [Networked]: 모든 클라이언트에 불 켜짐 상태 동기화
    [Networked, OnChangedRender(nameof(OnFireStateChanged))]
    public NetworkBool isLit { get; set; } = false;

    [Header("시각 효과")]
    public ParticleSystem fireParticles;
    public Light fireLight;

    [Header("Resources 내 SO 저장 경로")]
    [SerializeField] private string soFolderPath = "Items/"; // 예: Resources/ItemSO/

    [Header("요리 설정")]
    public float cookTime = 3f;

    [Header("휴식 설정 (스태미나 회복)")]
    public float restTickInterval = 2.0f;
    public int staminaRestoreAmount = 5;

    // 모닥불 범위 안에 있는 플레이어들 목록
    private List<NetworkPlayerStats> playersInZone = new List<NetworkPlayerStats>();
    private Coroutine restCoroutine;

    public override void Spawned()
    {
        UpdateFireState();
    }

    private void OnFireStateChanged()
    {
        UpdateFireState();
    }

    public void Ignite()
    {
        RPC_SetFireState(true);
    }

    public void Extinguish()
    {
        RPC_SetFireState(false);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetFireState(bool lit)
    {
        isLit = lit;

        if (isLit && playersInZone.Count > 0 && restCoroutine == null)
        {
            restCoroutine = StartCoroutine(RestRoutine());
        }
        else if (!isLit && restCoroutine != null)
        {
            StopCoroutine(restCoroutine);
            restCoroutine = null;
        }
    }

    private void UpdateFireState()
    {
        if (fireParticles != null)
        {
            if (isLit) fireParticles.Play();
            else fireParticles.Stop();
        }
        if (fireLight != null) fireLight.enabled = isLit;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 횃불 감지 (이름 기반)
        bool isTorch = other.gameObject.name.Contains("Torch") || 
                       (other.transform.parent != null && other.transform.parent.name.Contains("Torch"));

        if (isTorch && !isLit)
        {
            Debug.Log("🔥 [Campfire] 횃불을 감지하여 모닥불에 불을 붙입니다!");
            Ignite();
            return;
        }

        // 2. 플레이어 접근 감지
        NetworkPlayerStats playerStats = other.GetComponentInParent<NetworkPlayerStats>();
        if (playerStats != null)
        {
            if (!playersInZone.Contains(playerStats))
            {
                playersInZone.Add(playerStats);
                Debug.Log($"⛺ [Campfire] 플레이어 진입! 현재 범위 내 인원: {playersInZone.Count}명");
            }

            if (isLit && restCoroutine == null)
            {
                restCoroutine = StartCoroutine(RestRoutine());
            }
        }

        // 🌟 3. WorldItem 없이 Resources 탐색으로 요리 로직 처리
        if (isLit)
        {
            NetworkObject foodNetworkObj = other.GetComponentInParent<NetworkObject>();
            if (foodNetworkObj == null) return;

            // 프리팹 이름에서 (Clone) 제거하여 원래 아이템 이름 구하기
            string cleanPrefabName = foodNetworkObj.gameObject.name.Replace("(Clone)", "").Trim();

            // Resources 폴더에서 아이템 데이터(SO) 자동 로드
            Item itemData = Resources.Load<Item>($"{soFolderPath}{cleanPrefabName}");

            if (itemData != null)
            {
                ItemType itemType = itemData.GetItemType();
                Debug.Log($"🥩 [Campfire] 감지된 아이템: {itemData.getItemName()} | ItemType: {itemType}");

                if (itemType == ItemType.RawFood)
                {
                    if (HasStateAuthority)
                    {
                        Debug.Log($"🍳 [Campfire] {itemData.getItemName()} 요리 시작! (소요 시간: {cookTime}초)");
                        StartCoroutine(CookFoodNetwork(foodNetworkObj, itemData));
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ [Campfire] 클라이언트(Client) 권한이 없어 요리 코루틴을 실행하지 않습니다. (호스트에서 처리)");
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        NetworkPlayerStats playerStats = other.GetComponentInParent<NetworkPlayerStats>();
        if (playerStats != null)
        {
            if (playersInZone.Contains(playerStats))
            {
                playersInZone.Remove(playerStats);
                Debug.Log($"💨 [Campfire] 플레이어 이탈! 남은 인원: {playersInZone.Count}명");
            }

            if (playersInZone.Count == 0 && restCoroutine != null)
            {
                StopCoroutine(restCoroutine);
                restCoroutine = null;
            }
        }
    }

    private IEnumerator RestRoutine()
    {
        while (isLit && playersInZone.Count > 0)
        {
            yield return new WaitForSeconds(restTickInterval);

            for (int i = playersInZone.Count - 1; i >= 0; i--)
            {
                if (playersInZone[i] != null)
                {
                    playersInZone[i].RestoreStamina(staminaRestoreAmount);
                }
                else
                {
                    playersInZone.RemoveAt(i);
                }
            }
        }

        restCoroutine = null;
    }

    // 🌟 멀티플레이어 요리 코루틴
    private IEnumerator CookFoodNetwork(NetworkObject rawFoodNetworkObj, Item rawFoodData)
    {
        float timer = 0f;
        while (timer < cookTime)
        {
            timer += Time.deltaTime;
            // 요리 도중 누군가 음식을 집어 가져가거나 파괴되었을 경우 취소
            if (rawFoodNetworkObj == null || !rawFoodNetworkObj.IsValid)
            {
                Debug.LogWarning("❌ [Campfire] 요리 도중 음식 아이템이 사라졌거나 누군가 집어갔습니다. 취소합니다.");
                yield break;
            }
            yield return null;
        }

        if (rawFoodNetworkObj != null && rawFoodNetworkObj.IsValid)
        {
            Vector3 spawnPos = rawFoodNetworkObj.transform.position;
            Quaternion spawnRot = rawFoodNetworkObj.transform.rotation;

            Item cookedItemData = rawFoodData.GetCookedVersion();

            if (cookedItemData == null)
            {
                Debug.LogError($"❌ [Campfire] {rawFoodData.getItemName()}에 연결된 CookedVersion(조리된 버전) 데이터가 null입니다!");
                yield break;
            }

            GameObject cookedPrefab = cookedItemData.GetPrefab3D();
            if (cookedPrefab == null)
            {
                Debug.LogError($"❌ [Campfire] 조리된 음식[{cookedItemData.getItemName()}]의 3D 프리팹이 null입니다!");
                yield break;
            }

            // Fusion 네트워크 스폰 (조리된 음식 소환)
            NetworkObject spawnedCookedFood = Runner.Spawn(cookedPrefab, spawnPos, spawnRot);
            if (spawnedCookedFood != null)
            {
                Debug.Log($"🎉 [Campfire] 성공! 조리된 음식 [{cookedItemData.getItemName()}] 스폰 완료!");
            }

            // 기존 날음식 디스폰 (삭제)
            Runner.Despawn(rawFoodNetworkObj);
        }
    }
}