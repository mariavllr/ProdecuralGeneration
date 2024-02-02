using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Tessera.HexGeometryUtils;

namespace Tessera
{
    /// <summary>
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    internal class HexPrismGrid : IGrid
    {
        private readonly Vector3 origin;
        private readonly Vector3Int size;
        private readonly Vector3 tileSize;

        public HexPrismGrid(Vector3 origin, Vector3Int size, Vector3 tileSize)
        {
            this.origin = origin;
            this.size = size;
            this.tileSize = tileSize;
        }

        public Vector3Int Size => size;

        public int IndexCount => size.x * size.y * size.z;

        public ICellType CellType => HexPrismCellType.Instance;

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
        public bool FindCell(Vector3 tileCenter, Matrix4x4 tileLocalToGridMatrix, out Vector3Int cell, out CellRotation rotation)
        {
            return HexGeometryUtils.FindCell(origin, tileSize, tileCenter, tileLocalToGridMatrix, out cell, out rotation);
        }

        public bool FindCell(Vector3 position, out Vector3Int cell)
        {
            return HexGeometryUtils.FindCell(origin, tileSize, position, out cell);
        }


        public Vector3 GetCellCenter(Vector3Int cell)
        {
            return HexGeometryUtils.GetCellCenter(cell, origin, tileSize);
        }

        public IEnumerable<Vector3Int> GetCellsIntersectsApprox(Bounds bounds, bool useBounds)
        {
            return GetCells();
            // TODO: Fix this
            /*
            FindCell(bounds.min, out var min);
            FindCell(bounds.max, out var max);
            var minX = min.x;
            var maxX = max.x - (max.z - min.z) / 2;
            // Trim min/max by bounds
            var minY = Math.Max(min.y, 0);
            var maxY = Math.Min(max.y, size.y - 1);
            // Z has some slop due to the pointy ends of hexes
            var minZ = Math.Max(min.z - 1, 0);
            var maxZ = Math.Min(max.z + 1, size.z - 1);

            for (var x = minX; x <= maxX; x++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    var actualX = x + (z - min.z) / 2;

                    if (x < 0 || x >= size.x)
                        continue;

                    for (var y = minY; y <= maxY; y++)
                    {
                        yield return new Vector3Int(actualX, y, z);
                    }
                }
            }
            */
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
            rotation = CellType.GetIdentity();
            inverseFaceDir = CellType.Invert(faceDir);
            switch ((HexPrismFaceDir)faceDir)
            {
                case HexPrismFaceDir.Right: cell.x += 1; break;
                case HexPrismFaceDir.Left: cell.x -= 1; break;
                case HexPrismFaceDir.Up: cell.y += 1; break;
                case HexPrismFaceDir.Down: cell.y -= 1; break;
                case HexPrismFaceDir.ForwardLeft: cell.z += 1; break;
                case HexPrismFaceDir.ForwardRight: cell.x += 1; cell.z += 1; break;
                case HexPrismFaceDir.BackLeft: cell.x -= 1; cell.z -= 1; break;
                case HexPrismFaceDir.BackRight: cell.z -= 1; break;
            }
            dest = cell;
            return true;
        }

        public bool TryMoveByOffset(Vector3Int startCell, Vector3Int startOffset, Vector3Int destOffset, CellRotation startRotation, out Vector3Int destCell, out CellRotation destRotation)
        {
            destRotation = startRotation;
            return HexPrismCellType.Instance.TryMoveByOffset(startCell, startOffset, destOffset, startRotation, out destCell);
        }

        public IEnumerable<CellFaceDir> GetValidFaceDirs(Vector3Int cell)
        {
            return HexPrismCellType.Instance.GetFaceDirs();
        }

        public IEnumerable<CellRotation> GetMoveRotations()
        {
            yield return HexRotation.Identity;
        }

        #region Symmetry

        // Note hexagon bounds work differently from square bounds. This describes
        // a shape that is a parallelogon where all angles are multiples of 60.
        // Hence it can represent a hexagon, and all 3 rhombuses
        // Also all the values used ehre are in cube-coordinates
        public class HexBounds
        {
            // The min/max for each axis.
            // Note that these values are not themselves cube-co-ordinates
            public Vector3Int min { get; set; }
            public Vector3Int max { get; set; }

            // Must be given the extreme (acute) two points of a rhombus.
            public static HexBounds FromRhombus(Vector3Int a, Vector3Int b)
            {
                return new HexBounds
                {
                    min = Vector3Int.Min(a, b),
                    max = Vector3Int.Max(a, b),
                };
            }

            public static HexBounds operator*(HexRotation r, HexBounds b)
            {
                // min/max are not cube co-ordinates, but this works out anyway
                var v1 = CubeRotate(r, b.min);
                var v2 = CubeRotate(r, b.max);
                return new HexBounds
                {
                    min = Vector3Int.Min(v1, v2),
                    max = Vector3Int.Max(v1, v2),
                };
            }

            IEnumerable<Vector3Int> CubeCcs()
            {
                for(var x=min.x;x<max.x;x++)
                {
                    // TODO: Could do tighter bounds for y iteration
                    for(var y = min.y;y<max.y;y++)
                    {
                        var z = -x - y;
                        if (min.z <= z && z <= max.z)
                            yield return new Vector3Int(x, y, z);
                    }
                }
            }
        }

        public IEnumerable<(GridSymmetry, Vector3Int)> GetBoundsSymmetries(bool rotatable = true, bool reflectable = true, RotationGroupType rotationGroupType = RotationGroupType.All)
        {/*
            foreach (HexRotation r in HexPrismCellType.Instance.GetRotations(rotatable, reflectable, rotationGroupType))
            {
                if(r.IsReflection && r.Rotation == 0)
                {
                    rotatable = rotatable;
                }

                var bounds = HexBounds.FromRhombus(
                    ToCubeCoords(new Vector3Int(0, 0, size.z - 1)),
                    ToCubeCoords(new Vector3Int(size.x - 1, 0, 0))
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
            // Translation is in *cube co-ord* space.
            var cc = ToCubeCoords(cell);
            cc = s.translation + CubeRotate(s.rotation, cc);
            dest = FromCubeCords(cc, cell.y);
            r = s.rotation;
            return true;
            */
            dest = default;
            r = default;
            return false;
        }

        #endregion

    }
}
