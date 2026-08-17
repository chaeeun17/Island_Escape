using System.Collections;
using UnityEngine;

public class VRFadeController : MonoBehaviour
{
    [SerializeField] private Renderer fadeRenderer;
    [SerializeField] private float fadeDuration = 2f;

    private static readonly int ColorProp = Shader.PropertyToID("_Color");

    public IEnumerator FadeToTransparent()
    {
        Material mat = fadeRenderer.material;
        Color c = mat.GetColor(ColorProp);
        float t = 1f;

        while (t < fadeDuration)
        {
            t -= Time.deltaTime;
            c.a = Mathf.Clamp01(t / fadeDuration);
            mat.SetColor(ColorProp, c);
            yield return null;
        }

        c.a = 0f;
        mat.SetColor(ColorProp, c);
    }

    public IEnumerator FadeToWhite()
    {
        Material mat = fadeRenderer.material;
        Color c = mat.GetColor(ColorProp);
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / fadeDuration);
            mat.SetColor(ColorProp, c);
            yield return null;
        }

        c.a = 1f;
        mat.SetColor(ColorProp, c);
    }
}