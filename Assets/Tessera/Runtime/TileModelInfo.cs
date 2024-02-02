using DeBroglie;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Contains a basic summary of connecitivty information about a set of tiles.
    /// </summary>
    internal class TileModelInfo
    {
        public List<(Tile, float)> AllTiles { get; set; }

        public List<InternalAdjacency> InternalAdjacencies { get; set; }

        public Dictionary<CellFaceDir, List<(FaceDetails, Tile)>> TilesByDirection { get; set; }

        public BiMap<CellFaceDir, Direction> DirectionMapping { get; set; }

        public Dictionary<Tile, Tile> Canonicalization { get; set; }
        public ILookup<Tile, Tile> Uncanonicalization { get; set; }

        internal class InternalAdjacency
        {
            public Tile Src { get; set; }
            public Tile Dest { get; set; }
            public CellFaceDir OffsetDir { get; set; }
            public CellFaceDir GridDir { get; set; }
        }


        /// <summary>
        /// Summarizes the tiles, in preparation for building a model.
        /// </summary>
        internal static TileModelInfo Create(List<TileEntry> tiles, ICellType cellType)
        {
            var allTiles = new List<(Tile, float)>();
            var internalAdjacencies = new List<InternalAdjacency>();

            var directionMapping = DeBroglieUtils.GetDirectionMapping(cellType);

            var tilesByDirection = cellType.GetFaceDirs().ToDictionary(d => d, _ => new List<(FaceDetails, Tile)>());

            var tileCosts = new Dictionary<TesseraTile, int>();

            if (tiles == null || tiles.Count == 0)
            {
                throw new Exception("Cannot run generator with zero tiles configured.");
            }

            // Canonicalize
            var canonical = new Dictionary<Tile, Tile>();
            foreach (var tile in tiles.Select(x => x.tile))
            {
                var rots = cellType.GetRotations(tile.rotatable, tile.reflectable, tile.rotationGroupType).ToList();
                var done = new HashSet<CellRotation>();
                foreach (var rot1 in rots)
                {
                    if (tile.symmetric)
                    {
                        foreach (var rot2 in rots)
                        {
                            if (done.Contains(rot2))
                                continue;

                            if (IsPaintEquivalent(tile, rot1, rot2, out var _, out var realign))
                            {
                                foreach (var kv in realign)
                                {
                                    var modelTile1 = new ModelTile
                                    {
                                        Tile = tile,
                                        Rotation = rot1,
                                        Offset = kv.Key,
                                    };
                                    var modelTile2 = new ModelTile
                                    {
                                        Tile = tile,
                                        Rotation = rot2,
                                        Offset = kv.Value,
                                    };
                                    canonical[new Tile(modelTile2)] = new Tile(modelTile1);
                                }
                                //Debug.Log($"Canonicalize {tile} {rot2} -> {rot1}");
                                done.Add(rot2);
                            }
                        }
                    }
                    else
                    {
                        foreach(var offset in tile.offsets)
                        {
                            var modelTile = new Tile(new ModelTile
                            {
                                Tile = tile,
                                Rotation = rot1,
                                Offset = offset,
                            });
                            canonical[modelTile] = modelTile;
                        }
                    }
                }
            }
            var isCanonical = new HashSet<Tile>(canonical.Values);
            var uncanonical = canonical.ToLookup(x => x.Value, x => x.Key);

            // Generate all tiles, and extract their face details
            foreach (var tileEntry in tiles)
            {
                var tile = tileEntry.tile;

                if (tile == null)
                    continue;
                if (!IsContiguous(tile))
                {
                    Debug.LogWarning($"Cannot use {tile} as it is not contiguous");
                    continue;
                }

                foreach (var rot in cellType.GetRotations(tile.rotatable, tile.reflectable, tile.rotationGroupType))
                {
                    // Set up internal connections
                    foreach (var offset in tile.offsets)
                    {
                        var modelTile = new Tile(new ModelTile(tile, rot, offset));

                        if (!isCanonical.Contains(modelTile))
                            continue;

                        var frequency = tileEntry.weight * uncanonical[modelTile].Count() / tile.offsets.Count;
                        allTiles.Add((modelTile, frequency));

                        if (tile.offsets.Count > 1)
                        {
                            foreach (var faceDir in cellType.GetFaceDirs())
                            {
                                if (cellType.TryMove(offset, faceDir, out var offset2))
                                {
                                    if (tile.offsets.Contains(offset2))
                                    {
                                        var modelTile2 = new Tile(new ModelTile(tile, rot, offset2));

                                        if (!isCanonical.Contains(modelTile2))
                                            continue;

                                        internalAdjacencies.Add(new InternalAdjacency
                                        {
                                            Src = modelTile,
                                            Dest = modelTile2,
                                            OffsetDir = faceDir,
                                            GridDir = cellType.Rotate(faceDir, rot),
                                        });
                                    }
                                }
                            }
                        }
                    }

                    // Set up external connections
                    foreach (var (offset, faceDir, faceDetails) in tile.faceDetails)
                    {
                        var modelTile = new Tile(new ModelTile(tile, rot, offset));

                        if (!isCanonical.Contains(modelTile))
                            continue;

                        var (rFaceDir, rFaceDetails) = cellType.RotateBy(faceDir, faceDetails, rot);
                        tilesByDirection[rFaceDir].Add((rFaceDetails, modelTile));
                    }
                }
            }

            return new TileModelInfo
            {
                AllTiles = allTiles,
                InternalAdjacencies = internalAdjacencies,
                TilesByDirection = tilesByDirection,
                DirectionMapping = directionMapping,
                Canonicalization = canonical,
                Uncanonicalization = uncanonical,
            };
        }

        /// <summary>
        /// Returns true if the tile has the same paint in these two different orientations, and if so
        /// the mapping from rot1 to rot2.
        /// </summary>
        private static bool IsPaintEquivalent(TesseraTileBase tile, CellRotation rot1, CellRotation rot2, out CellRotation rotation, out IDictionary<Vector3Int, Vector3Int> realign)
        {
            var cellType = tile.CellType;
            rotation = cellType.Multiply(cellType.Invert(rot1), rot2);
            var offsets = new HashSet<Vector3Int>(tile.offsets);
            realign = cellType.Realign(offsets, rotation);
            if (realign == null)
            {
                return false;
            }
            // Check paint
            foreach(var (offset, faceDir, faceDetails) in tile.faceDetails)
            {
                var offset2 = realign[offset];
                var (faceDir2, faceDetails2) = cellType.RotateBy(faceDir, faceDetails, rotation);
                var otherFaceDetails = tile.Get(offset2, faceDir2);
                if (!faceDetails2.IsEquivalent(otherFaceDetails))
                    return false;
            }
            return true;
        }

        private static bool IsContiguous(TesseraTileBase tile)
        {
            if (!(tile is TesseraTile))
            {
                // TODO
                return true;
            }

            if (tile.offsets.Count == 1)
                return true;

            // Floodfill offset
            var offsets = new HashSet<Vector3Int>(tile.offsets);
            var toRemove = new Stack<Vector3Int>();
            toRemove.Push(offsets.First());
            while (toRemove.Count > 0)
            {
                var o = toRemove.Pop();
                offsets.Remove(o);

                foreach (CubeFaceDir faceDir in Enum.GetValues(typeof(CubeFaceDir)))
                {
                    var o2 = o + faceDir.Forward();
                    if (offsets.Contains(o2))
                    {
                        toRemove.Push(o2);
                    }
                }
            }

            return offsets.Count == 0;
        }
    }
}
