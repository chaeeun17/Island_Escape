using UnityEngine;
using TMPro;
using Fusion; // 🌟 Photon Fusion 라이브러리

public class NetworkCountdown : NetworkBehaviour
{
    [Header("Helicopter")]
    [SerializeField] private HelicopterController helicopterController;

    [Header("UI 설정 (인스펙터에서 직접 연결)")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("게임 내 하루가 현실 시간으로 몇 분인지 설정")]
    [SerializeField] private float realMinutesPerGameDay = 10f; 

    // 🌟 [Networked]: 호스트에서 관리하며 모든 클라이언트에 동기화되는 남은 시간(초) 및 상태
    [Networked] private float totalSurvivalTimeInSeconds { get; set; }
    [Networked] private NetworkBool isCountingDown { get; set; }

    public override void Spawned()
    {
        helicopterController = GameObject.Find("Helicopter").GetComponent<HelicopterController>();

        // 🌟 [핵심 1] 시작하자마자 UI를 끄지 않고, 켜둔 상태로 NO SIGNAL을 출력합니다.
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = GetRemainingTimeString(); // "NO SIGNAL"로 세팅
        }

        // 호스트(StateAuthority)만 초기 타이머 세팅 (3일치 시간 환산)
        if (HasStateAuthority)
        {
            float oneDayInSeconds = realMinutesPerGameDay * 60f;
            totalSurvivalTimeInSeconds = oneDayInSeconds * 3f;
            //isCountingDown = false;
            isCountingDown = true;

        }

        SubscribeEvents();
    }

    private void Start()
    {
        SubscribeEvents();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (SOSManager.Instance != null)
        {
            SOSManager.Instance.OnTutorialCompleted -= StartCountdown;
            SOSManager.Instance.OnTutorialCompleted += StartCountdown;
        }

        if (NetworkTutorialManager.Instance != null)
        {
            NetworkTutorialManager.Instance.OnTutorialCompleted -= StartCountdown;
            NetworkTutorialManager.Instance.OnTutorialCompleted += StartCountdown;
        }
    }

    private void UnsubscribeEvents()
    {
        if (SOSManager.Instance != null)
        {
            SOSManager.Instance.OnTutorialCompleted -= StartCountdown;
        }

        if (NetworkTutorialManager.Instance != null)
        {
            NetworkTutorialManager.Instance.OnTutorialCompleted -= StartCountdown;
        }
    }

    // 🌟 SOS/튜토리얼 완주 시 호출되는 시작 함수
    public void StartCountdown()
    {
        // 🌟 [핵심 2] 클라이언트가 이벤트를 받아도 호스트에게 상태 변경을 요청함
        RPC_RequestStartCountdown();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestStartCountdown()
    {
        if (isCountingDown) return;

        isCountingDown = true;
        RPC_BroadcastCountdownStarted();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastCountdownStarted()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }
        Debug.Log("🚀 카운트다운이 시작되었습니다!");
    }

    // 🌟 Fusion 네트워크 틱 갱신 (호스트에서 실시간 시간 차감)
    public override void FixedUpdateNetwork()
    {
        if (!isCountingDown) return;

        if (HasStateAuthority)
        {
            if (totalSurvivalTimeInSeconds > 0)
            {
                totalSurvivalTimeInSeconds -= Runner.DeltaTime;

                if (totalSurvivalTimeInSeconds <= 0)
                {
                    totalSurvivalTimeInSeconds = 0;
                    isCountingDown = false;

                    // 🌟 헬기 호출은 StateAuthority에서 직접 (CallHelicopter 내부에서도 
                    //    HasStateAuthority 체크하지만, 이중 방어 차원에서 여기도 안전)
                    if (helicopterController != null)
                    {
                        helicopterController.CallHelicopter();
                    }

                    // UI 등 시각 방송은 그대로 RPC 유지
                    RPC_TriggerSurvivalSuccessAll();
                }
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerSurvivalSuccessAll()
    {
        if (countdownText != null)
        {
            countdownText.text = GetRemainingTimeString();
        }

        Debug.Log("🎉 3일 생존 성공!");
    }

    // 🌟 [핵심 3] 조건문 제거: 시작 전/진행 중/종료 후 모두 실시간 텍스트 갱신
    public override void Render()
    {
        if (countdownText != null)
        {
            countdownText.text = GetRemainingTimeString();
        }
    }

    // 상태별 텍스트 반환
    public string GetRemainingTimeString()
    {
        // 1. 카운트다운 시작 전
        if (!isCountingDown && totalSurvivalTimeInSeconds > 0) 
        {
            return "NO SIGNAL";
        }

        // 2. 카운트다운 완료 후
        if (!isCountingDown && totalSurvivalTimeInSeconds == 0) 
        {
            return "RESCUED!\n00:00";
        }

        // 3. 카운트다운 진행 중
        float totalGameHoursLeft = (totalSurvivalTimeInSeconds / (realMinutesPerGameDay * 60f)) * 24f;

        int days = Mathf.FloorToInt(totalGameHoursLeft / 24f);
        int hours = Mathf.FloorToInt(totalGameHoursLeft % 24f);
        int minutes = Mathf.FloorToInt((totalGameHoursLeft * 60f) % 60);

        return $"{days} days\n{hours:D2}:{minutes:D2}";
    }
}