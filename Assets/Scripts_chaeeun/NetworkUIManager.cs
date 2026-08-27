using Fusion; // 🌟 Fusion 라이브러리 추가
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class NetworkUIManager : NetworkBehaviour
{
    public Transform headTransform; // XR Origin의 Camera

    [Header("연결할 UI 패널 오브젝트")]
    [SerializeField] private GameObject itemInventoryPanel; // 아이템 창 (B 버튼)
    [SerializeField] private GameObject craftMenuPanel;     // 크래프트 창 (Y 버튼)
    [SerializeField] private Canvas notifyPanel;
    [SerializeField] private Canvas failPanel;
    [SerializeField] private Canvas successPanel;

    [Header("컨트롤러 입력 매핑 (Input Actions)")]
    [SerializeField] private InputActionReference bButtonAction; // 오른손 (B)
    [SerializeField] private InputActionReference yButtonAction; // 왼손 (Y)

    private NetworkObject parentPlayerNetObj;
    // 🌟 bButtonAction/yButtonAction은 XRI 공용 InputActionReference라, 이 프로세스(게임 클라이언트)
    //    전체에서 공유되는 액션이다. Enable()을 호출한 적 없는 인스턴스(=내 소유가 아닌 다른
    //    플레이어의 복제본)까지 Despawn 시점에 무조건 Disable()을 호출해버리면, 그 시점에 이
    //    액션을 정상적으로 쓰고 있던 "진짜 로컬 플레이어"의 B/Y 입력까지 전역으로 함께 꺼져버리고
    //    다시 켜주는 코드가 없어 세션 내내 복구되지 않는다. 실제로 RegisterInputEvents()를
    //    호출해서 Enable()한 적이 있는 경우에만 짝을 맞춰 해제하도록 막는다.
    private bool _inputEventsRegistered;

    // 🌟 Fusion NetworkObject 생성(Spawn) 시점에 입력 권한 체크
    public override void Spawned()
    {
        parentPlayerNetObj = GetComponentInParent<NetworkObject>();

        if (parentPlayerNetObj != null && !parentPlayerNetObj.HasInputAuthority)
        {
            enabled = false;
            return;
        }

        if (itemInventoryPanel != null) itemInventoryPanel.SetActive(false);
        if (craftMenuPanel != null) craftMenuPanel.SetActive(false);

        RegisterInputEvents(); // 🌟 여기서 등록
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        UnregisterInputEvents(); // 🌟 여기서 해제
    }

    private void RegisterInputEvents()
    {
        if (bButtonAction != null)
        {
            bButtonAction.action.Enable();
            bButtonAction.action.performed += OnBButtonPressed;
        }
        if (yButtonAction != null)
        {
            yButtonAction.action.Enable();
            yButtonAction.action.performed += OnYButtonPressed;
        }

        _inputEventsRegistered = true;
    }

    private void UnregisterInputEvents()
    {
        // 🌟 Enable()한 적 없는(=내 소유가 아니었던) 인스턴스면 아무것도 하지 않는다.
        //    그렇지 않으면 이 프로세스에서 공유 중인 액션을 다른 누군가가 정상적으로 쓰고 있어도
        //    같이 Disable() 되어버린다.
        if (!_inputEventsRegistered) return;
        _inputEventsRegistered = false;

        if (bButtonAction != null)
        {
            bButtonAction.action.performed -= OnBButtonPressed;
            bButtonAction.action.Disable();
        }
        if (yButtonAction != null)
        {
            yButtonAction.action.performed -= OnYButtonPressed;
            yButtonAction.action.Disable();
        }
    }

    // OnEnable/OnDisable은 아예 제거하거나 비워두기

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
    }

    // 외부 스크립트에서 인벤토리/크래프트 UI가 열려있는지 확인할 수 있는 함수
    public bool IsAnyUIOpen()
    {
        bool isInvOpen = itemInventoryPanel != null && itemInventoryPanel.activeSelf;// itemInventoryPanel.activeSelf;
        bool isCraftOpen = craftMenuPanel != null && craftMenuPanel.activeSelf;
        return isInvOpen || isCraftOpen;
    }


    // =======================================
    public async void EndSessionAndReturnToLobby()
    {
        if (Runner != null)
        {
            await Runner.Shutdown();
        }

        // 세션 종료 후 각자 로컬에서 로비 씬으로 이동 (네트워크와 무관하게 개별 실행)
        SceneManager.LoadScene("StartScene 1");
    }

    public void ShowApproachNotice()
    {
        notifyPanel.enabled = true;
    }
    public void CloseApproachNotice()
    {
        notifyPanel.enabled = false;
    }
    public void ShowFailUI()
    {
        failPanel.enabled = true;
    }

    public void ShowSuccessUI()
    {
        successPanel.enabled = true;
    }
}