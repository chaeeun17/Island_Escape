using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager2 : MonoBehaviour
{
    public static UIManager2 Instance { get; private set; }

    [Header("연결할 UI 패널 오브젝트")]
    [SerializeField] private GameObject itemInventoryPanel; // 아이템 창 (B 버튼)
    [SerializeField] private GameObject craftMenuPanel;     // 크래프트 창 (Y 버튼)

    [Header("컨트롤러 입력 매핑 (Input Actions)")]
    [SerializeField] private InputActionReference bButtonAction; // 오른손 대장 서브 (B)
    [SerializeField] private InputActionReference yButtonAction; // 왼손 대장 서브 (Y)

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()
    {
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
            yButtonAction.action.Disable();
        }
    }

    void Start()
    {
        // 게임 시작 시 두 창은 당연히 닫아둡니다.
        if (itemInventoryPanel != null) itemInventoryPanel.SetActive(false);
        if (craftMenuPanel != null) craftMenuPanel.SetActive(false);
    }

    // 🎒 B 버튼 눌렸을 때: 아이템 창 토글
    private void OnBButtonPressed(InputAction.CallbackContext context)
    {
        if (itemInventoryPanel == null) return;

        // 크래프트 창이 켜져있다면 가독성을 위해 먼저 닫아주는 센스!
        if (craftMenuPanel != null && craftMenuPanel.activeSelf) craftMenuPanel.SetActive(false);

        // 현재 상태의 반대로 토글 (켜져있으면 끄고, 꺼져있으면 켜기)
        bool isCurrentlyActive = itemInventoryPanel.activeSelf;
        itemInventoryPanel.SetActive(!isCurrentlyActive);

        Debug.Log($"🎒 아이템 인벤토리 창 토글: {!isCurrentlyActive}");
    }

    // 🛠️ Y 버튼 눌렸을 때: 크래프트 창 토글
    private void OnYButtonPressed(InputAction.CallbackContext context)
    {
        if (craftMenuPanel == null) return;

        // 아이템 창이 켜져있다면 먼저 닫아줍니다.
        if (itemInventoryPanel != null && itemInventoryPanel.activeSelf) itemInventoryPanel.SetActive(false);

        bool isCurrentlyActive = craftMenuPanel.activeSelf;
        craftMenuPanel.SetActive(!isCurrentlyActive);

        Debug.Log($"🛠️ 크래프트 제작 창 토글: {!isCurrentlyActive}");
    }
}