using UnityEngine;
using Fusion;

public class WatchController : MonoBehaviour
{
    [Header("패널 설정")]
    [SerializeField] private GameObject statPanel; // 예: 남은 시간 표시 패널
    [SerializeField] private GameObject countdownPanel; // 예: 플레이어 스탯/정보 패널
    
    private NetworkObject localNetworkObject;
    private bool isStatPanelActive = true;

    private void Awake()
    {
        // 내 손/캐릭터의 네트워크 권한 확인용
        localNetworkObject = GetComponentInParent<NetworkObject>();
    }

    private void Start()
    {
        // 초기 상태: Panel A만 활성화
        SetPanelState(true);
    }

    /// <summary>
    /// 🌟 버튼 이벤트(On Click / Select Entered)에 이 함수만 연결하세요!
    /// </summary>
    public void TogglePanels()
    {
        // 멀티플레이어 권한 체크: 내 캐릭터의 손목시계 버튼을 눌렀을 때만 작동
        if (localNetworkObject != null && !localNetworkObject.HasInputAuthority)
        {
            return;
        }

        isStatPanelActive = !isStatPanelActive;
        SetPanelState(isStatPanelActive);
    }

    private void SetPanelState(bool showA)
    {
        if (statPanel != null) statPanel.SetActive(showA);
        if (countdownPanel != null) countdownPanel.SetActive(!showA);
    }
}
