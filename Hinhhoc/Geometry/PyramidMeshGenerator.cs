using UnityEngine;

/// <summary>
/// TẠO HÌNH CHÓP TỨ GIÁC (PYRAMID) - bản chỉ tạo mesh thuần.
/// Đáy ở Y=0, đỉnh ở Y=height. Đáy là hình vuông cạnh baseSize, tâm tại gốc.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PyramidMeshGenerator : MonoBehaviour
{
    [Header("Kích thước hình chóp")]
    [Min(0.01f)] public float baseSize = 1f;
    [Min(0.01f)] public float height = 1.5f;

    [Header("Vật liệu")]
    public Material material;

    void Start()
    {
        Generate();
    }

    void OnValidate()
    {
        if (!gameObject.activeInHierarchy) return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) Generate();
        };
#endif
    }

    void Generate()
    {
        Mesh mesh = CreatePyramidMesh();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer rend = GetComponent<MeshRenderer>();
        if (material != null)
        {
            rend.sharedMaterial = material;
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material defaultMat = new Material(shader);
            if (defaultMat.HasProperty("_BaseColor"))
                defaultMat.SetColor("_BaseColor", new Color(1f, 0.7f, 0.2f));
            if (defaultMat.HasProperty("_Color"))
                defaultMat.SetColor("_Color", new Color(1f, 0.7f, 0.2f));
            rend.sharedMaterial = defaultMat;
        }
    }

    Mesh CreatePyramidMesh()
    {
        Mesh mesh = new Mesh { name = "PyramidMesh" };
        float h = baseSize / 2f;

        Vector3[] vertices = new Vector3[]
        {
            // Đáy ABCD (0-3) — facing xuống
            new Vector3(-h, 0,  h), new Vector3( h, 0,  h), new Vector3( h, 0, -h), new Vector3(-h, 0, -h),
            // Mặt SAB (4-6)
            new Vector3(-h, 0, -h), new Vector3( h, 0, -h), new Vector3( 0, height, 0),
            // Mặt SBC (7-9)
            new Vector3( h, 0, -h), new Vector3( h, 0,  h), new Vector3( 0, height, 0),
            // Mặt SCD (10-12)
            new Vector3( h, 0,  h), new Vector3(-h, 0,  h), new Vector3( 0, height, 0),
            // Mặt SDA (13-15)
            new Vector3(-h, 0,  h), new Vector3(-h, 0, -h), new Vector3( 0, height, 0)
        };

        int[] triangles = new int[]
        {
            0, 1, 2, 0, 2, 3,
            4, 6, 5, 7, 9, 8, 10, 12, 11, 13, 15, 14
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}