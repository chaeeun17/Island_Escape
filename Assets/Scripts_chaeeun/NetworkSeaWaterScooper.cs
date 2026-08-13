using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Fusion;

public class NetworkSeaWaterScooper : NetworkBehaviour
{
    [Header("스폰할 바닷물 양동이 네트워크 프리팹 (NetworkObject 붙은 Prefab)")]
    [SerializeField] private NetworkPrefabRef seaWaterBucketPrefab;

    private XRInteractionManager interactionManager;

    private void Start()
    {
        interactionManager = UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();
    }

    // 🌟 바다 오브젝트의 XR Simple Interactable -> Select Entered 이벤트에 연결!
    public void OnSeaSelected(SelectEnterEventArgs args)
    {
        // 1. 바다를 잡으려고 시도한 Interactor(손)의 네트워크 권한 확인
        NetworkObject handNetObj = args.interactorObject.transform.GetComponentInParent<NetworkObject>();
        if (handNetObj != null && !handNetObj.HasInputAuthority)
        {
            return;
        }

        IXRInteractor interactor = args.interactorObject;
        if (interactor == null) return;

        // 🌟 [수정] XRI 최신 인터페이스(IXRSelectInteractor) 캐스팅 및 안전한 선택 감지
        if (interactor is IXRSelectInteractor selectInteractor)
        {
            // 바다(seaInteractable) 외에 이미 손에 다른 물건을 쥐고 있는 경우
            if (selectInteractor.hasSelection && selectInteractor.interactablesSelected.Count > 1)
            {
                Debug.LogWarning("⚠️ 손에 이미 다른 물건을 쥐고 있어 바닷물을 풀 수 없습니다.");
                return;
            }
        }

        Debug.Log("🌊 [바닷물 획득] 멀티플레이 바닷물 양동이 소환 시도!");

        // 3. 서버/호스트에 네트워크 양동이 스폰 요청
        Transform handTransform = interactor.transform;
        
        if (Runner != null && Runner.IsRunning)
        {
            // 네트워크 상에 양동이 스폰 (Runner.Spawn 사용)
            NetworkObject spawnedBucketNetObj = Runner.Spawn(
                seaWaterBucketPrefab, 
                handTransform.position, 
                handTransform.rotation, 
                Object.InputAuthority // 내 캐릭터에게 권한 부여
            );

            if (spawnedBucketNetObj != null)
            {
                GameObject spawnedBucket = spawnedBucketNetObj.gameObject;
                //spawnedBucket.name = "SeaWaterBucket";

                // 4. 생성된 네트워크 양동이를 내 손에 잡기 (Local)
                var grabInteractable = spawnedBucket.GetComponent<XRGrabInteractable>();
                if (grabInteractable != null && interactionManager != null)
                {
                    interactionManager.SelectExit(interactor as IXRSelectInteractor, args.interactableObject);
                    interactionManager.SelectEnter(interactor as IXRSelectInteractor, (IXRSelectInteractable)grabInteractable);

                    Debug.Log("★ 네트워크 바닷물 양동이가 손에 쥐어졌습니다! ★");
                }
            }
        }
    }
}