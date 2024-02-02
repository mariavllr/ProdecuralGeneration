using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tessera;
using System.Linq;
using System;

namespace Tessera
{
    public enum ChunkCleanupType
    {
        /// <summary>
        /// Never cleanup chunks
        /// </summary>
        None,
        /// <summary>
        /// Remove the GameObjects, but keep tile data so they can be recreated exactly
        /// </summary>
        Memoize,
        /// <summary>
        /// Remove everything associated with the chunk.
        /// </summary>
        Full,
    }

    public class InfiniteGenerator : MonoBehaviour
    {
        /// <summary>
        /// The generator that is used to fill the chunks. It also determines the size of each chunk.
        /// </summary>
        public TesseraGenerator generator;

        /// <summary>
        /// The number of chunks that can be generated concurrently. Note that turning this up can cause slightly worse quality output.
        /// </summary>
        public int parallelism = 1;

        /// <summary>
        /// Determines the volume in which chunks should be generated. You typically want to use a large trigger collider following the player or camera, to ensure that everything nearby is generated.
        /// </summary>
        public List<Collider> watchedColliders;


        /// <summary>
        /// If true, chunks repeat infinitely on this axis. If false, you can specify the <see cref="minXChunk"/>/<see cref="maxXChunk"/> to give a limit to the amount of chunks generated.
        /// </summary>
        public bool infiniteX = true;

        /// <summary>
        /// If true, chunks repeat infinitely on this axis. If false, you can specify the <see cref="minYChunk"/>/<see cref="maxYChunk"/> to give a limit to the amount of chunks generated.
        /// </summary>
        public bool infiniteY = false;

        /// <summary>
        /// If true, chunks repeat infinitely on this axis. If false, you can specify the <see cref="minZChunk"/>/<see cref="maxZChunk"/> to give a limit to the amount of chunks generated.
        /// </summary>
        public bool infiniteZ = true;

        public int minXChunk = 0;
        public int maxXChunk = 0;
        public int minYChunk = 0;
        public int maxYChunk = 0;
        public int minZChunk = 0;
        public int maxZChunk = 0;

        /// <summary>
        /// Fixes the seed for random number generator.
        /// If the value is zero, the seed is taken from Unity.Random.
        /// Note that generation is still non-deterministic even with a fixed seed.
        /// </summary>
        public int seed;

        /// <summary>
        /// Time between scans that detect if new chunks need creating
        /// </summary>
        public float scanInterval = 0;

        /// <summary>
        /// Maximum number of tiles to create per-update. Negative means unbounded.
        /// </summary>
        public float maxInstantiatePerUpdate = -1;

        /// <summary>
        /// Time between cleanups that remove chunks that are no longer needed.
        /// </summary>
        public float cleanupInterval = 1;

        /// <summary>
        /// Time a chunk is kept around for even if not near a watched collider.
        /// </summary>
        public float chunkPersistTime = 0;

        /// <summary>
        /// Maximum number of chunks to remove per update. Zero means unbounded.
        /// </summary>
        public int maxCleanupPerUpdate = -1;

        /// <summary>
        /// Determines what to do with unused chunks
        /// </summary>
        public ChunkCleanupType chunkCleanupType;

        private IDictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

        private IGrid chunkGrid;

        private int generatingCount = 0;

        private float scanTimer;
        private float cleanupTimer;

        private Queue<Action> createQueue = new Queue<Action>();
        private Queue<Chunk> cleanupQueue = new Queue<Chunk>();

        private void Start()
        {
            var cellType = generator.CellType;
            if (cellType == CubeCellType.Instance)
            {
                chunkGrid = new CubeGrid(Vector3.zero, Vector3Int.one, Vector3.Scale(generator.tileSize, generator.size));
            }
            else if(cellType == SquareCellType.Instance)
            {
                chunkGrid = new SquareGrid(Vector3.zero, Vector2Int.one, Vector3.Scale(generator.tileSize, generator.size));
            }
            else
            {
                throw new System.Exception($"InfiniteGenerator doesn't support celltype {cellType}");
            }
        }

