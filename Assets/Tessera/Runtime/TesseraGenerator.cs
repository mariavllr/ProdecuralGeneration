using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Topo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Tilemaps;

namespace Tessera
{
    public enum FailureMode
    {
        /// <summary>
        /// If a failure occurs, don't output anything
        /// </summary>
        Cancel,
        /// <summary>
        /// If a failure occurs, output the progress so far
        /// </summary>
        Last,
        /// <summary>
        /// If a failure occurs, backtrack to the last safe point.
        /// </summary>
        LastGood,
        /// <summary>
        /// Examines the progress so far for the minimal set of tiles that cause an issue
        /// </summary>
        Minimal,
    }

    /// <summary>
    /// Different models Tessera supports.
    /// The model dictates how nearby tiles relate to each other.
    /// </summary>
    public enum ModelType
    {
        /// <summary>
        /// The default model using the painting system to determine tile adjacencies.
        /// </summary>
        AdjacentPaint,
        /// <summary>
        /// See [overlapping](../articles/overlapping.md).
        /// </summary>
        Overlapping,
        /// <summary>
        /// See [overlapping](../articles/overlapping.md).
        /// </summary>
        Adjacent,
    }

    [Serializable]
    public class TileList
    {
        public List<TesseraTileBase> tiles;
    }


    /// <summary>
    /// GameObjects with this behaviour contain utilities to generate tile based levels using Wave Function Collapse (WFC).
    /// Call <see cref="Generate"/> or <see cref="StartGenerate"/> to run.
    /// The generation takes the following steps:
    /// * Inspect the tiles in <see cref="tiles"/> and work out how they rotate and connect to each other.
    /// * Setup any initial constraints that fix parts of the generation (<see cref="TesseraGenerateOptions.initialConstraints"/>).
    /// * Fix the boundary of the generation if <see cref="skyBox"/> is set.
    /// * Generate a set of tile instances that fits the above tiles and constraints.
    /// * Optionally <see cref="retries"/> or <see cref="backtrack"/>.
    /// * Instantiates the tile instances.
    /// </summary>
    [AddComponentMenu("Tessera/Tessera Generator")]
    public class TesseraGenerator : MonoBehaviour
    {


        [SerializeField]
        [Tooltip("The size of the generator area, counting in cells.")]
        private Vector3Int m_size = new Vector3Int(10, 1, 10);


        /// <summary>
        /// The size of the generator area, counting in cells each of size <see cref="tileSize"/>.
        /// </summary>
        public Vector3Int size
        {
            get { return m_size; }
            set
            {
                m_size = value;
            }
        }

        [SerializeField]
        [Tooltip("The local position of the center of the area to generate.")]
        private Vector3 m_center = Vector3.zero;

        /// <summary>
        /// The local position of the center of the area to generate.
        /// </summary>
        public Vector3 center
        {
            get
            {
                return m_center;
            }
            set
            {
                m_center = value;
            }
        }

        public Vector3 origin
        {
            get
            {
                var cellType = CellType;
                if (cellType == HexPrismCellType.Instance)
                {
                    return center - HexGeometryUtils.GetCellCenter(size - Vector3Int.one, Vector3.zero, tileSize) / 2;
                }
                else if (cellType == TrianglePrismCellType.Instance)
                {
                    var o = center - TrianglePrismGeometryUtils.GetCellCenter(size - Vector3Int.one, Vector3.zero, tileSize) / 2;
                    if((size.x & 1) == 1)
                    {
                        o.z += -TrianglePrismGeometryUtils.GetCellCenter(new Vector3Int(1, 0, 0), Vector3.zero, tileSize).z / 2;
                    }
                    return o;
                }
                else
                {
                    return center - CubeGeometryUtils.GetCellCenter(size - Vector3Int.one, Vector3.zero, tileSize) / 2;
                }
            }
            set
            {
                var cellType = CellType;
                if (cellType == HexPrismCellType.Instance)
                {
                    center = value + HexGeometryUtils.GetCellCenter(size - Vector3Int.one, Vector3.zero, tileSize) / 2;
                }
                else if (cellType == TrianglePrismCellType.Instance)
                {
                    center = value + TrianglePrismGeometryUtils.GetCellCenter(size - Vector3Int.one, Vector3.zero, tileSize) / 2;
                    if ((size.x & 1) == 1)
                    {
                        m_center.z += TrianglePrismGeometryUtils.GetCellCenter(new Vector3Int(1, 0, 0), Vector3.zero, tileSize).z / 2;
                    }
                }
                else
                {
                    center = value + CubeGeometryUtils.GetCellCenter(size - Vector3Int.one, Vector3.zero, tileSize) / 2;
                }
            }
        }

