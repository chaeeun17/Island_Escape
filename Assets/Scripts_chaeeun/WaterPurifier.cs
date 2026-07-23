using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class WaterPurifier : MonoBehaviour, IInteractable
{
    [Header("정수 소요 시간 (초)")]
    [SerializeField] private float purifyingTime = 3.0f;

   // 현재 정수 장치가 작동 중인지 체크 (정수 중에 또 바닷물을 넣는 것 방지)
    private bool isPurifying = false;

    public string GetInteractText()
    {
        return "정수중";
    }

    public void Interact()
    {
        if (isPurifying)
        {
            return;
        }

        // 플레이어의 레이저 손(인터랙터)을 찾음
        var interactor = Object.FindFirstObjectByType<NearFarInteractor>();
        if (interactor == null) return;

        // 현재 무언가 손에 쥐고 있는지 체크
        if (interactor.hasSelection)
        {
            // 손에 쥔 실제 게임 오브젝트를 가져옵니다.
            GameObject heldObject = interactor.firstInteractableSelected.transform.gameObject;
        
            if (heldObject.name == "SeaWaterBucket")
            {
                StartCoroutine(PurifyingProcessRoutine());
            }
            else
            {
                Debug.LogWarning($"❌ [조건 실패]");
            }
        }
        else
        {
            Debug.LogWarning("❌ [조건 실패] 맨손입니다. 바닷물 아이템을 쥐고 X를 누르세요.");
        }
    }

    private IEnumerator PurifyingProcessRoutine()
    {
        isPurifying = true;
        Debug.LogWarning($"🔥 [정수 시작] {purifyingTime}초간 정수가 진행됩니다... 뽀글뽀글...");

        // ==========================================
        // (선택 사항) 여기에 정수기 연기/물방울 이펙트 켜거나 소리를 재생하는 코드를 넣으면 좋습니다.
        // ==========================================

        yield return new WaitForSeconds(purifyingTime);

        // 대기 끝
        Debug.LogWarning("✨ [정수 완료!] 맑은 물이 생성되었습니다.");

/*
        if (cleanWaterPrefab != null)
        {
            // 스폰 위치는 지정된 출구(waterSpawnPoint)가 있으면 거기, 없으면 정수 장치 본체 위치로 합니다.
            Vector3 spawnPos = waterSpawnPoint != null ? waterSpawnPoint.position : transform.position;
            Quaternion spawnRot = waterSpawnPoint != null ? waterSpawnPoint.rotation : transform.rotation;

            // 맑은 물 아이템 소환!
            GameObject spawnedWater = Instantiate(cleanWaterPrefab, spawnPos, spawnRot);
            Debug.Log($"📦 정수 아이템 생성 성공: {spawnedWater.name}");
        }
        else
        {
            Debug.LogError("❌ [설정 오류] 스폰할 'Clean Water Prefab'이 등록되어 있지 않습니다!");
        }*/

        // 정수 완료 후 다시 바닷물을 넣을 수 있도록 상태 해제
        isPurifying = false;
    }
}