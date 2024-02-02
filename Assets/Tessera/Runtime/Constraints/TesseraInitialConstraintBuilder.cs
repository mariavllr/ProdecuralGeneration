using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    public class TesseraInitialConstraintBuilder
    {
        private Transform transform;

        private IGrid grid;

        internal TesseraInitialConstraintBuilder(Transform transform, IGrid grid)
        {
            this.transform = transform;

            this.grid = grid;
        }

        public IGrid Grid => grid;

        private IEnumerable<T> FindObjectsOfType<T>() where T : Component
        {
            return UnityEngine.Object.FindObjectsOfType<T>()
                .Where(x => x.gameObject.activeSelf);
                // Ignore anything that is *inside* a tile. Useful for multipass generation, still experimental
                //.Where(x => x.transform.parent?.GetComponentInParent<TesseraTileBase>() == null);
        }

        /// <summary>
        /// Searches the scene for all applicable game objects and converts them to ITesseraInitialConstraint
        /// </summary>
        public List<ITesseraInitialConstraint> SearchInitialConstraints()
        {
            var pins = FindObjectsOfType<TesseraPinned>().Where(x=>x!=null);
            var pinGos = new HashSet<GameObject>(pins.Select(x => x.gameObject));
            var cellType = grid.CellType;

            var pinConstraints = pins
                .Select(x => GetInitialConstraint(x, x.transform.localToWorldMatrix));
            var tileConstraints = FindObjectsOfType<TesseraTileBase>()
                .Where(x => !pinGos.Contains(x.gameObject))
                .Where(x => x.CellType == cellType)
                .Select(x => GetInitialConstraint(x, x.transform.localToWorldMatrix))
                .Cast<ITesseraInitialConstraint>();
            var volumeConstraints = FindObjectsOfType<TesseraVolume>()
                .Where(x => x != null)
                .Select(x => GetInitialConstraint(x))
                .Cast<ITesseraInitialConstraint>();
            return pinConstraints.Concat(tileConstraints).Concat(volumeConstraints)
                .Where(x => x != null)
                .ToList();
        }

        /// <summary>
        /// Gets the initial constraint for a given game object. 
        /// It checks for a TesseraPinned, TesseraTile or TesseraVolume component.
        /// </summary>
        public ITesseraInitialConstraint GetInitialConstraint(GameObject gameObject)
        {
            if (gameObject.GetComponent<TesseraPinned>() is TesseraPinned pin)
            {
                return GetInitialConstraint(pin);
            }
            else if (gameObject.GetComponent<TesseraTileBase>() is TesseraTileBase tesseraTile)
            {
                return GetInitialConstraint(tesseraTile, tesseraTile.transform.localToWorldMatrix);
            }
            else if (gameObject.GetComponent<TesseraVolume>() is TesseraVolume tesseraVolume)
            {
                return GetInitialConstraint(tesseraVolume);
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Gets the initial constraint from a given tile.
        /// The tile should be aligned with the grid defined by this generator.
        /// </summary>
        /// <param name="tile">The tile to inspect</param>
        public TesseraVolumeFilter GetInitialConstraint(TesseraVolume volume)
        {
            var colliders = volume.gameObject.GetComponents<Collider>();
            var cells = volume.invertArea 
                ? new HashSet<Vector3Int>(grid.GetCells())
                : new HashSet<Vector3Int>();

            foreach(var collider in colliders)
            {
                if (!collider.enabled)
                    continue;

                var worldBounds = collider.bounds;
                var localBounds = GeometryUtils.Multiply(transform.worldToLocalMatrix, worldBounds);
                foreach (var cell in grid.GetCellsIntersectsApprox(localBounds, true))
                {
                    var center = grid.GetCellCenter(cell);
                    var worldCenter = transform.TransformPoint(center);
                    if(collider.ClosestPoint(worldCenter) == worldCenter)
                    {
                        if(volume.invertArea)
                        {
                            cells.Remove(cell);
                        }
                        else
                        {
                            cells.Add(cell);
                        }
                    }
                }
            }
            return new TesseraVolumeFilter
            {
                name = volume.name,
                cells = cells.ToList(),
                tiles = volume.tiles,
                volumeType = volume.volumeType,
                
            };
        }

        /// <summary>
        /// Gets the initial constraint from a given tile.
        /// The tile should be aligned with the grid defined by this generator.
        /// </summary>
        /// <param name="tile">The tile to inspect</param>
        public TesseraInitialConstraint GetInitialConstraint(TesseraTileBase tile)
        {
            return GetInitialConstraint(tile, tile.transform.localToWorldMatrix);
        }

        /// <summary>
        /// Gets the initial constraint from a given tile at a given position.
        /// The tile should be aligned with the grid defined by this generator.
        /// </summary>
        /// <param name="tile">The tile to inspect</param>
        /// <param name="localToWorldMatrix">The matrix indicating the position and rotation of the tile</param>
        public TesseraInitialConstraint GetInitialConstraint(TesseraTileBase tile, Matrix4x4 localToWorldMatrix)
        {
            if (!grid.FindCell(tile.center, transform.worldToLocalMatrix * localToWorldMatrix, out var cell, out var rotation))
            {
                return null;
            }
            // TODO: Needs support for big tiles
            return new TesseraInitialConstraint
            {
                name = tile.name,
                faceDetails = tile.faceDetails,
                offsets = tile.offsets,
                cell = cell,
                rotation = rotation,
            };
        }

        /// <summary>
        /// Gets the initial constraint from a given pin at a given position.
        /// It should be aligned with the grid defined by this generator.
        /// </summary>
        /// <param name="pin">The pin to inspect</param>
        public ITesseraInitialConstraint GetInitialConstraint(TesseraPinned pin)
        {
            return GetInitialConstraint(pin, pin.transform.localToWorldMatrix);
        }


        /// <summary>
        /// Gets the initial constraint from a given pin at a given position.
        /// It should be aligned with the grid defined by this generator.
        /// </summary>
        /// <param name="pin">The pin to inspect</param>
        /// <param name="localToWorldMatrix">The matrix indicating the position and rotation of the tile</param>
        public ITesseraInitialConstraint GetInitialConstraint(TesseraPinned pin, Matrix4x4 localToWorldMatrix)
        {
            var tile = pin.tile ?? pin.GetComponent<TesseraTile>() ?? throw new Exception($"Tile not defined for {pin}");

            if (!grid.FindCell(tile.center, transform.worldToLocalMatrix * localToWorldMatrix, out var cell, out var rotation))
            {
                return null;
            }

            if (pin.pinType == PinType.Pin)
            {
                return new TesseraPinConstraint
                {
                    name = pin.name,
                    tile = tile,
                    cell = cell,
                    rotation = rotation,
                };
            }
            else
            {
                return new TesseraInitialConstraint
                {
                    name = tile.name,
                    faceDetails = tile.faceDetails,
                    offsets = pin.pinType == PinType.FacesOnly ? new List<Vector3Int>() : tile.offsets,
                    cell = cell,
                    rotation = rotation,
                };
            }
        }

        /// <summary>
        /// Converts a TesseraTileInstance to a ITesseraInitialConstraint.
        /// This allows you to easily use the output of one generation for later generations
        /// </summary>
        public ITesseraInitialConstraint GetInitialConstraint(TesseraTileInstance tileInstance, PinType pinType = PinType.Pin)
        {
            if (pinType == PinType.Pin)
            {
                return new TesseraPinConstraint
                {
                    name = tileInstance.Cell.ToString(),
                    tile = tileInstance.Tile,
                    cell = tileInstance.Cell,
                    rotation = tileInstance.CellRotation,
                };
            }
            else
            {
                return new TesseraInitialConstraint
                {
                    name = tileInstance.Cell.ToString(),
                    faceDetails = tileInstance.Tile.faceDetails,
                    offsets = pinType == PinType.FacesOnly ? new List<Vector3Int>() : tileInstance.Tile.offsets,
                    cell = tileInstance.Cell,
                    rotation = tileInstance.CellRotation,
                };
            }
        }
    }
}