        /// <summary>
        /// The area of generation.
        /// Setting this will cause the size to be rounded to a multiple of <see cref="tileSize"/>
        /// </summary>
        public Bounds bounds
        {
            get
            {
                return new Bounds(m_center, Vector3.Scale(tileSize, m_size));
            }
            set
            {
                m_center = value.center;
                m_size = new Vector3Int(
                    Math.Max(1, (int)Math.Round(value.size.x / tileSize.x)),
                    Math.Max(1, (int)Math.Round(value.size.y / tileSize.y)),
                    Math.Max(1, (int)Math.Round(value.size.z / tileSize.z))
                    );
            }
        }


        /// <summary>
        /// Sets which sort of model the generator uses.
        /// The model dictates how nearby tiles relate to each other.
        /// </summary>
        public ModelType modelType;

        /// <summary>
        /// The list of tiles eligible for generation.
        /// </summary>
        [Tooltip("The list of tiles eligible for generation.")]
        public List<TileEntry> tiles = new List<TileEntry>();

        /// <summary>
        /// For overlapping models, a list of objects to use as input samples.
        /// Each one will have its children inspected and read out.
        /// <see cref="ModelType.Overlapping"/>
        /// </summary>
        public List<GameObject> samples = new List<GameObject>();

        /// <summary>
        /// The size of the overlap parameter for the overlapping model.
        /// <see cref="ModelType.Overlapping"/>
        /// </summary>
        public Vector3Int overlapSize = new Vector3Int(3, 3, 3);

        /// <summary>
        /// The stride between each cell in the generation.
        /// "big" tiles may occupy a multiple of this tile size.
        /// </summary>
        [Tooltip("The stride between each cell in the generation.")]
        public Vector3 tileSize = Vector3.one;

        /// <summary>
        /// If set, backtracking will be used during generation.
        /// Backtracking can find solutions that would otherwise be failures,
        /// but can take a long time.
        /// </summary>
        [Tooltip("If set, backtracking will be used during generation.\nBacktracking can find solutions that would otherwise be failures, but can take a long time.")]
        public bool backtrack = false;

        /// <summary>
        /// Fixes the seed for random number generator.
        /// If the value is zero, the seed is taken from Unity.Random 
        /// </summary>
        [Tooltip("Fixes the seed for random number generator.\n If the value is zero, the seed is taken from Unity.Random.")]
        public int seed = 0;

        /// <summary>
        /// If backtracking is off, how many times to retry generation if a solution
        /// cannot be found.
        /// </summary>
        [Tooltip("How many times to retry generation if a solution cannot be found.")]
        public int retries = 5;

        /// <summary>
        /// How many steps to take before retrying from the start.
        /// </summary>
        [Tooltip("How many steps to take before retrying from the start.")]
        public int stepLimit = 0;

        /// <summary>
        /// Controls the algorithm used internally for Wave Function Collapse.
        /// </summary>
        [Tooltip("Controls the algorithm used internally for Wave Function Collapse.")]
        public TesseraWfcAlgorithm algorithm;

        /// <summary>
        /// Records undo/redo when run by pressing the Generate button in the Inspector.
        /// </summary>
        [Tooltip("Records undo/redo when run in the editor.")]
        public bool recordUndo = true;

        /// <summary>
        /// Controls what is output when the generation fails.
        /// </summary>
        public FailureMode failureMode = FailureMode.Cancel;

        /// <summary>
        /// Game object to show in cells that have yet to be fully solved.
        /// </summary>
        public GameObject uncertaintyTile;

        /// <summary>
        /// Game object to show in cells that cannot be solved.
        /// </summary>
        public GameObject contradictionTile;

        /// <summary>
        /// If true, the uncertainty tiles shrink as the solver gets more certain.
        /// </summary>
        public bool scaleUncertainyTile = true;

        /// <summary>
        /// If set, this tile is used to define extra initial constraints for the boundary.
        /// </summary>
        [Tooltip("If set, this tile is used to define extra initial constraints for the boundary.")]
        public TesseraTileBase skyBox = null;

        /// <summary>
        /// If true, then active tiles in the scene will be taken as initial constraints.
        /// If false, then no initial constraints are used.
        /// Using <see cref="TesseraGenerateOptions.initialConstraints"/> overrides either outcome.
        /// </summary>
        public bool searchInitialConstraints = true;

        /// <summary>
        /// Inherited from the first tile in <see cref="tiles"/>.
        /// </summary>
        public TesseraPalette palette => tiles.Select(x => x.tile?.palette).FirstOrDefault() ?? TesseraPalette.defaultPalette;

        /// <summary>
        /// If set, then tiles are generated on the surface of this mesh instead of a regular grid.
        /// </summary>
        [Tooltip("If set, then tiles are generated on the surface of this mesh instead of a regular grid.")]
        public Mesh surfaceMesh;

