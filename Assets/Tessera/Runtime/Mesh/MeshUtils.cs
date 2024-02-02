using DeBroglie.Rot;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{


    /// <summary>
    /// Utility for working with meshes.
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    public static class MeshUtils
    {
        // Creates an axis aligned cube that corresponds with a box collider
        private static Mesh CreateBoxMesh(Vector3 center, Vector3 size)
        {
            Vector3[] vertices = {
                new Vector3 (-0.5f, -0.5f, -0.5f),
                new Vector3 (+0.5f, -0.5f, -0.5f),
                new Vector3 (+0.5f, +0.5f, -0.5f),
                new Vector3 (-0.5f, +0.5f, -0.5f),
                new Vector3 (-0.5f, +0.5f, +0.5f),
                new Vector3 (+0.5f, +0.5f, +0.5f),
                new Vector3 (+0.5f, -0.5f, +0.5f),
                new Vector3 (-0.5f, -0.5f, +0.5f),
            };
            vertices = vertices.Select(v => center + Vector3.Scale(size, v)).ToArray();
            int[] triangles = {
                0, 2, 1,
	            0, 3, 2,
                2, 3, 4,
	            2, 4, 5,
                1, 2, 5,
	            1, 5, 6,
                0, 7, 4,
	            0, 4, 3,
                5, 4, 7,
	            5, 7, 6,
                0, 6, 7,
	            0, 1, 6
            };

            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            return mesh;
        }

        /// <summary>
        /// Applies Transform gameObject and its children.
        /// Components affected:
        /// * MeshFilter
        /// * MeshColldier
        /// * BoxCollider
        /// </summary>
        public static void TransformRecursively(GameObject gameObject, MeshDeformation meshDeformation)
        {
            foreach (var child in gameObject.GetComponentsInChildren<MeshFilter>())
            {
                var childDeformation = (child.transform.worldToLocalMatrix * gameObject.transform.localToWorldMatrix) * meshDeformation * (gameObject.transform.worldToLocalMatrix * child.transform.localToWorldMatrix);
                if (!child.sharedMesh.isReadable) continue;
                var mesh = childDeformation.Deform(child.sharedMesh);
                mesh.hideFlags = HideFlags.HideAndDontSave;
                child.mesh = mesh;
            }
            foreach (var child in gameObject.GetComponentsInChildren<Collider>())
            {
                var childDeformation = (child.transform.worldToLocalMatrix * gameObject.transform.localToWorldMatrix) * meshDeformation * (gameObject.transform.worldToLocalMatrix * child.transform.localToWorldMatrix);
                if (child is MeshCollider meshCollider)
                {
                    meshCollider.sharedMesh = childDeformation.Deform(meshCollider.sharedMesh);
                }
                else if(child is BoxCollider boxCollider)
                {
                    // Convert box colliders to mesh colliders.
                    var childGo = child.gameObject;
                    var newMeshCollider = childGo.AddComponent<MeshCollider>();
                    newMeshCollider.enabled = child.enabled;
                    newMeshCollider.hideFlags = child.hideFlags;
                    newMeshCollider.isTrigger = child.isTrigger;
                    newMeshCollider.sharedMaterial = child.sharedMaterial;
                    newMeshCollider.name = child.name;
                    newMeshCollider.convex = false;// Cannot be sure of this
                    var mesh = CreateBoxMesh(boxCollider.center, boxCollider.size);
                    mesh.hideFlags = HideFlags.HideAndDontSave;
                    newMeshCollider.sharedMesh = childDeformation.Deform(mesh);
                    if (Application.isPlaying)
                    {
                        GameObject.Destroy(child);
                    }
                    else
                    {
                        GameObject.DestroyImmediate(child);
                    }
                }
                else
                {
                    Debug.LogWarning($"Collider {child} is not a type Tessera supports deforming onto a mesh.");

                }
            }
        }

        /// <summary>
        /// Matrix that transforms from tile local co-ordinates to a unit centered cube, mapping the cube at the given offset to the unit cube.
        /// </summary>
        public static Matrix4x4 TileToCube(TesseraTile tile, Vector3Int offset)
        {
            var translate = Matrix4x4.Translate(-tile.center - Vector3.Scale(offset, tile.tileSize));
            var scale = Matrix4x4.Scale(new Vector3(1.0f / tile.tileSize.x, 1.0f / tile.tileSize.y, 1.0f / tile.tileSize.z));
            return scale * translate;
        }

        public static Matrix4x4 TileToTri(TesseraTrianglePrismTile tile, Vector3Int offset)
        {
            var translate = Matrix4x4.Translate(-TrianglePrismGeometryUtils.GetCellCenter(offset, tile.center, tile.tileSize));
            var scale = Matrix4x4.Scale(new Vector3(1.0f / tile.tileSize.x, 1.0f / tile.tileSize.y, 1.0f / tile.tileSize.x));// Note xyx format!
            return scale * translate;
        }

        /// <summary>
        /// Deforms from tile local space to the surface of the mesh
        /// </summary>
        public static MeshDeformation GetDeformation(MeshData surfaceMesh, float tileHeight, float surfaceOffset, bool smoothNormals, TesseraTileInstance i)
        {
            if (i.Cells.Count() == 1)
            {
                var cell = i.Cells.First();
                var rotation = i.CellRotations.First();
                var offset = i.Tile.offsets.First();

                return GetDeformation(surfaceMesh, tileHeight, surfaceOffset, smoothNormals, i, cell, offset, rotation);
            }
            else
            {
                // For big tiles, we need to load the transform for every cell the tile covers
                // and apply the correct one

                var isQuad = i.Tile.CellType is CubeCellType;
                var cellType = isQuad ? (ICellType)CubeCellType.Instance : TrianglePrismCellType.Instance;

                var initialTransformsByOffset = new Dictionary<Vector3Int, MeshDeformation>();
                var offsetsPositions = new List<(Vector3Int, Vector3)>();
                var deformationsByOffset = new Dictionary<Vector3Int, MeshDeformation>();
                for(var x=0;x<i.Tile.offsets.Count;x++)
                {
                    var cell = i.Cells[x];
                    var rotation = i.CellRotations[x];
                    var offset = i.Tile.offsets[x];
                    deformationsByOffset[offset] = initialTransformsByOffset[offset] = GetDeformation(surfaceMesh, tileHeight, surfaceOffset, smoothNormals, i, cell, offset, rotation);
                    offsetsPositions.Add((offset, cellType.GetCellCenter(offset, i.Tile.center, i.Tile.tileSize)));
                }
                MeshDeformation GetNearest(Vector3 v)
                {
                    // TODO: Lift to CellType method?
                    Vector3Int offset;
                    if (isQuad)
                    {
                        var v2 = v - i.Tile.center;
                        offset = new Vector3Int(
                            (int)Math.Round(v2.x / i.Tile.tileSize.x),
                            (int)Math.Round(v2.y / i.Tile.tileSize.y),
                            (int)Math.Round(v2.z / i.Tile.tileSize.z)
                            );
                    }
                    else
                    {
                        TrianglePrismGeometryUtils.FindCell(i.Tile.center, i.Tile.tileSize, v, out offset);
                    }
                    if (deformationsByOffset.TryGetValue(offset, out var nearest))
                    {
                        return nearest;
                    }
                    var p = cellType.GetCellCenter(offset, i.Tile.center, i.Tile.tileSize);
                    var nearestOffset = Vector3Int.zero;
                    var nearestDist = float.PositiveInfinity;
                    foreach (var (o, op) in offsetsPositions)
                    {
                        var dist = (op - p).sqrMagnitude;
                        if(dist < nearestDist)
                        {
                            nearestOffset = o;
                            nearestDist = dist;
                        }
                    }
                    nearest = deformationsByOffset[offset] = initialTransformsByOffset[nearestOffset];
                    return nearest;
                }
                Vector3 DeformPoint(Vector3 p)
                {
                    return GetNearest(p).DeformPoint(p);
                }
                Vector3 DeformNormal(Vector3 p, Vector3 v)
                {
                    return GetNearest(p).DeformNormal(p, v);
                }
                Vector4 DeformTangent(Vector3 p, Vector4 t)
                {
                    return GetNearest(p).InnerDeformTangent(p, t);
                }
                return new MeshDeformation(DeformPoint, DeformNormal, DeformTangent, deformationsByOffset.First().Value.InvertWinding);
            }
        }

        /// <summary>
        /// Deforms from tile local space to the suface of the mesh, based on a particular offset
        /// </summary>
        private static MeshDeformation GetDeformation(MeshData surfaceMesh, float tileHeight, float surfaceOffset, bool smoothNormals, TesseraTileInstance i, Vector3Int cell, Vector3Int offset, CellRotation rotation)
        {
            var meshDeformation = GetDeformation(surfaceMesh, tileHeight, surfaceOffset, smoothNormals, cell.x, cell.y, cell.z);

            Matrix4x4 tileToCell;
            Matrix4x4 tileMatrix;
            if (i.Tile is TesseraTile cubeTile)
            {
                tileToCell = TileToCube(cubeTile, offset);
                tileMatrix = CubeCellType.Instance.GetMatrix(rotation);
            }
            else if(i.Tile is TesseraTrianglePrismTile triTile)
            {
                tileToCell = TileToTri(triTile, offset);
                tileMatrix = TrianglePrismCellType.Instance.GetMatrix(rotation);
            }
            else
            {
                throw new Exception();
            }

            return meshDeformation * tileMatrix * tileToCell;
        }


        /// <summary>
        /// Transforms from a unit cube centered on the origin to the surface of the mesh
        /// </summary>
        public static MeshDeformation GetDeformation(MeshData surfaceMesh, float tileHeight, float surfaceOffset, bool smoothNormals, int face, int layer, int subMesh)
        {
            var isQuads = surfaceMesh.GetTopology(subMesh) == MeshTopology.Quads;

            var trilinearInterpolatePoint = isQuads 
                ? QuadInterpolation.InterpolatePosition(surfaceMesh, subMesh, face, tileHeight * layer + surfaceOffset - tileHeight / 2, tileHeight * layer + surfaceOffset + tileHeight / 2)
                : TriangleInterpolation.InterpolatePosition(surfaceMesh, subMesh, face, tileHeight * layer + surfaceOffset - tileHeight / 2, tileHeight * layer + surfaceOffset + tileHeight / 2);

            var trilinearInterpolateNormal = !smoothNormals ? null : isQuads
                ? QuadInterpolation.InterpolateNormal(surfaceMesh, subMesh, face)
                : TriangleInterpolation.InterpolateNormal(surfaceMesh, subMesh, face);

            var trilinearInterpolateTangent = !smoothNormals ? null : isQuads
                ? QuadInterpolation.InterpolateTangent(surfaceMesh, subMesh, face)
                : TriangleInterpolation.InterpolateTangent(surfaceMesh, subMesh, face);

            var trilinearInterpolateUv = !smoothNormals ? null : isQuads
                ? QuadInterpolation.InterpolateUv(surfaceMesh, subMesh, face)
                : TriangleInterpolation.InterpolateUv(surfaceMesh, subMesh, face);

            void GetJacobi(Vector3 p, out Matrix4x4 jacobi)
            {
                var m = 1e-3f;

                // TODO: Do some actual differentation
                var t = trilinearInterpolatePoint(p);
                var dx = (trilinearInterpolatePoint(p + Vector3.right * m) - t) / m;
                var dy = (trilinearInterpolatePoint(p + Vector3.up * m) - t) / m;
                var dz = (trilinearInterpolatePoint(p + Vector3.forward * m) - t) / m;

                if (!smoothNormals)
                {
                    jacobi = new Matrix4x4(dx, dy, dz, new Vector4(0, 0, 0, 1));
                }
                else
                {
                    // If you want normals that are continuous on the boundary between cells,
                    // we cannot use the actual jacobi matrix (above) as it is discontinuous.

                    // The same problem comes up for uv interpolation, which is why many meshes
                    // come with a precalculated tangent field for bump mapping etc.

                    // We can re-use that pre-computation by calculating the difference between
                    // the naive uv jacobi and the one given by the tangents, and then
                    // applying that to interpolation jacobi

                    // This code is not 100% correct, but it seems to give acceptable results.
                    // TODO: Do we really need all the normalization?


                    var normal = trilinearInterpolateNormal(p).normalized;
                    var tangent4 = trilinearInterpolateTangent(p);
                    var tangent3 = ((Vector3)tangent4).normalized;
                    var bitangent = (tangent4.w * Vector3.Cross(normal, tangent3)).normalized;

                    // TODO: Do some actual differentation
                    var t2 = trilinearInterpolateUv(p);
                    var dx2 = (trilinearInterpolateUv(p + Vector3.right * m) - t2) / m;
                    //var dy2 = (trilinearInterpolateUv(p + Vector3.up * m) - t2) / m;// Always zero
                    var dz2 = (trilinearInterpolateUv(p + Vector3.forward * m) - t2) / m;

                    var j3 = new Matrix4x4(
                        new Vector3(dx2.x, 0, dx2.y).normalized,
                        new Vector3(0, 1, 0),
                        new Vector3(dz2.x, 0, dz2.y).normalized,
                        new Vector4(0, 0, 0, 1)
                        );

                    var j1 = new Matrix4x4(tangent3 * dx.magnitude, normal * dy.magnitude, bitangent * dz.magnitude, new Vector4(0, 0, 0, 1));

                    jacobi = j3 * j1;
                }
            }

            Vector3 DeformNormal(Vector3 p, Vector3 v)
            {
                GetJacobi(p, out var jacobi);
                return jacobi.inverse.transpose.MultiplyVector(v).normalized;
            }

            Vector4 DeformTangent(Vector3 p, Vector4 v)
            {
                GetJacobi(p, out var jacobi);
                return jacobi * v;
            }

            return new MeshDeformation(trilinearInterpolatePoint, DeformNormal, DeformTangent, false);
        }
    }
}
