using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomUI : MonoBehaviour
{
    [Header("입력 필드")]
    [SerializeField] private TMP_InputField sessionNameInput;     // 방 ID/이름 인풋필드
    [SerializeField] private TMP_InputField sessionPasswordInput; // 방 비밀번호 인풋필드

    [Header("시작/생성 버튼")]
    [SerializeField] private Button startBtn; // 비활성화/활성화할 Start 버튼

    public string sessionName => sessionNameInput != null ? sessionNameInput.text : "";
    public string sessionPassword => sessionPasswordInput != null ? sessionPasswordInput.text : "";

    private void Awake()
    {
        // 1. 인풋필드에 글자가 입력/삭제될 때마다 유효성 검사 연결
        if (sessionNameInput != null)
        {
            sessionNameInput.onValueChanged.AddListener(OnInputValueChanged);
        }

        if (sessionPasswordInput != null)
        {
            sessionPasswordInput.onValueChanged.AddListener(OnInputValueChanged);
        }
    }

    private void Start()
    {
        // 2. 시작 시 초기 상태 검사 (처음엔 비어있으므로 Start 버튼 비활성화)
        ValidateInputs();
    }

    private void OnDestroy()
    {
        // 이벤트 해제
        if (sessionNameInput != null) sessionNameInput.onValueChanged.RemoveListener(OnInputValueChanged);
        if (sessionPasswordInput != null) sessionPasswordInput.onValueChanged.RemoveListener(OnInputValueChanged);
    }

    private void OnInputValueChanged(string text)
    {
        ValidateInputs();
    }

    /// <summary>
    /// 방 ID와 비밀번호가 모두 채워져 있는지 검사하여 버튼 활성화 여부 결정
    /// </summary>
    private void ValidateInputs()
    {
        if (startBtn == null) return;

        bool isNameValid = sessionNameInput != null && !string.IsNullOrWhiteSpace(sessionNameInput.text);
        bool isPasswordValid = sessionPasswordInput != null && !string.IsNullOrWhiteSpace(sessionPasswordInput.text);

        // 둘 다 비어있지 않고 유효할 때만 버튼 활성화 (클릭 가능)
        startBtn.interactable = isNameValid && isPasswordValid;
    }
}