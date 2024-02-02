using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Utility for creating a MeshGrid.
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    internal class MeshGridBuilder
    {
        private ISet<Vector2Int> faces = new HashSet<Vector2Int>();
        private IDictionary<(Vector2Int, CellFaceDir), (Vector2Int, CellFaceDir, CellRotation)> moves = new Dictionary<(Vector2Int, CellFaceDir), (Vector2Int, CellFaceDir, CellRotation)>();

        private  MeshGridBuilder()
        {

        }

        private void AddQuad(Vector2Int face1, Vector2Int face2, int edge, int inverseEdge)
        {
            var faceDir = GetFaceDirQuad(edge);
            var inverseFaceDir = GetFaceDirQuad(inverseEdge);
            var rotation = (-edge + inverseEdge + 6) % 4;
            var cellRotation = CubeRotation.Identity;
            for (var i = 0; i < rotation; i++)
            {
                cellRotation *= CubeRotation.RotateXZ;
            }
            faces.Add(face1);
            faces.Add(face2);
            moves[(face1, faceDir)] = (face2, inverseFaceDir, cellRotation);
            moves[(face2, inverseFaceDir)] = (face1, faceDir, cellRotation.Invert());
        }

        private void AddTriangle(Vector2Int face1, Vector2Int face2, int edge, int inverseEdge)
        {
            var faceDir = GetFaceDirTriangle(edge);
            var inverseFaceDir = GetFaceDirTriangle(inverseEdge);
            var rotation = (edge * 2 - inverseEdge * 2 + 9) % 6;
            var cellRotation = TriangleRotation.RotateCCW60(rotation);
            faces.Add(face1);
            faces.Add(face2);
            moves[(face1, faceDir)] = (face2, inverseFaceDir, cellRotation);
            moves[(face2, inverseFaceDir)] = (face1, faceDir, cellRotation.Invert());
        }

        private static Vector3Int FaceAndLayer(Vector2Int face, int layer)
        {
            return new Vector3Int(face.x, layer, face.y);
        }

        private Dictionary<(Vector3Int, CellFaceDir), (Vector3Int, CellFaceDir, CellRotation)> GetMovesQuads(int layers)
        {
            var output = new Dictionary<(Vector3Int, CellFaceDir), (Vector3Int, CellFaceDir, CellRotation)>();
            for (var layer = 0; layer < layers; layer++)
            {
                foreach (var kv in moves)
                {
                    var (fromFace, dir) = kv.Key;
                    var (toFace, inverseDir, rotation) = kv.Value;
                    output[(FaceAndLayer(fromFace, layer), dir)] = (FaceAndLayer(toFace, layer), inverseDir, rotation);
                }
                foreach (var face in faces)
                {
                    if (layer > 0)
                    {
                        output[(FaceAndLayer(face, layer), (CellFaceDir)CubeFaceDir.Down)] = (FaceAndLayer(face, layer - 1), (CellFaceDir)CubeFaceDir.Up, CubeRotation.Identity);
                    }
                    if (layer < layers - 1)
                    {
                        output[(FaceAndLayer(face, layer), (CellFaceDir)CubeFaceDir.Up)] = (FaceAndLayer(face, layer + 1), (CellFaceDir)CubeFaceDir.Down, CubeRotation.Identity);
                    }
                }
            }
            return output;
        }
        private Dictionary<(Vector3Int, CellFaceDir), (Vector3Int, CellFaceDir, CellRotation)> GetMovesTriangles(int layers)
        {
            var output = new Dictionary<(Vector3Int, CellFaceDir), (Vector3Int, CellFaceDir, CellRotation)>();
            for (var layer = 0; layer < layers; layer++)
            {
                foreach (var kv in moves)
                {
                    var (fromFace, dir) = kv.Key;
                    var (toFace, inverseDir, rotation) = kv.Value;
                    output[(FaceAndLayer(fromFace, layer), dir)] = (FaceAndLayer(toFace, layer), inverseDir, rotation);
                }
                foreach (var face in faces)
                {
                    if (layer > 0)
                    {
                        output[(FaceAndLayer(face, layer), (CellFaceDir)TrianglePrismFaceDir.Down)] = (FaceAndLayer(face, layer - 1), (CellFaceDir)TrianglePrismFaceDir.Up, TriangleRotation.Identity);
                    }
                    if (layer < layers - 1)
                    {
                        output[(FaceAndLayer(face, layer), (CellFaceDir)TrianglePrismFaceDir.Up)] = (FaceAndLayer(face, layer + 1), (CellFaceDir)TrianglePrismFaceDir.Down, TriangleRotation.Identity);
                    }
                }
            }
            return output;
        }

        public static Dictionary<(Vector3Int, CellFaceDir), (Vector3Int, CellFaceDir, CellRotation)> Build(MeshData mesh, int layers)
        {
            var vertices = mesh.vertices;
            // From a pair of points, to a ((face, subMesh), edgeIndex)
            var edgeData = new Dictionary<(Vector3, Vector3), (Vector2Int, int)>();
            var builder = new MeshGridBuilder();

            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                if (mesh.GetTopology(subMesh) == MeshTopology.Quads)
                {
                    var indices = mesh.GetIndices(subMesh);
                    for (var i = 0; i < indices.Length; i += 4)
                    {
                        for (var e = 0; e < 4; e++)
                        {
                            var v1 = vertices[indices[i + e]];
                            var v2 = vertices[indices[i + (e + 1) % 4]];
                            if (edgeData.TryGetValue((v2, v1), out var data))
                            {
                                var face1 = i / 4;
                                var (face2, inverseEdge) = data;
                                builder.AddQuad(new Vector2Int(face1, subMesh), face2, e, inverseEdge);
                            }
                            else
                            {
                                edgeData[(v1, v2)] = (new Vector2Int(i / 4, subMesh), e);
                            }
                        }
                    }
                }
                else if (mesh.GetTopology(subMesh) == MeshTopology.Triangles)
                {
                    var indices = mesh.GetIndices(subMesh);
                    for (var i = 0; i < indices.Length; i += 3)
                    {
                        for (var e = 0; e < 3; e++)
                        {
                            var v1 = vertices[indices[i + e]];
                            var v2 = vertices[indices[i + (e + 1) % 3]];
                            if (edgeData.TryGetValue((v2, v1), out var data))
                            {
                                var face1 = i / 3;
                                var (face2, inverseEdge) = data;
                                builder.AddTriangle(new Vector2Int(face1, subMesh), face2, e, inverseEdge);
                            }
                            else
                            {
                                edgeData[(v1, v2)] = (new Vector2Int(i / 3, subMesh), e);
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception($"Mesh topology {mesh.GetTopology(subMesh)} not supported.");

                }
            }
            if (mesh.GetTopology(0) == MeshTopology.Quads)
            {
                return builder.GetMovesQuads(layers);
            }
            else
            {
                return builder.GetMovesTriangles(layers);
            }



        }

        private CellFaceDir GetFaceDirQuad(int i)
        {
            switch (i)
            {
                case 0: return (CellFaceDir)CubeFaceDir.Left;
                case 1: return (CellFaceDir)CubeFaceDir.Forward;
                case 2: return (CellFaceDir)CubeFaceDir.Right;
                case 3: return (CellFaceDir)CubeFaceDir.Back;
            }
            throw new Exception();
        }

        private CellFaceDir GetFaceDirTriangle(int i)
        {
            switch (i)
            {
                case 0: return (CellFaceDir)TrianglePrismFaceDir.ForwardRight;
                case 1: return (CellFaceDir)TrianglePrismFaceDir.Back;
                case 2: return (CellFaceDir)TrianglePrismFaceDir.ForwardLeft;
            }
            throw new Exception();
        }
    }
}
