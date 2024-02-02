using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Topo;
using DeBroglie.Trackers;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Ensures that the generation is symmetric when x-axis mirrored.
    /// If there are any tile constraints, they will not be mirrored.
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    [AddComponentMenu("Tessera/Mirror Constraint", 20)]
    [RequireComponent(typeof(TesseraGenerator))]
    public class MirrorConstraint : TesseraConstraint
    {
        // Unused legacy field
        [SerializeField]
        private bool hasSymmetricTiles;

        // Unused legacy field
        [SerializeField]
        private List<TesseraTileBase> symmetricTilesX = new List<TesseraTileBase>();

        // Unused legacy field
        [SerializeField]
        private List<TesseraTileBase> symmetricTilesY = new List<TesseraTileBase>();

        // Unused legacy field
        [SerializeField]
        private List<TesseraTileBase> symmetricTilesZ = new List<TesseraTileBase>();

        public Axis axis;

        public enum Axis
        {
            X,
            Y,
            Z,
            W,
        }

        internal override IEnumerable<ITileConstraint> GetTileConstraint(TileModelInfo tileModelInfo, IGrid grid)
        {
            var generator = GetComponent<TesseraGenerator>();
            if (generator.surfaceMesh != null)
            {
                throw new Exception("Mirror constraint not supported on surface meshes");
            }

            var cellType = generator.CellType;
            var mirrorOps = MirrorUtils.GetMirrorOps(cellType);
            var modelTiles = new HashSet<ModelTile>(tileModelInfo.AllTiles.Select(x => (ModelTile)x.Item1.Value));
            CellRotation cellRotation;

            if (cellType is CubeCellType)
            {
                cellRotation = (CellRotation)(axis == Axis.X ? CubeRotation.ReflectX : axis == Axis.Y ? CubeRotation.ReflectY : CubeRotation.ReflectZ);
            }
            else if (cellType is SquareCellType)
            {
                cellRotation = (CellRotation)(axis == Axis.X ? SquareRotation.ReflectX : SquareRotation.ReflectY);
            }
            else if (cellType is HexPrismCellType)
            {
                cellRotation = (CellRotation)(axis == Axis.X ? HexRotation.ReflectX : axis == Axis.Y ? throw new Exception("HexPrisms cannot be mirrored in vertical axis") : axis == Axis.Z ? HexRotation.ReflectForwardLeft : HexRotation.ReflectForwardRight);
            }
            else if (cellType is TrianglePrismCellType)
            {
                // TODO
                cellRotation = TriangleRotation.ReflectX;
            }
            else
            {
                throw new Exception($"Unknown cellType {cellType.GetType()}");
            }

            yield return new InnerMirrorConstraint
            {
                grid = grid,
                mirrorOps = mirrorOps,
                cellType = cellType,
                canonicalization = tileModelInfo.Canonicalization,
                rotation = cellRotation,
            };
        }

        private class InnerMirrorConstraint : SymmetryConstraint
        {
            public IGrid grid;
            public MirrorUtils.IMirrorOps mirrorOps;
            public ICellType cellType;
            public Dictionary<Tile, Tile> canonicalization;
            public CellRotation rotation;

            protected override bool TryMapIndex(TilePropagator propagator, int i, out int i2)
            {
                return mirrorOps.ReflectIndex(rotation, grid, propagator.Topology, i, out i2);
            }

            protected override bool TryMapTile(Tile tile, out Tile tile2)
            {
                var modelTile = (ModelTile)tile.Value;

                var newRotation = cellType.Multiply(rotation, modelTile.Rotation);
                var modelTile2 = new Tile(new ModelTile
                {
                    Tile = modelTile.Tile,
                    Rotation = newRotation,
                    Offset = modelTile.Offset,
                });
                return canonicalization.TryGetValue(modelTile2, out tile2);
            }
        }
    }
}
