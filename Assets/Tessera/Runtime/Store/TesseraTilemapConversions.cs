using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    internal static class TesseraTilemapConversions
    {
        internal static IEnumerable<TesseraTileInstance> ToTileInstances(TesseraTilemap tilemap, TRS align = null)
        {
            return ToTileInstances(tilemap.Data, tilemap.Grid);
        }

        internal static IEnumerable<TesseraTileInstance> ToTileInstances(IDictionary<Vector3Int, ModelTile> data, IGrid grid, TRS align = null)
        {
            var filled = new HashSet<Vector3Int>();
            foreach (var kv in data)
            {
                var cell = kv.Key;
                var modelTile = kv.Value;
                // Skip if already filled
                if (filled.Contains(cell))
                    continue;
                var tile = modelTile.Tile;
                if (tile == null)
                    continue;

                var ti = ToTileInstance(cell, modelTile, grid);

                // Fill locations
                foreach (var p2 in ti.Cells)
                {
                    filled.Add(p2);
                }

                if(align != null && ti != null)
                {
                    ti.Align(align);
                }

                if (ti != null)
                {
                    yield return ti;
                }
            }
        }

        internal static IEnumerable<KeyValuePair<Vector3Int, ModelTile>> GrowModelTile(Vector3Int p, ModelTile modelTile, IGrid grid)
        {
            var rot = modelTile.Rotation;
            var tile = modelTile.Tile;
            for (var i = 0; i < tile.offsets.Count; i++)
            {
                var offset = tile.offsets[i];
                if (!grid.TryMoveByOffset(p, modelTile.Offset, offset, rot, out var cell, out var rotation))
                {
                    throw new Exception($"BigTile {modelTile.Tile} is not fully contained in topology. This indicates an internal error.");
                }
                var mt2 = new ModelTile { Tile = tile, Offset = offset, Rotation = rotation };
                yield return new KeyValuePair<Vector3Int, ModelTile>(cell, mt2);
            }
        }

        // TODO: Optimize this method or allow users to spread the cost over multiple frames.
        private static TesseraTileInstance ToTileInstance(Vector3Int cell, ModelTile modelTile, IGrid grid)
        {
            var rot = modelTile.Rotation;
            var tile = modelTile.Tile;
            var cellType = grid.CellType;

            var tileTrs = new TRS(cellType.GetMatrix(rot));

            var localTrs = grid.GetTRS(cell) * tileTrs * new TRS(-cellType.GetCellCenter(modelTile.Offset, tile.center, modelTile.Tile.tileSize));

            var cells = new Vector3Int[tile.offsets.Count];
            var rotations = new CellRotation[tile.offsets.Count];
            for (var i = 0; i < tile.offsets.Count; i++)
            {
                var offset = tile.offsets[i];
                if (!grid.TryMoveByOffset(cell, modelTile.Offset, offset, rot, out var endCell, out var endRotation))
                {
                    throw new Exception($"BigTile {modelTile.Tile} is not fully contained in topology. This indicates an internal error.");
                }
                cells[i] = endCell;
                rotations[i] = endRotation;
            }
            var instance = new TesseraTileInstance
            {
                Tile = tile,
                LocalPosition = localTrs.Position,
                LocalRotation = localTrs.Rotation,
                LocalScale = localTrs.Scale,
                Cell = cells[0],
                Cells = cells,
                CellRotation = rot,
                CellRotations = rotations,
            };
            if (grid is QuadMeshGrid quadMeshGrid)
            {
                instance.MeshDeformation = MeshUtils.GetDeformation(quadMeshGrid.SurfaceMesh, quadMeshGrid.TileHeight, quadMeshGrid.SurfaceOffset, quadMeshGrid.SurfaceSmoothNormals, instance);
            }
            else if (grid is TriangleMeshGrid triangleMeshGrid)
            {
                instance.MeshDeformation = MeshUtils.GetDeformation(triangleMeshGrid.SurfaceMesh, triangleMeshGrid.TileHeight, triangleMeshGrid.SurfaceOffset, triangleMeshGrid.SurfaceSmoothNormals, instance);
            }
            return instance;
        }

        internal static IDictionary<Vector3Int, ModelTile> ToModelTiles(IList<TesseraTileInstance> instances)
        {
            var results = new Dictionary<Vector3Int, ModelTile>();
            foreach (var i in instances)
            {
                for (var j = 0; j < i.Cells.Length; j++)
                {
                    results[i.Cells[j]] = new ModelTile(i.Tile, i.CellRotations[j], i.Tile.offsets[j]);
                }
            }
            return results;
        }
    }
}
