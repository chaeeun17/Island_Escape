using UnityEngine;
using UnityEngine.InputSystem;
using Fusion; // 🌟 Fusion 라이브러리 추가

public class NetworkUIManager : NetworkBehaviour
{
    public static NetworkUIManager Instance { get; private set; }

    public Transform headTransform; // XR Origin의 Camera

    [Header("연결할 UI 패널 오브젝트")]
    [SerializeField] private GameObject itemInventoryPanel; // 아이템 창 (B 버튼)
    [SerializeField] private GameObject craftMenuPanel;     // 크래프트 창 (Y 버튼)

    [Header("컨트롤러 입력 매핑 (Input Actions)")]
    [SerializeField] private InputActionReference bButtonAction; // 오른손 (B)
    [SerializeField] private InputActionReference yButtonAction; // 왼손 (Y)

    private NetworkObject parentPlayerNetObj;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 🌟 Fusion NetworkObject 생성(Spawn) 시점에 입력 권한 체크
    public override void Spawned()
    {
        // 최상위 플레이어의 NetworkObject 탐색
        parentPlayerNetObj = GetComponentInParent<NetworkObject>();

        // 내 플레이어(HasInputAuthority)가 아닌 멀티 상대방 캐릭터라면 UI 수신 스크립트 비활성화
        if (parentPlayerNetObj != null && !parentPlayerNetObj.HasInputAuthority)
        {
            enabled = false;
            return;
        }

        // 초기 시작 시 UI 창 끄기
        if (itemInventoryPanel != null) itemInventoryPanel.SetActive(false);
        if (craftMenuPanel != null) craftMenuPanel.SetActive(false);
    }

    void OnEnable()
    {
        // 🌟 내 플레이어 권한이 있을 때만 버튼 이벤트 바인딩
        if (parentPlayerNetObj != null && !parentPlayerNetObj.HasInputAuthority) return;

        // B 버튼 이벤트 연결
        if (bButtonAction != null)
        {
            bButtonAction.action.Enable();
            bButtonAction.action.performed += OnBButtonPressed;
        }

        // Y 버튼 이벤트 연결
        if (yButtonAction != null)
        {
            yButtonAction.action.Enable();
            yButtonAction.action.performed += OnYButtonPressed;
        }
    }

    void OnDisable()
    {
        if (bButtonAction != null)
        {
            bButtonAction.action.performed -= OnBButtonPressed;
            bButtonAction.action.Disable();
        }

        if (yButtonAction != null)
        {
            yButtonAction.action.performed -= OnYButtonPressed;
        }
    }

    // 🎒 B 버튼 눌렸을 때: 아이템 창 토글
    private void OnBButtonPressed(InputAction.CallbackContext context)
    {
        if (!itemInventoryPanel.activeSelf)
        {
            Vector3 forward = headTransform.forward;
            forward.y = 0; // 위아래 기울임 무시 (수평 배치)
            forward.Normalize();

            Vector3 spawnPos = headTransform.position + forward * 1.5f;
            spawnPos.y = headTransform.position.y - 0.1f; // 살짝 아래

            itemInventoryPanel.transform.position = spawnPos;
            itemInventoryPanel.transform.rotation = Quaternion.LookRotation(
                itemInventoryPanel.transform.position - headTransform.position);
        }

        itemInventoryPanel.SetActive(!itemInventoryPanel.activeSelf);
        /*
        if (itemInventoryPanel == null) return;

        // 크래프트 창이 켜져있다면 먼저 닫기
        if (craftMenuPanel != null && craftMenuPanel.activeSelf) craftMenuPanel.SetActive(false);

        bool isCurrentlyActive = itemInventoryPanel.activeSelf;
        itemInventoryPanel.SetActive(!isCurrentlyActive);

        Debug.Log($"🎒 [내 캐릭터] 아이템 인벤토리 창 토글: {!isCurrentlyActive}");
        */
    }

    // 🛠️ Y 버튼 눌렸을 때: 크래프트 창 토글
    private void OnYButtonPressed(InputAction.CallbackContext context)
    {
        if (!craftMenuPanel.activeSelf)
        {
            Vector3 forward = headTransform.forward;
            forward.y = 0; // 위아래 기울임 무시 (수평 배치)
            forward.Normalize();

            Vector3 spawnPos = headTransform.position + forward * 1.5f;
            spawnPos.y = headTransform.position.y - 0.1f; // 살짝 아래

            craftMenuPanel.transform.position = spawnPos;
            craftMenuPanel.transform.rotation = Quaternion.LookRotation(
                craftMenuPanel.transform.position - headTransform.position);
        }

        craftMenuPanel.SetActive(!craftMenuPanel.activeSelf);
        /*
        if (craftMenuPanel == null) return;

        // 아이템 창이 켜져있다면 먼저 닫기
        if (itemInventoryPanel != null && itemInventoryPanel.activeSelf) itemInventoryPanel.SetActive(false);

        bool isCurrentlyActive = craftMenuPanel.activeSelf;
        craftMenuPanel.SetActive(!isCurrentlyActive);

        Debug.Log($"🛠️ [내 캐릭터] 크래프트 제작 창 토글: {!isCurrentlyActive}");
        */
    }

    // 외부 스크립트에서 인벤토리/크래프트 UI가 열려있는지 확인할 수 있는 함수
    public bool IsAnyUIOpen()
    {
        bool isInvOpen = itemInventoryPanel != null && itemInventoryPanel.activeSelf;
        bool isCraftOpen = craftMenuPanel != null && craftMenuPanel.activeSelf;
        return isInvOpen || isCraftOpen;
    }
}