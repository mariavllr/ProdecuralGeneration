using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// GameObjects with this behaviour record adjacency information for use with a <see cref="TesseraGenerator"/>.
    /// </summary>
    [AddComponentMenu("Tessera/Tessera Hex Tile")]
    public class TesseraHexTile : TesseraTileBase
    {
        public TesseraHexTile()
        {
            faceDetails = new List<OrientedFace>
            {
                new OrientedFace(Vector3Int.zero, (CellFaceDir)HexPrismFaceDir.Left, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)HexPrismFaceDir.Right, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)HexPrismFaceDir.Up, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)HexPrismFaceDir.Down, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)HexPrismFaceDir.ForwardLeft, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)HexPrismFaceDir.ForwardRight, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)HexPrismFaceDir.BackLeft, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)HexPrismFaceDir.BackRight, new FaceDetails() ),
            };
        }

        public override ICellType CellType => HexPrismCellType.Instance;

        /// <summary>
        /// Configures the tile as a "big" tile that occupies several cells.
        /// Keeps <see cref="TesseraTileBase.offsets"/> and <see cref="TesseraTileBase.faceDetails"/> in sync.
        /// </summary>
        public override void AddOffset(Vector3Int o)
        {
            if (offsets.Contains(o))
                return;
            offsets.Add(o);
            void Check(Vector3Int delta, HexPrismFaceDir dir, HexPrismFaceDir inverseDir)
            {
                var o2 = o + delta;
                if (offsets.Contains(o2))
                {
                    faceDetails.RemoveAll(x => x.offset == o2 && x.faceDir == (CellFaceDir)inverseDir);
                }
                else
                {
                    faceDetails.Add(new OrientedFace(o, (CellFaceDir)dir, new FaceDetails()));
                }
            }
            Check(new Vector3Int(0, 1, 0), HexPrismFaceDir.Up, HexPrismFaceDir.Down);
            Check(new Vector3Int(0, -1, 0), HexPrismFaceDir.Down, HexPrismFaceDir.Up);
            Check(new Vector3Int(1, 0, 0), HexPrismFaceDir.Right, HexPrismFaceDir.Left);
            Check(new Vector3Int(-1, 0, 0), HexPrismFaceDir.Left, HexPrismFaceDir.Right);
            Check(new Vector3Int(0, 0, 1), HexPrismFaceDir.ForwardLeft, HexPrismFaceDir.BackRight);
            Check(new Vector3Int(0, 0, -1), HexPrismFaceDir.BackRight, HexPrismFaceDir.ForwardLeft);
            Check(new Vector3Int(1, 0, 1), HexPrismFaceDir.ForwardRight, HexPrismFaceDir.BackLeft);
            Check(new Vector3Int(-1, 0, -1), HexPrismFaceDir.BackLeft, HexPrismFaceDir.ForwardRight);

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
            void Check(Vector3Int delta, HexPrismFaceDir dir, HexPrismFaceDir inverseDir)
            {
                var o2 = o + delta;
                if (offsets.Contains(o2))
                {
                    faceDetails.Add(new OrientedFace(o2, (CellFaceDir)inverseDir, new FaceDetails()));
                }
                else
                {
                    faceDetails.RemoveAll(x => x.offset == o && x.faceDir == (CellFaceDir)dir);
                }
            }
            Check(new Vector3Int(0, 1, 0), HexPrismFaceDir.Up, HexPrismFaceDir.Down);
            Check(new Vector3Int(0, -1, 0), HexPrismFaceDir.Down, HexPrismFaceDir.Up);
            Check(new Vector3Int(1, 0, 0), HexPrismFaceDir.Right, HexPrismFaceDir.Left);
            Check(new Vector3Int(-1, 0, 0), HexPrismFaceDir.Left, HexPrismFaceDir.Right);
            Check(new Vector3Int(0, 0, 1), HexPrismFaceDir.ForwardLeft, HexPrismFaceDir.BackRight);
            Check(new Vector3Int(0, 0, -1), HexPrismFaceDir.BackRight, HexPrismFaceDir.ForwardLeft);
            Check(new Vector3Int(1, 0, 1), HexPrismFaceDir.ForwardRight, HexPrismFaceDir.BackLeft);
            Check(new Vector3Int(-1, 0, -1), HexPrismFaceDir.BackLeft, HexPrismFaceDir.ForwardRight);
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