        /// <summary>
        /// Height above the surface mesh that the bottom layer of tiles is generated at.
        /// </summary>
        [Tooltip("Height above the surface mesh that the bottom layer of tiles is generated at.")]
        public float surfaceOffset;

        /// <summary>
        /// Controls how normals are treated for meshes deformed to fit the surfaceMesh.
        /// </summary>
        [Tooltip("Controls how normals are treated for meshes deformed to fit the surfaceMesh.")]
        public bool surfaceSmoothNormals;

        /// <summary>
        /// If true, and a <see cref="surfaceMesh"/> is set with multiple submeshes (materials),
        /// then use <see cref="surfaceSubmeshTiles"/>.
        /// </summary>
        [Tooltip("If true, filters which tiles appear on each material (submesh) of the surface mesh.")]
        public bool filterSurfaceSubmeshTiles;

        /// <summary>
        /// A list of tiles to filter each submesh of <see cref="surfaceMesh"/> to.
        /// Ignored unless <see cref="filterSurfaceSubmeshTiles"/> is true.
        /// </summary>
        public List<TileList> surfaceSubmeshTiles = new List<TileList>();


        /// <summary>
        /// Clears previously generated content.
        /// </summary>
        public void Clear()
        {
            var output = GetComponent<ITesseraTileOutput>() ?? new InstantiateOutput(transform);
            output.ClearTiles(UnityEngineInterface.Instance);
        }

        /// <summary>
        /// Synchronously runs the generation process described in the class docs.
        /// </summary>
        /// <param name="onCreate">Called for each newly generated tile. By default, they are Instantiated in the scene.</param>
        public TesseraCompletion Generate(TesseraGenerateOptions options = null)
        {
            var e = StartGenerate(options);
            while (e.MoveNext()) { }
            return e.Result;
        }

        /// <summary>
        /// Runs Clear, then Generate
        /// </summary>
        public TesseraCompletion Regenerate(TesseraGenerateOptions options = null)
        {
            Clear();
            return Generate(options);
        }

        // Avoid using this, it is expensive. It's mostly here so that I can continue offering a compatible API.
        private IGrid GetGrid()
        {
            var cellType = CellType;
            if (surfaceMesh != null)
            {
                var surfaceMeshData = new MeshData(surfaceMesh);
                var moves = MeshGridBuilder.Build(surfaceMeshData, size.y);

                // Build the extended topology
                if (cellType is CubeCellType)
                {
                    return new QuadMeshGrid(cellType, surfaceMeshData, size.y, tileSize.y, surfaceOffset, surfaceSmoothNormals, moves);
                }
                else if (cellType is TrianglePrismCellType)
                {
                    return new TriangleMeshGrid(cellType, surfaceMeshData, size.y, tileSize.y, surfaceOffset, surfaceSmoothNormals, moves);
                }
                else
                {
                    throw new Exception();
                }
            }
            else if (cellType == HexPrismCellType.Instance)
            {
                return new HexPrismGrid(origin, size, tileSize);
            }
            else if (cellType == TrianglePrismCellType.Instance)
            {
                return new TrianglePrismGrid(origin, size, tileSize);
            }
            else
            if (cellType == SquareCellType.Instance)
            {
                return new SquareGrid(origin, new Vector2Int(size.x, size.y), new Vector2(tileSize.x, tileSize.y));
            }
            else if(cellType == CubeCellType.Instance)
            {
                return new CubeGrid(origin, size, tileSize);
            }
            else
            {
                throw new Exception($"Unknown cell type ${cellType}");
            }
        }

        public ITesseraTileOutput GetTileOutput(bool forceIncremental = false)
        {
            var component = GetComponent<ITesseraTileOutput>();
            if(component != null)
            {
                return component;
            }
            if(forceIncremental)
            {
                return new UpdatableInstantiateOutput(transform);
            }
            return new InstantiateOutput(transform);
        }

