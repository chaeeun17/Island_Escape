using UnityEngine;
using System.Collections.Generic;

public class MeshGrassSpawner : MonoBehaviour
{
    public MeshFilter targetMeshFilter;
    public List<GameObject> grassPrefab = new List<GameObject>();
    public int attemptCount = 2000;      // 풀 배치를 시도할 횟수
    [Range(0f, 1f)]
    public float greenThreshold = 0.5f; // 이 값보다 초록색(G)이 진한 곳에만 심김

    [ContextMenu("Spawn Grass by Vertex Color")]
    public void SpawnGrassByColor()
    {
        if (targetMeshFilter == null || grassPrefab == null) return;

        Mesh mesh = targetMeshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3[] normals = mesh.normals;
        Color[] colors = mesh.colors; // 메시의 버텍스 컬러 배열

        // 버텍스 컬러 데이터가 없는 메시인 경우 경고 후 종료
        if (colors == null || colors.Length == 0)
        {
            Debug.LogError("대상 메시에 Vertex Color 데이터가 존재하지 않습니다!");
            return;
        }

        Transform meshTransform = targetMeshFilter.transform;
        int spawnedCount = 0;

        for (int i = 0; i < attemptCount; i++)
        {
            // 1. 무작위 삼각형 선택
            int triIndex = Random.Range(0, triangles.Length / 3) * 3;
            int i0 = triangles[triIndex];
            int i1 = triangles[triIndex + 1];
            int i2 = triangles[triIndex + 2];

            // 2. 삼각형 내부 무작위 비율(Barycentric) 계산
            float r1 = Random.value;
            float r2 = Random.value;
            if (r1 + r2 > 1) { r1 = 1 - r1; r2 = 1 - r2; }
            float r0 = 1 - r1 - r2;

            // 3. 샘플링된 좌표의 '보간된 버텍스 컬러' 계산
            // 세 꼭짓점의 색상을 위치 비율에 맞춰 섞어줍니다.
            Color blendedColor = (r0 * colors[i0]) + (r1 * colors[i1]) + (r2 * colors[i2]);

            // 4. 초록색(G) 채널 값이 기준치보다 낮으면 이 위치는 패스(스킵)
            if (blendedColor.g < greenThreshold) continue;

            // 5. 기준 통과 시 위치 및 회전 계산 후 풀 생성
            Vector3 localPos = (r0 * vertices[i0]) + (r1 * vertices[i1]) + (r2 * vertices[i2]);
            Vector3 worldPos = meshTransform.TransformPoint(localPos);

            Vector3 localNormal = (r0 * normals[i0]) + (r1 * normals[i1]) + (r2 * normals[i2]);
            Vector3 worldNormal = meshTransform.TransformDirection(localNormal).normalized;

            GameObject grass = Instantiate(grassPrefab[Random.Range(0, grassPrefab.Count)], worldPos, Quaternion.identity, transform);
            grass.transform.up = worldNormal;
            grass.transform.Rotate(Vector3.up, Random.Range(0f, 360f), Space.Self);
            grass.transform.localScale *= Random.Range(0.8f, 1.2f);

            spawnedCount++;
        }

        Debug.Log($"풀 생성 완료! 시도 횟수: {attemptCount}번 중 {spawnedCount}개 생성 성공.");
    }
}
