using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    [AddComponentMenu("Tessera/Separation Constraint", 21)]
    [RequireComponent(typeof(TesseraGenerator))]
    public class SeparationConstraint : TesseraConstraint
    {
        /// <summary>
        /// The set of tiles to count
        /// </summary>
        public List<TesseraTileBase> tiles;

        /// <summary>
        /// The count to be compared against.
        /// </summary>
        public int minDistance = 10;

        internal override IEnumerable<ITileConstraint> GetTileConstraint(TileModelInfo tileModelInfo, IGrid grid)
        {
            // Filter big tiles to just a single model tile to avoid double counting
            var modelTiles = GetModelTiles(tiles)
                .Where(x => x.Offset == x.Tile.offsets[0])
                .Select(x => new Tile(x));

            yield return new DeBroglie.Constraints.SeparationConstraint
            {
                Tiles = new HashSet<Tile>(modelTiles),
                MinDistance = minDistance,
            };
        }
    }
}