        internal TesseraGeneratorHelper CreateTesseraGeneratorHelper(TesseraGenerateOptions options = null)
        {
            var t1 = DateTime.Now;
            options = options ?? new TesseraGenerateOptions();
            var progress = options.progress;

            var seed = options.seed ?? this.seed;
            if (seed == 0) seed =  UnityEngine.Random.Range(int.MinValue, int.MaxValue);

            var xororng = new XoRoRNG(seed);

            Validate();

            var actualInitialConstraints = new List<ITesseraInitialConstraint>();

            var cellType = CellType;

            List<TesseraTilemap> samples = null;

            List<TileEntry> actualTiles;
            if (modelType == ModelType.Overlapping || modelType == ModelType.Adjacent)
            {
                samples = this.samples.Select(ToTesseraTilemap).Where(x => x != null).ToList();

                actualTiles = samples
                    .SelectMany(x => x.Data.Values)
                    .Select(x => x.Tile)
                    .Distinct()
                    // TODO: Does the weight actually matter?
                    .Select(x => new TileEntry { tile = x, weight = 1 })
                    .ToList();

                // TODO: To validation
                if(samples.Count == 0)
                {
                    throw new Exception($"No samples specified.");
                }
                if(actualTiles.Count == 0)
                {
                    throw new Exception($"No tiles found in the samples");
                }
            }
            else if(modelType == ModelType.AdjacentPaint)
            {
                actualTiles = tiles;
            }
            else
            {
                throw new Exception($"Unknown model type {modelType}");
            }

            var tileModelInfo = TileModelInfo.Create(actualTiles, cellType);

            var grid = GetGrid();

            // TODO: Move this somewhere more appropriate
            if (surfaceMesh != null && filterSurfaceSubmeshTiles)
            {
                foreach (var (subMesh, tileList) in surfaceSubmeshTiles.Select((x, i) => (i, x)))
                {
                    var cells = grid.GetCells().Where(x => x.z == subMesh).ToList();
                    var volumeFilter = new TesseraVolumeFilter
                    {
                        name = "Submesh " + subMesh.ToString(),
                        tiles = tileList.tiles,
                        cells = cells,
                    };

                    actualInitialConstraints?.Add(volumeFilter);
                }
            }


            var initialConstraintBuilder = new TesseraInitialConstraintBuilder(transform, grid);
            var initialConstraints = options.initialConstraints ??
                (searchInitialConstraints ? (IEnumerable<ITesseraInitialConstraint>)initialConstraintBuilder.SearchInitialConstraints() : null) ??
                new ITesseraInitialConstraint[0];

            actualInitialConstraints.AddRange(initialConstraints);

            var constraints = GetTileConstraints(tileModelInfo, grid);

            var actualSkyBox = skyBox == null ? null : new TesseraInitialConstraint
            {
                faceDetails = skyBox.faceDetails,
                offsets = skyBox.offsets,
            };

            var stats = new TesseraStats { createHelperTime = (DateTime.Now - t1).TotalSeconds };

            var helperOptions = new TesseraGeneratorHelperOptions
            {
                grid = grid,
                palette = palette,
                modelType = modelType,
                tileModelInfo = tileModelInfo,
                samples = samples,
                overlapSize = overlapSize,
                initialConstraints = actualInitialConstraints,
                constraints = constraints,
                skyBox = actualSkyBox,
                backtrack = backtrack,
                stepLimit = stepLimit,
                algorithm = algorithm,
                progress = progress,
                progressTiles = null,
                xororng = xororng,
                cancellationToken = options.cancellationToken,
                failureMode = failureMode,
                stats = stats,
            };

            return new TesseraGeneratorHelper(helperOptions);
        }

        /// <summary>
        /// Asynchronously runs the generation process described in the class docs, for use with StartCoroutine.
        /// </summary>
        /// <remarks>The default instantiation is still synchronous, so this can still cause frame glitches unless you override onCreate.</remarks>
        public EnumeratorWithResult<TesseraCompletion> StartGenerate(TesseraGenerateOptions options = null)
        {
            return new EnumeratorWithResult<TesseraCompletion>(StartGenerateInner(options));
        }
        
        private IEnumerator StartGenerateInner(TesseraGenerateOptions options = null)
        {
            options = options ?? new TesseraGenerateOptions();

            var generatorHelper = CreateTesseraGeneratorHelper(options);


            for (var r = 0; r < retries; r++)
            {
                var name = gameObject.name;
                TilePropagator propagator;
                TilePropagator Run()
                {
                    try
                    {
                        Profiler.BeginThreadProfiling("Tessera Generation", name);
                        generatorHelper.FullRun(r >= retries - 1);
                        return generatorHelper.Propagator;
                    }
                    finally
                    {
                        Profiler.EndThreadProfiling();
                    }
                }

                if (options.multithreaded && Application.platform != RuntimePlatform.WebGLPlayer)
                {
                    var runTask = Task.Run(Run, options.cancellationToken);

                    while (!runTask.IsCompleted)
                        yield return null;

                    options.cancellationToken.ThrowIfCancellationRequested();

                    propagator = runTask.Result;
                }
                else
                {
                    propagator = Run();
                }

                var status = propagator.Status;

                var contradictionTile = new ModelTile {};

                var result = propagator.ToValueArray<ModelTile?>(contradiction: contradictionTile);


                if (status == DeBroglie.Resolution.Contradiction)
                {
                    if (r < retries - 1)
                    {
                        continue;
                    }
                }


                // Log out stats about the run
                /*
                var stats = generatorHelper.Stats;
                Debug.Log($@"Tessera timings (ms):
    Total Time = {stats.totalTile * 1000:n1}
    Create Helper Time = {stats.createHelperTime * 1000:n1}
    Initialize Time = {stats.initializeTime * 1000:n1}
    Create Propagator Time = {stats.createPropagatorTime * 1000:n1}
    Initial Constraints Time = {stats.initialConstraintsTime * 1000:n1}
    Skybox Time = {stats.skyboxTime * 1000:n1}
    Ban Big Tiles Time = {stats.banBigTilesTime * 1000:n1}
    Run Time = {stats.runTime * 1000:n1}
    Post-Process Time = {stats.postProcessTime * 1000:n1}
");
                */

                var completion = new TesseraCompletion();
                completion.retries = r;
                completion.backtrackCount = propagator.BacktrackCount;
                completion.success = status == DeBroglie.Resolution.Decided;
                completion.tileData = DeBroglieUtils.ToTileDictionary(result, generatorHelper.Grid);
                completion.contradictionLocation = completion.success ? null : DeBroglieUtils.GetContradictionLocation(result, generatorHelper.Grid);
                completion.isIncremental = false;
                completion.grid = generatorHelper.Grid;
                completion.gridTransform = TRS.World(transform);


                if (options.onComplete != null)
                {
                    options.onComplete(completion);
                }
                else
                {
                    HandleComplete(options, completion);
                }

                if(completion.success == false && failureMode != FailureMode.Cancel && (uncertaintyTile != null || this.contradictionTile != null))
                {
                    InstantiateUncertaintyObjects(generatorHelper.Grid, propagator);
                }

                yield return completion;

                // Exit retries
                break;
            }
        }

