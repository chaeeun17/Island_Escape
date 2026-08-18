using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // URP를 사용할 경우 필수

public enum Stats
{
    HUNGER,
    THIRST,
    STAMINA,
}

public class WarningVignetteController : MonoBehaviour
{
    public Volume globalVolume; // Inspector에서 Global Volume 할당
    private Vignette vignette;
    private Coroutine increaseVignette;
    private Coroutine decreaseVignette;

    void Start()
    {
        // 1. Volume 컴포넌트가 할당되지 않았다면 자기 자신에서 찾기
        if (globalVolume == null)
            globalVolume = GetComponent<Volume>();

        // 2. Volume Profile에서 Vignette 효과 가져오기
        if (globalVolume.profile.TryGet(out vignette))
        {
            // 초기 상태 설정 (경고 꺼짐)
            vignette.intensity.value = 0f;
        }
        else
        {
            Debug.LogWarning("Volume Profile에 Vignette 효과가 없습니다!");
        }
    }

    // 경고 켜기 함수
    public void EnableWarningVignette(Stats stats)
    {
        Debug.Log("Enable Vignette 호출");
        if (vignette != null)
        {
            Color warningColor = Color.blue;
            switch (stats)
            {
                case Stats.HUNGER:
                    warningColor = Color.red;
                    break;
                case Stats.STAMINA:
                    warningColor = Color.purple;
                    break;
            }
            vignette.color.Override(warningColor);

            // 강도를 0.5로 설정 (0 ~ 1 사이 값)
            // vignette.intensity.Override(0.5f);
            if(increaseVignette == null)
            {
                StopAllCoroutines();
                increaseVignette = StartCoroutine(IncreaseVignetteLevel());
            }
        }
    }

    IEnumerator IncreaseVignetteLevel()
    {
        float intensity = (float)vignette.intensity;
        while (intensity < 0.6f)
        {
            vignette.intensity.Override(intensity += 0.01f);
            yield return null;
        }
        increaseVignette = null;
    }

    // 경고 끄기 함수
    public void DisableWarningVignette()
    {
        if (vignette != null)
        {
            // vignette.intensity.Override(0f);
            if (decreaseVignette == null)
            {
                StopAllCoroutines();
                decreaseVignette = StartCoroutine(DecreaseVignetteLevel());
            }
        }
    }

    private IEnumerator DecreaseVignetteLevel()
    {
        float intensity = (float)vignette.intensity;
        while (intensity > 0.01f)
        {
            vignette.intensity.Override(intensity -= 0.01f);
            yield return null;
        }
        decreaseVignette = null;
    }
}