using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SeaWaterScooper : MonoBehaviour
{
    [Header("스폰할 바닷물 양동이 프리팹")]
    [SerializeField] private GameObject seaWaterBucketPrefab;

    [Header("내 캐릭터의 양손 NearFarInteractor 직접 연결 (필수)")]
    [SerializeField] private NearFarInteractor leftNearFarInteractor;
    [SerializeField] private NearFarInteractor rightNearFarInteractor;

    // 하드웨어 그랩 버튼 다이렉트 감시
    private InputAction leftGripAction;
    private InputAction rightGripAction;

    // 현재 왼손/오른손 레이저가 바다를 가리키고 있는지 여부
    private bool isLeftHandHovering = false;
    private bool isRightHandHovering = false;

    // 바다 오브젝트에 붙어있는 XR Simple Interactable (레이저 조준 감지용)
    private XRSimpleInteractable seaInteractable;

    void Awake()
    {
        // 하드웨어 그랩 버튼 물리 신호 강제 획득
        leftGripAction = new InputAction("LeftGripDirect", binding: "<XRController>{LeftHand}/grip");
        rightGripAction = new InputAction("RightGripDirect", binding: "<XRController>{RightHand}/grip");

        // 바다 메쉬 자체에 XRSimpleInteractable을 붙여서 레이저가 조준할 수 있도록 준비합니다.
        seaInteractable = GetComponent<XRSimpleInteractable>();
        if (seaInteractable == null)
        {
            seaInteractable = gameObject.AddComponent<XRSimpleInteractable>();
        }

    }

    void OnEnable()
    {
        leftGripAction?.Enable();
        rightGripAction?.Enable();
    }

    void OnDisable()
    {
        leftGripAction?.Disable();
        rightGripAction?.Disable();
    }

    void Update()
    {
        // 1. NearFarInteractor가 바다의 Interactable을 조준하고 있는지 실시간 체크
        CheckLaserHover();

        // 2. 바다를 조준한 상태에서 그랩 버튼을 쥐었는지 판별
        bool leftGripPressed = isLeftHandHovering && leftGripAction != null && leftGripAction.ReadValue<float>() > 0.5f;
        bool rightGripPressed = isRightHandHovering && rightGripAction != null && rightGripAction.ReadValue<float>() > 0.5f;

        if (leftGripPressed || rightGripPressed)
        {
            // 연속 스폰 방지
            isLeftHandHovering = false;
            isRightHandHovering = false;

            // 왼손이 쥐었는지 오른손이 쥐었는지 판별
            NearFarInteractor targetInteractor = leftGripPressed ? leftNearFarInteractor : rightNearFarInteractor;

            if (targetInteractor == null) targetInteractor = Object.FindFirstObjectByType<NearFarInteractor>();
            if (targetInteractor == null) return;

            // ⚠️ 이미 손에 무언가(다른 물건 등)를 쥐고 있다면 바닷물을 뜨지 못하게 막습니다.
            if (targetInteractor.hasSelection)
            {
                Debug.LogWarning("⚠️ 손에 이미 물건을 쥐고 있어 바닷물을 풀 수 없습니다.");
                return;
            }

            Debug.LogWarning("🌊 [바닷물 획득] 바다 조준 중 그랩 입력 확인!");

            if (seaWaterBucketPrefab != null)
            {
                // 컨트롤러 위치에 바닷물 양동이 프리팹 생성
                Transform handTransform = targetInteractor.transform;
                GameObject spawnedBucket = Instantiate(seaWaterBucketPrefab, handTransform.position, handTransform.rotation);
                
                // 생성된 양동이 이름 설정 (정수기가 잘 인식하도록)
                spawnedBucket.name = "SeaWaterBucket";

                var grabInteractable = spawnedBucket.GetComponent<XRGrabInteractable>();
                if (grabInteractable != null)
                {
                    // XRI 매니저를 통해 내 손아귀에 챡! 강제 장착
                    Object.FindFirstObjectByType<XRInteractionManager>()?.SelectEnter(targetInteractor, (IXRSelectInteractable)grabInteractable);
                    Debug.LogWarning("★ 바닷물 양동이가 손에 쥐어졌습니다! ★");
                }
            }
        }
    }

    // 🌟 NearFarInteractor가 현재 바다(seaInteractable)를 가리키고 있는지 안전하게 확인합니다.
    private void CheckLaserHover()
    {
        isLeftHandHovering = false;
        isRightHandHovering = false;

        if (seaInteractable == null) return;

        // 1. 왼손 Interactor가 바다를 조준 목록에 두고 있는지 확인
        if (leftNearFarInteractor != null && leftNearFarInteractor.interactablesHovered.Count > 0) // 🌟 interactablesHovered 로 전면 수정!
        {
            foreach (var target in leftNearFarInteractor.interactablesHovered)
            {
                if (target as XRSimpleInteractable == seaInteractable)
                {
                    isLeftHandHovering = true;
                    break;
                }
            }
        }

        // 2. 오른손 Interactor가 바다를 조준 목록에 두고 있는지 확인
        if (rightNearFarInteractor != null && rightNearFarInteractor.interactablesHovered.Count > 0) // 🌟 interactablesHovered 로 전면 수정!
        {
            foreach (var target in rightNearFarInteractor.interactablesHovered)
            {
                if (target as XRSimpleInteractable == seaInteractable)
                {
                    isRightHandHovering = true;
                    break;
                }
            }
        }
    }
}