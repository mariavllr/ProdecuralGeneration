using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    public interface ITesseraTileOutput
    {
        /// <summary>
        /// Is this output safe to use with AnimatedGenerator
        /// </summary>
        bool SupportsIncremental { get; }

        /// <summary>
        /// Is the output currently empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Clear the output
        /// </summary>
        void ClearTiles(IEngineInterface engine);

        /// <summary>
        /// Update a chunk of tiles.
        /// If inremental updates are supported, then:
        ///  * Tiles can replace other tiles, as indicated by the <see cref="TesseraTileInstance.Cells"/> field.
        ///  * A tile of null indicates that the tile should be erased
        /// </summary>
        void UpdateTiles(TesseraCompletion completion, IEngineInterface engine);
    }

    internal class ForEachOutput : ITesseraTileOutput
    {
        private Action<TesseraTileInstance> onCreate;

        public ForEachOutput(Action<TesseraTileInstance> onCreate)
        {
            this.onCreate = onCreate;
        }

        public bool IsEmpty => throw new NotImplementedException();

        public bool SupportsIncremental => throw new NotImplementedException();

        public void ClearTiles(IEngineInterface engine)
        {
            throw new NotImplementedException();
        }

        public void UpdateTiles(TesseraCompletion completion, IEngineInterface engine)
        {
            foreach (var i in completion.tileInstances)
            {
                onCreate(i);
            }
        }
    }

    public class InstantiateOutput : ITesseraTileOutput
    {
        private readonly Transform transform;

        public InstantiateOutput(Transform transform)
        {
            this.transform = transform;
        }

        public bool IsEmpty => transform.childCount == 0;

        public bool SupportsIncremental => false;

        public void ClearTiles(IEngineInterface engine)
        {
            var children = transform.Cast<Transform>().ToList();
            foreach (var child in children)
            {
                engine.Destroy(child.gameObject);
            }
        }

        public void UpdateTiles(TesseraCompletion completion, IEngineInterface engine)
        {
            foreach (var i in completion.tileInstances)
            {
                foreach (var go in TesseraGenerator.Instantiate(i, transform))
                {
                    engine.RegisterCreatedObjectUndo(go);
                }
            }
        }
    }

    internal class UpdatableInstantiateOutput : ITesseraTileOutput
    {
        private Dictionary<Vector3Int, GameObject[]> instantiated = new Dictionary<Vector3Int, GameObject[]>();
        private readonly Transform transform;

        public UpdatableInstantiateOutput(Transform transform)
        {
            this.transform = transform;
        }

        public bool IsEmpty => transform.childCount == 0;

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
        }

        public void UpdateTiles(TesseraCompletion completion, IEngineInterface engine)
        {
            foreach(var kv in completion.tileData)
            {
                Clear(kv.Key, engine);
            }
            foreach (var i in completion.tileInstances)
            {
                if (i.Tile != null)
                {
                    instantiated[i.Cells.First()] = TesseraGenerator.Instantiate(i, transform);
                }
            }
        }
    }
}
