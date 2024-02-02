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
    public struct HexRotation
    {
        short value;

        private HexRotation(short value)
        {
            this.value = value;
        }

        public bool IsReflection => value < 0;

        public int Rotation => value < 0 ? ~value : value;

        public static HexRotation Identity => new HexRotation(0);

        public static HexRotation ReflectX => new HexRotation(~0);

        public static HexRotation ReflectForwardLeft => new HexRotation(~4);
        public static HexRotation ReflectForwardRight => new HexRotation(~2);

        public static HexRotation ReflectZ => new HexRotation(~0);

        public static HexRotation RotateCCW => new HexRotation(1);

        public static HexRotation Rotate60(int i) => new HexRotation((short)(((i % 6) + 6) % 6));

        public static HexRotation[] All => new[]
        {
            new HexRotation(0),
            new HexRotation(1),
            new HexRotation(2),
            new HexRotation(3),
            new HexRotation(4),
            new HexRotation(5),
            new HexRotation(~0),
            new HexRotation(~1),
            new HexRotation(~2),
            new HexRotation(~3),
            new HexRotation(~4),
            new HexRotation(~5),
        };

        public HexRotation Invert()
        {
            if (IsReflection)
            {
                return this;
            }
            else
            {
                return new HexRotation((short)((6 - value) % 6));
            }
        }

        public override bool Equals(object obj)
        {
            return obj is HexRotation rotation &&
                   value == rotation.value;
        }

        public override int GetHashCode()
        {
            return -541619832 + value.GetHashCode();
        }

        public static bool operator ==(HexRotation a, HexRotation b)
        {
            return a.value == b.value;
        }

        public static bool operator !=(HexRotation a, HexRotation b)
        {
            return a.value != b.value;
        }

        public static int operator *(HexRotation a, int side)
        {
            return (a.IsReflection ? a.Rotation - side + 9 : a.Rotation + side) % 6;
        }

        public static HexRotation operator *(HexRotation a, HexRotation b)
        {
            var isReflection = a.IsReflection ^ b.IsReflection;
            var rotation = a * (b * 0);
            return new HexRotation(isReflection ? (short)~((rotation + 3) % 6) : (short)rotation);
        }

        public static HexPrismFaceDir operator *(HexRotation rotation, HexPrismFaceDir faceDir)
        {
            if (faceDir.IsUpDown())
            {
                return faceDir;
            }
            else
            {
                var newSide = rotation * faceDir.GetSide();
                return HexGeometryUtils.FromSide(newSide);
            }
        }

        public static implicit operator HexRotation(CellRotation r) => new HexRotation((short)r);

        public static implicit operator CellRotation(HexRotation r) => (CellRotation)r.value;
    }
}
