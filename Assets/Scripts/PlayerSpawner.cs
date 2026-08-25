using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    StartSceneUI ui;
    // ������ �±׷� ã�� ���� �������� Transform�� ������ ����Ʈ
    private List<Transform> _spawnPoints = new List<Transform>();

    private NetworkRunner _runner;

    [SerializeField] private NetworkPrefabRef _playerPrefab;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    public static string CurrentRoomName { get; private set; }
    public static string CurrentRoomPassword { get; private set; }

    //Ȥ�ó� ���Ŀ� password�� �ٲܼ��� �ֵ��� �صӽô�...
    private string roomPassword;
    bool isStarting = false;
    async void StartGame(GameMode mode)
    {
        if (isStarting) return;
        isStarting = true;

        CurrentRoomName = ui.sessionName;
        CurrentRoomPassword = ui.sessionPassword;

        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        StartGameArgs args = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = ui.sessionName,
            Scene = SceneRef.FromIndex(1),//������ ���� Scene���� ����
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };
        if (mode == GameMode.Host)
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
        await _runner.StartGame(args);
        isStarting = false;
    }
    private void Awake()
    {
        if(ui == null)
        {
            ui = FindAnyObjectByType<StartSceneUI>();
        }
    }
    private void Start()
    {
        ui.InitializeAllButtons(StartGame);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // �� �ε尡 �̹� ������, ���� ���� �� �� �÷��̾��� ����
            if (_spawnPoints.Count > 0 && !_spawnedCharacters.ContainsKey(player))
            {
                SpawnPlayer(runner, player);
            }
        }
    }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // ������ Host�� �ƴ϶�� _spawnedCharacters���� �ƹ��͵� ä������ �ʾҴ�.
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // 1. �� �ε尡 ���� ������ SpawnPoint �±� ����
        FindSpawnPoints();

        // 2. ���� ó���� Server/Host ���Ѹ� ����
        if (runner.IsServer)
        {
            // ���� �濡 ������ �ִ� ��� �÷��̾� ��� ��ȸ
            foreach (var player in runner.ActivePlayers)
            {
                // ���� �������� ���� �÷��̾��� ���� ����
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

        Debug.Log($"[Spawn ���] player={player.PlayerId}, playerObject == null ? {playerObject == null}, pos={spawnPosition}");

        _spawnedCharacters.Add(player, playerObject);
        runner.SetPlayerObject(player, playerObject);

        StartCoroutine(CheckAlive(playerObject, player));
    }
    private System.Collections.IEnumerator CheckAlive(NetworkObject obj, PlayerRef player)
    {
        yield return new WaitForSeconds(1f);
        Debug.Log($"[1�� �� ���� Ȯ��] player={player.PlayerId}, obj == null ? {obj == null}");
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
            Debug.LogWarning("[CharacterSpawner] ������ 'SpawnPoint' �±׸� ã�� ���߽��ϴ�.");
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
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        bool wasInStartScene = SceneManager.GetActiveScene().buildIndex == 0;
        if (wasInStartScene)
        {
            //���࿡ StartScene�� ������ Runner�� ����
            //���� ����
            Destroy(_runner);
        }
        else
        {
            //�̿� ��ҿ� �־����� StartScene���� �̵� �� �� ������Ʈ ����(�ʱ�ȭ)
            SceneManager.LoadScene(0);
            //���� ����
            Destroy(gameObject);
        }

    }
    public void OnConnectedToServer(NetworkRunner runner){ }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        string attempted = System.Text.Encoding.UTF8.GetString(token);
        if (attempted == roomPassword)
            request.Accept();
        else
            request.Refuse();
    }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { Debug.LogError($"ConnectionFailed + {reason.ToString()}"); }
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

