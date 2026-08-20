using System.Collections;
using Fusion;
using UnityEngine;

public class VRFadeController : NetworkBehaviour
{
    [SerializeField] private Renderer fadeRenderer;
    [SerializeField] private float fadeDuration = 2f;
    private static readonly int ColorProp = Shader.PropertyToID("_BaseColor");

    private Material matInstance;
    private Coroutine currentFade;

    public override void Spawned()
    {
        var rootNO = GetComponentInParent<NetworkObject>(true);

        if (rootNO == null || !rootNO.HasInputAuthority)
        {
            fadeRenderer.enabled = false;
            enabled = false;
            return;
        }

        matInstance = fadeRenderer.material;
    }

    public void StartFadeToTransparent()
    {
        // if (!Object.HasInputAuthority) return; // 안전장치
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeToTransparent());
    }

    public void StartFadeToWhite()
    {
        // if (!Object.HasInputAuthority) return;
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeToWhite());
    }

    private IEnumerator FadeToTransparent()
    {
        Color c = matInstance.GetColor(ColorProp);

        if (c.a <= 0.01f) yield break;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = 1f - Mathf.Clamp01(t / fadeDuration);
            matInstance.SetColor(ColorProp, c);
            yield return null;
        }
        c.a = 0f;
        matInstance.SetColor(ColorProp, c);
    }

    private IEnumerator FadeToWhite()
    {
        Color c = matInstance.GetColor(ColorProp);

        if (c.a >= 0.99f) yield break;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / fadeDuration);
            matInstance.SetColor(ColorProp, c);
            yield return null;
        }
        c.a = 1f;
        matInstance.SetColor(ColorProp, c);
    }
}