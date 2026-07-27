using Fusion;
using System.Collections;
using UnityEngine;

public class ItemSpawner : NetworkBehaviour
{
    [Header("Source")]
    public MeshFilter terrainMesh;

    [Header("Items")]
    public NetworkObject branch;
    public NetworkObject leaf;
    public NetworkObject stone;
    public NetworkObject clam;

    public float branchDensity = 0.6f;
    public float stoneDensity = 0.6f;
    public float clamDensity = 0.5f;

    [Header("Density")]
    public int samplesPerTriangle = 10;

    public override void Spawned()
    {
        // 호스트/서버만 스폰 수행
        if (HasStateAuthority)
        {
            SpawnItems();
        }
    }
    void SpawnItems()
    {
        Mesh mesh = terrainMesh.sharedMesh;

        Vector3[] verts = mesh.vertices;
        Color[] colors = mesh.colors;
        int[] tris = mesh.triangles;

        Transform tr = terrainMesh.transform;

        for (int i = 0; i < tris.Length; i += 3)
        {
            int i0 = tris[i];
            int i1 = tris[i + 1];
            int i2 = tris[i + 2];

            Vector3 v0 = verts[i0];
            Vector3 v1 = verts[i1];
            Vector3 v2 = verts[i2];

            Color c0 = colors[i0];
            Color c1 = colors[i1];
            Color c2 = colors[i2];

            for (int s = 0; s < samplesPerTriangle; s++)
            {
                float r1 = Random.value;
                float r2 = Random.value;

                if (r1 + r2 > 1)
                {
                    r1 = 1 - r1;
                    r2 = 1 - r2;
                }

                float r3 = 1 - r1 - r2;

                Vector3 pos =
                    v0 * r1 +
                    v1 * r2 +
                    v2 * r3;

                Color color =
                    c0 * r1 +
                    c1 * r2 +
                    c2 * r3;

                Vector3 worldPos =
                    tr.TransformPoint(pos);

                Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);

                // 1. 초록색 구역 (나무 스폰)
                if (color.g > 0.7f && color.r < 0.3f && color.b < 0.3f && branch != null)
                {
                    if (Random.value > branchDensity)
                        continue;

                    // Instantiate(Random.value > 0.5f ? branch : leaf, worldPos, rot, transform);
                    // Runner를 통해 네트워크 스폰
                    Runner.Spawn(Random.value > 0.5f ? branch : leaf, worldPos,rot, inputAuthority: null);
                }

                // 2. 파란색 구역 (돌 스폰)
                if (color.b > 0.7f && color.r < 0.3f && color.g < 0.3f && stone != null)
                {
                    if (Random.value > stoneDensity)
                        continue;

                    // Instantiate(stone, worldPos, rot, transform);
                    Runner.Spawn(stone, worldPos, rot, inputAuthority: null);
                }

                // 3. 빨간색 구역 (조개 스폰)
                if (color.r > 0.7f && color.g < 0.3f && color.b < 0.3f && clam != null)
                {
                    if (Random.value > clamDensity)
                        continue;

                    // Instantiate(clam, worldPos, rot, transform);
                    Runner.Spawn(clam, worldPos, rot, inputAuthority: null);
                }
            }
        }
    }
}
