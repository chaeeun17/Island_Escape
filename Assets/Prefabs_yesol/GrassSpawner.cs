using UnityEngine;
using System.Collections.Generic;

public class GrassSpawner : MonoBehaviour
{
    [Header("Source")]
    public MeshFilter terrainMesh;

    [Header("Grass")]
    public Mesh grassMesh;
    public Material grassMaterial;

    [Header("Density")]
    public int samplesPerTriangle = 10;

    // List<Matrix4x4> matrices = new();

    [Header("Culling")]
    public float cullDistance = 50f;

    public float chunkSize = 32f;

    Dictionary<Vector2Int, GrassChunk> chunks =
        new Dictionary<Vector2Int, GrassChunk>();

    void Start()
    {
        GenerateGrass();
    }

    void GenerateGrass()
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

                float noiseDensity = Mathf.PerlinNoise(worldPos.x * 0.03f, worldPos.z * 0.03f);

                float density = color.g * color.g * noiseDensity;
                // float density = color.g;

                if (Random.value > density)
                    continue;

                float noiseScale = Mathf.PerlinNoise(worldPos.x * 0.1f, worldPos.z * 0.1f);

                float scale = Mathf.Lerp(0.5f, 1.5f, noiseScale);
                //float scale = Random.Range(0.8f, 1.3f);

                Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);

                Matrix4x4 matrix =
                    Matrix4x4.TRS(
                        worldPos,
                        rot,
                        Vector3.one * scale);

                Vector2Int chunkCoord =
                    new Vector2Int(
                        Mathf.FloorToInt(worldPos.x / chunkSize),
                        Mathf.FloorToInt(worldPos.z / chunkSize));

                if (!chunks.TryGetValue(chunkCoord, out GrassChunk chunk))
                { 
                    chunk = new GrassChunk();

                    Vector3 center =
                        new Vector3(
                            (chunkCoord.x + 0.5f) * chunkSize,
                            worldPos.y,
                            (chunkCoord.y + 0.5f) * chunkSize);

                    chunk.bounds =
                        new Bounds(
                            center,
                            new Vector3(
                                chunkSize,
                                100,
                                chunkSize));

                    chunks.Add(chunkCoord, chunk);
                }
                chunk.grass.Add(matrix);
            }
        }

        Debug.Log($"Chunks: {chunks.Count}");
    }

    void Update()
    {
        Camera cam = Camera.main;

        if (cam == null)
            return;

        Vector3 camPos = cam.transform.position;

        float cullDistSq = cullDistance * cullDistance;

        foreach (var pair in chunks)
        {
            GrassChunk chunk = pair.Value;

            float sqrDist =
                (chunk.bounds.center - camPos)
                .sqrMagnitude;

            if (sqrDist > cullDistSq)
                continue;

            DrawChunk(chunk);
        }
    }

    void DrawChunk(GrassChunk chunk)
    {
        const int batchSize = 1023;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        for (int i = 0; i < chunk.grass.Count; i += batchSize)
        {
            if (!GeometryUtility.TestPlanesAABB(planes, chunk.bounds))
            {
                continue;
            }

            int count = Mathf.Min(batchSize, chunk.grass.Count - i);

            Graphics.RenderMeshInstanced(new RenderParams(grassMaterial), grassMesh, 0, chunk.grass.GetRange(i, count));
        }
    }
}