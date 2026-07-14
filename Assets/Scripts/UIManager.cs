using UnityEngine;
using UnityEngine.InputSystem; // 1. Input System 네임스페이스 필수 추가

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject inventoryPanel;
    public GameObject craftingPanel;

    [Header("VR Input Actions (인스펙터에서 할당)")]
    // 이 변수들에 인스펙터에서 XRI Default Input Actions의 특정 버튼 액션을 매핑합니다.
    public InputActionReference inventoryAction;
    public InputActionReference craftingAction;

    private void OnEnable()
    {
        // 스크립트 활성화 시 액션 감지 시작
        if (inventoryAction != null) inventoryAction.action.Enable();
        if (craftingAction != null) craftingAction.action.Enable();
    }

    private void OnDisable()
    {
        // 스크립트 비활성화 시 액션 감지 중지
        if (inventoryAction != null) inventoryAction.action.Disable();
        if (craftingAction != null) craftingAction.action.Disable();
    }

    private void Update()
    {
        // 이번 프레임에 인벤토리 버튼이 눌렸는지 확인
        if (inventoryAction != null && inventoryAction.action.WasPressedThisFrame())
        {
            ToggleInventory();
        }

        // 이번 프레임에 제작대 버튼이 눌렸는지 확인
        if (craftingAction != null && craftingAction.action.WasPressedThisFrame())
        {
            ToggleCrafting();
        }
    }

    public void ToggleInventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }

    public void ToggleCrafting()
    {
        if (craftingPanel != null)
            craftingPanel.SetActive(!craftingPanel.activeSelf);
    }
}