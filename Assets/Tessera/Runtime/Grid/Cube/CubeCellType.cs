using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    // Cubes
    public class CubeCellType : ICellType
    {
        private static CubeCellType instance;

        public static CubeCellType Instance => instance ?? (instance = new CubeCellType());

        public IEnumerable<CellFaceDir> GetFaceDirs() => new[]
        {
            (CellFaceDir)CubeFaceDir.Right,
            (CellFaceDir)CubeFaceDir.Left,
            (CellFaceDir)CubeFaceDir.Up,
            (CellFaceDir)CubeFaceDir.Down,
            (CellFaceDir)CubeFaceDir.Forward,
            (CellFaceDir)CubeFaceDir.Back,
        };

        public IEnumerable<(CellFaceDir, CellFaceDir)> GetFaceDirPairs() => new[]
        {
            ((CellFaceDir)CubeFaceDir.Right, (CellFaceDir)CubeFaceDir.Left),
            ((CellFaceDir)CubeFaceDir.Up, (CellFaceDir)CubeFaceDir.Down),
            ((CellFaceDir)CubeFaceDir.Forward, (CellFaceDir)CubeFaceDir.Back),
        };

        public CellFaceDir Invert(CellFaceDir faceDir)
        {
            return (CellFaceDir)(1 ^ (int)faceDir);
        }

        private readonly static IDictionary<RotationGroupType, CellRotation[]> RotationsByGroupType = new Dictionary<RotationGroupType, CellRotation[]>()
        {
            {
                RotationGroupType.None,
                new[]
                {
                    (CellRotation)(CubeRotation.Identity),
                    (CellRotation)(CubeRotation.ReflectX),
                }
            },
            {
                RotationGroupType.XZ,
                new[]
                {
                    (CellRotation)(CubeRotation.Identity),
                    (CellRotation)(CubeRotation.RotateXZ),
                    (CellRotation)(CubeRotation.RotateXZ * CubeRotation.RotateXZ),
                    (CellRotation)(CubeRotation.RotateXZ * CubeRotation.RotateXZ * CubeRotation.RotateXZ),
                    (CellRotation)(CubeRotation.ReflectX * CubeRotation.Identity),
                    (CellRotation)(CubeRotation.ReflectX * CubeRotation.RotateXZ),
                    (CellRotation)(CubeRotation.ReflectX * CubeRotation.RotateXZ * CubeRotation.RotateXZ),
                    (CellRotation)(CubeRotation.ReflectX * CubeRotation.RotateXZ * CubeRotation.RotateXZ * CubeRotation.RotateXZ),
                }
            },
            {
                RotationGroupType.XY,
                new[]
                {
                    (CellRotation)(CubeRotation.Identity),
                    (CellRotation)(CubeRotation.RotateXY),
                    (CellRotation)(CubeRotation.RotateXY * CubeRotation.RotateXY),
                    (CellRotation)(CubeRotation.RotateXY * CubeRotation.RotateXY * CubeRotation.RotateXY),
                    (CellRotation)(CubeRotation.ReflectX * CubeRotation.Identity),
                    (CellRotation)(CubeRotation.ReflectX * CubeRotation.RotateXY),
                    (CellRotation)(CubeRotation.ReflectX * CubeRotation.RotateXY * CubeRotation.RotateXY),
                    (CellRotation)(CubeRotation.ReflectX * CubeRotation.RotateXY * CubeRotation.RotateXY * CubeRotation.RotateXY),
                }
            },
            {
                RotationGroupType.YZ,
                new[]
                {
                    (CellRotation)(CubeRotation.Identity),
                    (CellRotation)(CubeRotation.RotateYZ),
                    (CellRotation)(CubeRotation.RotateYZ * CubeRotation.RotateYZ),
                    (CellRotation)(CubeRotation.RotateYZ * CubeRotation.RotateYZ * CubeRotation.RotateYZ),
                    (CellRotation)(CubeRotation.ReflectX * CubeRotation.Identity),
                    (CellRotation)(CubeRotation.ReflectX * CubeRotation.RotateYZ),
                    (CellRotation)(CubeRotation.ReflectX * CubeRotation.RotateYZ * CubeRotation.RotateYZ),
                    (CellRotation)(CubeRotation.ReflectX * CubeRotation.RotateYZ * CubeRotation.RotateYZ * CubeRotation.RotateYZ),
                }
            },
            {
                RotationGroupType.All,
                CubeRotation.All.Select(x => (CellRotation)x).ToArray()
            },
        };

        public IList<CellRotation> GetRotations(bool rotatable, bool reflectable, RotationGroupType rotationGroupType)
        {
            if (rotatable)
            {
                var rotations = RotationsByGroupType[rotationGroupType];
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
                        (CellRotation)(CubeRotation.Identity),
                        (CellRotation)(CubeRotation.ReflectX),
                    };
                }
                else
                {

                    return new[]
                    {
                        (CellRotation)(CubeRotation.Identity),
                    };
                }
            }
        }

        public CellRotation Multiply(CellRotation a, CellRotation b)
        {
            return (a * (CubeRotation)b);
        }

        public CellRotation Invert(CellRotation a)
        {
            return ((CubeRotation)a).Invert();
        }

        public CellRotation GetIdentity()
        {
            return CubeRotation.Identity;
        }

        public CellFaceDir Rotate(CellFaceDir faceDir, CellRotation rotation)
        {
            var cubeRotation = (CubeRotation)rotation;
            var cubeDir = (CubeFaceDir)faceDir;
            return (CellFaceDir)(cubeRotation * cubeDir);
        }

        public (CellFaceDir, FaceDetails) RotateBy(CellFaceDir faceDir, FaceDetails faceDetails, CellRotation rot)
        {
            var cubeRotation = (CubeRotation)rot;

            var (a, b) = CubeGeometryUtils.RotateBy((CubeFaceDir)faceDir, faceDetails, cubeRotation.ToMatrixInt());
            return ((CellFaceDir)a, b);
        }

        public bool TryMove(Vector3Int offset, CellFaceDir dir, out Vector3Int dest)
        {
            dest = offset + ((CubeFaceDir)dir).Forward();
            return true;
        }

        public IEnumerable<CellFaceDir> FindPath(Vector3Int startOffset, Vector3Int endOffset)
        {
            var offset = startOffset;
            while (offset.x < endOffset.x)
            {
                yield return (CellFaceDir)CubeFaceDir.Right;
                offset.x += 1;
            }
            while (offset.x > endOffset.x)
            {
                yield return (CellFaceDir)CubeFaceDir.Left;
                offset.x -= 1;
            }
            while (offset.y < endOffset.y)
            {
                yield return (CellFaceDir)CubeFaceDir.Up;
                offset.y += 1;
            }
            while (offset.y > endOffset.y)
            {
                yield return (CellFaceDir)CubeFaceDir.Down;
                offset.y -= 1;
            }
            while (offset.z < endOffset.z)
            {
                yield return (CellFaceDir)CubeFaceDir.Forward;
                offset.z += 1;
            }
            while (offset.z > endOffset.z)
            {
                yield return (CellFaceDir)CubeFaceDir.Back;
                offset.z -= 1;
            }
        }

        public Matrix4x4 GetMatrix(CellRotation cellRotation)
        {
            var m = ((CubeRotation)cellRotation).ToMatrixInt();
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
            destCell = startCell + ((CubeRotation)rotation) * (destOffset - startOffset);
            return true;
        }

        public IDictionary<Vector3Int, Vector3Int> Realign(ISet<Vector3Int> shape, CellRotation rotation)
        {
            var cubeRotation = ((CubeRotation)rotation);
            var min = shape.Aggregate(Vector3Int.Min);
            var max = shape.Aggregate(Vector3Int.Max);
            var bounds = cubeRotation * new BoundsInt(min, max - min + Vector3Int.one);
            var translation = bounds.min - min;
            var result = shape.ToDictionary(offset => offset, offset => translation + cubeRotation * offset);
            if (!result.Values.All(shape.Contains))
                return null;
            return result;
        }
    }
}
