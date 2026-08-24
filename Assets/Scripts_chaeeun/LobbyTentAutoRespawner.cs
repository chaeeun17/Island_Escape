using System.Collections;
using UnityEngine;
using Fusion;

public class LobbyTentAutoRespawner : NetworkBehaviour
{
    [Header("텐트 프리팹 (NetworkObject)")]
    [SerializeField] private NetworkPrefabRef tentPrefab;

    [Header("스폰 위치 (비어있으면 현재 위치)")]
    [SerializeField] private Transform spawnPoint;

    [Header("재생성 대기 시간 (초)")]
    [SerializeField] private float respawnDelay = 3f;

    private NetworkObject _currentTent;
    private bool _isRespawning = false;

    public override void Spawned()
    {
        // 호스트만 스폰 권한을 가짐
        if (Object.HasStateAuthority)
        {
            SpawnTent();
        }
    }

    public override void FixedUpdateNetwork()
    {
        // 호스트에서만 체크
        if (!Object.HasStateAuthority) return;

        // 텐트가 아직 생성 중이지 않은데, 현재 텐트가 파괴/Despawn되어 사라졌다면
        if (!_isRespawning && _currentTent == null)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    private void SpawnTent()
    {
        if (tentPrefab == null || Runner == null) return;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        _currentTent = Runner.Spawn(tentPrefab, pos, rot);
        _isRespawning = false;
        Debug.Log("⛺ [Lobby] 로비 전용 텐트 스폰 완료");
    }

    private IEnumerator RespawnRoutine()
    {
        _isRespawning = true;
        Debug.Log($"⏳ [Lobby] 텐트가 사라짐 감지! {respawnDelay}초 후 재생성합니다.");

        yield return new WaitForSeconds(respawnDelay);

        SpawnTent();
    }
}