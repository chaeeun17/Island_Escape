using UnityEngine;

public class HelicopterPath : MonoBehaviour
{
    [Header("Path Settings")]
    [SerializeField] private Color pathColor = Color.yellow;
    [SerializeField] private float pointRadius = 1.0f;

    // 씬 뷰(Scene View)에서 경로를 그리는 기즈모 함수
    private void OnDrawGizmos()
    {
        Gizmos.color = pathColor;

        // 자식 오브젝트(Waypoint 지점들) 순회
        int childCount = transform.childCount;
        if (childCount < 2) return;

        for (int i = 0; i < childCount; i++)
        {
            Transform currentPoint = transform.GetChild(i);

            // 각 지점에 구체 표시
            Gizmos.DrawWireSphere(currentPoint.position, pointRadius);

            // 다음 지점이 있다면 선으로 연결
            if (i < childCount - 1)
            {
                Transform nextPoint = transform.GetChild(i + 1);
                Gizmos.DrawLine(currentPoint.position, nextPoint.position);
            }
        }
    }
}