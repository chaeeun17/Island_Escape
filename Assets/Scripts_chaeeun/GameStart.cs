using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Fusion;

public class GameStart : MonoBehaviour
{

    [Header("플레이어별 시작 위치 목록 (0번 유저 -> 0번 위치, 1번 유저 -> 1번 위치)")]
    [Tooltip("비어있을 경우 씬에서 'GameStartPoint' 태그를 가진 오브젝트를 자동으로 탐색합니다.")]
    [SerializeField] private List<Transform> startPositions = new List<Transform>();

    /// <summary>
    /// 게임 시작 버튼 클릭 이벤트
    /// </summary>
    public void OnClickGameStart()
    {
        Debug.Log("🕹️ [GameStart] 게임 시작 버튼 클릭됨!");

        // 1. SOS 미션 시작
        if (SOSManager.Instance != null)
        {
            SOSManager.Instance.StartSOSGame();
        }

        // 3. 플레이어 위치 이동 처리
        StartCoroutine(TeleportAllPlayersRoutine());
    }

    private IEnumerator TeleportAllPlayersRoutine()
    {
        // 스폰 지점이 비어있다면 태그로 자동 탐색
        FindStartPositionsIfNeeded();

        if (startPositions == null || startPositions.Count == 0)
        {
            Debug.LogError("❌ [GameStart] 이동할 시작 위치(startPositions)가 설정되지 않았고, 'GameStartPoint' 태그도 찾지 못했습니다!");
            yield break;
        }

        // 씬에 스폰되어 있는 모든 Player 검색
        Player[] allPlayers = UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
        Debug.Log($"🔎 [GameStart] 발견된 Player 수: {allPlayers.Length}명");

        for (int i = 0; i < allPlayers.Length; i++)
        {
            Player player = allPlayers[i];
            if (player == null) continue;

            // PlayerId 또는 순번에 따라 위치 지정
            int targetIndex = i % startPositions.Count;
            if (player.Object != null && player.Object.InputAuthority != Fusion.PlayerRef.None)
            {
                targetIndex = player.Object.InputAuthority.PlayerId % startPositions.Count;
            }

            Transform targetPoint = startPositions[targetIndex];
            if (targetPoint != null)
            {
                // 🌟 Player.cs에 추가한 CustomNetworkCharacterController 텔레포트 호출
                player.TeleportTo(targetPoint.position, targetPoint.rotation);
                Debug.Log($"✅ [GameStart] Player {i} -> {targetPoint.name}({targetPoint.position}) 이동 호출");
            }
        }

        yield return null;
    }

    private void FindStartPositionsIfNeeded()
    {
        if (startPositions == null || startPositions.Count == 0)
        {
            GameObject[] foundObjects = GameObject.FindGameObjectsWithTag("GameStartPoint");
            foreach (var obj in foundObjects)
            {
                startPositions.Add(obj.transform);
            }
        }
    }

    [Header("이동할 시작 씬 인덱스")]
    [SerializeField] private int startSceneIndex = 0;

    public void OnClickExitLobby()
    {
        Debug.Log("🚪 [LobbyExitManager] 방 나가기 요청...");

        StartCoroutine(ShutdownAndReturnToStartScene());
    }

    private IEnumerator ShutdownAndReturnToStartScene()
    {
        // 1. 현재 실행 중인 NetworkRunner 찾기
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null && NetworkRunner.Instances.Count > 0)
        {
            runner = NetworkRunner.Instances[0];
        }

        // 2. Fusion Runner 세션 종료 (호스트가 종료하면 클라이언트들도 OnShutdown을 받음)
        if (runner != null && runner.IsRunning)
        {
            Debug.Log("🛑 [LobbyExitManager] NetworkRunner Shutdown 실행");
            runner.Shutdown();
        }

        // 3. 씬 전환 전 1프레임 대기
        yield return null;

        // 4. 로컬 플레이어를 StartScene으로 이동
        SceneManager.LoadScene(startSceneIndex);
    }
}