        /// <summary>
        /// For validation purposes
        /// </summary>
        public IEnumerable<TesseraTileBase> GetMissizedTiles()
        {
            bool IsMissized(TesseraTileBase tile)
            {
                if (surfaceMesh != null) return false;
                var ignoreZ = tile.CellType is HexPrismCellType || tile.CellType is SquareCellType;
                if (size.x != 1 && tile.tileSize.x != tileSize.x) return true;
                if (size.y != 1 && tile.tileSize.y != tileSize.y) return true;
                if (size.z != 1 && tile.tileSize.z != tileSize.z && !ignoreZ) return true;

                return false;
            }

            return tiles.Select(x => x.tile)
                .Where(x => x != null)
                .Where(x => IsMissized(x));
        }


        /// <summary>
        /// For validation purposes
        /// </summary>
        public IList<ICellType> GetCellTypes()
        {
            return tiles.Select(x => x.tile)
                .Where(x => x != null)
                .Select(x => x.CellType)
                .Distinct()
                .ToList();
        }


        /// <summary>
        /// Checks tiles are consistently setup
        /// </summary>
        internal void Validate()
        {
            var allTiles = tiles.Select(x => x.tile).Where(x => x != null);
            if (surfaceMesh != null)
            {
                if (surfaceMesh.GetTopology(0) != MeshTopology.Quads && CellType is CubeCellType)
                {
                    Debug.LogWarning($"Mesh topology {surfaceMesh.GetTopology(0)} not supported with cubes. You need to select \"Keep Quads\" in the import options.");
                }
                if (surfaceMesh.GetTopology(0) != MeshTopology.Triangles && CellType is TrianglePrismCellType)
                {
                    Debug.LogWarning($"Mesh topology {surfaceMesh.GetTopology(0)} not supported with triangles. You need to deselect \"Keep Quads\" in the import options.");
                }
                if (!surfaceMesh.isReadable)
                {
                    Debug.LogWarning($"Surface mesh needs to be readable.");
                }
                //if (surfaceSmoothNormals && !surfaceMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent))
                if (surfaceSmoothNormals && surfaceMesh.tangents.Length == 0)
                {
                    Debug.LogWarning($"Surface mesh needs tangents to calculate smoothed normals. You need to select \"Calculate\" the tangent field of the import options.");
                }
                var unreadable = allTiles.Where(tile => tile.GetComponentsInChildren<MeshFilter>().Any(mf => !mf.sharedMesh.isReadable)).ToList();
                if (unreadable.Count > 0)
                {
                    Debug.LogWarning($"Some tiles have meshes that are not readable. They will not be transformed to fit the mesh. E.g {unreadable.First().name}");
                }
                if(filterSurfaceSubmeshTiles)
                {
                    for(var i=0;i< surfaceSubmeshTiles.Count;i++)
                    {
                        if(surfaceSubmeshTiles[i].tiles.Count == 0)
                        {
                            Debug.LogWarning($"Submesh {i} is filtered to zero tiles. Generation is impossible");
                        }
                    }
                }

                return;
            }

            if (Application.isPlaying && GetComponent<ITesseraTileOutput>() is TesseraMeshOutput)
            {
                var unreadable = allTiles.Where(tile => tile.GetComponentsInChildren<MeshFilter>().Any(mf => !mf.sharedMesh.isReadable)).ToList();
                if (unreadable.Count > 0)
                {
                    Debug.LogWarning($"Some tiles have meshes that are not readable. They will not be added to the mesh output. E.g {unreadable.First().name}");
                }
            }
            var cellTypes = GetCellTypes();
            if (cellTypes.Count > 1)
            {
                Debug.LogWarning($"You cannot mix tiles of multiple cell types, such as {string.Join(", ", cellTypes.Select(x => x.GetType().Name))} .\n");
            }

            var missizedTiles = GetMissizedTiles().ToList();
            if (missizedTiles.Count > 0)
            {
                Debug.LogWarning($"Some tiles do not have the same tileSize as the generator, {tileSize}, this can cause unexpected behaviour.\n" +
                    "NB: Big tiles should still share the same value of tileSize\n" +
                    "Affected tiles:\n" +
                    string.Join("\n", missizedTiles)
                    );
            }
            var palette = tiles.Select(x => x.tile?.palette).FirstOrDefault();
            var wrongPaletteTiles = allTiles.Where(x => x.palette != palette).ToList();
            if(wrongPaletteTiles.Count > 0)
            {
                Debug.LogWarning($"Some tiles do not all have the same palette.\b" +
                    "Affected tiles:\n" +
                    string.Join("\n", wrongPaletteTiles)
                    );
            }
        }

