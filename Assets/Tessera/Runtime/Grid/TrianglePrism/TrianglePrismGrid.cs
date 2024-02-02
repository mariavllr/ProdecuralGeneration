using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Tessera.TrianglePrismGeometryUtils;

namespace Tessera
{
    /// <summary>
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    internal class TrianglePrismGrid : IGrid
    {
        private readonly Vector3 origin;
        private readonly Vector3Int size;
        private readonly Vector3 tileSize;

        public TrianglePrismGrid(Vector3 origin, Vector3Int size, Vector3 tileSize)
        {
            this.origin = origin;
            this.size = size;
            this.tileSize = tileSize;
        }

        public ICellType CellType => TrianglePrismCellType.Instance;

        public int IndexCount => size.x * size.y * size.z;

        public Vector3Int GetCell(int index)
        {
            var x = index % size.x;
            var i = index / size.x;
            var y = i % size.y;
            var z = i / size.y;
            return new Vector3Int(x, y, z);
        }

        public bool FindCell(Vector3 tileCenter, Matrix4x4 tileLocalToGridMatrix, out Vector3Int cell, out CellRotation rotation)
        {
            return TrianglePrismGeometryUtils.FindCell(origin, tileSize, tileCenter, tileLocalToGridMatrix, out cell, out rotation);
        }

        public bool FindCell(Vector3 position, out Vector3Int cell)
        {
            return TrianglePrismGeometryUtils.FindCell(origin, tileSize, position, out cell);
        }

        public Vector3 GetCellCenter(Vector3Int cell)
        {
            return TrianglePrismGeometryUtils.GetCellCenter(cell, origin, tileSize);
        }

        public IEnumerable<Vector3Int> GetCells()
        {
            for (var x = 0; x < size.x; x++)
            {
                for (var y = 0; y < size.y; y++)
                {
                    for (var z = 0; z < size.z; z++)
                    {
                        yield return new Vector3Int(x, y, z);
                    }
                }
            }
        }

        public IEnumerable<Vector3Int> GetCellsIntersectsApprox(Bounds bounds, bool useBounds)
        {
            // TODO: Perf
            return GetCells();
        }

        public int GetIndex(Vector3Int cell)
        {
            return cell.x + cell.y * size.x + cell.z * size.x * size.y;
        }

        public TRS GetTRS(Vector3Int cell)
        {
            return new TRS(GetCellCenter(cell));
        }

        public bool InBounds(Vector3Int cell)
        {
            return CubeGeometryUtils.InBounds(cell, size);
        }

        public bool TryMove(Vector3Int cell, CellFaceDir faceDir, out Vector3Int dest, out CellFaceDir inverseFaceDir, out CellRotation rotation)
        {
            rotation = TriangleRotation.Identity;
            inverseFaceDir = TrianglePrismCellType.Instance.Invert(faceDir);
            dest = cell + ((TrianglePrismFaceDir)faceDir).OffsetDelta();
            return ((TrianglePrismFaceDir)faceDir).IsValid(cell);
        }

        public bool TryMoveByOffset(Vector3Int startCell, Vector3Int startOffset, Vector3Int destOffset, CellRotation startRotation, out Vector3Int destCell, out CellRotation destRotation)
        {
            destRotation = startRotation;
            return TrianglePrismCellType.Instance.TryMoveByOffset(startCell, startOffset, destOffset, startRotation, out destCell);
        }

        private static CellFaceDir[] PointsUpDirs = TrianglePrismCellType.Instance.GetFaceDirs()
            .Where(d => ((TrianglePrismFaceDir)d).IsValid(true))
            .ToArray();

        private static CellFaceDir[] PointsDownDirs = TrianglePrismCellType.Instance.GetFaceDirs()
            .Where(d => ((TrianglePrismFaceDir)d).IsValid(false))
            .ToArray();

        public IEnumerable<CellFaceDir> GetValidFaceDirs(Vector3Int cell)
        {
            return TrianglePrismGeometryUtils.PointsUp(cell) ? PointsUpDirs : PointsDownDirs;
        }

        public IEnumerable<CellRotation> GetMoveRotations()
        {
            yield return TriangleRotation.Identity;
        }

        #region Symmetry


        // See HexBounds which is similar but better documented
        public class TriBounds
        {
            public Vector3Int min { get; set; }
            public Vector3Int max { get; set; }

            public static TriBounds FromRhombus(Vector3Int a, Vector3Int b)
            {
                return new TriBounds
                {
                    min = Vector3Int.Min(a, b),
                    max = Vector3Int.Max(a, b),
                };
            }

            public static TriBounds operator *(TriangleRotation r, TriBounds b)
            {
                // min/max are not cube co-ordinates, but this works out anyway
                var v1 = CoordRotate(r, b.min);
                var v2 = CoordRotate(r, b.max);
                return new TriBounds
                {
                    min = Vector3Int.Min(v1, v2),
                    max = Vector3Int.Max(v1, v2),
                };
            }

            IEnumerable<Vector3Int> Tris()
            {
                for (var x = min.x; x < max.x; x++)
                {
                    // TODO: Could do tighter bounds for y iteration
                    for (var y = min.y; y < max.y; y++)
                    {
                        var z = -x - y;
                        if (min.z <= z && z <= max.z)
                            yield return new Vector3Int(x, y, z);
                        z = -1 - x - y;
                        if (min.z <= z && z <= max.z)
                            yield return new Vector3Int(x, y, z);
                    }
                }
            }
        }

        public IEnumerable<(GridSymmetry, Vector3Int)> GetBoundsSymmetries(bool rotatable = true, bool reflectable = true, RotationGroupType rotationGroupType = RotationGroupType.All)
        {
            /*
            foreach (TriangleRotation r in TrianglePrismCellType.Instance.GetRotations(rotatable, reflectable, rotationGroupType))
            {
                var bounds = TriBounds.FromRhombus(
                    ToTriCoords(Vector3Int.zero),
                    ToTriCoords(size - Vector3Int.one)
                    );

                var rBounds = r * bounds;

                yield return (
                    new GridSymmetry { rotation = r, translation = -rBounds.min },
                    Vector3Int.zero// Currently, HexPrismGrid cannot be rotated easily as it assumes it's always a rhombus in a particular orientation.
                );
            }
            */
            // TODO: This method doesn't really make sense until we can specify the bounds in a more sophisticated way.
            return new (GridSymmetry, Vector3Int)[0];
        }

        public bool TryApplySymmetry(GridSymmetry s, Vector3Int cell, out Vector3Int dest, out CellRotation r)
        {
            /*
            // Translation is in *co-ord* space.
            var cc = ToTriCoords(cell);
            cc = s.translation + CoordRotate(s.rotation, cc);
            var result = FromTriCoords(cc, cell.y);
            r = s.rotation;
            if (result == null)
            {
                dest = default;
                return false;
            }
            else
            {
                dest = result.Value;
                return true;
            }
            */
            dest = default;
            r = default;
            return false;
        }

        #endregion
    }
}
