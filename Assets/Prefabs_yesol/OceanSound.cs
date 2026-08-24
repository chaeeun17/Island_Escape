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
        if (oceanSound == null) return;

        // 플레이어가 할당되지 않았다면 찾기를 시도합니다.
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("PlayerRoot");

            // [핵심 방어 코드] 
            // Server 모드이거나 아직 유저가 안 들어와서 플레이어가 없다면, 
            // 거리 계산을 하지 않고 바로 함수를 빠져나갑니다 (return).
            if (player == null)
            {
                if (audioSource != null) audioSource.volume = 0f;
                return;
            }
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
        // [2차 방어 코드] 여기서 player가 null이면 에러가 나므로 한 번 더 확인합니다.
        if (player == null) return float.MaxValue;

        Collider colA = player.GetComponent<CharacterController>();

        // CharacterController가 없는 경우를 대비한 추가 방어
        if (colA == null) return float.MaxValue;

        BoxCollider[] collidersB = oceanSound.GetComponentsInChildren<BoxCollider>();

        float minDistance = float.MaxValue;
        foreach (var colB in collidersB)
        {
            Vector3 closestOnA = colA.ClosestPoint(colB.transform.position);
            Vector3 closestOnB = colB.ClosestPoint(closestOnA);

            if (closestOnA == closestOnB) return 0f;

            Vector3 finalClosestOnA = colA.ClosestPoint(closestOnB);
            if (finalClosestOnA == closestOnB) return 0f;

            float currentDistance = Vector3.Distance(finalClosestOnA, closestOnB);
            if (currentDistance < minDistance)
            {
                minDistance = currentDistance;
            }
        }

        return minDistance;
    }
}