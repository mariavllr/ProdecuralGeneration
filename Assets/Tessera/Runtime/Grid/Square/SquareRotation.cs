using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Represents rotations / reflections of a square
    /// </summary>
    public struct SquareRotation
    {
        short value;

        private SquareRotation(short value)
        {
            this.value = value;
        }

        public bool IsReflection => value < 0;

        public int Rotation => value < 0 ? ~value : value;

        public static SquareRotation Identity => new SquareRotation(0);

        public static SquareRotation ReflectX => new SquareRotation(~2);

        public static SquareRotation ReflectY => new SquareRotation(~0);

        public static SquareRotation RotateCCW => new SquareRotation(1);

        public static SquareRotation Rotate90(int i) => new SquareRotation((short)(((i % 4) + 4) % 4));

        public static SquareRotation[] All => new[]
        {
            new SquareRotation(0),
            new SquareRotation(1),
            new SquareRotation(2),
            new SquareRotation(3),
            new SquareRotation(~0),
            new SquareRotation(~1),
            new SquareRotation(~2),
            new SquareRotation(~3),
        };

        public SquareRotation Invert()
        {
            if (IsReflection)
            {
                return this;
            }
            else
            {
                return new SquareRotation((short)((4 - value) % 4));
            }
        }

        internal MatrixInt3x3 ToMatrixInt()
        {
            MatrixInt3x3 m;
            // TODO: This does not look right
            switch((IsReflection ? 6 - Rotation : Rotation) % 4)
            {
                case 0:
                    m = new MatrixInt3x3 { col1 = Vector3Int.right, col2 = Vector3Int.up };
                    break;
                case 1:
                    m = new MatrixInt3x3 { col1 = new Vector3Int(0, 1, 0), col2 = new Vector3Int(-1, 0, 0) };
                    break;
                case 2:
                    m = new MatrixInt3x3 { col1 = new Vector3Int(-1, 0, 0), col2 = new Vector3Int(0, -1, 0) };
                    break;
                case 3:
                    m = new MatrixInt3x3 { col1 = new Vector3Int(0, -1, 0), col2 = new Vector3Int(1, 0, 0) };
                    break;
                default:
                    throw new Exception();
            }
            m.col3 = new Vector3Int(0, 0, 1);

            if(IsReflection)
            {
                m.col1.x *= -1;
                m.col2.x *= -1;
            }

            return m;
        }

        public override bool Equals(object obj)
        {
            return obj is SquareRotation rotation &&
                   value == rotation.value;
        }

        public override int GetHashCode()
        {
            return 45106587 + value.GetHashCode();
        }

        public static bool operator ==(SquareRotation a, SquareRotation b)
        {
            return a.value == b.value;
        }

        public static bool operator !=(SquareRotation a, SquareRotation b)
        {
            return a.value != b.value;
        }

        public static int operator *(SquareRotation a, int side)
        {
            return (a.IsReflection ? a.Rotation - side + 4 : a.Rotation + side) % 4;
        }

        public static SquareRotation operator *(SquareRotation a, SquareRotation b)
        {
            var isReflection = a.IsReflection ^ b.IsReflection;
            var rotation = a * (b * 0);
            return new SquareRotation(isReflection ? (short)~rotation : (short)rotation);
        }

        public static SquareFaceDir operator *(SquareRotation rotation, SquareFaceDir faceDir)
        {
            var newSide = rotation * faceDir.GetSide();
            return SquareGeometryUtils.FromSide(newSide);
        }

        public static Vector3Int operator *(SquareRotation r, Vector3Int v)
        {
            switch (r.value)
            {
                case 0: break;
                case 1:
                    (v.x, v.y) = (-v.y, v.x);
                    break;
                case 2:
                    (v.x, v.y) = (-v.x, -v.y);
                    break;
                case 3:
                    (v.x, v.y) = (v.y, -v.x);
                    break;
                case ~0:
                    v.y = -v.y;
                    break;
                case ~1:
                    (v.x, v.y) = (v.y, v.x);
                    break;
                case ~2:
                    v.x = -v.x;
                    break;
                case ~3:
                    (v.x, v.y) = (-v.y, -v.x);
                    break;
            }
            return v;
        }

        public static BoundsInt operator *(SquareRotation r, BoundsInt bounds)
        {
            var a = r * bounds.min;
            var b = r * (bounds.max - Vector3Int.one);
            var min = Vector3Int.Min(a, b);
            var max = Vector3Int.Max(a, b);
            return new BoundsInt(min, max - min + Vector3Int.one);
        }

        public static implicit operator SquareRotation(CellRotation r) => new SquareRotation((short)r);

        public static implicit operator CellRotation(SquareRotation r) => (CellRotation)r.value;
    }
}
