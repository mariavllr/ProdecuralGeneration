using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// GameObjects with this behaviour record adjacency information for use with a <see cref="TesseraGenerator"/>.
    /// </summary>
    [AddComponentMenu("Tessera/Tessera Triangle Prism Tile")]
    public class TesseraTrianglePrismTile : TesseraTileBase
    {
        public TesseraTrianglePrismTile()
        {
            faceDetails = new List<OrientedFace>
            {
                new OrientedFace(Vector3Int.zero, (CellFaceDir)TrianglePrismFaceDir.Back, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)TrianglePrismFaceDir.Up, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)TrianglePrismFaceDir.Down, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)TrianglePrismFaceDir.ForwardLeft, new FaceDetails() ),
                new OrientedFace(Vector3Int.zero, (CellFaceDir)TrianglePrismFaceDir.ForwardRight, new FaceDetails() ),
            };
        }

        public override ICellType CellType => TrianglePrismCellType.Instance;

        /// <summary>
        /// Configures the tile as a "big" tile that occupies several cells.
        /// Keeps <see cref="TesseraTileBase.offsets"/> and <see cref="TesseraTileBase.faceDetails"/> in sync.
        /// </summary>
        public override void AddOffset(Vector3Int o)
        {
            if (offsets.Contains(o))
                return;
            offsets.Add(o);
            void Check(TrianglePrismFaceDir dir, TrianglePrismFaceDir inverseDir)
            {
                var o2 = o + dir.OffsetDelta();
                if (offsets.Contains(o2))
                {
                    faceDetails.RemoveAll(x => x.offset == o2 && x.faceDir == (CellFaceDir)inverseDir);
                }
                else
                {
                    faceDetails.Add(new OrientedFace(o, (CellFaceDir)dir, new FaceDetails()));
                }
            }
            Check(TrianglePrismFaceDir.Up, TrianglePrismFaceDir.Down);
            Check(TrianglePrismFaceDir.Down, TrianglePrismFaceDir.Up);
            if (TrianglePrismGeometryUtils.PointsUp(o))
            {
                Check(TrianglePrismFaceDir.Back, TrianglePrismFaceDir.Forward);
                Check(TrianglePrismFaceDir.ForwardLeft, TrianglePrismFaceDir.BackRight);
                Check(TrianglePrismFaceDir.ForwardRight, TrianglePrismFaceDir.BackLeft);
            }
            else
            {
                Check(TrianglePrismFaceDir.Forward, TrianglePrismFaceDir.Back);
                Check(TrianglePrismFaceDir.BackRight, TrianglePrismFaceDir.ForwardLeft);
                Check(TrianglePrismFaceDir.BackLeft, TrianglePrismFaceDir.ForwardRight);
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
            void Check(TrianglePrismFaceDir dir, TrianglePrismFaceDir inverseDir)
            {
                var o2 = o + dir.OffsetDelta();
                if (offsets.Contains(o2))
                {
                    faceDetails.Add(new OrientedFace(o2, (CellFaceDir)inverseDir, new FaceDetails()));
                }
                else
                {
                    faceDetails.RemoveAll(x => x.offset == o && x.faceDir == (CellFaceDir)dir);
                }
            }
            Check(TrianglePrismFaceDir.Up, TrianglePrismFaceDir.Down);
            Check(TrianglePrismFaceDir.Down, TrianglePrismFaceDir.Up);
            if (TrianglePrismGeometryUtils.PointsUp(o))
            {
                Check(TrianglePrismFaceDir.Back, TrianglePrismFaceDir.Forward);
                Check(TrianglePrismFaceDir.ForwardLeft, TrianglePrismFaceDir.BackRight);
                Check(TrianglePrismFaceDir.ForwardRight, TrianglePrismFaceDir.BackLeft);
            }
            else
            {
                Check(TrianglePrismFaceDir.Forward, TrianglePrismFaceDir.Back);
                Check(TrianglePrismFaceDir.BackRight, TrianglePrismFaceDir.ForwardLeft);
                Check(TrianglePrismFaceDir.BackLeft, TrianglePrismFaceDir.ForwardRight);
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