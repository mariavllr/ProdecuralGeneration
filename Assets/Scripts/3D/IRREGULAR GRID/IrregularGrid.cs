using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IrregularGrid : MonoBehaviour
{
    GameObject sphere;
    int ring_count;
    float ring_radius;
    int point_count;
    int point_index;
    Vector2[] points;
    Vector3[] triangles;
    int triangleIndex;

    struct Vector2i
    {
        public int x, y;
        public Vector2i(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    struct Vector3i
    {
        public int x, y, z;
        public Vector3i(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    struct Vector4i
    {
        public int x, y, z, w;
        public Vector4i(int x, int y, int z, int w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }

    private void Start()
    {
        Dictionary<int, Vector2i> edge_to_quads = new Dictionary<int, Vector2i>();
        Dictionary<Vector2i, int> vertex_couple_to_edge = new Dictionary<Vector2i, int>();
        Vector4i[] quad_to_edges = new Vector4i[0];

        ring_count = 5;
        ring_radius = 3f;
        point_count = 1;
        for (int i = 0; i < ring_count; ++i)
        {
            point_count += 6 * i; //each next ring has 6 more points than the previous one
        }

        //Crear los puntos
        points = GenerateGridVertices(ring_count, ring_radius);
        point_index = 0;
        InvokeRepeating("CreateSphere", 0f, 0.2f);

        //Triangular
        triangles = TriangulateHexGridVertices(ring_count, points);
        for (int i = 0; i < triangles.Length; i++)
        {
            print(triangles[i]);
        }
        float delay = 0.05f * (points.Length + 2);
        Invoke("StartCreatingTriangles", delay);

    }


    void CreateSphere()
    {
        print("Point index: " + point_index + ". Point count: " + point_count);
        if (point_index < point_count)
        {
            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = points[point_index];
            point_index++;
        }
        else
        {
            CancelInvoke("CreateSphere");
        }
    }

    void StartCreatingTriangles()
    {
        triangleIndex = 0;

        // Inicia un temporizador para crear cada triángulo
        InvokeRepeating("CreateNextTriangle", 0f, 0.5f);
    }

    void CreateNextTriangle()
    {
        if (triangleIndex < triangles.Length)
        {
            // Obtiene el triángulo actual
            Vector3 triangle = triangles[triangleIndex];

            // Crea el triángulo usando las coordenadas de los puntos
            CreateTriangle(points[(int)triangle.x], points[(int)triangle.y], points[(int)triangle.z]);

            // Incrementa el índice del triángulo
            triangleIndex++;
        }
        else
        {
            // Detén el temporizador si ya se crearon todos los triángulos
            CancelInvoke("CreateNextTriangle");
        }
    }

    void CreateTriangle(Vector2 vertex1, Vector2 vertex2, Vector2 vertex3)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = ToVector3Array(new Vector2[] { vertex1, vertex2, vertex3 });

        int[] trianglesIndex = new int[] { 0, 1, 2 };
        // Asigna los índices al objeto Mesh
        mesh.triangles = trianglesIndex;

        // Crea un objeto GameObject y asigna el objeto Mesh
        GameObject triangleObject = new GameObject("Triangle");
        MeshFilter meshFilter = triangleObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        mesh.RecalculateNormals();

        // Añade un componente MeshRenderer
        MeshRenderer meshRenderer = triangleObject.AddComponent<MeshRenderer>();

        // Crea un material con renderizado de alambre (wireframe)
        Material wireframeMaterial = new Material(Shader.Find("Standard"));
        wireframeMaterial.SetFloat("_Mode", 1); // Establece el modo de renderizado de alambre
        wireframeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        wireframeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        wireframeMaterial.SetInt("_ZWrite", 0);
        wireframeMaterial.DisableKeyword("_ALPHATEST_ON");
        wireframeMaterial.EnableKeyword("_ALPHABLEND_ON");
        wireframeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        wireframeMaterial.renderQueue = 3000; // Ajusta según sea necesario
        wireframeMaterial.SetColor("_Color", Color.white); // Ajusta según sea necesario

        // Asigna el material al MeshRenderer
        meshRenderer.material = wireframeMaterial;

    }

    // Convierte un array de Vector2 a un array de Vector3 (añadiendo z=0)
    Vector3[] ToVector3Array(Vector2[] input)
    {
        Vector3[] output = new Vector3[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            output[i] = new Vector3(input[i].x, input[i].y, 0f);
        }
        return output;
    }


    static Vector2 HexPoint(float radius, int point_i)
    {
        var angle = -Mathf.PI / 2f + Mathf.PI / 3f * point_i;
        var x = radius * Mathf.Cos(angle);
        var y = radius * Mathf.Sin(angle);
        return new Vector2(x, y);
    }
    static int GetArithmeticProgressionSum(int a, int d, int n)
    {
        return n * (2 * a + (n - 1) * d) / 2;
    }

    static int GetVertexIndexByRing(int ring_index, int point_index)
    {
        return 1 + GetArithmeticProgressionSum(0, 6, ring_index) + point_index;
    }

    public static Vector2[] GenerateGridVertices(int ring_count, float ring_radius)
    {
        int point_count = GetArithmeticProgressionSum(1, 6, ring_count + 1);
        Vector2[] points = new Vector2[point_count];
        points[0] = Vector2.zero; //Center
        var point_index = 1;

        for(int ring_i = 1; ring_i <= ring_count; ++ring_i)
        {
            var r = ring_radius * ring_i;
            for(int point_i = 0; point_i < 6; ++point_i)
            {
                var start_point = HexPoint(r, point_i);
                var end_point = HexPoint(r, point_i + 1);
                var side_point_count = ring_i;
                var side_step = 1f / side_point_count;
                for (int side_point_i = 0; side_point_i < side_point_count; ++side_point_i)
                {
                    if (point_index < point_count)
                    {
                        points[point_index++] = Vector2.Lerp(start_point, end_point, side_step * side_point_i);
                    }
                }
            }
        }
        return points;
    }

    //Devuelve el INDICE de los VERTICES que forman los triangulos
    Vector3[] TriangulateHexGridVertices(int ring_count, Vector2[] vertices)
    {
        int triangle_count = 6 * GetArithmeticProgressionSum(1, 2, ring_count);
        var triangles = new Vector3[triangle_count];

        var triangle_i = 0;
        for(int ring_i = 1; ring_i <= ring_count; ++ring_i)
        {
            var point_count = 6 * ring_i;
            var side_point_count = ring_i;
            for(int point_i = 0; point_i < point_count; ++point_i)
            {
                var a = GetVertexIndexByRing(ring_i, point_i);
                var b = GetVertexIndexByRing(ring_i, (point_i+1) % point_count);
                var c = 0; //Center for the first triangle c point
                if (ring_i > 1)
                {
                    c = GetVertexIndexByRing(ring_i - 1, point_i - (point_i / side_point_count));
                }
                triangles[triangle_i++] = new Vector3(a, b, c);
            }
        }
        return triangles;
    }
}
