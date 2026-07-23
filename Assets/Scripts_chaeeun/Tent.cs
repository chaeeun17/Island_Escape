using UnityEngine;
using System.Collections;

public class Tent : MonoBehaviour, IInteractable
{
    // 현재 플레이어가 잠든 상태인지를 기억하는 변수
    private bool isPlayerSleeping = false;

    [Header("페이드 연출 시간 설정")]
    [SerializeField] private float fadeDuration = 1.0f; // 화면이 닫히고 열리는 시간(초)
    [SerializeField] private float sleepDelay = 5.0f;    // 암전된 상태로 유지될 시간(자는 시간)

    public string GetInteractText()
    {
        // 현재 상태에 따라 X버튼 UI에 뜰 텍스트를 다르게 넘겨줍니다.
        if (isPlayerSleeping)
        {
            return "잠에서 깨기";
        }
        else
        {
            return "잠자기";
        }
    }

    public void Interact()
    {
        if (isPlayerSleeping) return;

        // 수면 코루틴 시작!
        StartCoroutine(SleepRoutine());
    }

    private IEnumerator SleepRoutine()
    {
        isPlayerSleeping = true;
        Debug.Log("[텐트] 수면 시퀀스 시작...");

        // 화면 암전 (Fade Out) 시작하고 완전히 까매질 때까지 대기
        if (FadeManager.Instance != null)
        {
            yield return FadeManager.Instance.FadeOut(fadeDuration);
        }

        // 완전히 암전된 상태에서 스탯 정산 (플레이어 눈에는 안 보임)
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.RestoreStamina(10);
        }

        // 까만 화면 상태로 지정한 시간만큼 대기 (쿨쿨 자는 느낌 연출)
        yield return new WaitForSeconds(sleepDelay);

        // 잠에서 깨어나며 화면 밝아지기 (Fade In)
        if (FadeManager.Instance != null)
        {
            yield return FadeManager.Instance.FadeIn(fadeDuration);
        }

        isPlayerSleeping = false;
        Destroy(gameObject);
    }
}