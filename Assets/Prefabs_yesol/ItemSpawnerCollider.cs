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

        if(spawnCount == 1)
        {
            // 1개 스폰 시 특수 처리
            Runner.Spawn(
                itemPrefabs[0],
                spawnArea.transform.position,
                inputAuthority: null
            );
            return;
        }

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