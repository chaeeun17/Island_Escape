using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    [Header("페이드용 UI Image (검은색 패널)")]
    [SerializeField] private Image fadeImage;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 화면이 어두워지는 함수 (Fade Out)
    public Coroutine FadeOut(float duration)
    {
        return StartCoroutine(FadeRoutine(0f, 1f, duration));
    }

    // 화면이 밝아지는 함수 (Fade In)
    public Coroutine FadeIn(float duration)
    {
        return StartCoroutine(FadeRoutine(1f, 0f, duration));
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration)
    {
        if (fadeImage == null) yield break;

        // 페이드 시작 시 UI 활성화
        fadeImage.gameObject.SetActive(true);

        float timer = 0f;
        Color color = fadeImage.color;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            // 부드러운 보간법(Lerp)으로 알파값 조절
            color.a = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;

        // 완전히 밝아졌다면 UI를 꺼서 시야를 방해하지 않게 함
        if (endAlpha == 0f)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }
}