using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class MeshToOBJ : Editor
{
    [MenuItem("Tools/Export Selected Mesh to OBJ")]
    public static void ExportSelectionToOBJ()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("에러", "하이러키에서 메쉬가 있는 오브젝트를 선택해주세요.", "확인");
            return;
        }

        GameObject go = Selection.activeGameObject;
        MeshFilter mf = go.GetComponent<MeshFilter>();

        if (mf == null || mf.sharedMesh == null)
        {
            EditorUtility.DisplayDialog("에러", "선택한 오브젝트에 MeshFilter 또는 Mesh 데이터가 없습니다.", "확인");
            return;
        }

        Mesh mesh = mf.sharedMesh;
        string path = EditorUtility.SaveFilePanel("OBJ 저장", "Assets", go.name + "_Mesh", "obj");

        if (!string.IsNullOrEmpty(path))
        {
            string objText = MeshToObjString(mesh);
            File.WriteAllText(path, objText);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("성공", "Mesh가 OBJ로 성공적으로 추출되었습니다!", "확인");
        }
    }

    private static string MeshToObjString(Mesh m)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("g ").Append(m.name).Append("\n");

        foreach (Vector3 v in m.vertices)
            sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));

        sb.Append("\n");
        foreach (Vector2 v in m.uv)
            sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));

        sb.Append("\n");
        for (int material = 0; material < m.subMeshCount; material++)
        {
            int[] triangles = m.GetTriangles(material);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                sb.Append(string.Format("f {0}/{0} {1}/{1} {2}/{2}\n",
                    triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
            }
        }
        return sb.ToString();
    }
}