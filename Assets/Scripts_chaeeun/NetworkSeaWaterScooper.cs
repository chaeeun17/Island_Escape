using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Fusion;
using System.Collections;

public class NetworkSeaWaterScooper : NetworkBehaviour
{
    [Header("스폰할 바닷물 양동이 네트워크 프리팹 (NetworkObject 붙은 Prefab)")]
    [SerializeField] private NetworkPrefabRef seaWaterBucketPrefab;

    private XRInteractionManager interactionManager;
    private XRSimpleInteractable simpleInteractable;
    private IXRSelectInteractor lastInteractor;

    private void Awake()
    {
        simpleInteractable = GetComponent<XRSimpleInteractable>();
        if (simpleInteractable != null)
        {
            Collider[] childColliders = GetComponentsInChildren<Collider>();
            simpleInteractable.colliders.Clear();
            foreach (var col in childColliders)
            {
                simpleInteractable.colliders.Add(col);
            }
        }
    }

    private void Start()
    {
        interactionManager = UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();
    }

    public void OnSeaSelected(SelectEnterEventArgs args)
    {
        // 1. 내 로컬 캐릭터인지 검증
        NetworkObject handNetObj = args.interactorObject.transform.GetComponentInParent<NetworkObject>();
        if (handNetObj != null && !handNetObj.HasInputAuthority)
        {
            return;
        }

        if (args.interactorObject is not IXRSelectInteractor selectInteractor) return;

        // 이미 손에 다른 것을 쥐고 있다면 스킵
        if (selectInteractor.hasSelection && selectInteractor.interactablesSelected.Count > 1)
        {
            Debug.LogWarning("⚠️ 손에 이미 다른 물건을 쥐고 있어 바닷물을 풀 수 없습니다.");
            return;
        }

        lastInteractor = selectInteractor;

        // 2. 컨트롤러의 Attach Transform 기준 위치/회전 계산
        Transform handAttach = selectInteractor.GetAttachTransform(args.interactableObject);
        Vector3 spawnPos = handAttach != null ? handAttach.position : selectInteractor.transform.position;
        Quaternion spawnRot = handAttach != null ? handAttach.rotation : selectInteractor.transform.rotation;

        Debug.Log("🌊 [바닷물 획득] 바닷물 양동이 스폰 및 손 부착 요청!");

        // 3. 호스트에 스폰 요청
        if (Runner != null && Runner.IsRunning)
        {
            RPC_RequestSpawnBucket(
                Runner.LocalPlayer, 
                spawnPos, 
                spawnRot
            );
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestSpawnBucket(PlayerRef requester, Vector3 spawnPos, Quaternion spawnRot)
    {
        if (Runner != null && Runner.IsRunning)
        {
            NetworkObject spawnedBucketNetObj = Runner.Spawn(
                seaWaterBucketPrefab,
                spawnPos,
                spawnRot,
                requester
            );

            // 4. 스폰 완료 후 클라이언트에게 손으로 잡으라는 RPC 브로드캐스트
            if (spawnedBucketNetObj != null)
            {
                RPC_AttachToHand(requester, spawnedBucketNetObj.Id);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AttachToHand(PlayerRef requester, NetworkId bucketNetId)
    {
        // 스폰을 요청했던 플레이어 본인의 기기에서만 실행
        if (Runner.LocalPlayer == requester)
        {
            StartCoroutine(AttachRoutine(bucketNetId));
        }
    }

    private IEnumerator AttachRoutine(NetworkId bucketNetId)
    {
        // Fusion 네트워크 오브젝트 렌더링 및 XRI 컴포넌트 준비 대기
        NetworkObject bucketNetObj = null;
        float timeout = 2.0f;
        float elapsed = 0f;

        while (bucketNetObj == null && elapsed < timeout)
        {
            Runner.TryFindObject(bucketNetId, out bucketNetObj);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (bucketNetObj == null || lastInteractor == null) yield break;

        XRGrabInteractable grabInteractable = bucketNetObj.GetComponentInChildren<XRGrabInteractable>();
        if (grabInteractable != null && interactionManager != null)
        {
            // 바다 선택 상태를 해제하고 양동이를 손에 즉시 강제 쥐어줌
            interactionManager.SelectExit(lastInteractor, simpleInteractable);
            interactionManager.SelectEnter(lastInteractor, (IXRSelectInteractable)grabInteractable);
            Debug.Log("★ [완료] 양동이가 손에 자동으로 쥐어졌습니다!");
        }
    }
}