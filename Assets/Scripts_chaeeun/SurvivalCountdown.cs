using UnityEngine;
using TMPro;
using System.Collections;

public class SurvivalCountdown : MonoBehaviour
{
    [Header("UI 설정")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("게임 내 하루가 현실 시간으로 몇 분인지 설정")]
    [SerializeField] private float realMinutesPerGameDay = 10f; 

    private float totalSurvivalTimeInSeconds;
    private bool isCountingDown = false;

    void Start()
    {
        float oneDayInSeconds = realMinutesPerGameDay * 60f;
        totalSurvivalTimeInSeconds = oneDayInSeconds * 3f; 

        if (countdownText != null) countdownText.gameObject.SetActive(false);

        // 완성 이벤트를 구독
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnTutorialCompleted += StartCountdown;
        }
    }

    // 오브젝트가 파괴되거나 꺼질 때 메모리 누수 방지를 위해 반드시 구독을 해제해 줍니다.
    void OnDestroy()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnTutorialCompleted -= StartCountdown;
        }
    }

    // 기존 StartCountdown 함수 로직은 그대로 유지됩니다.
    public void StartCountdown()
    {
        if (isCountingDown) return;
        
        if (countdownText != null) countdownText.gameObject.SetActive(true);

        isCountingDown = true;
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        while (totalSurvivalTimeInSeconds > 0)
        {
            totalSurvivalTimeInSeconds -= Time.deltaTime;

            // 남은 초를 "X일 XX시간 XX분" 형태로 파싱하여 UI에 업데이트
            UpdateCountdownUI();

            yield return null;
        }

        // 카운트다운이 0이 되었을 때 (생존 성공!)
        totalSurvivalTimeInSeconds = 0;
        UpdateCountdownUI();
        OnSurvivalSuccess();
    }

    private void UpdateCountdownUI()
    {
        if (countdownText == null) return;

        // 현실 초 단위를 게임 내 시간 비율로 환산
        float totalGameHoursLeft = (totalSurvivalTimeInSeconds / (realMinutesPerGameDay * 60f)) * 24f;

        int days = Mathf.FloorToInt(totalGameHoursLeft / 24f);
        int hours = Mathf.FloorToInt(totalGameHoursLeft % 24f);
        int minutes = Mathf.FloorToInt((totalGameHoursLeft * 60f) % 60);

        // 예: "구조까지 남은 시간: 3일 00시간 00분"
        countdownText.text = $"구조까지 남은 시간\n{days}일 {hours:D2}시간 {minutes:D2}분";
    }

    private void OnSurvivalSuccess()
    {
        isCountingDown = false;
        Debug.Log("🎉 3일 동안 생존에 성공했습니다! 탈출 엔딩 시나리오 발동!");
        // 여기에 게임 클리어 UI를 띄우거나 엔딩 크레딧으로 넘어가는 로직 연결
    }
}