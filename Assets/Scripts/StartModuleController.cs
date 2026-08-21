using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static Unity.Collections.Unicode;

[RequireComponent(typeof(StartModuleUI))]
public class StartModuleController : MonoBehaviour, INetworkRunnerCallbacks
{
    StartModuleUI ui;
    // 씬에서 태그로 찾은 스폰 지점들의 Transform을 저장할 리스트
    private List<Transform> _spawnPoints = new List<Transform>();

    private NetworkRunner _runner;

    [SerializeField] private NetworkPrefabRef _playerPrefab;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private string roomPassword;
    async void StartGame(GameMode mode)
    {

        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        // Create the NetworkSceneInfo from the current scene
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        var scene = SceneRef.FromIndex(sceneIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }
        StartGameArgs args = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = ui.sessionName,
            Scene = SceneRef.FromIndex(sceneIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };
        if(mode == GameMode.Host)
        {
            roomPassword = ui.sessionPassword;
            ui.ShowOKMessage("Creating Room", () => { });
        }
        else
        {
            args.ConnectionToken = System.Text.Encoding.UTF8.GetBytes(ui.sessionPassword);
            ui.ShowOKMessage("Joining Room", () => { });
        }
        // Start or join (depends on gamemode) a session with a specific name
        var result = await _runner.StartGame(args);
        if (!result.Ok)
        {
            string createOrJoin = mode == GameMode.Host ? "Create" : "Join";
            ui.ShowOKMessage($"Failed To {createOrJoin} Room\n{result.ShutdownReason}", ui.ShowPreviousPanel);
        }
        else
        {
            ui.ShowGameStartPanel();
        }
    }
    async void Logout()
    {
        /*
        if (_runner.TryGetPlayerObject(_runner.LocalPlayer, out NetworkObject myPlayerObject))
        {
            _runner.Despawn(myPlayerObject);
        }

        await _runner.Shutdown();
        */
        //일단은 게임 종료
    }
    private void Awake()
    {
        ui = GetComponent<StartModuleUI>();
        
    }
    private void Start()
    {
        ui.InitializeAllButtons(StartGame,() => { Logout(); });
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // 씬 로드가 이미 끝났고, 아직 스폰 안 된 플레이어라면 스폰
            if (_spawnPoints.Count > 0 && !_spawnedCharacters.ContainsKey(player))
            {
                SpawnPlayer(runner, player);
            }
        }
    }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // 1. 씬 로드가 끝난 시점에 SpawnPoint 태그 수집
        FindSpawnPoints();

        // 2. 스폰 처리는 Server/Host 권한만 수행
        if (runner.IsServer)
        {
            // 현재 방에 접속해 있는 모든 플레이어 목록 순회
            foreach (var player in runner.ActivePlayers)
            {
                // 아직 스폰되지 않은 플레이어라면 스폰 진행
                if (!_spawnedCharacters.ContainsKey(player))
                {
                    SpawnPlayer(runner, player);
                }
            }
        }
    }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        Transform spawnPoint = GetSpawnPoint(player);
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        NetworkObject playerObject = runner.Spawn(
            _playerPrefab,
            spawnPosition,
            spawnRotation,
            inputAuthority: player
        );

        Debug.Log($"[Spawn 결과] player={player.PlayerId}, playerObject == null ? {playerObject == null}, pos={spawnPosition}");

        _spawnedCharacters.Add(player, playerObject);

        StartCoroutine(CheckAlive(playerObject, player));
    }
    private System.Collections.IEnumerator CheckAlive(NetworkObject obj, PlayerRef player)
    {
        yield return new WaitForSeconds(1f);
        Debug.Log($"[1초 후 생존 확인] player={player.PlayerId}, obj == null ? {obj == null}");
    }

    private void FindSpawnPoints()
    {
        _spawnPoints.Clear();
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");

        foreach (GameObject obj in spawnPointObjects)
        {
            _spawnPoints.Add(obj.transform);
        }

        if (_spawnPoints.Count == 0)
        {
            Debug.LogWarning("[CharacterSpawner] 씬에서 'SpawnPoint' 태그를 찾지 못했습니다.");
        }
    }
    private Transform GetSpawnPoint(PlayerRef player)
    {
        if (_spawnPoints == null || _spawnPoints.Count == 0) return null;

        int index = player.PlayerId % _spawnPoints.Count;
        return _spawnPoints[index];
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        string attempted = System.Text.Encoding.UTF8.GetString(token);
        if (attempted == roomPassword)
            request.Accept();
        else
            request.Refuse();
    }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
