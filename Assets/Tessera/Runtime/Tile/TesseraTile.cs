using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{

    /// <summary>
    /// GameObjects with this behaviour record adjacency information for use with a <see cref="TesseraGenerator"/>.
    /// </summary>
    [AddComponentMenu("Tessera/Tessera Tile")]
    public class TesseraTile : TesseraTileBase
    {

        public TesseraTile()
        {
            faceDetails = new List<OrientedFace>
            {
                new OrientedFace(Vector3Int.zero, (CellFaceDir)CubeFaceDir.Left, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)CubeFaceDir.Right, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)CubeFaceDir.Up, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)CubeFaceDir.Down, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)CubeFaceDir.Forward, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)CubeFaceDir.Back, new FaceDetails() ),
            };
        }

        public override ICellType CellType => CubeCellType.Instance;

        /// <summary>
        /// Configures the tile as a "big" tile that occupies several cells.
        /// Keeps <see cref="TesseraTileBase.offsets"/> and <see cref="TesseraTileBase.faceDetails"/> in sync.
        /// </summary>
        public override void AddOffset(Vector3Int o)
        {
            if (offsets.Contains(o))
                return;
            offsets.Add(o);
            foreach (CubeFaceDir faceDir in Enum.GetValues(typeof(CubeFaceDir)))
            {
                var o2 = o + faceDir.Forward();
                if (offsets.Contains(o2))
                {
                    faceDetails.RemoveAll(x => x.offset == o2 && x.faceDir == (CellFaceDir)faceDir.Inverted());
                }
                else
                {
                    faceDetails.Add(new OrientedFace(o, (CellFaceDir)faceDir, new FaceDetails()));
                }
            }
        }

        /// <summary>
        /// Configures the tile as a "big" tile that occupies several cells.
        /// Keeps <see cref="TesseraTileBase.offsets"/> and <see cref="TesseraTileBase.faceDetails"/> in sync.
        /// </summary>
        public override void RemoveOffset(Vector3Int o)
        {
            if (!offsets.Contains(o))
                return;
            offsets.Remove(o);
            foreach (CubeFaceDir faceDir in Enum.GetValues(typeof(CubeFaceDir)))
            {
                var o2 = o + faceDir.Forward();
                if (offsets.Contains(o2))
                {
                    faceDetails.Add(new OrientedFace(o2, (CellFaceDir)faceDir.Inverted(), new FaceDetails()));
                }
                else
                {
                    faceDetails.RemoveAll(x => x.offset == o && x.faceDir == (CellFaceDir)faceDir);
                }
            }
        }

        public BoundsInt GetBounds()
        {
            var min = offsets[0];
            var max = min;
            foreach (var o in offsets)
            {
                min = Vector3Int.Min(min, o);
                max = Vector3Int.Max(max, o);
            }

            return new BoundsInt(min, max - min);
        }
    }
}