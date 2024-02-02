using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    internal class CubeGrid : IGrid
    {
        // The center of cell (0, 0, 0)
        private readonly Vector3 origin;
        private readonly Vector3Int size;
        private readonly Vector3 tileSize;

        public CubeGrid(Vector3 origin, Vector3Int size, Vector3 tileSize)
        {
            this.origin = origin;
            this.size = size;
            this.tileSize = tileSize;
        }

        public Vector3Int Size => size;

        public int IndexCount => size.x * size.y * size.z;

        public ICellType CellType => CubeCellType.Instance;

        public Vector3Int GetCell(int index)
        {
            var x = index % size.x;
            var i = index / size.x;
            var y = i % size.y;
            var z = i / size.y;
            return new Vector3Int(x, y, z);
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

        public int GetIndex(Vector3Int cell)
        {
            return cell.x + cell.y * size.x + cell.z * size.x * size.y;
        }

        public bool InBounds(Vector3Int cell)
        {
            return CubeGeometryUtils.InBounds(cell, size);
        }

        public Vector3 GetCellCenter(Vector3Int cell)
        {
            return CubeGeometryUtils.GetCellCenter(cell, origin, tileSize);
        }

        public TRS GetTRS(Vector3Int cell)
        {
            return new TRS(GetCellCenter(cell));
        }

        public bool FindCell(
            Vector3 tileCenter,
            Matrix4x4 tileLocalToGridMatrix,
            out Vector3Int cell,
            out CellRotation rotation)
        {
            return CubeGeometryUtils.FindCell(
                origin,
                tileSize,
                tileCenter,
                tileLocalToGridMatrix,
                out cell,
                out rotation);
        }

        public bool FindCell(Vector3 position, out Vector3Int cell)
        {
            return CubeGeometryUtils.FindCell(origin, tileSize, position, out cell);
        }

        public bool TryMove(Vector3Int cell, CellFaceDir faceDir, out Vector3Int dest, out CellFaceDir inverseFaceDir, out CellRotation rotation)
        {
            rotation = CubeRotation.Identity;
            inverseFaceDir = CellType.Invert(faceDir);
            switch((CubeFaceDir)faceDir)
            {
                case CubeFaceDir.Right: cell.x += 1; break;
                case CubeFaceDir.Left: cell.x -= 1; break;
                case CubeFaceDir.Up: cell.y += 1; break;
                case CubeFaceDir.Down: cell.y -= 1; break;
                case CubeFaceDir.Forward: cell.z += 1; break;
                case CubeFaceDir.Back: cell.z -= 1; break;
            }
            dest = cell;
            return true;
        }

        public bool TryMoveByOffset(Vector3Int startCell, Vector3Int startOffset, Vector3Int destOffset, CellRotation startRotation, out Vector3Int dest, out CellRotation destRotation)
        {
            var cubeRotation = (CubeRotation)startRotation;
            dest = startCell + cubeRotation * (destOffset - startOffset);
            destRotation = cubeRotation;
            return true;
        }

        public IEnumerable<CellFaceDir> GetValidFaceDirs(Vector3Int cell)
        {
            return CubeCellType.Instance.GetFaceDirs();
        }

        public IEnumerable<CellRotation> GetMoveRotations()
        {
            yield return CubeRotation.Identity;
        }

        public IEnumerable<Vector3Int> GetCellsIntersectsApprox(Bounds bounds, bool useBounds)
        {
            if (CubeGeometryUtils.FindCell(origin, tileSize, bounds.min, out var minCell) &&
                CubeGeometryUtils.FindCell(origin, tileSize, bounds.max, out var maxCell))
            {
                if (useBounds)
                {
                    // Filter to in bounds
                    minCell = Vector3Int.Max(minCell, Vector3Int.zero);
                    maxCell = Vector3Int.Min(maxCell, size - Vector3Int.one);
                }

                // Loop over cels
                for (var x = minCell.x; x <= maxCell.x; x++)
                {
                    for (var y = minCell.y; y <= maxCell.y; y++)
                    {
                        for (var z = minCell.z; z <= maxCell.z; z++)
                        {
                            yield return new Vector3Int(x, y, z); ;
                        }
                    }
                }
            }
        }

        #region Symmetry
        public IEnumerable<(GridSymmetry, Vector3Int)> GetBoundsSymmetries(bool rotatable = true, bool reflectable = true, RotationGroupType rotationGroupType = RotationGroupType.All)
        {
            foreach(CubeRotation r in CubeCellType.Instance.GetRotations(rotatable, reflectable, rotationGroupType))
            {
                var bounds = new BoundsInt(Vector3Int.zero, Size);
                var rBounds = r * bounds;
                yield return (
                    new GridSymmetry { rotation = r, translation = Vector3Int.zero - rBounds.min },
                    rBounds.size
                );
            }
        }

        public bool TryApplySymmetry(GridSymmetry s, Vector3Int cell, out Vector3Int dest, out CellRotation r)
        {
            r = s.rotation;
            dest = s.translation + ((CubeRotation)s.rotation) * cell;
            return true;
        }

        #endregion
    }
}