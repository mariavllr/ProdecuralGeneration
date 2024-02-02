using System.Collections.Generic;
using System.Linq;
using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Models;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Keeps track of the number of tiles in a given set, and ensure it is less than / more than a given number.
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    [AddComponentMenu("Tessera/Count Constraint", 21)]
    [RequireComponent(typeof(TesseraGenerator))]
    public class CountConstraint : TesseraConstraint
    {
        /// <summary>
        /// The set of tiles to count
        /// </summary>
        public List<TesseraTileBase> tiles;

        /// <summary>
        /// How to compare the count of <see cref="tiles"/> to <see cref="count"/>.
        /// </summary>
        public CountComparison comparison;

        /// <summary>
        /// The count to be compared against.
        /// </summary>
        public int count = 10;

        /// <summary>
        /// If set, this constraint will attempt to pick tiles as early as possible.
        /// This can give a better random distribution, but higher chance of contradictions.
        /// </summary>
        public bool eager;

        internal override IEnumerable<ITileConstraint> GetTileConstraint(TileModelInfo tileModelInfo, IGrid grid)
        {
            // Filter big tiles to just a single model tile to avoid double counting
            var modelTiles = GetModelTiles(tiles)
                .Where(x => x.Offset == x.Tile.offsets[0])
                .Select(x => new Tile(x));
                
            yield return new DeBroglie.Constraints.CountConstraint
            {
                Tiles = new HashSet<Tile>(modelTiles),
                Comparison = comparison,
                Count = count,
                Eager = eager,
            };
        }
    }
}
