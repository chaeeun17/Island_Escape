using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private Button createRoomBtn;
    [SerializeField] private Button joinRoomBtn;

    // 씬에서 태그로 찾은 스폰 지점들의 Transform을 저장할 리스트
    private List<Transform> _spawnPoints = new List<Transform>();

    private NetworkRunner _runner;

    [SerializeField] private NetworkPrefabRef _playerPrefab;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private void Awake()
    {
        createRoomBtn.onClick.AddListener(() => StartGame(GameMode.Host));
        joinRoomBtn.onClick.AddListener(() => StartGame(GameMode.Client));
    }

    async void StartGame(GameMode mode)
    {
        createRoomBtn.interactable = false;
        joinRoomBtn.interactable = false;

        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>();
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
        }
        _runner.ProvideInput = true;

        // Create the NetworkSceneInfo from the current scene
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        var sceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null)
        {
            sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        // Start or join (depends on gamemode) a session with a specific name
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = SceneRef.FromIndex(1),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
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

        _spawnedCharacters.Add(player, playerObject);
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
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
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