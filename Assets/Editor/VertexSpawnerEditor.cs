using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class VertexSpawnerEditor : EditorWindow
{
    public GameObject terrainMeshObject;
    public GameObject treePrefab1;
    public GameObject treePrefab2;
    public GameObject treePrefab3;
    public GameObject rockPrefab;

    [MenuItem("Tools/Vertex Color Spawner")]
    public static void ShowWindow()
    {
        GetWindow<VertexSpawnerEditor>("Vertex Spawner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Vertex Color 기반 오브젝트 배치 툴", EditorStyles.boldLabel);

        terrainMeshObject = (GameObject)EditorGUILayout.ObjectField("지형 Mesh 오브젝트", terrainMeshObject, typeof(GameObject), true);

        treePrefab1 = (GameObject)EditorGUILayout.ObjectField("Green 구역1", treePrefab1, typeof(GameObject), false);
        treePrefab2 = (GameObject)EditorGUILayout.ObjectField("Green 구역2", treePrefab2, typeof(GameObject), false);
        treePrefab3 = (GameObject)EditorGUILayout.ObjectField("Green 구역3", treePrefab3, typeof(GameObject), false);

        rockPrefab = (GameObject)EditorGUILayout.ObjectField("Blue 구역", rockPrefab, typeof(GameObject), false);

        if (GUILayout.Button("오브젝트 배치 시작"))
        {
            SpawnObjects();
        }
    }

    /*
    private void SpawnObjects()
    {
        if (terrainMeshObject == null) { Debug.LogError("지형 오브젝트를 지정해주세요."); return; }

        MeshFilter meshFilter = terrainMeshObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) { Debug.LogError("MeshFilter를 찾을 수 없습니다."); return; }

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Color[] colors = mesh.colors;

        if (colors == null || colors.Length == 0)
        {
            Debug.LogError("Mesh에 버텍스 컬러 데이터가 없습니다! 블렌더 설정을 확인하세요.");
            return;
        }

        int spawnCount = 0;
        GameObject[] treeList = { treePrefab1, treePrefab2, treePrefab3 };
        // 성능을 위해 모든 버텍스를 다 쓰지 않고 일정 간격(예: 5개당 1개)으로 검사하거나 랜덤 확률을 부여할 수 있습니다.
        for (int i = 0; i < vertices.Length; i += 3)
        {
            Color vertexColor = colors[i];

            // 로컬 좌표를 월드 좌표로 변환
            Vector3 worldPos = terrainMeshObject.transform.TransformPoint(vertices[i]);

            // 1. 초록색 구역 (나무 스폰) - 오차 감안해 0.5 이상일 때
            if (vertexColor.g > 0.6f && vertexColor.r < 0.3f && vertexColor.b < 0.3f && treePrefab1 != null && treePrefab2 != null && treePrefab3 != null)
            {
                // 너무 빽빽하게 깔리는 걸 방지하기 위한 랜덤 확률 (30% 확률로만 스폰)
                if (Random.value < 0.3f)
                {
                    GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(treeList[Random.Range(0, 3)]);
                    spawned.transform.position = worldPos + new Vector3(0, -0.05f, 0);
                    spawned.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0); // Y축 랜덤 회전
                    float scale = Random.Range(0.8f, 1.2f);
                    spawned.transform.localScale = new Vector3(scale, scale, scale);
                    spawned.transform.parent = terrainMeshObject.transform;
                    Undo.RegisterCreatedObjectUndo(spawned, "Spawn Tree");
                    spawnCount++;
                }
            }
            // 2. 파란색 구역 (돌 스폰)
            else if (vertexColor.b > 0.6f && vertexColor.r < 0.3f && vertexColor.g < 0.3f && rockPrefab != null)
            {
                if (Random.value < 0.2f)
                {
                    GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab);
                    spawned.transform.position = worldPos;
                    // 돌은 울퉁불퉁한 지형 법선에 맞추거나 랜덤하게 회전
                    spawned.transform.rotation = Random.rotation;
                    spawned.transform.parent = terrainMeshObject.transform;
                    Undo.RegisterCreatedObjectUndo(spawned, "Spawn Rock");
                    spawnCount++;
                }
            }
        }

        Debug.Log($"배치 완료! 총 {spawnCount}개의 오브젝트가 생성되었습니다.");
    }
    */
    private void SpawnObjects()
    {
        if (terrainMeshObject == null) { Debug.LogError("지형 오브젝트를 지정해주세요."); return; }

        MeshFilter meshFilter = terrainMeshObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) { Debug.LogError("MeshFilter를 찾을 수 없습니다."); return; }

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Color[] colors = mesh.colors;

        if (colors == null || colors.Length == 0)
        {
            Debug.LogError("Mesh에 버텍스 컬러 데이터가 없습니다!");
            return;
        }

        int spawnCount = 0;
        GameObject[] treeList = { treePrefab1, treePrefab2, treePrefab3 };

        // 중복 배치를 방지하기 위해 이미 설치된 좌표를 저장하는 해시셋 (1m 단위를 정수로 변환해 체크)
        HashSet<Vector3Int> occupiedPositions = new HashSet<Vector3Int>();

        // 최소 배치 거리 설정 (예: 1.5m 이내에는 나무나 돌이 겹치지 않게 함)
        float minDistanceBetweenObjects = 1.5f;

        // 중복 정점 누락을 방지하기 위해 단일 정점 단위로 전체 순회하되, 필터링으로 제어합니다.
        for (int i = 0; i < vertices.Length; i++)
        {
            Color vertexColor = colors[i];
            Vector3 worldPos = terrainMeshObject.transform.TransformPoint(vertices[i]);

            // 현재 검사 중인 위치를 정수 그리드 좌표로 변환 (예: 1.5m 해상도 격자)
            Vector3Int gridPos = new Vector3Int(
                Mathf.RoundToInt(worldPos.x / minDistanceBetweenObjects),
                Mathf.RoundToInt(worldPos.y / minDistanceBetweenObjects),
                Mathf.RoundToInt(worldPos.z / minDistanceBetweenObjects)
            );

            // 이미 이 격자(또는 근처)에 무언가 설치되었다면 스킵!
            if (occupiedPositions.Contains(gridPos)) continue;

            // 1. 초록색 구역 (나무 스폰)
            if (vertexColor.g > 0.6f && vertexColor.r < 0.3f && vertexColor.b < 0.3f && treePrefab1 != null && treePrefab2 != null && treePrefab3 != null)
            {
                if (Random.value < 0.07f)
                {
                    GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(treeList[Random.Range(0, treeList.Length)]);
                    spawned.transform.position = worldPos + new Vector3(Random.Range(0f, 1f), 0, Random.Range(0f, 1f)); //+ new Vector3(0, -0.1f, 0);
                    spawned.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

                    float scale = Random.Range(2f, 4f);
                    spawned.transform.localScale = new Vector3(scale, scale, scale);
                    spawned.transform.parent = terrainMeshObject.transform;

                    Undo.RegisterCreatedObjectUndo(spawned, "Spawn Tree");
                    spawnCount++;

                    // 설치된 격자 좌표를 기록하여 중복 차단
                    occupiedPositions.Add(gridPos);
                }
            }
            // 2. 파란색 구역 (돌 스폰)
            else if (vertexColor.b > 0.6f && vertexColor.r < 0.3f && vertexColor.g < 0.3f && rockPrefab != null)
            {
                if (Random.value < 0.2f)
                {
                    GameObject spawned = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab);
                    spawned.transform.position = worldPos;
                    spawned.transform.rotation = Random.rotation;
                    spawned.transform.parent = terrainMeshObject.transform;

                    Undo.RegisterCreatedObjectUndo(spawned, "Spawn Rock");
                    spawnCount++;

                    // 설치된 격자 좌표를 기록하여 중복 차단
                    occupiedPositions.Add(gridPos);
                }
            }
        }

        Debug.Log($"배치 완료! 총 {spawnCount}개의 오브젝트가 생성되었습니다.");
    }
}