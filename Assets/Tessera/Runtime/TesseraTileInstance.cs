using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeBroglie.Rot;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Represents a request to instantiate a TesseraTile, post generation.
    /// </summary>
    public class TesseraTileInstance
    {
        public TesseraTileBase Tile { get; internal set; }
        // TRS in World space
        public Vector3 Position { get; internal set; }
        public Quaternion Rotation { get; internal set; }
        public Vector3 LossyScale { get; internal set; }
        // TRS in generator space
        public Vector3 LocalPosition { get; internal set; }
        public Quaternion LocalRotation { get; internal set; }
        public Vector3 LocalScale { get; internal set; }

        /// <summary>
        /// The grid cell this instance fills. (for big tiles, this is the cell of the first offset) 
        /// </summary>
        public Vector3Int Cell { get; internal set; }
        
        /// <summary>
        /// The rotation this instance is placed at (for big tiles, this is the cell of the first offset)
        /// </summary>
        public CellRotation CellRotation { get; internal set; }

        /// <summary>
        /// The cells this instance fills, in the same order as the tile offsets. 
        /// </summary>
        public Vector3Int[] Cells { get; internal set; }

        /// <summary>
        /// The rotations this instance instance, in the same order as the tile offsets.
        /// Most grids will have same rotation for all offsets.
        /// </summary>
        public CellRotation[] CellRotations { get; internal set; }

        /// <summary>
        /// Gives a mesh deformation from tile space to generator space. 
        /// Null for grids that do not have deformed tiles.
        /// </summary>
        public MeshDeformation MeshDeformation { get;  internal set; }
        public TesseraTileInstance Clone()
        {
            var i = (TesseraTileInstance)MemberwiseClone();
            i.Cells = i.Cells.ToArray();
            i.CellRotations = i.CellRotations.ToArray();
            return i;
        }

        /// <summary>
        /// Sets Position/Rotation/Scale from the local versions and a given transform
        /// </summary>
        public void Align(TRS transform)
        {
            var localTrs = new TRS(LocalPosition, LocalRotation, LocalScale);
            var worldTrs = transform * localTrs;
            Position = worldTrs.Position;
            Rotation = worldTrs.Rotation;
            LossyScale = worldTrs.Scale;
        }
    }
}
