using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Fusion;

// 🌟 NetworkBehaviour 상속 및 IInteractable 구현
public class NetworkWaterPurifier : NetworkBehaviour, IInteractable
{
    [Header("정수 소요 시간 (초)")]
    [SerializeField] private float purifyingTime = 3.0f;

    [Header("생성될 맑은 물 네트워크 프리팹")]
    [SerializeField] private NetworkPrefabRef cleanWaterPrefab;

    [Header("맑은 물 스폰 위치")]
    [SerializeField] private Transform waterSpawnPoint;

    // 🌟 정수 진행 중 여부를 네트워크 동기화 ([Networked])
    [Networked] private NetworkBool isPurifying { get; set; }

    public string GetInteractText()
    {
        if (isPurifying)
        {
            return "정수 진행 중...";
        }
        else
        {
            return "정수하기 (바닷물 양동이 필요)";
        }
    }

    public void Interact()
    {
        // 이미 누군가 정수 중이라면 중복 실행 방지
        if (isPurifying) return;

        // 1. 내 캐릭터의 로컬 손(Interactor) 및 손에 든 아이템 탐색
        IXRSelectInteractor currentInteractor = GetLocalHandInteractor();
        if (currentInteractor == null) return;

        // 2. 손에 무언가를 잡고 있는지 확인
        if (currentInteractor.hasSelection)
        {
            // 첫 번째 잡고 있는 오브젝트 가져오기
            var selectedInteractable = currentInteractor.interactablesSelected[0];
            GameObject heldObject = selectedInteractable.transform.gameObject;

            // 손에 든 물건이 "SeaWaterBucket"인지 확인 (Clone 붙은 이름 대응을 위해 Contains 써주는 게 안전)
            if (heldObject.name.Contains("SeaWaterBucket"))
            {
                NetworkObject bucketNetObj = heldObject.GetComponentInParent<NetworkObject>();
                
                if (bucketNetObj != null)
                {
                    // RPC로 정수기 가동 및 양동이 제거 요청!
                    RPC_RequestPurifying(bucketNetObj);
                }
            }
            else
            {
                Debug.LogWarning("❌ [정수기] 바닷물 양동이가 아닙니다!");
            }
        }
        else
        {
            Debug.LogWarning("❌ [정수기] 맨손입니다. 바닷물 양동이를 손에 쥐고 상호작용하세요.");
        }
    }

    // 🌟 1. 클라이언트 -> 호스트 : "이 바닷물 양동이 넣고 정수기 돌릴게!"
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestPurifying(NetworkObject bucketNetObj)
    {
        if (isPurifying) return;

        isPurifying = true; // 정수 중 상태 ON (네트워크 동기화)

        // 사용한 바닷물 양동이는 포톤 네트워크 상에서 파괴(Despawn)
        if (bucketNetObj != null && Runner.IsRunning)
        {
            Runner.Despawn(bucketNetObj);
        }

        // 전체 플레이어 화면에 정수기 작동 시퀀스 실행
        RPC_BroadcastPurifySequence();
    }

    // 🌟 2. 호스트 -> 모두 : 정수기 뽀글뽀글 카운트다운 시작
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastPurifySequence()
    {
        StartCoroutine(PurifyingProcessRoutine());
    }

    private IEnumerator PurifyingProcessRoutine()
    {
        Debug.LogWarning($"🔥 [정수 시작] {purifyingTime}초간 정수가 진행됩니다... 뽀글뽀글...");

        // ==========================================
        // (선택) 정수기 사운드나 연기/물방울 이펙트 재생 코드를 여기에 추가!
        // ==========================================

        yield return new WaitForSeconds(purifyingTime);

        Debug.LogWarning("✨ [정수 완료!] 맑은 물이 생성되었습니다.");

        // 🌟 호스트(StateAuthority)만 맑은 물 네트워크 프리팹을 스폰합니다!
        if (HasStateAuthority)
        {
            Vector3 spawnPos = waterSpawnPoint != null ? waterSpawnPoint.position : transform.position;
            Quaternion spawnRot = waterSpawnPoint != null ? waterSpawnPoint.rotation : transform.rotation;

            if (cleanWaterPrefab.IsValid)
            {
                NetworkObject spawnedWater = Runner.Spawn(cleanWaterPrefab, spawnPos, spawnRot);
                if (spawnedWater != null)
                {
                    spawnedWater.name = "CleanWater";
                    Debug.Log($"📦 맑은 물 생성 성공: {spawnedWater.name}");
                }
            }
            else
            {
                Debug.LogError("❌ [설정 오류] cleanWaterPrefab 이 인스펙터에 지정되지 않았습니다!");
            }

            isPurifying = false; // 정수 상태 해제
        }
    }

    // 🔍 내 플레이어의 손(XR Interactor)을 안전하게 찾는 도우미 함수
    private IXRSelectInteractor GetLocalHandInteractor()
    {
        // 씬 내의 모든 Interactor 중 내 네트워크 권한(HasInputAuthority)을 가진 손을 찾음
        foreach (var interactor in UnityEngine.Object.FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None))
        {
            NetworkObject netObj = interactor.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.HasInputAuthority && interactor is IXRSelectInteractor selectInteractor)
            {
                if (selectInteractor.hasSelection)
                {
                    return selectInteractor; // 물건을 잡고 있는 손을 우선 반환
                }
            }
        }

        // 혹시 물건을 안 잡았더라도 내 손 반환
        foreach (var interactor in UnityEngine.Object.FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None))
        {
            NetworkObject netObj = interactor.GetComponentInParent<NetworkObject>();
            if (netObj != null && netObj.HasInputAuthority && interactor is IXRSelectInteractor selectInteractor)
            {
                return selectInteractor;
            }
        }

        return null;
    }
}