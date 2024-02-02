using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Actual tiles used internally.
    /// There's a many-to-one relationship between ModelTile and TesseraTile
    /// due to rotations and "big" tile support.
    /// </summary>
    public struct ModelTile
    {
        public ModelTile(TesseraTileBase tile, CellRotation rotation, Vector3Int offset)
        {
            Tile = tile;
            Rotation = rotation;
            Offset = offset;
        }

        public TesseraTileBase Tile { get; set; }
        public CellRotation Rotation { get; set; }
        public Vector3Int Offset { get; set; }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Rotation.GetHashCode();
                hash = hash * 23 + (Tile == null ? 0 : Tile.GetHashCode());
                hash = hash * 23 + Offset.GetHashCode();
                return hash;
            }
        }

        public bool Equals(ModelTile other)
        {
            return Rotation == other.Rotation && Tile == other.Tile && Offset == other.Offset;
        }

        public override bool Equals(object obj)
        {
            if (obj is ModelTile other)
            {
                return Equals(other);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override string ToString()
        {
            return Tile.name.ToString() + Offset.ToString() + Rotation.ToString();
        }
    }
}
