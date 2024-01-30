using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edge : MonoBehaviour
{
    private static Dictionary<(Vertex, Vertex), Edge> edges = new Dictionary<(Vertex, Vertex), Edge>();
    public Vertex a, b;
    public List<Face> faces;
    private Edge(Vertex a, Vertex b) {
        this.a = a;
        this.b = b;
        faces = new List<Face>();
    }

    public static Edge Get(Vertex a, Vertex b) {

        if(a.point.x > b.point.x && a.point.y > b.point.y)
        {
            //A > B if(a.point > b.point)
            (a, b) = (b, a); //mirar mejor, hay dudas
        }

        var key = (a, b);
        if (edges.ContainsKey(key)) { return edges[key]; }

        else {
            var edge = new Edge(a, b);
            edges.Add(key, edge);
            return edge;
        }
    }
    public void AddFace(Face face)
    {
        faces.Add(face);
    }
    
    public Vector2 GetCenter()
    {
        return (a.point + b.point) / 2f;
    }
}
