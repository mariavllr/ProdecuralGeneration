using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    internal class QuadMeshGrid : IGrid
    {
        private readonly ICellType cellType;
        private readonly MeshData surfaceMesh;
        private readonly int layerCount;
        private readonly int[] faceCounts;
        private readonly int maxFaceCount;
        private readonly int subMeshCount;
        private readonly int indexCount;
        private readonly float tileHeight;
        private readonly float surfaceOffset;
        private readonly IDictionary<(Vector3Int, CellFaceDir), (Vector3Int, CellFaceDir, CellRotation)> moves;
        private readonly bool surfaceSmoothNormals;
        private readonly MeshDetails meshDetails;

        public QuadMeshGrid(ICellType cellType, MeshData surfaceMesh, int layerCount, float tileHeight, float surfaceOffset, bool surfaceSmoothNormals, IDictionary<(Vector3Int, CellFaceDir), (Vector3Int, CellFaceDir, CellRotation)> moves)
        {
            if (!(cellType is CubeCellType))
            {
                throw new NotImplementedException();
            }
            this.cellType = cellType;
            this.surfaceMesh = surfaceMesh;
            this.layerCount = layerCount;
            this.tileHeight = tileHeight;
            this.surfaceOffset = surfaceOffset;
            this.surfaceSmoothNormals = surfaceSmoothNormals;
            this.moves = moves;
            this.faceCounts =  Enumerable.Range(0, surfaceMesh.subMeshCount).Select(i => surfaceMesh.indices[i].Length / 4).ToArray();
            this.maxFaceCount = faceCounts.Max();
            this.subMeshCount = surfaceMesh.subMeshCount;
            this.indexCount = maxFaceCount * layerCount * subMeshCount;
            meshDetails = BuildMeshDetails();
        }

        public MeshData SurfaceMesh => surfaceMesh;

        public float TileHeight => tileHeight;
        public float SurfaceOffset => surfaceOffset;
        public bool SurfaceSmoothNormals => surfaceSmoothNormals;


        public int IndexCount => indexCount;

        public ICellType CellType => cellType;

        //public ITopology Topology => topology;

        public Vector3Int GetCell(int index)
        {
            return new Vector3Int(index % maxFaceCount, (index / maxFaceCount) % layerCount, index / maxFaceCount / layerCount);
        }
        public IEnumerable<Vector3Int> GetCells()
        {
            for(var y=0;y<layerCount;y++)
            {
                for(var z=0;z<subMeshCount;z++)
                {
                    for(var x=0;x<faceCounts[z];x++)
                    {
                        yield return new Vector3Int(x, y, z);
                    }
                }
            }
        }

        public bool FindCell(Vector3 tileCenter, Matrix4x4 tileLocalToGridMatrix, out Vector3Int cell, out CellRotation rotation)
        {
            var mt = tileLocalToGridMatrix;
            var p = mt.MultiplyPoint(Vector3.zero);
            var pb = new Bounds(p, Vector3.zero);
            foreach(var c in GetCellsIntersectsApprox(pb, false))
            {
                var trs = GetTRS(c);
                var m = trs.ToMatrix().inverse;
                if (CubeGeometryUtils.FindCell(Vector3.zero, Vector3.one, tileCenter, m  * tileLocalToGridMatrix, out cell, out rotation) && cell == Vector3Int.zero)
                {
                    cell = c;
                    return true;
                }
            }
            cell = default;
            rotation = default;
            return false;
        }

        public bool FindCell(Vector3 position, out Vector3Int cell)
        {
            var pb = new Bounds(position, Vector3.zero);

            foreach (var c in GetCellsIntersectsApprox(pb, false))
            {
                var trs = GetTRS(c);
                var m = trs.ToMatrix().inverse;
                if (CubeGeometryUtils.FindCell(Vector3.zero, Vector3.one, m * position, out cell) && cell == Vector3Int.zero)
                {
                    cell = c;
                    return true;
                }
            }
            cell = default;
            return false;
        }

        public IEnumerable<Vector3Int> GetCellsIntersectsApprox(Bounds bounds, bool useBounds)
        {
            var minHashCell = Vector3Int.Max(meshDetails.hashCellBounds.min, meshDetails.GetHashCell(bounds.min) - Vector3Int.one);
            var maxHashCell = Vector3Int.Min(meshDetails.hashCellBounds.max, meshDetails.GetHashCell(bounds.max) + Vector3Int.one);

            // Use a spatial hash to locate cells near the tile, and test each one.
            for (var x = minHashCell.x; x <= maxHashCell.x; x++)
            {
                for (var y = minHashCell.y; y <= maxHashCell.y; y++)
                {
                    for (var z = minHashCell.z; z <= maxHashCell.z; z++)
                    {
                        var h = new Vector3Int(x, y, z);
                        if (meshDetails.hashedCells.TryGetValue(h, out var cells))
                        {
                            foreach (var c in cells)
                            {
                                yield return c;
                            }
                        }
                    }
                }
            }
        }

        public Vector3 GetCellCenter(Vector3Int cell)
        {
            return GetTRS(cell).Position;
        }

        public TRS GetTRS(Vector3Int cell)
        {
            return meshDetails.trs[cell];
        }


        private TRS GetTRSInner(Vector3Int cell)
        {
            var meshDeformation = MeshUtils.GetDeformation(surfaceMesh, tileHeight, surfaceOffset, surfaceSmoothNormals, cell.x, cell.y, cell.z);
            var center = meshDeformation.DeformPoint(Vector3.zero);
            var e = 1e-4f;
            var x = (meshDeformation.DeformPoint(Vector3.right * e) - center) / e;
            var y = (meshDeformation.DeformPoint(Vector3.up * e) - center) / e;
            var z = (meshDeformation.DeformPoint(Vector3.forward * e) - center) / e;
            var m = new Matrix4x4(x, y, z, new Vector4(center.x, center.y, center.z, 1));

            return new TRS(m);
        }

        public int GetIndex(Vector3Int cell)
        {
            return cell.x + cell.y * maxFaceCount + cell.z * maxFaceCount * layerCount;
        }

        public bool InBounds(Vector3Int cell)
        {
            return 
                0 <= cell.y && cell.y < layerCount && 
                0 <= cell.z && cell.z < subMeshCount &&
                0 <= cell.x && cell.x < faceCounts[cell.z];
        }

        public bool TryMove(Vector3Int cell, CellFaceDir faceDir, out Vector3Int dest, out CellFaceDir inverseFaceDir, out CellRotation rotation)
        {
            if (moves.TryGetValue((cell, faceDir), out var t))
            {
                (dest, inverseFaceDir, rotation) = t;
                return true;
            }
            else
            {
                dest = default;
                inverseFaceDir = default;
                rotation = default;
                return false;
            }
        }

        public bool TryMoveByOffset(Vector3Int startCell, Vector3Int startOffset, Vector3Int destOffset, CellRotation startRotation, out Vector3Int destCell, out CellRotation destRotation)
        {
            return DefaultGridImpl.TryMoveByOffset(this, startCell, startOffset, destOffset, startRotation, out destCell, out destRotation);
        }

        public IEnumerable<CellFaceDir> GetValidFaceDirs(Vector3Int cell)
        {
            return CubeCellType.Instance.GetFaceDirs();
        }

        public IEnumerable<CellRotation> GetMoveRotations()
        {
            yield return CubeRotation.Identity;
            yield return CubeRotation.RotateXZ;
            yield return CubeRotation.RotateXZ * CubeRotation.RotateXZ;
            yield return CubeRotation.RotateXZ * CubeRotation.RotateXZ * CubeRotation.RotateXZ;
        }


        #region Symmetry
        // Not supported
        public IEnumerable<(GridSymmetry, Vector3Int)> GetBoundsSymmetries(bool rotatable = true, bool reflectable = true, RotationGroupType rotationGroupType = RotationGroupType.All)
        {
            yield return (new GridSymmetry { }, Vector3Int.zero);
        }

        public bool TryApplySymmetry(GridSymmetry s, Vector3Int cell, out Vector3Int dest, out CellRotation r)
        {
            dest = cell;
            r = CubeRotation.Identity;
            return true;
        }
        #endregion

        private MeshDetails BuildMeshDetails()
        {
            var trs = new Dictionary<Vector3Int, TRS>();
            var hashCellSize = new Vector3(float.Epsilon, float.Epsilon, float.Epsilon);
            foreach (var cell in GetCells())
            {
                var cellTrs = trs[cell] = GetTRSInner(cell);
                var dim = GeometryUtils.Abs(cellTrs.ToMatrix().MultiplyVector(Vector3.one));
                hashCellSize = Vector3.Max(hashCellSize, dim);
            }
            var meshDetails = new MeshDetails
            {
                trs = trs,
                hashCellSize = hashCellSize,
                hashedCells = new Dictionary<Vector3Int, List<Vector3Int>>(),
            };
            Vector3Int? hashCellMin = null;
            Vector3Int? hashCellMax = null;

            foreach (var cell in GetCells())
            {
                var cellTrs = trs[cell];
                var hashCell = meshDetails.GetHashCell(cellTrs.Position);
                if (!meshDetails.hashedCells.TryGetValue(hashCell, out var cellList))
                {
                    cellList = meshDetails.hashedCells[hashCell] = new List<Vector3Int>();
                }
                cellList.Add(cell);
                hashCellMin = hashCellMin == null ? hashCell : Vector3Int.Min(hashCellMin.Value, hashCell);
                hashCellMax = hashCellMax == null ? hashCell : Vector3Int.Max(hashCellMax.Value, hashCell);
            }
            meshDetails.hashCellBounds = hashCellMin == null ? new BoundsInt() : new BoundsInt(hashCellMin.Value, hashCellMax.Value - hashCellMin.Value);

            return meshDetails;
        }

        // Structure caching some additional data about the mesh
        private class MeshDetails
        {
            public Dictionary<Vector3Int, TRS> trs;
            public Vector3 hashCellSize;
            public BoundsInt hashCellBounds;
            public Dictionary<Vector3Int, List<Vector3Int>> hashedCells;

            public Vector3Int GetHashCell(Vector3 v) => Vector3Int.FloorToInt(new Vector3(v.x / hashCellSize.x, v.y / hashCellSize.y, v.z / hashCellSize.z));
        }
    }

}