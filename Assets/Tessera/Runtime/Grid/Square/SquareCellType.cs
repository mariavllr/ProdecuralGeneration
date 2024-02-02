using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    public class SquareCellType : ICellType
    {
        private static SquareCellType instance;

        public static SquareCellType Instance => instance ?? (instance = new SquareCellType());

        public IEnumerable<CellFaceDir> GetFaceDirs() => new[]
        {
            (CellFaceDir)SquareFaceDir.Right,
            (CellFaceDir)SquareFaceDir.Left,
            (CellFaceDir)SquareFaceDir.Up,
            (CellFaceDir)SquareFaceDir.Down,
        };

        public IEnumerable<(CellFaceDir, CellFaceDir)> GetFaceDirPairs() => new[]
        {
            ((CellFaceDir)SquareFaceDir.Right, (CellFaceDir)SquareFaceDir.Left),
            ((CellFaceDir)SquareFaceDir.Up, (CellFaceDir)SquareFaceDir.Down),
        };

        public CellFaceDir Invert(CellFaceDir faceDir)
        {
            return (CellFaceDir)((4 + (int)faceDir) % 8);
        }

        private readonly static IDictionary<RotationGroupType, CellRotation[]> RotationsByGroupType = new Dictionary<RotationGroupType, CellRotation[]>()
        {
            {
                RotationGroupType.None,
                new[]
                {
                    (CellRotation)(SquareRotation.Identity),
                    (CellRotation)(SquareRotation.ReflectX),
                }
            },
            {
                RotationGroupType.XY,
                SquareRotation.All.Select(x => (CellRotation)x).ToArray()
            },
            {
                RotationGroupType.All,
                SquareRotation.All.Select(x => (CellRotation)x).ToArray()
            },
        };

        public IList<CellRotation> GetRotations(bool rotatable, bool reflectable, RotationGroupType rotationGroupType)
        {
            if (rotatable)
            {
                if(!RotationsByGroupType.TryGetValue(rotationGroupType, out var rotations))
                {
                    throw new Exception($"Couldn't find rotation group {rotationGroupType}");
                }
                if(reflectable)
                {
                    return rotations;
                }
                else
                {
                    return new ArraySegment<CellRotation>(rotations, 0, rotations.Length / 2);
                }
            }
            else
            {

                if (reflectable)
                {
                    return new[]
                    {
                        (CellRotation)(SquareRotation.Identity),
                        (CellRotation)(SquareRotation.ReflectX),
                    };
                }
                else
                {

                    return new[]
                    {
                        (CellRotation)(SquareRotation.Identity),
                    };
                }
            }
        }

        public CellRotation Multiply(CellRotation a, CellRotation b)
        {
            return (a * (SquareRotation)b);
        }

        public CellRotation Invert(CellRotation a)
        {
            return ((SquareRotation)a).Invert();
        }

        public CellRotation GetIdentity()
        {
            return SquareRotation.Identity;
        }

        public CellFaceDir Rotate(CellFaceDir faceDir, CellRotation rotation)
        {
            var squareRotation = (SquareRotation)rotation;
            var squareDir = (SquareFaceDir)faceDir;
            return (CellFaceDir)(squareRotation * squareDir);
        }

        public (CellFaceDir, FaceDetails) RotateBy(CellFaceDir faceDir, FaceDetails faceDetails, CellRotation rot)
        {
            var (a, b) = SquareGeometryUtils.RotateBy((SquareFaceDir)faceDir, faceDetails, rot);
            return ((CellFaceDir)a, b);
        }

        public bool TryMove(Vector3Int offset, CellFaceDir dir, out Vector3Int dest)
        {
            dest = offset + ((SquareFaceDir)dir).Forward();
            return true;
        }

        public IEnumerable<CellFaceDir> FindPath(Vector3Int startOffset, Vector3Int endOffset)
        {
            var offset = startOffset;
            while (offset.x < endOffset.x)
            {
                yield return (CellFaceDir)SquareFaceDir.Right;
                offset.x += 1;
            }
            while (offset.x > endOffset.x)
            {
                yield return (CellFaceDir)SquareFaceDir.Left;
                offset.x -= 1;
            }
            while (offset.y < endOffset.y)
            {
                yield return (CellFaceDir)SquareFaceDir.Up;
                offset.y += 1;
            }
            while (offset.y > endOffset.y)
            {
                yield return (CellFaceDir)SquareFaceDir.Down;
                offset.y -= 1;
            }
        }

        public Matrix4x4 GetMatrix(CellRotation cellRotation)
        {
            var m = ((SquareRotation)cellRotation).ToMatrixInt();
            return new Matrix4x4(
                new Vector4(m.col1.x, m.col1.y, m.col1.z, 0),
                new Vector4(m.col2.x, m.col2.y, m.col2.z, 0),
                new Vector4(m.col3.x, m.col3.y, m.col3.z, 0),
                new Vector4(0, 0, 0, 1));
        }

        public Vector3 GetCellCenter(Vector3Int offset, Vector3 center, Vector3 tileSize)
        {
            return center + Vector3.Scale(offset, tileSize);
        }

        public bool TryMoveByOffset(Vector3Int startCell, Vector3Int startOffset, Vector3Int destOffset, CellRotation rotation, out Vector3Int destCell)
        {
            destCell = startCell + ((SquareRotation)rotation) * (destOffset - startOffset);
            return true;
        }

        public IDictionary<Vector3Int, Vector3Int> Realign(ISet<Vector3Int> shape, CellRotation rotation)
        {
            var m = ((SquareRotation)rotation).ToMatrixInt();
            var min = shape.Aggregate(Vector3Int.Min);
            var max = shape.Aggregate(Vector3Int.Max);
            var r1 = m.Multiply(min);
            var r2 = m.Multiply(max);
            var newMin = Vector3Int.Min(r1, r2);
            var translation = min - newMin;
            var result = shape.ToDictionary(offset => offset, offset => translation + m.Multiply(offset));
            if (!result.Values.All(shape.Contains))
                return null;
            return result;
        }
    }
}
