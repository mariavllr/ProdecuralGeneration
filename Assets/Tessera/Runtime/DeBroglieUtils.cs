using DeBroglie;
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
    // Utilities for converting from IGrid to ITopology, and similar mappings
    internal static class DeBroglieUtils
    {
        #region Topology Conversion
        // NB: For convenience, the topology indices always match the grid indices
        public static ITopology GetTopology(IGrid grid, Vector3Int? sizeOverride = null)
        {
            if (grid is CubeGrid cg)
            {
                var size = sizeOverride ?? cg.Size;
                return new GridTopology(size.x, size.y, size.z, false);
            }
            if (grid is SquareGrid sg)
            {
                var size = sizeOverride ?? sg.Size;
                return new GridTopology(size.x, size.y, false);
            }
            if(sizeOverride != null)
            {
                throw new Exception("");
            }
            if (grid is HexPrismGrid hg)
            {
                return new GridTopology(DirectionSet.Hexagonal3d, hg.Size.x, hg.Size.y, hg.Size.z, false, false, false, null);
            }
            if(grid is QuadMeshGrid || grid is TriangleMeshGrid || grid is TrianglePrismGrid)
            {
                return new GenericTopology(grid);
            }
            throw new Exception($"Unsupported Grid type {grid.GetType()}");
        }

        public static TileModel GetTileModel(IGrid grid, ModelType modelType, TileModelInfo tileModelInfo, List<TesseraTilemap> samples, Vector3Int overlapSize, TesseraPalette palette)
        {
            if(modelType == ModelType.AdjacentPaint)
            {
                return GetAdjacentPaintTileModel(grid, tileModelInfo, palette);
            }
            else if (modelType == ModelType.Overlapping)
            {
                return GetOverlappedTileModel(grid, tileModelInfo, samples, overlapSize, palette);
            }
            else if ( modelType == ModelType.Adjacent)
            {
                return GetAdjacentTileModel(grid, tileModelInfo, samples, palette);
            }
            else
            {
                throw new Exception($"Unkown model type {modelType}");
            }
        }

        #region SampleBased
        private static IList<ITopoArray<Tile>> SampleToTopoArrays(IGrid grid, TileModelInfo tileModelInfo, TesseraTilemap sample)
        {
            var results = new List<ITopoArray<Tile>>();

            // TODO: Validate sample topology is compatible

            // TODO: Be smarter about which symmetries the tiles actually support
            // Currently this is very naive, and just generates lots of empty samples.
            foreach (var (gridSymmetry, size) in sample.Grid.GetBoundsSymmetries())
            {
                // Convert a sample to an ITopoArray
                var sampleTopology = GetTopology(sample.Grid, size);
                var sampleData = new Tile[sampleTopology.IndexCount];
                foreach (var kv in sample.Data)
                {
                    var cell = kv.Key;
                    var modelTile = kv.Value;
                    if (!grid.TryApplySymmetry(gridSymmetry, cell, out var destCell, out var cellRotation))
                    {
                        //Debug.LogWarning($"Couldn't rotate {cell} by {gridSymmetry.rotation}");
                        continue;
                    }
                    // Canonical rotation
                    var modelTileRotated = new ModelTile
                    {
                        Tile = modelTile.Tile,
                        Offset = modelTile.Offset,
                        Rotation = grid.CellType.Multiply(cellRotation, modelTile.Rotation),
                    };
                    if (!tileModelInfo.Canonicalization.TryGetValue(new Tile(modelTileRotated), out var canonicalTile))
                    {
                        //Debug.LogWarning($"Couldn't find canonical tile for {cell} rotation {mt.Rotation}");
                        continue;
                    }

                    // Insert into sample
                    var index = sampleTopology.GetIndex(destCell.x, destCell.y, destCell.z);
                    if (index < 0 || index >= sampleData.Length)
                    {
                        throw new Exception();
                    }
                    sampleData[index] = canonicalTile;
                }
                var mask = sampleTopology.GetIndices().Select(i => sampleData[i] != new Tile(null)).ToArray();
                var sampleArray = TopoArray.Create(sampleData, sampleTopology.WithMask(mask));
                results.Add(sampleArray);
            }
            return results;
        }

        public static TileModel GetOverlappedTileModel(IGrid grid, TileModelInfo tileModelInfo, List<TesseraTilemap> samples, Vector3Int overlapSize, TesseraPalette palette)
        {
            if (grid is CubeGrid || grid is SquareGrid)
            {
                // Hack to make working with flat grids easier

                Vector3Int gridSize;
                if (grid is CubeGrid cg)
                {
                    gridSize = cg.Size;
                    overlapSize = samples.Aggregate(overlapSize, (s, sample) => Vector3Int.Min(s, ((CubeGrid)sample.Grid).Size));
                }
                else if (grid is SquareGrid sg)
                {
                    gridSize = sg.Size;
                    overlapSize = samples.Aggregate(overlapSize, (s, sample) => Vector3Int.Min(s, ((SquareGrid)sample.Grid).Size));
                }
                else
                {
                    throw new Exception();
                }

                var model = new OverlappingModel(overlapSize.x, overlapSize.y, overlapSize.z);

                foreach (var sample in samples)
                {
                    foreach (var topoArray in SampleToTopoArrays(grid, tileModelInfo, sample))
                    {
                        var topology = topoArray.Topology;
                        if (topology.Width < overlapSize.x || topology.Height < overlapSize.y || topology.Depth < overlapSize.z)
                            continue;
                        model.AddSample(topoArray);

                    }
                }
                return model;
            }
            else
            {
                throw new Exception("Overlapping model only supports cube grid");
            }
        }


        public static TileModel GetAdjacentTileModel(IGrid grid, TileModelInfo tileModelInfo, List<TesseraTilemap> samples, TesseraPalette palette)
        {
            if (grid is CubeGrid || grid is SquareGrid)
            {
                var model = new AdjacentModel();

                foreach (var sample in samples)
                {
                    foreach (var topoArray in SampleToTopoArrays(grid, tileModelInfo, sample))
                    {
                        var topology = topoArray.Topology;
                        model.AddSample(topoArray);

                    }
                }
                return model;
            }
            else
            {
                throw new Exception("Adjacent model only supports cube grid");
            }
        }


        #endregion

        #region AdjacentPaint
        // This is tightly couped with GetTopology, should they be merged?
        public static TileModel GetAdjacentPaintTileModel(IGrid grid, TileModelInfo tileModelInfo, TesseraPalette palette)
        {
            var cellType = grid.CellType;
            if (grid is QuadMeshGrid || grid is TriangleMeshGrid || grid is TrianglePrismGrid)
            {
                var directionMapping = GetDirectionMapping(grid.CellType);
                var edgeLabelMapping = GetEdgeLabelMapping(grid, directionMapping);
                var info = new GraphInfo
                {
                    DirectionsCount = directionMapping.Count,
                    EdgeLabelCount = edgeLabelMapping.Count,
                    EdgeLabelInfo = edgeLabelMapping.OrderBy(t=>t.Item2).Select(t =>
                    {
                        var ((direction, cellRotation), edgeLabel) = t;
                        var inverseDirection = directionMapping[cellType.Invert(cellType.Rotate(directionMapping[direction], cellRotation))];
                        var rotation = new DeBroglie.Rot.Rotation();// Unused by Tessera
                        return (direction, inverseDirection, rotation);
                    })
                    .ToArray(),
                };

                var model = new GraphAdjacentModel(info);

                foreach (var (tile, frequency) in tileModelInfo.AllTiles)
                {
                    model.SetFrequency(tile, frequency);
                }

                var allTiles = new HashSet<Tile>(tileModelInfo.AllTiles.Select(x => x.Item1));

                foreach (var ia in tileModelInfo.InternalAdjacencies)
                {
                    var d = tileModelInfo.DirectionMapping[ia.GridDir];
                    foreach (var el in Enumerable.Range(0, info.EdgeLabelCount))
                    {
                        var elInfo = info.EdgeLabelInfo[el];
                        var (_, cellRotation) = edgeLabelMapping[(EdgeLabel)el];
                        if (elInfo.Item1 != d)
                            continue;
                        var mt = (ModelTile)ia.Src.Value;
                        var mt2 = (ModelTile)ia.Dest.Value;
                        var otherTile = new Tile(new ModelTile
                        {
                            Tile = mt2.Tile,
                            Offset = mt2.Offset,
                            Rotation = cellType.Multiply(cellRotation, mt2.Rotation),
                        });
                        if (allTiles.Contains(otherTile))
                        {
                            model.AddAdjacency(ia.Src, otherTile, (EdgeLabel)el);
                        }
                    }
                }

                for (var el = 0; el < info.EdgeLabelCount; el++)
                {
                    var elInfo = info.EdgeLabelInfo[el];
                    var tiles1 = tileModelInfo.TilesByDirection[tileModelInfo.DirectionMapping[elInfo.Item1]];
                    var tiles2 = tileModelInfo.TilesByDirection[tileModelInfo.DirectionMapping[elInfo.Item2]];
                    var adjacencies = GetAdjacencies(palette, el, tiles1, tiles2);
                    foreach (var (t1, t2, _) in adjacencies)
                    {
                        model.AddAdjacency(t1, t2, (EdgeLabel)el);
                    }
                }
                return model;
            }
            else if(grid is HexPrismGrid || grid is CubeGrid || grid is SquareGrid)
            {
                var model = cellType == HexPrismCellType.Instance ? new AdjacentModel(DirectionSet.Hexagonal3d) : cellType == SquareCellType.Instance ? new AdjacentModel(DirectionSet.Cartesian2d) : new AdjacentModel(DirectionSet.Cartesian3d);

                foreach (var (tile, frequency) in tileModelInfo.AllTiles)
                {
                    model.SetFrequency(tile, frequency);
                }

                foreach (var ia in tileModelInfo.InternalAdjacencies)
                {
                    var d = tileModelInfo.DirectionMapping[ia.GridDir];
                    model.AddAdjacency(ia.Src, ia.Dest, d);
                }

                var adjacencies = cellType.GetFaceDirPairs().SelectMany(t => {
                    return GetAdjacencies(palette, tileModelInfo.DirectionMapping[t.Item1], tileModelInfo.TilesByDirection[t.Item1], tileModelInfo.TilesByDirection[t.Item2]);
                }).ToList();

                foreach (var (t1, t2, d) in adjacencies)
                {
                    model.AddAdjacency(t1, t2, d);
                }
                return model;
            }
            throw new Exception($"Unsupported Grid type {grid.GetType()}");
        }

        private static IEnumerable<(Tile, Tile, T)> GetAdjacencies<T>(TesseraPalette palette, T d, List<(FaceDetails, Tile)> tiles1, List<(FaceDetails, Tile)> tiles2)
        {
            foreach (var (fd1, t1) in tiles1)
            {
                foreach (var (fd2, t2) in tiles2)
                {
                    if (palette.Match(fd1, fd2))
                    {
                        yield return (t1, t2, d);
                    }
                }
            }
        }

        #endregion

        public static BiMap<CellFaceDir, Direction> CubeMapping = new BiMap<CellFaceDir, Direction>(new[]
        {
            ((CellFaceDir)CubeFaceDir.Right, Direction.XPlus),
            ((CellFaceDir)CubeFaceDir.Left, Direction.XMinus),
            ((CellFaceDir)CubeFaceDir.Up, Direction.YPlus),
            ((CellFaceDir)CubeFaceDir.Down, Direction.YMinus),
            ((CellFaceDir)CubeFaceDir.Forward, Direction.ZPlus),
            ((CellFaceDir)CubeFaceDir.Back, Direction.ZMinus),
        });

        public static BiMap<CellFaceDir, Direction> SquareMapping = new BiMap<CellFaceDir, Direction>(new[]
        {
            ((CellFaceDir)SquareFaceDir.Right, Direction.XPlus),
            ((CellFaceDir)SquareFaceDir.Left, Direction.XMinus),
            ((CellFaceDir)SquareFaceDir.Up, Direction.YPlus),
            ((CellFaceDir)SquareFaceDir.Down, Direction.YMinus),
        });

        public static BiMap<CellFaceDir, Direction> HexPrismMapping = new BiMap<CellFaceDir, Direction>(new[]
        {
            ((CellFaceDir)HexPrismFaceDir.Right, Direction.XPlus),
            ((CellFaceDir)HexPrismFaceDir.Left, Direction.XMinus),
            ((CellFaceDir)HexPrismFaceDir.Up, Direction.YPlus),
            ((CellFaceDir)HexPrismFaceDir.Down, Direction.YMinus),
            ((CellFaceDir)HexPrismFaceDir.ForwardRight, (Direction)6),
            ((CellFaceDir)HexPrismFaceDir.ForwardLeft, Direction.ZPlus),
            ((CellFaceDir)HexPrismFaceDir.BackRight, Direction.ZMinus),
            ((CellFaceDir)HexPrismFaceDir.BackLeft, (Direction)7),
        });

        public static BiMap<CellFaceDir, Direction> TrianglePrismMapping = new BiMap<CellFaceDir, Direction>(new[]
        {
            // Note this have been rotated by 90 degrees
            ((CellFaceDir)TrianglePrismFaceDir.Back, Direction.XPlus),
            ((CellFaceDir)TrianglePrismFaceDir.Forward, Direction.XMinus),
            ((CellFaceDir)TrianglePrismFaceDir.Up, Direction.YPlus),
            ((CellFaceDir)TrianglePrismFaceDir.Down, Direction.YMinus),
            ((CellFaceDir)TrianglePrismFaceDir.ForwardRight, Direction.ZPlus),
            ((CellFaceDir)TrianglePrismFaceDir.ForwardLeft, (Direction)7),
            ((CellFaceDir)TrianglePrismFaceDir.BackRight, (Direction)6),
            ((CellFaceDir)TrianglePrismFaceDir.BackLeft, Direction.ZMinus),
        });

        public static BiMap<CellFaceDir, Direction> GetDirectionMapping(ICellType cellType)
        {
            if(cellType is CubeCellType)
            {
                return CubeMapping;
            }
            if (cellType is SquareCellType)
            {
                return SquareMapping;
            }
            if (cellType is HexPrismCellType)
            {
                return HexPrismMapping;
            }
            if (cellType is TrianglePrismCellType)
            {
                return TrianglePrismMapping;
            }
            throw new Exception();
        }

        private static BiMap<(Direction, CellRotation), EdgeLabel> GetEdgeLabelMapping(IGrid grid, BiMap<CellFaceDir, Direction> directionMapping) {
            var i = 0;

            return new BiMap<(Direction, CellRotation), EdgeLabel>(
                    grid.CellType.GetFaceDirs()
                        .Select(faceDir => directionMapping[faceDir])
                        .SelectMany(dir => grid.GetMoveRotations().Select(rotation => ((dir, rotation), (EdgeLabel)(i++)))));
        }

        // Converts from an IGrid to an ITopology
        public class GenericTopology : ITopology
        {
            private readonly IGrid grid;
            private readonly BiMap<CellFaceDir, Direction> directionMapping;
            private readonly BiMap<(Direction, CellRotation), EdgeLabel> edgeLabelMapping;
            private readonly Vector3Int size;
            private readonly int directionsCount;
            private readonly bool[] mask;

            public GenericTopology(IGrid grid, bool[] mask = null)
            {
                this.grid = grid;
                directionMapping = GetDirectionMapping(grid.CellType);
                edgeLabelMapping = GetEdgeLabelMapping(grid, directionMapping);
                var minCell = grid.GetCells().Aggregate(Vector3Int.Max);
                var maxCell = grid.GetCells().Aggregate(Vector3Int.Max);
                if(minCell.x < 0 || minCell.y < 0|| minCell.z < 0)
                {
                    throw new NotImplementedException();
                }
                size = maxCell + Vector3Int.one;
                directionsCount = grid.CellType.GetFaceDirs().Count();
                if (mask == null)
                {

                    this.mask = new bool[grid.IndexCount];
                    foreach (var cell in grid.GetCells())
                    {
                        this.mask[grid.GetIndex(cell)] = true;
                    }
                }
                else
                {
                    this.mask = mask;
                }
            }

            public int IndexCount => grid.IndexCount;

            public int DirectionsCount => directionsCount;

            public int Width => size.x;

            public int Height => size.y;

            public int Depth => size.z;

            public bool[] Mask => mask;

            public void GetCoord(int index, out int x, out int y, out int z)
            {
                var v = grid.GetCell(index);
                x = v.x;
                y = v.y;
                z = v.z;
            }

            public int GetIndex(int x, int y, int z)
            {
                return grid.GetIndex(new Vector3Int(x, y, z));
            }

            public bool TryMove(int index, Direction direction, out int dest, out Direction inverseDirection, out EdgeLabel edgeLabel)
            {
                // TODO: Cache
                var cell = grid.GetCell(index);
                var faceDir = directionMapping[direction];
                if(!grid.TryMove(cell, faceDir, out var destCell, out var inverseFaceDir, out var cellRotation) || !grid.InBounds(destCell))
                {
                    dest = default;
                    inverseDirection = default;
                    edgeLabel = default;
                    return false;
                }
                dest = grid.GetIndex(destCell);
                inverseDirection = directionMapping[inverseFaceDir];
                edgeLabel = edgeLabelMapping[(direction, cellRotation)];
                return true;
            }

            // TODO: Perf
            public bool TryMove(int index, Direction direction, out int dest)
            {
                return TryMove(index, direction, out dest, out var _, out var _);
            }

            // TODO: Perf
            public bool TryMove(int x, int y, int z, Direction direction, out int dest, out Direction inverseDirection, out EdgeLabel edgeLabel)
            {
                return TryMove(grid.GetIndex(new Vector3Int(x, y, z)), direction, out dest, out inverseDirection, out edgeLabel);
            }

            // TODO: Perf
            public bool TryMove(int x, int y, int z, Direction direction, out int dest)
            {
                return TryMove(grid.GetIndex(new Vector3Int(x, y, z)), direction, out dest);
            }

            // TODO: Perf
            public bool TryMove(int x, int y, int z, Direction direction, out int destx, out int desty, out int destz)
            {
                var b = TryMove(grid.GetIndex(new Vector3Int(x, y, z)), direction, out var dest);
                var destCell = grid.GetCell(dest);
                destx = destCell.x;
                desty = destCell.y;
                destz = destCell.z;
                return b;
            }

            public ITopology WithMask(bool[] mask)
            {
                var m2 = new bool[IndexCount];
                for(var i=0;i<IndexCount;i++)
                {
                    m2[i] = this.mask[i] && mask[i];
                }
                return new GenericTopology(grid, m2);
            }
        }

        internal static Vector3Int? GetContradictionLocation(ITopoArray<ModelTile?> result, IGrid grid)
        {
            var topology = result.Topology;
            var mask = topology.Mask ?? Enumerable.Range(0, topology.IndexCount).Select(x => true).ToArray();

            var empty = mask.ToArray();
            for (var x = 0; x < topology.Width; x++)
            {
                for (var y = 0; y < topology.Height; y++)
                {
                    for (var z = 0; z < topology.Depth; z++)
                    {
                        var p = new Vector3Int(x, y, z);
                        // Skip if already filled
                        if (!empty[grid.GetIndex(p)])
                            continue;
                        var modelTile = result.Get(x, y, z);
                        if (modelTile == null)
                            continue;
                        var tile = modelTile.Value.Tile;
                        if (tile == null)
                        {
                            return new Vector3Int(x, y, z);
                        }
                    }
                }
            }

            return null;
        }

        #endregion


        #region ModelTile conversion
        /// <summary>
        /// Converts from DeBroglie's array format back to Tessera's.
        /// Note these do not have a world position, you'll need to call .Align on them first.
        /// </summary>
        internal static IDictionary<Vector3Int, ModelTile> ToTileDictionary(ITopoArray<ModelTile?> result, IGrid grid)
        {
            var data = new Dictionary<Vector3Int, ModelTile>();
            var topology = result.Topology;
            var mask = topology.Mask ?? Enumerable.Range(0, topology.IndexCount).Select(x => true).ToArray();

            var empty = mask.ToArray();
            for (var x = 0; x < topology.Width; x++)
            {
                for (var y = 0; y < topology.Height; y++)
                {
                    for (var z = 0; z < topology.Depth; z++)
                    {
                        var modelTile = result.Get(x, y, z);
                        if (modelTile == null)
                            continue;
                        data[new Vector3Int(x, y, z)] = modelTile.Value;
                    }
                }
            }

            return data;
        }

        #endregion
    }
}