        void Update()
        {
            if (Time.unscaledTime > scanTimer)
            {
                scanTimer = Time.unscaledTime + scanInterval;
                Scan();
            }

            if(Time.unscaledTime > cleanupTimer && chunkCleanupType != ChunkCleanupType.None)
            {
                cleanupTimer = Time.unscaledTime + cleanupInterval;
                Cleanup();
            }

            for (var i = 0; maxInstantiatePerUpdate < 0 || i < maxInstantiatePerUpdate; i++)
            {
                if (createQueue.Count == 0)
                    break;
                createQueue.Dequeue()();
            }


            for (var i = 0; maxCleanupPerUpdate < 0 || i < maxCleanupPerUpdate; i++)
            {
                if (cleanupQueue.Count == 0)
                    break;

                var chunk = cleanupQueue.Dequeue();

                // Check chunk again in case something has changed.
                if (!ShouldRemove(chunk))
                    continue;

                if (chunkCleanupType == ChunkCleanupType.Full)
                {
                    Destroy(chunk.gameObject);
                    chunks.Remove(chunk.chunkCell);
                }
                else if (chunkCleanupType == ChunkCleanupType.Memoize)
                {
                    if (chunk.status == ChunkStatus.Generated)
                    {
                        Destroy(chunk.gameObject);
                        chunk.gameObject = null;
                        chunk.status = ChunkStatus.Memoized;
                    }
                }
            }

        }

        private void Scan()
        {
            if (generatingCount < parallelism)
            {
                var chunk = GetReadyChunks().FirstOrDefault();
                if (chunk == null)
                    return;

                StartChunk(chunk);
            }
        }

        private bool ShouldRemove(Chunk chunk) => 
            // Currently, these states cannot be interupted.
            chunk.status != ChunkStatus.Generating && 
            chunk.status != ChunkStatus.Instantiating && 
            // Some time has passed since we last needed this chunk.
            chunk.lastWatch + chunkPersistTime < Time.unscaledTime;

        private void Cleanup()
        {
            var t = Time.unscaledTime;
            foreach(var chunk in GetWatchedChunks())
            {
                chunk.lastWatch = t;
            }
            var toRemove = chunks.Values
                .Where(ShouldRemove);

            foreach (var chunk in toRemove)
            {
                cleanupQueue.Enqueue(chunk);
            }
        }

        private void StartChunk(Chunk chunk)
        {
            if (chunk.status == ChunkStatus.Memoized)
            {
                CompleteChunk(chunk, chunk.completion);
                return;
            }

            if (chunk.status != ChunkStatus.Pending)
                throw new System.Exception();

            chunk.status = ChunkStatus.Generating;
            chunk.lastWatch = Time.unscaledTime;
            generatingCount++;

            Debug.Log($"Started generating chunk {chunk.chunkCell}");

            var icb = generator.GetInitialConstraintBuilder();
            var initialConstraints = new List<ITesseraInitialConstraint>();
            foreach (var nChunk in GetNeighbours(chunk.chunkCell))
            {
                if (nChunk.status == ChunkStatus.Generated || nChunk.status == ChunkStatus.Instantiating)
                {
                    foreach (var i in nChunk.completion.tileInstances)
                    {
                        // Fixup. Is there a way to avoid this?
                        var cellOffset = Vector3Int.Scale(nChunk.chunkCell - chunk.chunkCell, generator.size);
                        var translated = TranslateInstance(i, cellOffset);

                        var ic = icb.GetInitialConstraint(translated, PinType.FacesAndInterior);
                        initialConstraints.Add(ic);
                    }
                }
            }

            StartCoroutine(generator.StartGenerate(new TesseraGenerateOptions
            {
                onComplete = c => CompleteChunk(chunk, c),
                initialConstraints = initialConstraints,
                seed = seed == 0 ? seed : HashCombine(seed, chunk.chunkCell.x, chunk.chunkCell.y, chunk.chunkCell.z),
            }));
        }

        // Unity's GetHashCode doesn't appear to be deterministic from run to run?
        int HashCombine(int v1, int v2, int v3, int v4)
        {
            unchecked
            {
                int hash = 5381;

                hash = ((hash << 5) + hash) ^ v1;
                hash = ((hash << 5) + hash) ^ v2;
                hash = ((hash << 5) + hash) ^ v3;
                hash = ((hash << 5) + hash) ^ v4;

                return hash;
            }
        }


