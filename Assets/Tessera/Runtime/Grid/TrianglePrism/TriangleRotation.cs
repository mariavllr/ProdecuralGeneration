using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tessera
{
    /// <summary>
    /// Represents rotations / reflections of a hexagon
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    public struct TriangleRotation
    {
        short value;

        private TriangleRotation(short value)
        {
            this.value = value;
        }

        public bool IsReflection => value < 0;

        // Measured in multiples of 60 degrees
        public int Rotation => value < 0 ? ~value : value;

        public static TriangleRotation Identity => new TriangleRotation(0);

        public static TriangleRotation ReflectX => new TriangleRotation(~0);

        public static TriangleRotation ReflectY => new TriangleRotation(~3);

        public static TriangleRotation RotateCCW => new TriangleRotation(2);

        public static TriangleRotation RotateCW => new TriangleRotation(4);

        public static TriangleRotation RotateCCW60(int i) => new TriangleRotation((short)(((i % 6) + 6) % 6));

        public static TriangleRotation[] All => new[]
        {
            new TriangleRotation(0),
            new TriangleRotation(1),
            new TriangleRotation(2),
            new TriangleRotation(3),
            new TriangleRotation(4),
            new TriangleRotation(5),
            new TriangleRotation(~0),
            new TriangleRotation(~1),
            new TriangleRotation(~2),
            new TriangleRotation(~3),
            new TriangleRotation(~4),
            new TriangleRotation(~5),
        };

        public TriangleRotation Invert()
        {
            if(IsReflection)
            {
                return this;
            }
            else
            {
                return new TriangleRotation((short)((6 - value) % 6));
            }
        }

        public override bool Equals(object obj)
        {
            return obj is TriangleRotation rotation &&
                   value == rotation.value;
        }

        public override int GetHashCode()
        {
            return -541619832 + value.GetHashCode();
        }

        public static bool operator ==(TriangleRotation a, TriangleRotation b)
        {
            return a.value == b.value;
        }

        public static bool operator !=(TriangleRotation a, TriangleRotation b)
        {
            return a.value != b.value;
        }

        public static int operator *(TriangleRotation a, int side)
        {
            return (a.IsReflection ? a.Rotation - side + 6 : a.Rotation + side) % 6;
        }

        public static TriangleRotation operator *(TriangleRotation a, TriangleRotation b)
        {
            var isReflection = a.IsReflection ^ b.IsReflection;
            var rotation = a * (b * 0);
            return new TriangleRotation(isReflection ? (short)~rotation : (short)rotation);
        }

        public static TrianglePrismFaceDir operator *(TriangleRotation rotation, TrianglePrismFaceDir faceDir)
        {
            if (faceDir.IsUpDown())
            {
                return faceDir;
            }
            else
            {
                var newSide = rotation * faceDir.GetSide();
                return TrianglePrismGeometryUtils.FromSide(newSide);
            }
        }

        public static implicit operator TriangleRotation(CellRotation r) => new TriangleRotation((short)r);

        public static implicit operator CellRotation(TriangleRotation r) => (CellRotation)r.value;
    }
}
