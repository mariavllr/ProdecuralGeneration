using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Forces a network of tiles to connect with each other, so there is always a complete path between them.
    /// Two tiles connect along the path if:
    /// * Both tiles are in <see cref="pathTiles"/> (if <see cref="hasPathTiles"/> set); and
    /// * The central color of the sides of the tiles leading to each other are in <see cref="pathColors"/> (if <see cref="pathColors"/> set)
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    [AddComponentMenu("Tessera/Path Constraint", 21)]
    [RequireComponent(typeof(TesseraGenerator))]
    public class PathConstraint : TesseraConstraint
    {
        /// <summary>
        /// If set, <see cref="pathTiles"/> is used to determine path tiles.
        /// </summary>
        [Tooltip("Enables a filter for which tiles the path can connect through")]
        public bool hasPathTiles;

        /// <summary>
        /// If <see cref="hasPathTiles"/>, this set filters tiles that the path can connect through.
        /// </summary>
        [Tooltip("This set filters tiles that the path can connect through")]
        public List<TesseraTileBase> pathTiles = new List<TesseraTileBase>();

        /// <summary>
        /// If set, <see cref="pathColors"/> is used to determine path tiles and sides.
        /// </summary>
        [Tooltip("Enables a paint-based fitler for which tiles that the path can connect through")]
        public bool hasPathColors;

        /// <summary>
        /// If <see cref="hasPathColors"/>, this set filters tiles that the path can connect through. Only the central square on each face is inspected.
        /// </summary>
        [Tooltip("This set specifies the paint colors the path can connect through. Only the central square on each face is inspected.")]
        public List<int> pathColors = new List<int>();

        /// <summary>
        /// If set, the the generator will prefer generating tiles near the path.
        /// </summary>
        [Tooltip("If set, the the generator will prefer generating tiles near the path.")]
        public bool prioritize;

        /// <summary>
        /// If Set, then the constraint forces that all path tiles must have a contiguous path between them.
        /// </summary>
        [Tooltip("If set, then the constraint forces that all path tiles must have a contiguous path between them.")]
        public bool connected = true;

        /// <summary>
        /// If set, forces there to be at least two non-overlapping valid paths between any two connected path tiles.
        /// </summary>
        [Tooltip("If set, forces there to be at least two non-overlapping valid paths between any two connected path tiles.")]
        public bool loops = false;

        /// <summary>
        /// If set, bans all cycles, forcing a tree or forest.
        /// </summary>
        [Tooltip("If set, bans all cycles, forcing a the patch to branch like a tree or forest.")]
        public bool acyclic = false;

        /// <summary>
        /// Enable this if your path tileset includes no forks or junctions, it can improve the search quality.
        /// </summary>
        [Tooltip("Enable this if your path tileset includes no forks or junctions, it can improve the search quality.")]
        public bool parity = false;

        internal override IEnumerable<ITileConstraint> GetTileConstraint(TileModelInfo tileModelInfo, IGrid grid)
        {
            IPathSpec pathSpec;
            if (hasPathColors)
            {
                var colorSet = new HashSet<int>(pathColors);
                var pathTilesSet = new HashSet<TesseraTileBase>(pathTiles);
                var generator = GetComponent<TesseraGenerator>();

                var exits = new Dictionary<Tile, ISet<Direction>>();

                // External connections are valid exits only if the color in the center of the face matches
                foreach (var kv in tileModelInfo.TilesByDirection)
                {
                    var dir = tileModelInfo.DirectionMapping[kv.Key];
                    foreach(var (faceDetails, tile) in kv.Value)
                    {
                        var modelTile = (ModelTile)tile.Value;
                        if (hasPathTiles && !pathTilesSet.Contains(modelTile.Tile))
                            continue;
                        if (!colorSet.Contains(faceDetails.center))
                            continue;
                        if(!exits.TryGetValue(tile, out var directions))
                        {
                            directions = exits[tile] = new HashSet<Direction>();
                        }
                        directions.Add(dir);
                    }
                }

                // Internal connections are valid exits if the tile tile has any external exits
                var externalTiles = new HashSet<TesseraTileBase>(exits.Select(x => ((ModelTile)x.Key.Value).Tile));
                foreach (var ia in tileModelInfo.InternalAdjacencies)
                {
                    var modelTile1 = (ModelTile)ia.Src.Value;
                    if (hasPathTiles && !pathTilesSet.Contains(modelTile1.Tile))
                        continue;
                    if (!externalTiles.Contains(modelTile1.Tile))
                        continue;
                    if (!exits.TryGetValue(ia.Src, out var directions))
                    {
                        directions = exits[ia.Src] = new HashSet<Direction>();
                    }
                    var dir = tileModelInfo.DirectionMapping[ia.GridDir];
                    directions.Add(dir);
                }
                pathSpec = new EdgedPathSpec { Exits = exits };

            }
            else if (hasPathTiles)
            {
                var actualPathTiles = new HashSet<Tile>(GetModelTiles(pathTiles).Select(x => new Tile(x)));
                pathSpec = new PathSpec { Tiles = actualPathTiles };
            }
            else
            {
                throw new Exception("One of hasColors or hasPathTiles must be set for PathConstraints");
            }

            if (connected)
            {
                yield return new ConnectedConstraint
                {
                    PathSpec = pathSpec,
                    UsePickHeuristic = prioritize,
                    
                };
            }
            if (loops)
            {
                yield return new LoopConstraint
                {
                    PathSpec = pathSpec,
                };
            }
            if (acyclic)
            {
                yield return new AcyclicConstraint
                {
                    PathSpec = pathSpec,
                };
            }
            if(parity)
            {
                if (pathSpec is EdgedPathSpec)
                {
                    yield return new ParityConstraint
                    {
                        PathSpec = (EdgedPathSpec)pathSpec,
                    };
                }
                else
                {
                    throw new NotImplementedException("Paritiy constraint currently requires path colors to be set.");
                }
            }
        }
    }
}