        private void CompleteChunk(Chunk chunk, TesseraCompletion completion)
        {
            if (chunk.status != ChunkStatus.Generating && chunk.status != ChunkStatus.Memoized)
                throw new System.Exception();

            if (chunk.status == ChunkStatus.Generating)
            {
                chunk.completion = completion;
                generatingCount--;
                Debug.Log($"Finished generating chunk {chunk.chunkCell}");
            };

            chunk.status = completion.success ? ChunkStatus.Instantiating : ChunkStatus.Broken;
            chunk.lastWatch = Time.unscaledTime;

            if (completion.success)
            {
                var go = chunk.gameObject = new GameObject($"Chunk {chunk.chunkCell}");
                go.transform.parent = transform;
                go.transform.localPosition = chunkGrid.GetCellCenter(chunk.chunkCell);

                foreach (var i in completion.tileInstances)
                {
                    createQueue.Enqueue(() =>
                    {
                        i.Align(TRS.World(go.transform));
                        TesseraGenerator.Instantiate(i, go.transform);
                    });
                }

                createQueue.Enqueue(() =>
                {
                    chunk.status = ChunkStatus.Generated;
                });
            }
            else
            {
                Debug.LogWarning($"Couldn't generate chunk {chunk.chunkCell}");
            }
        }

        private TesseraTileInstance TranslateInstance(TesseraTileInstance i, Vector3Int offset)
        {
            i = i.Clone();
            i.Cell = i.Cell + offset;
            i.Cells = i.Cells.Select(x => x + offset).ToArray();
            return i;
        }

        // Gets the chunk at a given chunk cell. Will lazily construct a pending chunk.
        private Chunk GetChunk(Vector3Int chunkCell)
        {
            if (chunks.TryGetValue(chunkCell, out var chunk))
                return chunk;

            var inBounds = (infiniteX || (minXChunk <= chunkCell.x && chunkCell.x <= maxXChunk)) &&
                (infiniteY || (minYChunk <= chunkCell.y && chunkCell.y <= maxYChunk)) &&
                (infiniteZ || (minZChunk <= chunkCell.z && chunkCell.z <= maxZChunk));

            return chunks[chunkCell] = new Chunk
            {
                status = inBounds ? ChunkStatus.Pending : ChunkStatus.OutOfBounds,
                chunkCell = chunkCell,
            };
        }

        /// <summary>
        /// Gets the chunks adjacent to the given chunk.
        /// </summary>
        private IEnumerable<Chunk> GetNeighbours(Vector3Int chunkCell)
        {
            foreach (var dir in chunkGrid.GetValidFaceDirs(chunkCell))
            {
                if (chunkGrid.TryMove(chunkCell, dir, out var neighbour, out var _, out var _))
                {
                    yield return GetChunk(neighbour);
                }
            }
        }

        // Recommends pending chunks to generate, in order of priority.
        private IEnumerable<Chunk> GetWatchedChunks()
        {
            var nearChunks = new Dictionary<Vector3Int, float>();
            void Add(Vector3Int chunkCell, float distance)
            {
                if (nearChunks.TryGetValue(chunkCell, out var currentDistance))
                {
                    if (currentDistance < distance)
                        return;
                }
                nearChunks[chunkCell] = distance;
            }
            foreach (var collider in watchedColliders)
            {
                var localBounds = GeometryUtils.Multiply(transform.worldToLocalMatrix, collider.bounds);
                foreach (var chunkCell in chunkGrid.GetCellsIntersectsApprox(localBounds, false))
                {
                    var chunkCenter = chunkGrid.GetCellCenter(chunkCell);
                    Add(chunkCell, (collider.bounds.center - chunkCenter).magnitude);
                }
            }
            return nearChunks.OrderBy(x => x.Value).Select(x => x.Key).Select(GetChunk);
        }

        private IEnumerable<Chunk> GetReadyChunks()
        {
            foreach(var chunk in GetWatchedChunks())
            {
                // Only interested in pending chunks (which we can generate)
                // and memoized chunks (which we can re-instantiate)
                if (chunk.status != ChunkStatus.Pending && chunk.status != ChunkStatus.Memoized)
                    continue;

                // Two adjacent chunks cannot be generating at the same time.
                if (parallelism > 1)
                {
                    if (GetNeighbours(chunk.chunkCell).Any(c => c.status == ChunkStatus.Generating))
                        continue;
                }
                yield return chunk;
            }
        }

        private class Chunk
        {
            public ChunkStatus status;
            public Vector3Int chunkCell;
            public TesseraCompletion completion;
            public GameObject gameObject;
            internal float lastWatch;
        }

        private enum ChunkStatus
        {
            // Never generate this chunk
            OutOfBounds,
            // Chunk is ready for generating
            Pending,
            // Chunk is running the generator in another thread
            Generating,
            // Chunk generation has finished, but some tiles still need instantiating
            Instantiating,
            // Generation and instantiating finished successfully
            Generated,
            // Generation didn't finish successfully
            Broken,
            // Generation has finished, no tiles currently instantiated
            Memoized,
        }
    }
}