using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vertex : MonoBehaviour
{
    private static Dictionary<Vector2, Vertex> vertices = new Dictionary<Vector2, Vertex>();
    public Vector2 point;
    public bool is_outer;
    private Vertex(Vector2 point) {
        this.point = point;
    }
    public static Vertex Get(Vector2 point) {
        if (vertices.ContainsKey(point)) {
            return vertices[point];
        }
        else
        {
            var vertex = new Vertex(point);
            vertices.Add(point, vertex);
            return vertex;
        }
    }
}
