using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sylves;

public class TSGrid : MonoBehaviour
{
    //HexGrid hexGrid = new HexGrid(4, HexOrientation.PointyTopped);
    // Start is called before the first frame update
    void Start()
    {
        var triangleGrid = new TriangleGrid(0.5f, TriangleOrientation.FlatSides, bound: TriangleBound.Hexagon(4));
        var meshData = triangleGrid.ToMeshData();

        // Randomly pair the triangles of that grid
        meshData = meshData.RandomPairing();

        // Split into quads
        meshData = ConwayOperators.Ortho(meshData);

        // Weld duplicate vertices together (needed for Relax)
        meshData = meshData.Weld();

        // Smooth the resulting mesh
        meshData = meshData.Relax();

        //----------------------------------------------------
        // Create a new GameObject
        GameObject quadObject = new GameObject("GRID");
        quadObject.transform.position = Vector3.zero; // Adjust as needed

        // Add MeshFilter and MeshRenderer components
        MeshFilter meshFilter = quadObject.AddComponent<MeshFilter>();
        meshFilter.mesh = meshData.ToMesh() ;

        MeshRenderer meshRenderer = quadObject.AddComponent<MeshRenderer>();


        // Create a material (you may want to assign your own material)
        Material material = new Material(Shader.Find("Standard"));
        material.color = Color.white; // Set color as needed

        // Assign the material to the MeshRenderer
        meshRenderer.material = material;

        // Add the quadObject to the scene
        quadObject.transform.SetParent(transform);


    }

   /* MeshData GetMeshData(Sylves.Cell hex)
    {
        var triangleGrid = new TriangleGrid(0.5f, TriangleOrientation.FlatSides, bound: TriangleBound.Hexagon(4));
        var meshData = triangleGrid.ToMeshData();
        meshData = Matrix4x4.Translate(hexGrid.GetCellCenter(hex)) * meshData;
        var seed = HashUtils.Hash(hex);
        meshData = meshData.RandomPairing(new System.Random(seed).NextDouble);
        meshData = ConwayOperators.Ortho(meshData);
        meshData = meshData.Weld();
        return meshData;
    }*/

    // Update is called once per frame
    void Update()
    {
        
    }
}
