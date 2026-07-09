using UnityEngine;

public class FishingLine : MonoBehaviour
{
    [Header("낚싯줄 마디들을 순서대로 넣어주세요")]
    [SerializeField] private Transform[] lineSegments;

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();

        // Line Renderer의 정점 개수를 마디 개수와 맞춤
        lineRenderer.positionCount = lineSegments.Length;
    }

    void LateUpdate()
    {
        // 물리 연산이 끝난 후(LateUpdate) 정점 위치를 실시간 업데이트
        for (int i = 0; i < lineSegments.Length; i++)
        {
            if (lineSegments[i] != null)
            {
                lineRenderer.SetPosition(i, lineSegments[i].position);
            }
        }
    }
}