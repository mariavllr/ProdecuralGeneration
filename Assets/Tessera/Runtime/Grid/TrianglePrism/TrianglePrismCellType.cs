using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Tessera.TrianglePrismGeometryUtils;

namespace Tessera
{
    /// <summary>
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    public class TrianglePrismCellType : ICellType
    {
        private static TrianglePrismCellType instance;

        public static TrianglePrismCellType Instance => instance ?? (instance = new TrianglePrismCellType());

        public IEnumerable<CellFaceDir> FindPath(Vector3Int startOffset, Vector3Int endOffset)
        {
            var offset = startOffset;
            var pointsUp = TrianglePrismGeometryUtils.PointsUp(offset);
            while (offset != endOffset)
            {
                if(offset.y > endOffset.y)
                {
                    offset.y -= 1;
                    yield return (CellFaceDir)TrianglePrismFaceDir.Down;
                    continue;
                }
                if (offset.y < endOffset.y)
                {
                    offset.y += 1;
                    yield return (CellFaceDir)TrianglePrismFaceDir.Up;
                    continue;
                }
                var dx = endOffset.x - offset.x;
                var dz = endOffset.z - offset.z;
                if (pointsUp)
                {
                    if (dz < 0)
                    {
                        offset += TrianglePrismFaceDir.Back.OffsetDelta();
                        yield return (CellFaceDir)TrianglePrismFaceDir.Back;
                    }
                    else if(dx + dz > 0)
                    {
                        offset += TrianglePrismFaceDir.ForwardRight.OffsetDelta();
                        yield return (CellFaceDir)TrianglePrismFaceDir.ForwardRight;
                    }
                    else
                    {
                        offset += TrianglePrismFaceDir.ForwardLeft.OffsetDelta();
                        yield return (CellFaceDir)TrianglePrismFaceDir.ForwardLeft;
                    }
                }
                else
                {
                    if (dz > 0)
                    {
                        offset += TrianglePrismFaceDir.Forward.OffsetDelta();
                        yield return (CellFaceDir)TrianglePrismFaceDir.Forward;
                    }
                    else if (dx + dz > 0)
                    {
                        offset += TrianglePrismFaceDir.BackRight.OffsetDelta();
                        yield return (CellFaceDir)TrianglePrismFaceDir.BackRight;
                    }
                    else
                    {
                        offset += TrianglePrismFaceDir.BackLeft.OffsetDelta();
                        yield return (CellFaceDir)TrianglePrismFaceDir.BackLeft;
                    }
                }
                pointsUp = !pointsUp;
                
            }
        }

        public Vector3 GetCellCenter(Vector3Int offset, Vector3 center, Vector3 tileSize)
        {
            return TrianglePrismGeometryUtils.GetCellCenter(offset, center, tileSize);
        }

        public IEnumerable<CellFaceDir> GetFaceDirs() => new[]
        {
            (CellFaceDir)TrianglePrismFaceDir.Forward,
            (CellFaceDir)TrianglePrismFaceDir.Back,
            (CellFaceDir)TrianglePrismFaceDir.Up,
            (CellFaceDir)TrianglePrismFaceDir.Down,
            (CellFaceDir)TrianglePrismFaceDir.ForwardRight,
            (CellFaceDir)TrianglePrismFaceDir.ForwardLeft,
            (CellFaceDir)TrianglePrismFaceDir.BackRight,
            (CellFaceDir)TrianglePrismFaceDir.BackLeft
        };

        public IEnumerable<(CellFaceDir, CellFaceDir)> GetFaceDirPairs() => new[]
        {
            ((CellFaceDir)TrianglePrismFaceDir.Forward, (CellFaceDir)TrianglePrismFaceDir.Back),
            ((CellFaceDir)TrianglePrismFaceDir.Up, (CellFaceDir)TrianglePrismFaceDir.Down),
            ((CellFaceDir)TrianglePrismFaceDir.ForwardRight, (CellFaceDir)TrianglePrismFaceDir.BackLeft),
            ((CellFaceDir)TrianglePrismFaceDir.ForwardLeft, (CellFaceDir)TrianglePrismFaceDir.BackRight),
        };

        public CellRotation GetIdentity()
        {
            return TriangleRotation.Identity;
        }

        public Matrix4x4 GetMatrix(CellRotation rotation)
        {
            var triRotation = (TriangleRotation)rotation;
            var m = triRotation.IsReflection ? Matrix4x4.Scale(new Vector3(-1, 1, 1)) : Matrix4x4.identity;
            m = Matrix4x4.Rotate(Quaternion.Euler(0, -60 * triRotation.Rotation, 0)) * m;
            return m;
        }

        private readonly static IDictionary<RotationGroupType, CellRotation[]> RotationsByGroupType = new Dictionary<RotationGroupType, CellRotation[]>()
        {
            {
                RotationGroupType.None,
                new[]
                {
                    (CellRotation)(TriangleRotation.Identity),
                    (CellRotation)(TriangleRotation.ReflectX),
                }
            },
            {
                RotationGroupType.XZ,
                TriangleRotation.All.Select(x => (CellRotation)x).ToArray()

            },
            {
                RotationGroupType.All,
                TriangleRotation.All.Select(x => (CellRotation)x).ToArray()
            },
        };

        public IList<CellRotation> GetRotations(bool rotatable = true, bool reflectable = true, RotationGroupType rotationGroupType = RotationGroupType.All)
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
                        (CellRotation)(TriangleRotation.Identity),
                        (CellRotation)(TriangleRotation.ReflectX),
                    };
                }
                else
                {

                    return new[]
                    {
                        (CellRotation)(TriangleRotation.Identity),
                    };
                }
            }
        }

        public CellFaceDir Invert(CellFaceDir faceDir)
        {
            switch ((TrianglePrismFaceDir)faceDir)
            {
                case TrianglePrismFaceDir.Forward: return (CellFaceDir)TrianglePrismFaceDir.Back;
                case TrianglePrismFaceDir.Back: return (CellFaceDir)TrianglePrismFaceDir.Forward;
                case TrianglePrismFaceDir.Up: return (CellFaceDir)TrianglePrismFaceDir.Down;
                case TrianglePrismFaceDir.Down: return (CellFaceDir)TrianglePrismFaceDir.Up;
                case TrianglePrismFaceDir.ForwardRight: return (CellFaceDir)TrianglePrismFaceDir.BackLeft;
                case TrianglePrismFaceDir.ForwardLeft: return (CellFaceDir)TrianglePrismFaceDir.BackRight;
                case TrianglePrismFaceDir.BackRight: return (CellFaceDir)TrianglePrismFaceDir.ForwardLeft;
                case TrianglePrismFaceDir.BackLeft: return (CellFaceDir)TrianglePrismFaceDir.ForwardRight;
                default: throw new Exception();
            }
        }

        public CellRotation Invert(CellRotation a)
        {
            return ((TriangleRotation)a).Invert();
        }

        public CellRotation Multiply(CellRotation a, CellRotation b)
        {
            return (a * (TriangleRotation)b);
        }

        public CellFaceDir Rotate(CellFaceDir faceDir, CellRotation rotation)
        {
            return (CellFaceDir)(((TriangleRotation)rotation) * ((TrianglePrismFaceDir)faceDir));
        }

        public (CellFaceDir, FaceDetails) RotateBy(CellFaceDir faceDir, FaceDetails faceDetails, CellRotation rotation)
        {
            var trianglePrismFaceDir = (TrianglePrismFaceDir)faceDir;
            var triangleRotation = (TriangleRotation)rotation;
            if (trianglePrismFaceDir.IsUpDown())
            {
                var newFaceDetails = faceDetails.Clone();
                if (triangleRotation.IsReflection)
                    newFaceDetails.TriangleReflectX();
                var cwRotations = trianglePrismFaceDir == TrianglePrismFaceDir.Up ? triangleRotation.Rotation : 6 - triangleRotation.Rotation;
                for (var i = 0; i < cwRotations; i++)
                    newFaceDetails.TriangleRotateCcw60();
                return (faceDir, newFaceDetails);
            }
            else
            {
                var newSide = triangleRotation * trianglePrismFaceDir.GetSide();
                var newFaceDetails = faceDetails.Clone();
                if (triangleRotation.IsReflection)
                    newFaceDetails.ReflectX();
                return ((CellFaceDir)TrianglePrismGeometryUtils.FromSide(newSide), newFaceDetails);
            }
        }

        public bool TryMove(Vector3Int offset, CellFaceDir dir, out Vector3Int dest)
        {
            dest = offset + ((TrianglePrismFaceDir)dir).OffsetDelta();
            return ((TrianglePrismFaceDir)dir).IsValid(offset);
        }

        public bool TryMoveByOffset(Vector3Int startCell, Vector3Int startOffset, Vector3Int destOffset, CellRotation rotation, out Vector3Int destCell)
        {
            var c1 = ToTriCoords(startCell);
            var c2 = ToTriCoords(destOffset);
            var c3 = ToTriCoords(startOffset);
            var destCc = c1 + CoordRotate(rotation, c2 - c3);
            var r = FromTriCoords(destCc, startCell.y + destOffset.y - startOffset.y);
            if(r == null)
            {
                destCell = default;
                return false;
            }
            else
            {
                destCell = r.Value;
                return true;
            }
        }

        public IDictionary<Vector3Int, Vector3Int> Realign(ISet<Vector3Int> shape, CellRotation rotation)
        {
            var ccs = shape.Select(TrianglePrismGeometryUtils.ToTriCoords).ToList();
            var min = ccs.Aggregate(Vector3Int.Min);
            var max = ccs.Aggregate(Vector3Int.Max);
            var a = TrianglePrismGeometryUtils.CoordRotate(rotation, min);
            var b = TrianglePrismGeometryUtils.CoordRotate(rotation, max);
            var newMin = Vector3Int.Min(a, b);
            var translation = min - newMin;
            // Check that the combination of rotation and translation actually works
            if ((((TriangleRotation)rotation).Rotation % 2 == 1) ^ (translation.x + translation.y + translation.z != 0))
                return null;
            var result = new Dictionary<Vector3Int, Vector3Int>();
            foreach(var (cc, cell) in ccs.Zip(shape, (cc, cell) => (cc, cell)))
            {
                var from = cell;
                var to = TrianglePrismGeometryUtils.FromTriCoords(translation + TrianglePrismGeometryUtils.CoordRotate(rotation, cc), cell.y);
                if (from ==null || to == null)
                    return null;
                result[from] = to.Value;
            }
            if (!result.Values.All(shape.Contains))
                return null;
            return result;
        }
    }
}
