using UnityEngine;
[RequireComponent(typeof(LineRenderer))]
public class FishingLineRenderer : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Transform rodTip;
    [SerializeField] private Transform fishingFloat;

    [Header("Line")]
    [SerializeField] private int resolution = 30;

    [Header("Sag")]
    [SerializeField] private float sagStrength = 0.35f;
    [SerializeField] private float maxSag = 3f;

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = resolution;
    }

    private void LateUpdate()
    {
        DrawLine();
    }

    void DrawLine()
    {
        Vector3 start = rodTip.position;
        Vector3 end = fishingFloat.position;

        float distance = Vector3.Distance(start, end);

        // 거리가 멀수록 조금 더 처짐
        float sag = Mathf.Min(distance * sagStrength, maxSag);

        // 중간점
        Vector3 control =
            (start + end) * 0.5f +
            Vector3.down * sag;

        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);

            line.SetPosition(i, Bezier(start, control, end, t));
        }
    }

    Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;

        return
            u * u * a +
            2f * u * t * b +
            t * t * c;
    }
}