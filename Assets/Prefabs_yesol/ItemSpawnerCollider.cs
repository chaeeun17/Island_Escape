using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class ItemSpawnerCollider : NetworkBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private List<NetworkObject> itemPrefabs;
    [SerializeField] private BoxCollider spawnArea;
    [SerializeField] private int spawnCount = 10;

    [Header("지형 바닥 검사 설정")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastDistance = 10f;

    // NetworkObject가 네트워크 상에 완전히 스폰되었을 때 자동 호출
    public override void Spawned()
    {
        // 호스트/서버만 스폰 수행
        if (HasStateAuthority)
        {
            SpawnItems();
        }
    }

    public void SpawnItems()
    {
        if (spawnArea == null || itemPrefabs == null || itemPrefabs.Count == 0) return;

        Bounds bounds = spawnArea.bounds;

        for (int i = 0; i < spawnCount; i++)
        {
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            Vector3 spawnPoint = new Vector3(randomX, bounds.max.y, randomZ);

            if (Physics.Raycast(spawnPoint, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                spawnPoint = hit.point;
            }
            else
            {
                continue;
            }

            NetworkObject selectedPrefab = itemPrefabs[Random.Range(0, itemPrefabs.Count)];
            Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Runner를 통해 네트워크 스폰
            Runner.Spawn(
                selectedPrefab,
                spawnPoint,
                randomRotation,
                inputAuthority: null
            );
        }
    }
}
/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawnerCollider : MonoBehaviour
{
    [Header("스폰 설정")]
    public List<GameObject> itemPrefabs; // 스폰할 아이템 프리팹 리스트
    public BoxCollider spawnArea;       // 스폰할 영역 (Trigger BoxCollider)
    public int spawnCount = 10;         // 생성할 아이템 개수

    [Header("지형 바닥 검사 설정")]
    public LayerMask groundLayer;       // 바닥(지형) 레이어
    public float raycastDistance = 10f; // 바닥을 찾기 위한 레이 길이

    private void Start()
    {
        SpawnItems();
    }

    public void SpawnItems()
    {
        if (spawnArea == null || itemPrefabs == null || itemPrefabs.Count == 0)
        {
            Debug.LogWarning("스폰 영역 또는 아이템 프리팹이 설정되지 않았습니다.");
            return;
        }

        Bounds bounds = spawnArea.bounds;

        for (int i = 0; i < spawnCount; i++)
        {
            // 1. BoxCollider 영역 내 무작위 X, Z 좌표 추출 (Y는 영역의 최상단 사용)
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            Vector3 spawnPoint = new Vector3(randomX, bounds.max.y, randomZ);

            // 2. 아래쪽으로 Raycast를 쏘아 실제 바닥 높이(Y)를 구함 (공중에 뜨거나 땅에 파묻힘 방지)
            if (Physics.Raycast(spawnPoint, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                spawnPoint = hit.point; // 바닥 표면 위치로 조정
            }
            else
            {
                // 바닥을 찾지 못한 경우 (절벽 밖 등) 해당 시도는 넘어감
                continue;
            }

            // 3. 랜덤 아이템 및 회전값 선택 후 생성
            GameObject selectedPrefab = itemPrefabs[Random.Range(0, itemPrefabs.Count)];
            Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            Instantiate(selectedPrefab, spawnPoint, randomRotation, transform);
        }
    }

    // 에디터 씬 뷰에서 스폰 영역을 시각적으로 확인하기 위한 Gizmo
    private void OnDrawGizmosSelected()
    {
        if (spawnArea != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawCube(spawnArea.bounds.center, spawnArea.bounds.size);
        }
    }
}
*/