using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    internal class SquareGrid : IGrid
    {
        private readonly Vector3 origin;
        private readonly Vector2Int size;
        private readonly Vector2 tileSize;

        public SquareGrid(Vector3 origin, Vector2Int size, Vector2 tileSize)
        {
            this.origin = origin;
            this.size = size;
            this.tileSize = tileSize;
        }

        public Vector3Int Size => new Vector3Int(size.x, size.y, 1);

        public int IndexCount => size.x * size.y;

        public ICellType CellType => SquareCellType.Instance;

        public Vector3Int GetCell(int index)
        {
            var x = index % size.x;
            var y = index / size.x;
            return new Vector3Int(x, y, 0);
        }

        public IEnumerable<Vector3Int> GetCells()
        {
            for (var x = 0; x < size.x; x++)
            {
                for (var y = 0; y < size.y; y++)
                {
                    yield return new Vector3Int(x, y, 0);
                }
            }
        }

        public int GetIndex(Vector3Int cell)
        {
            return cell.x + cell.y * size.x;
        }

        public bool InBounds(Vector3Int cell)
        {
            return SquareGeometryUtils.InBounds(cell, size);
        }

        public Vector3 GetCellCenter(Vector3Int cell)
        {
            return SquareGeometryUtils.GetCellCenter(cell, origin, tileSize);
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
            return SquareGeometryUtils.FindCell(
                origin,
                tileSize,
                tileCenter,
                tileLocalToGridMatrix,
                out cell,
                out rotation);
        }

        public bool FindCell(Vector3 position, out Vector3Int cell)
        {
            return SquareGeometryUtils.FindCell(origin, tileSize, position, out cell);
        }

        public bool TryMove(Vector3Int cell, CellFaceDir faceDir, out Vector3Int dest, out CellFaceDir inverseFaceDir, out CellRotation rotation)
        {
            rotation = SquareRotation.Identity;
            inverseFaceDir = CellType.Invert(faceDir);
            switch((SquareFaceDir)faceDir)
            {
                case SquareFaceDir.Right: cell.x += 1; break;
                case SquareFaceDir.Left: cell.x -= 1; break;
                case SquareFaceDir.Up: cell.y += 1; break;
                case SquareFaceDir.Down: cell.y -= 1; break;
            }
            dest = cell;
            return true;
        }

        public bool TryMoveByOffset(Vector3Int startCell, Vector3Int startOffset, Vector3Int destOffset, CellRotation startRotation, out Vector3Int dest, out CellRotation destRotation)
        {
            var squareRotation = (SquareRotation)startRotation;
            dest = startCell + squareRotation * (destOffset - startOffset);
            destRotation = squareRotation;
            return true;
        }

        public IEnumerable<CellFaceDir> GetValidFaceDirs(Vector3Int cell)
        {
            return SquareCellType.Instance.GetFaceDirs();
        }

        public IEnumerable<CellRotation> GetMoveRotations()
        {
            yield return SquareRotation.Identity;
        }

        public IEnumerable<Vector3Int> GetCellsIntersectsApprox(Bounds bounds, bool useBounds)
        {
            if (SquareGeometryUtils.FindCell(origin, tileSize, bounds.min, out var minCell) &&
                SquareGeometryUtils.FindCell(origin, tileSize, bounds.max, out var maxCell))
            {
                // Filter to in bounds
                if (useBounds)
                {
                    minCell = Vector3Int.Max(minCell, Vector3Int.zero);
                    maxCell = Vector3Int.Min(maxCell, Size - Vector3Int.one);
                }

                // Loop over cels
                for (var x = minCell.x; x <= maxCell.x; x++)
                {
                    for (var y = minCell.y; y <= maxCell.y; y++)
                    {
                        yield return new Vector3Int(x, y, 0);
                    }
                }
            }
        }



        #region Symmetry
        public IEnumerable<(GridSymmetry, Vector3Int)> GetBoundsSymmetries(bool rotatable = true, bool reflectable = true, RotationGroupType rotationGroupType = RotationGroupType.All)
        {
            foreach (SquareRotation r in SquareCellType.Instance.GetRotations(rotatable, reflectable, rotationGroupType))
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
            dest = s.translation + ((SquareRotation)s.rotation) * cell;
            return true;
        }

        #endregion
    }
}