        /// <summary>
        /// Converts generator constraints into a format suitable for DeBroglie.
        /// </summary>
        private List<ITileConstraint> GetTileConstraints(TileModelInfo tileModelInfo, IGrid grid)
        {
            var l = new List<ITileConstraint>();
            foreach (var constraintComponent in GetComponents<TesseraConstraint>())
            {
                if (constraintComponent.enabled)
                {
                    var constraints = constraintComponent.GetTileConstraint(tileModelInfo, grid);
                    l.AddRange(constraints);
                }
            }
            return l;
        }

        // Default behaviour when generation is compete, if not overriden by TesseraGenerateOptions.onComplete
        private void HandleComplete(TesseraGenerateOptions options, TesseraCompletion completion)
        {
            completion.LogErrror();

            if (!completion.success && failureMode == FailureMode.Cancel)
            {
                return;
            }

            ITesseraTileOutput to = null;
            if(options.onCreate != null)
            {
                to = new ForEachOutput(options.onCreate);
            }
            else
            {
                to = GetTileOutput();
            }

            to.UpdateTiles(completion, UnityEngineInterface.Instance);
        }

        // See showUncertainty
        private void InstantiateUncertaintyObjects(IGrid grid, TilePropagator propagator)
        {
            var tileCount = propagator.TileModel.Tiles.Count();
            const float MinScale = 0.0f;
            const float MaxScale = 1.0f;

            var modelTileSets = propagator.ToValueSets<ModelTile>();

            foreach (var cell in grid.GetCells())
            {
                var i = grid.GetIndex(cell);
                var modelTiles = modelTileSets.Get(i);
                if (modelTiles == null || modelTiles.Count == 1)
                {
                    continue;
                }
                var isContradiction = modelTiles.Count == 0;
                // TODO: A lot of this seems shared with GetTesseraTileInstance. Refactor?
                var tiles = modelTiles.Select(x => x.Tile).Distinct().ToList();
                var name = (isContradiction ? "Contradiction" : "Uncertain") + $" ({ cell.x }, { cell.y}, { cell.z})";
                //var go = new GameObject(name);
                var go = Instantiate(isContradiction && contradictionTile != null ? contradictionTile : uncertaintyTile);
                go.name = name;
                go.transform.parent = transform;
                go.transform.localPosition = grid.GetCellCenter(cell);
                if(scaleUncertainyTile && !isContradiction)
                {
                    var scale = (MaxScale - MinScale) * modelTiles.Count / tileCount + MinScale;
                    go.transform.localScale = go.transform.localScale * scale;
                }
                if (!isContradiction)
                {
                    var volume = go.AddComponent<TesseraVolume>();
                    volume.generator = this;
                    volume.tiles = tiles;
                }
            }
        }

        /// <summary>
        /// Indicates the cell type of the tiles set up.
        /// </summary>
        public ICellType CellType => tiles.Select(x => x.tile).Where(x => x != null).FirstOrDefault()?.CellType ?? CubeCellType.Instance;

        public TesseraInitialConstraintBuilder GetInitialConstraintBuilder()
        {
            return new TesseraInitialConstraintBuilder(transform, GetGrid());
        }

