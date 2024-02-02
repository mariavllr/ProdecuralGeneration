using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    [Serializable]
    public class TileMapping
    {
        public TesseraTileBase from;

        public GameObject to;

        public bool instantiateChildrenOnly;
    }

    /// <summary>
    /// Attach this to a TesseraGenerator to control how tiles are instantiated.
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    [RequireComponent(typeof(TesseraGenerator))]
    [AddComponentMenu("Tessera/Tessera Instantiate Output", 40)]
    public class TesseraInstantiateOutput : MonoBehaviour, ITesseraTileOutput
    {
        public Transform parent;

        public TileMapping[] tileMappings;

        private Dictionary<Vector3Int, GameObject[]> instantiated = new Dictionary<Vector3Int, GameObject[]>();

        public TesseraInstantiateOutput()
        {
        }

        private Transform GetParent() => parent == null ? transform : parent;

        public bool IsEmpty => GetParent().childCount == 0;

        public bool SupportsIncremental => true;

        private void Clear(Vector3Int p, IEngineInterface engine)
        {
            if (instantiated.TryGetValue(p, out var gos) && gos != null)
            {
                foreach (var go in gos)
                {
                    engine.Destroy(go);
                }
            }

            instantiated[p] = null;
        }

        public void ClearTiles(IEngineInterface engine)
        {
            foreach (var k in instantiated.Keys.ToList())
            {
                Clear(k, engine);
            }
            var children = GetParent().Cast<Transform>().ToList();
            foreach (var child in children)
            {
                engine.Destroy(child.gameObject);
            }
        }

        public void UpdateTiles(TesseraCompletion completion, IEngineInterface engine)
        {
            var parent = GetParent();
            var tileMappingsDict = tileMappings.ToDictionary(x => x.from);
            foreach (var i in completion.tileInstances)
            {
                foreach (var p in i.Cells)
                {
                    Clear(p, engine);
                }
                if (i.Tile == null)
                    continue;
                var prefab = i.Tile.gameObject;
                var instantiateChildrenOnly = i.Tile.instantiateChildrenOnly;
                if (tileMappingsDict.TryGetValue(i.Tile, out var mapping))
                {
                    prefab = mapping.to;
                    instantiateChildrenOnly = mapping.instantiateChildrenOnly;
                }
                if (i.Tile != null)
                {
                    instantiated[i.Cells.First()] = TesseraGenerator.Instantiate(i, parent, prefab, instantiateChildrenOnly);
                }
            }
        }
    }
}
