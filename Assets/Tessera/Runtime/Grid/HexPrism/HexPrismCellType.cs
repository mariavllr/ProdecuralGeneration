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
    public class HexPrismCellType : ICellType
    {
        private static HexPrismCellType instance;

        public static HexPrismCellType Instance => instance ?? (instance = new HexPrismCellType());

        public IEnumerable<CellFaceDir> GetFaceDirs() => new[]
        {
            (CellFaceDir)HexPrismFaceDir.Left,
            (CellFaceDir)HexPrismFaceDir.Right,
            (CellFaceDir)HexPrismFaceDir.Up,
            (CellFaceDir)HexPrismFaceDir.Down,
            (CellFaceDir)HexPrismFaceDir.ForwardRight,
            (CellFaceDir)HexPrismFaceDir.ForwardLeft,
            (CellFaceDir)HexPrismFaceDir.BackRight,
            (CellFaceDir)HexPrismFaceDir.BackLeft
        };

        public IEnumerable<(CellFaceDir, CellFaceDir)> GetFaceDirPairs() => new[]
        {
            ((CellFaceDir)HexPrismFaceDir.Right, (CellFaceDir)HexPrismFaceDir.Left),
            ((CellFaceDir)HexPrismFaceDir.Up, (CellFaceDir)HexPrismFaceDir.Down),
            ((CellFaceDir)HexPrismFaceDir.ForwardRight, (CellFaceDir)HexPrismFaceDir.BackLeft),
            ((CellFaceDir)HexPrismFaceDir.ForwardLeft, (CellFaceDir)HexPrismFaceDir.BackRight),
        };

        public CellFaceDir Invert(CellFaceDir faceDir)
        {
            switch ((HexPrismFaceDir)faceDir)
            {
                case HexPrismFaceDir.Left: return (CellFaceDir)HexPrismFaceDir.Right;
                case HexPrismFaceDir.Right: return (CellFaceDir)HexPrismFaceDir.Left;
                case HexPrismFaceDir.Up: return (CellFaceDir)HexPrismFaceDir.Down;
                case HexPrismFaceDir.Down: return (CellFaceDir)HexPrismFaceDir.Up;
                case HexPrismFaceDir.ForwardRight: return (CellFaceDir)HexPrismFaceDir.BackLeft;
                case HexPrismFaceDir.ForwardLeft: return (CellFaceDir)HexPrismFaceDir.BackRight;
                case HexPrismFaceDir.BackRight: return (CellFaceDir)HexPrismFaceDir.ForwardLeft;
                case HexPrismFaceDir.BackLeft: return (CellFaceDir)HexPrismFaceDir.ForwardRight;
                default: throw new Exception();
            }
        }

        private readonly static IDictionary<RotationGroupType, CellRotation[]> RotationsByGroupType = new Dictionary<RotationGroupType, CellRotation[]>()
        {
            {
                RotationGroupType.None,
                new[]
                {
                    (CellRotation)(HexRotation.Identity),
                    (CellRotation)(HexRotation.ReflectX),
                }
            },
            {
                RotationGroupType.XZ,
                HexRotation.All.Select(x => (CellRotation)x).ToArray()

            },
            {
                RotationGroupType.All,
                HexRotation.All.Select(x => (CellRotation)x).ToArray()
            },
        };

        public IList<CellRotation> GetRotations(bool rotatable, bool reflectable, RotationGroupType rotationGroupType)
        {
            if (rotatable)
            {
                var rotations = RotationsByGroupType[rotationGroupType];
                if (reflectable)
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
                        (CellRotation)(HexRotation.Identity),
                        (CellRotation)(HexRotation.ReflectX),
                    };
                }
                else
                {

                    return new[]
                    {
                        (CellRotation)(HexRotation.Identity),
                    };
                }
            }
        }

        public CellRotation Multiply(CellRotation a, CellRotation b)
        {
            return (a * (HexRotation)b);
        }

        public CellRotation Invert(CellRotation a)
        {
            return ((HexRotation)a).Invert();
        }

        public CellRotation GetIdentity()
        {
            return HexRotation.Identity;
        }

        public CellFaceDir Rotate(CellFaceDir faceDir, CellRotation rotation)
        {
            return (CellFaceDir)(((HexRotation)rotation) * ((HexPrismFaceDir)faceDir));
        }

        public (CellFaceDir, FaceDetails) RotateBy(CellFaceDir faceDir, FaceDetails faceDetails, CellRotation rotation)
        {
            var hexPrismFaceDir = (HexPrismFaceDir)faceDir;
            var hexRotation = (HexRotation)rotation;
            if (hexPrismFaceDir.IsUpDown())
            {
                var newFaceDetails = faceDetails.Clone();
                if (hexRotation.IsReflection)
                    newFaceDetails.HexReflectX();
                var ccwRotations = hexPrismFaceDir == HexPrismFaceDir.Up ? 6 - hexRotation.Rotation : hexRotation.Rotation;
                for (var i = 0; i < ccwRotations; i++)
                    newFaceDetails.HexRotateCcw();
                return (faceDir, newFaceDetails);
            }
            else
            {
                var newSide = hexRotation * hexPrismFaceDir.GetSide();
                var newFaceDetails = faceDetails.Clone();
                if (hexRotation.IsReflection)
                    newFaceDetails.ReflectX();
                return ((CellFaceDir)HexGeometryUtils.FromSide(newSide), newFaceDetails);
            }
        }

        public bool TryMove(Vector3Int offset, CellFaceDir dir, out Vector3Int dest)
        {
            dest = offset + ((HexPrismFaceDir)dir).ForwardInt();
            return true;
        }

        public IEnumerable<CellFaceDir> FindPath(Vector3Int startOffset, Vector3Int endOffset)
        {
            var offset = startOffset;

            while (offset.y < endOffset.y)
            {
                yield return (CellFaceDir)HexPrismFaceDir.Up;
                offset.y += 1;
            }
            while (offset.y > endOffset.y)
            {
                yield return (CellFaceDir)HexPrismFaceDir.Down;
                offset.y -= 1;
            }
            while (offset.x < endOffset.x && offset.z < endOffset.z)
            {
                yield return (CellFaceDir)HexPrismFaceDir.ForwardRight;
                offset.x += 1;
                offset.z += 1;
            }
            while (offset.x > endOffset.x && offset.z > endOffset.z)
            {
                yield return (CellFaceDir)HexPrismFaceDir.BackLeft;
                offset.x -= 1;
                offset.z -= 1;
            }
            while (offset.x < endOffset.x)
            {
                yield return (CellFaceDir)HexPrismFaceDir.Right;
                offset.x += 1;
            }
            while (offset.x > endOffset.x)
            {
                yield return (CellFaceDir)HexPrismFaceDir.Left;
                offset.x -= 1;
            }
            while (offset.z < endOffset.z)
            {
                yield return (CellFaceDir)HexPrismFaceDir.ForwardLeft;
                offset.z += 1;
            }
            while (offset.z > endOffset.z)
            {
                yield return (CellFaceDir)HexPrismFaceDir.BackRight;
                offset.z -= 1;
            }
        }

        public Matrix4x4 GetMatrix(CellRotation rotation)
        {
            var hexRotation = (HexRotation)rotation;
            var m = hexRotation.IsReflection ? Matrix4x4.Scale(new Vector3(-1, 1, 1)) : Matrix4x4.identity;
            m = Matrix4x4.Rotate(Quaternion.Euler(0, -60 * hexRotation.Rotation, 0)) * m;
            return m;
        }

        public Vector3 GetCellCenter(Vector3Int offset, Vector3 center, Vector3 tileSize)
        {
            return HexGeometryUtils.GetCellCenter(offset, center, tileSize);
        }

        public bool TryMoveByOffset(Vector3Int startCell, Vector3Int startOffset, Vector3Int destOffset, CellRotation rotation, out Vector3Int destCell)
        {
            destCell = startCell + HexGeometryUtils.Rotate((HexRotation)rotation, destOffset - startOffset);
            return true;
        }


        public IDictionary<Vector3Int, Vector3Int> Realign(ISet<Vector3Int> shape, CellRotation rotation)
        {
            var ccs = shape.Select(HexGeometryUtils.ToCubeCoords).ToList();
            var min = ccs.Aggregate(Vector3Int.Min);
            var max = ccs.Aggregate(Vector3Int.Max);
            var a = HexGeometryUtils.CubeRotate(rotation, min);
            var b = HexGeometryUtils.CubeRotate(rotation, max);
            var newMin = Vector3Int.Min(a, b);
            var translation = min - newMin;
            var result = ccs.Zip(shape, (cc, cell) => (cc, cell))
                .ToDictionary(t => t.cell, t => HexGeometryUtils.FromCubeCords(translation + HexGeometryUtils.CubeRotate(rotation, t.cc), t.cell.y));
            if (!result.Values.All(shape.Contains))
                return null;
            return result;
        }
    }
}
