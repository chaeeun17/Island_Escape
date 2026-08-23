using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

[RequireComponent(typeof(TMP_InputField))]
public class VRKeyboard : MonoBehaviour
{
    [Header("씬에 배치한 XRKeyboard 오브젝트 연결")]
    [SerializeField] private XRKeyboard keyboard;

    private TMP_InputField _inputField;

    private void Awake()
    {
        _inputField = GetComponent<TMP_InputField>();

        // 키보드를 직접 연결하지 않았을 경우 씬에서 자동 탐색
        if (keyboard == null)
        {
            keyboard = UnityEngine.Object.FindFirstObjectByType<XRKeyboard>(FindObjectsInactive.Include);
        }

        // TMP_InputField의 자체 select 이벤트 등록
        _inputField.onSelect.AddListener(OnFieldSelected);
    }

    private void OnDestroy()
    {
        if (_inputField != null)
        {
            _inputField.onSelect.RemoveListener(OnFieldSelected);
        }
    }

    private void OnFieldSelected(string currentText)
    {
        if (keyboard != null && !keyboard.gameObject.activeSelf)
        {
            // 키보드 활성화
            keyboard.gameObject.SetActive(true);
        }
    }
}