        /// <summary>
        /// Utility function that instantiates a tile instance in the scene.
        /// This is the default function used when you do not pass <c>onCreate</c> to the Generate method.
        /// It is essentially the same as Unity's normal Instantiate method with extra features:
        /// * respects <see cref="TesseraTileBase.instantiateChildrenOnly"/>
        /// * applies mesh transformations (Pro only)
        /// </summary>
        /// <param name="instance">The instance being created.</param>
        /// <param name="parent">The game object to parent the new game object to. This does not affect the world position of the instance</param>
        /// <returns>The game objects created.</returns>
        public static GameObject[] Instantiate(TesseraTileInstance instance, Transform parent)
        {
            return Instantiate(instance, parent, instance.Tile.gameObject, instance.Tile.instantiateChildrenOnly);
        }

        /// <summary>
        /// Utility function that instantiates a tile instance in the scene.
        /// This is the default function used when you do not pass <c>onCreate</c> to the Generate method.
        /// It is essentially the same as Unity's normal Instantiate method with extra features:
        /// * respects <see cref="TesseraTileBase.instantiateChildrenOnly"/>
        /// * applies mesh transformations (Pro only)
        /// </summary>
        /// <param name="instance">The instance being created.</param>
        /// <param name="parent">The game object to parent the new game object to. This does not affect the world position of the instance</param>
        /// <param name="gameObject">The game object to actually instantiate</param>
        /// <param name="instantiateChildrenOnly">Should gameObject be created, or just it's children.</param>
        /// <returns>The game objects created.</returns>
        public static GameObject[] Instantiate(TesseraTileInstance instance, Transform parent, GameObject gameObject, bool instantiateChildrenOnly)
        {
            var transformsAndGameObjects = InstantiateUntransformed(instance, parent, gameObject, instantiateChildrenOnly);
            var gameObjects = transformsAndGameObjects.Select(x => x.Item2).ToArray();
            if (instance.MeshDeformation != null)
            {
                var cell = instance.Cells.First();
                foreach (var (localTransform, go) in transformsAndGameObjects)
                {
                    // MeshDeformation maps vertices from tile space to generator space.
                    // We include some matrices so we can go from child space -> tile space -[deform]-> generator space -> go space
                    var meshDeformation = TRS.Local(go.transform).ToMatrix().inverse * instance.MeshDeformation * localTransform;
                    MeshUtils.TransformRecursively(go, meshDeformation);
                }
            }
            else
            {
                // Flip box transformations to stop Unity whining: "BoxColliders does not support negative scale or size."
                foreach (var go in gameObjects)
                {
                    var localScale = go.transform.localScale;
                    var flip = new Vector3(Math.Sign(localScale.x), Math.Sign(localScale.y), Math.Sign(localScale.z));
                    if (flip == Vector3.one)
                        continue;
                    foreach(var bc in go.GetComponentsInChildren<BoxCollider>())
                    {
                        bc.size = Vector3.Scale(flip, bc.size);
                    }
                }
            }
            return gameObjects;
        }


        private static (Matrix4x4, GameObject)[] InstantiateUntransformed(TesseraTileInstance instance, Transform parent, GameObject gameObject, bool instantiateChildrenOnly)
        {
            var cell = instance.Cell;
            if (instantiateChildrenOnly)
            {
                // These two methods are mostly equivalent, but we need to investigate which is actually faster in Unity
                if (true)
                {
                    var worldTransform = Matrix4x4.TRS(instance.Position, instance.Rotation, instance.LossyScale);
                    var localTransform = Matrix4x4.TRS(instance.LocalPosition, instance.LocalRotation, instance.LossyScale);
                    return gameObject.transform.Cast<Transform>().Select(child =>
                    {
                        var childToInstance = gameObject.transform.worldToLocalMatrix * child.transform.localToWorldMatrix;
                        var local = new TRS(localTransform * childToInstance);
                        var world = new TRS(worldTransform * childToInstance);
                        var go = GameObject.Instantiate(child.gameObject, world.Position, world.Rotation, parent);
                        go.transform.localScale = local.Scale;
                        go.name = child.gameObject.name + $" ({cell.x}, {cell.y}, {cell.z})";
                        return (childToInstance, go);
                    }).ToArray();
                }
                /*
                else
                {
                    var go = GameObject.Instantiate(instance.Tile.gameObject, instance.Position, instance.Rotation, parent);
                    go.transform.localScale = instance.LocalScale;
                    var children = new List<GameObject>();
                    foreach (Transform child in go.transform)
                    {
                        children.Add(child.gameObject);
                        child.SetParent(parent);
                        child.name = child.name + $" ({cell.x}, {cell.y}, {cell.z})";
                    }
                    Destroy(go);
                    return children.Select(x => (Matrix4x4.identity, x)).ToArray();
                }
                */
            }
            else
            {
                var go = GameObject.Instantiate(gameObject, instance.Position, instance.Rotation, parent);
                go.transform.localScale = instance.LocalScale;
                go.name = gameObject.name + $" ({cell.x}, {cell.y}, {cell.z})";
                return new[] { (Matrix4x4.identity, go) };
            }
        }

