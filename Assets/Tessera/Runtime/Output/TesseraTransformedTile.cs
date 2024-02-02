using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tessera
{
    /// <summary>
    /// Definesa Unity tile that has a specific transform applied to it.
    /// Used by <see cref="TesseraTilemapOutput"/>
    /// </summary>
    public class TesseraTransformedTile : Tile
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        public bool useWorld;

        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {
            if (go)
            {
                if (useWorld)
                {
                    go.transform.position = this.position;
                    go.transform.rotation = rotation;
                    go.transform.localScale = localScale;
                }
                else
                {
                    go.transform.localPosition = this.position;
                    go.transform.localRotation = this.rotation;
                    go.transform.localScale = localScale;
                }
            }
            return base.StartUp(position, tilemap, go);
        }

        // TODO: Find some way of deleting an entire big tile when any part of it is deleted.
        /*
        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            var isDeleted = tilemap.GetTile(position) != this;
            base.RefreshTile(position, tilemap);
        }
        */
    }
}
