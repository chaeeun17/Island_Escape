using UnityEngine;

public class OceanSound : MonoBehaviour
{
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private float maxVolume = 1f;
    [SerializeField] private AudioSource audioSource;
    private GameObject oceanSound;
    private GameObject player;

    private void Awake()
    {
        oceanSound = GameObject.Find("OceanSound");
    }

    void Update()
    {
        SetVolumeWithDistance();
    }

    private void SetVolumeWithDistance()
    {
        if(player == null)
        {
            player = GameObject.FindGameObjectWithTag("PlayerRoot");
        }

        float minDistance = GetMinDistance();
        if (minDistance < maxDistance)
        {
            audioSource.volume = (maxDistance - minDistance) / maxDistance;
        }
        else
        {
            audioSource.volume = 0;
        }

    }

    float GetMinDistance()
    {
        Collider colA = player.GetComponent<CharacterController>();
        BoxCollider[] collidersB = oceanSound.GetComponentsInChildren<BoxCollider>();

        float minDistance = float.MaxValue;
        foreach (var colB in collidersB)
        {
            // 1. colB의 중심점을 기준으로 colA의 가장 가까운 점을 찾음
            Vector3 closestOnA = colA.ClosestPoint(colB.transform.position);

            // 2. 위에서 구한 점(closestOnA)이 colB 내부 또는 표면에 있는지 확인
            Vector3 closestOnB = colB.ClosestPoint(closestOnA);

            // [핵심] 만약 closestOnA가 colB 내부에 있다면, colB.ClosestPoint는 closestOnA와 같은 좌표를 반환함
            if (closestOnA == closestOnB)
            {
                return 0f; // 하나라도 완전히 겹쳐 있다면 즉시 거리 0 반환
            }

            // 3. 겹치지 않았다면 역방향으로 한 번 더 확인 (정밀도 보완)
            Vector3 finalClosestOnA = colA.ClosestPoint(closestOnB);
            if (finalClosestOnA == closestOnB)
            {
                return 0f; // 겹침 감지 시 즉시 0 반환
            }

            // 4. 둘 다 외부에 있을 때만 실제 거리 계산
            float currentDistance = Vector3.Distance(finalClosestOnA, closestOnB);

            if (currentDistance < minDistance)
            {
                minDistance = currentDistance;
            }
        }

        return minDistance;
    }
}