        /// <summary>
        /// Reads the tiles in a given game object into a list of TesseraTileInstance
        /// </summary>
        private TesseraTilemap ToTesseraTilemap(GameObject parent)
        {
            if (parent == null)
                return null;

            var transform = parent.transform;
            var tileSize = Vector3.zero;
            IGrid tempGrid = null;

            var data = new Dictionary<Vector3Int, ModelTile>();

            var tilemap = transform.GetComponent<Tilemap>();

            var tilesByPrefix = new PrefixLookup<TesseraTileBase>();
            foreach(var tile in tiles)
            {
                tilesByPrefix.Add(tile.tile.name, tile.tile);
            }
            TesseraTileBase FindTile(GameObject go)
            {
                // Use pin if available
                var pinned = go.GetComponent<TesseraPinned>();
                if (pinned != null)
                {
                    return pinned.tile;
                }
                return FindTileByName(go.name);
            }


            TesseraTileBase FindTileByName(string name)
            {
                if (tilesByPrefix.TryFindLongestPrefix(name, out var tile))
                {
                    return tile;
                }
                throw new Exception($"Couldn't find tile for {name}");
            }




            if (tilemap != null)
            {
                // Create grid
                var layoutGrid = tilemap.layoutGrid;
                if(tilemap.cellLayout == GridLayout.CellLayout.Rectangle)
                {
                    tempGrid = new SquareGrid(Vector3.zero, Vector2Int.one, tilemap.cellSize);
                }
                else
                {
                    throw new Exception($"Unsupported tilemap cellLayout {tilemap.cellLayout}");
                }

                foreach(var cell in tilemap.cellBounds.allPositionsWithin)
                {
                    var unityTile = tilemap.GetTile(cell);
                    if (unityTile == null)
                        continue;
                    var tile = FindTileByName(unityTile.name);
                    // TODO: Rotation, big tiles
                    if (tile.offsets.Count > 1)
                        throw new Exception("Big tiles not supported when reading samples from tilemaps");
                    if(tilemap.GetTransformMatrix(cell).rotation != Quaternion.identity)
                        throw new NotImplementedException("Rotations not supported when reading samples from tilemaps");
                    var modelTile = new ModelTile { Tile = tile, Offset = tile.offsets[0], Rotation = tempGrid.CellType.GetIdentity() };
                    data[cell] = modelTile;
                }
            }
            else
            {
                bool foundFirst = false;
                foreach (Transform childTransform in transform)
                {

                    // Figure out the tile in question
                    var tile = FindTile(childTransform.gameObject);
                    if (tile == null)
                        continue;

                    if (tile is TesseraTile cubeTile)
                    {
                        // Infer the grid to use
                        if (!foundFirst)
                        {
                            foundFirst = true;
                            tileSize = cubeTile.tileSize;
                            // CubeGrid origin is the center of the origin cell. We offset to give more unity normal behaviour
                            // TODO: Could we infer if this is necessary?
                            tempGrid = new CubeGrid(tileSize / 2, Vector3Int.one, tileSize);
                        }

                        // Work out position of this cell in the temp grid
                        var baseOffset = tile.offsets.First();
                        var baseOffsetCenter = tile.CellType.GetCellCenter(baseOffset, tile.center, tileSize);
                        if (!tempGrid.FindCell(baseOffsetCenter, transform.worldToLocalMatrix * childTransform.transform.localToWorldMatrix, out var cell, out var cellRotation))
                        {
                            Debug.LogWarning($"Couldn't find cell position for cell {tile.name}");
                            continue;
                        }

                        var modelTile = new ModelTile { Tile = tile, Offset = baseOffset, Rotation = cellRotation };
                        foreach(var kv in TesseraTilemapConversions.GrowModelTile(cell, modelTile, tempGrid))
                        {
                            data[kv.Key] = kv.Value;
                        }
                    }
                    else
                    {
                        throw new NotImplementedException("Non-cube tiles not supported");
                    }
                }

                if (!foundFirst)
                {
                    return null;
                }
            }

            // Translate tempGrid to the actual grid.
            // This is necessary as grid bounds implicitly start from zero
            var minCell = data.Keys.Aggregate(Vector3Int.Min);
            var maxCell = data.Keys.Aggregate(Vector3Int.Max);
            var size = maxCell - minCell + Vector3Int.one;
            var grid = tempGrid is CubeGrid ? (IGrid)new CubeGrid(tempGrid.GetCellCenter(minCell), size, tileSize) :
                tempGrid is SquareGrid ? new SquareGrid(Vector3.zero, new Vector2Int(size.x, size.y), tileSize) :
                throw new Exception();

            data = data.ToDictionary(kv => kv.Key - minCell, kv => kv.Value);

            return new TesseraTilemap
            {
                Grid = grid,
                Data = data,
            };
        }

    }
}