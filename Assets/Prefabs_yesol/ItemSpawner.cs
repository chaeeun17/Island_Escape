using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("Source")]
    public MeshFilter terrainMesh;

    [Header("Items")]
    public GameObject item;

    [Header("Density")]
    public int samplesPerTriangle = 10;

    void Start()
    {
        StartCoroutine(SpawnItemWait());
    }

    IEnumerator SpawnItemWait()
    {
        yield return new WaitForEndOfFrame();
        SpawnItems();
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

                float density = 0.6f;

                if (Random.value > density)
                    continue;

                Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);

                Instantiate(item, worldPos, rot, transform);
            }
        }
    }
}
