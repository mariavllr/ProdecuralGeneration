using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tessera
{
    /// <summary>
    /// Attach this to a TesseraGenerator to output the tiles to a Unity Tilemap component instead of directly instantiating them.
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    [RequireComponent(typeof(TesseraGenerator))]
    [AddComponentMenu("Tessera/Tessera Tilemap Output", 40)]
    public class TesseraTilemapOutput : MonoBehaviour, ITesseraTileOutput
    {
        /// <summary>
        /// The tilemap to write results to.
        /// </summary>
        public Tilemap tilemap;

        /// <summary>
        /// If true, TesseraTiles that have a SpriteRenderer will be recorded to the Tilemap as that sprite. 
        /// This is more efficient, but you will lose any other components on the object.
        /// </summary>
        public bool useSprites;

        /// <summary>
        /// If true, tiles will be transformed to align with the world space position of the generator.
        /// </summary>
        public bool useWorld = true;

        public bool IsEmpty => tilemap == null || tilemap.GetUsedTilesCount() == 0;

        public bool SupportsIncremental => true;

        public void ClearTiles(IEngineInterface engine)
        {
            if (tilemap.gameObject != null)
            {
                engine.RegisterCompleteObjectUndo(tilemap.gameObject);
            }
            tilemap.ClearAllTiles();
        }

        public void UpdateTiles(TesseraCompletion completion, IEngineInterface engine)
        {
            if (tilemap.gameObject != null)
            {
                engine.RegisterCompleteObjectUndo(tilemap.gameObject);
            }

            var generator = GetComponent<TesseraGenerator>();

            if(generator.surfaceMesh != null)
            {
                throw new Exception("TesseraTilemapOutput doesn't support mesh surfaces");
            }

            Vector3Int GeneratorCellToMapCell(Vector3Int p)
            {
                if (useWorld)
                {
                    // Find posiiton in tilemap that best corresponds to p
                    var worldP = generator.transform.TransformPoint(completion.grid.GetCellCenter(p));
                    return tilemap.WorldToCell(worldP);
                }
                else
                {
                    return p;
                }
            }

            foreach (var i in completion.tileInstances)
            {
                var mapCells = i.Cells.Select(GeneratorCellToMapCell).ToList();
                foreach (var c in mapCells)
                {
                    tilemap.SetTile(c, null);
                }
                var go = i.Tile?.gameObject;
                var m = Matrix4x4.identity;
                if (i.Tile?.instantiateChildrenOnly ?? false)
                {
                    var childCount = go.transform.childCount;
                    if (childCount == 0)
                    {
                        go = null;
                    }
                    else if (childCount == 1)
                    {
                        var child = go.transform.GetChild(0);
                        m = Matrix4x4.TRS(child.localPosition, child.localRotation, child.localScale);
                        go = child.gameObject;
                    }
                    else
                    {
                        throw new Exception($"Cannot put children of {i.Tile} in Tilemap: each tile can only hold a single item. Consider disabling instantiateChildrenOnly");
                    }
                }

                // Detect sprites. We special case these to go directly into the tilemap as sprites.
                var sr = go?.GetComponent<SpriteRenderer>();
                Sprite tileSprite = null;
                GameObject tileGameObject = null;
                if (useSprites && sr != null)
                {
                    tileSprite = sr.sprite;
                }
                else
                {
                    tileGameObject = go;
                }

                var mapCell = mapCells.OrderBy(x => x.x).ThenBy(x => x.y).ThenBy(x => x.z).First();

                // Get the position of game object in world space
                if(i.Tile == null)
                {
                    m = Matrix4x4.identity;
                }
                else if (useWorld)
                {
                    m = Matrix4x4.TRS(i.Position, i.Rotation, i.LossyScale) * m;
                }
                else
                {
                    m = Matrix4x4.TRS(i.LocalPosition, i.LocalRotation, i.LocalScale) * m;
                }
                // Get the position of the game object in local space
                m = tilemap.transform.worldToLocalMatrix * m;
                // Now find it relative to the cell
                m = Matrix4x4.Translate(-tilemap.LocalToWorld(tilemap.CellToLocalInterpolated(mapCell + tilemap.tileAnchor))) * m;

                var mapTile = ScriptableObject.CreateInstance<TesseraTransformedTile>();
                if (useWorld)
                {
                    mapTile.position = i.Position;
                    mapTile.rotation = i.Rotation;
                    mapTile.localScale = i.LocalScale;
                }
                else
                {
                    mapTile.position = i.LocalPosition;
                    mapTile.rotation = i.LocalRotation;
                    mapTile.localScale = i.LocalScale;
                }
                mapTile.useWorld = useWorld;
                mapTile.gameObject = tileGameObject;
                mapTile.sprite = tileSprite;

                tilemap.SetTile(
                    mapCell,
                    mapTile);

                // Set the transform for sprites. Transform for game objects is done in TransformTile itself.
                if (tileSprite != null)
                {
                    tilemap.SetTransformMatrix(mapCell, m);
                }
            }
        }


    }
}
