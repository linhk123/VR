using UnityEngine;

/// <summary>
/// TẠO HÌNH NÓN (CONE) - bản chỉ tạo mesh thuần.
/// Đáy ở Y=0, đỉnh ở Y=height. Đáy tròn tâm tại gốc.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ConeMeshGenerator : MonoBehaviour
{
    [Header("Kích thước hình nón")]
    [Min(0.01f)] public float radius = 0.5f;
    [Min(0.01f)] public float height = 1.5f;
    [Range(12, 60)] public int segments = 24;

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
        Mesh mesh = CreateConeMesh();
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
                defaultMat.SetColor("_BaseColor", new Color(0.75f, 0.4f, 1f));
            if (defaultMat.HasProperty("_Color"))
                defaultMat.SetColor("_Color", new Color(0.75f, 0.4f, 1f));
            rend.sharedMaterial = defaultMat;
        }
    }

    Mesh CreateConeMesh()
    {
        Mesh mesh = new Mesh { name = "ConeMesh" };
        int sideVertexCount = segments * 3;
        int baseVertexCount = segments + 1;
        Vector3[] vertices = new Vector3[sideVertexCount + baseVertexCount];
        int[] triangles = new int[segments * 3 * 2];

        int vi = 0, ti = 0;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float a1 = i * angleStep * Mathf.Deg2Rad;
            float a2 = ((i + 1) % segments) * angleStep * Mathf.Deg2Rad;
            Vector3 apex = new Vector3(0, height, 0);
            Vector3 b1 = new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius);
            Vector3 b2 = new Vector3(Mathf.Cos(a2) * radius, 0, Mathf.Sin(a2) * radius);
            vertices[vi] = apex; vertices[vi + 1] = b1; vertices[vi + 2] = b2;
            triangles[ti] = vi; triangles[ti + 1] = vi + 1; triangles[ti + 2] = vi + 2;
            vi += 3; ti += 3;
        }

        int centerIdx = vi;
        vertices[vi++] = Vector3.zero;
        int ringStart = vi;
        for (int i = 0; i < segments; i++)
        {
            float a = i * angleStep * Mathf.Deg2Rad;
            vertices[vi++] = new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
        }
        for (int i = 0; i < segments; i++)
        {
            triangles[ti] = centerIdx;
            triangles[ti + 1] = ringStart + ((i + 1) % segments);
            triangles[ti + 2] = ringStart + i;
            ti += 3;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}