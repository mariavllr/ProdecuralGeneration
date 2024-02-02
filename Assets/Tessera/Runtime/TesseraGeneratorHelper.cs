using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Topo;
using DeBroglie.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Tessera
{
    /**
     * Holds the actual implementation of the generation
     * In a format that is appropriate to run in threads.
     * I.e. all relevant GameObject properties have been copied into POCOs
     */
    internal class TesseraGeneratorHelper
    {

        // Configuration.
        // This is all loaded from TesseraGenerator
        private TesseraGeneratorHelperOptions options;
        TesseraPalette palette;
        TileModel model;
        TileModelInfo tileModelInfo;
        IGrid grid;
        Action<string, float> progress;
        Action<ITopoArray<ISet<ModelTile>>> progressTiles;
        XoRoRNG xororng;
        TesseraStats stats;

        // State. This is intialized in Setup()
        ITopology topology; //< Topology before masking by initial constraint. 1:1 with grid
        ITopology maskedTopology; //< Topology after masking by intiial constraint. 1:1 with propagator
        ITopology maskedTopologyWithPins; //< Used for filtering some constraints
        DeBroglie.Resolution lastStatus;
        TilePropagator propagator;
        private (long state0, long state1) lastRngState;

        private bool isInitialized;
        private ChangeTracker changeTracker = null;


        public TesseraGeneratorHelper(
            TesseraGeneratorHelperOptions options)
        {
            this.options = options;
            this.palette = options.palette;
            this.tileModelInfo = options.tileModelInfo;
            this.grid = options.grid;
            this.progress = options.progress;
            this.progressTiles = options.progressTiles;
            this.xororng = options.xororng;
            this.stats = options.stats;
        }

        public TilePropagator Propagator => propagator;

        public IGrid Grid => grid;

        public TesseraStats Stats => stats;

        public void FullRun(bool postprocess)
        {
            Init();
            CreatePropagator();
            Setup();
            Run();
            if (postprocess)
                PostProcess();
        }

        internal void Init()
        {
            progress?.Invoke("Initializing", 0.0f);
            if (!isInitialized)
            {
                isInitialized = true;
                var t1 = DateTime.Now;
                model = DeBroglieUtils.GetTileModel(grid, options.modelType, tileModelInfo, options.samples, options.overlapSize, palette);
                stats.initializeTime += (DateTime.Now - t1).TotalSeconds;
            }
        }

        internal void CreatePropagator()
        {
            progress?.Invoke("Creating propagator", 0.0f);
            var t1 = DateTime.Now;
            topology = DeBroglieUtils.GetTopology(grid);

            maskedTopology = GetMaskedTopology(false);
            maskedTopologyWithPins = GetMaskedTopology(true);

            var propagatorOptions = new TilePropagatorOptions
            {
                BacktrackType = options.backtrack ? BacktrackType.Backtrack : BacktrackType.None,
                RandomDouble = xororng.NextDouble,
                Constraints = options.constraints.ToArray(),
                ModelConstraintAlgorithm = (DeBroglie.Wfc.ModelConstraintAlgorithm)options.algorithm,
            };
            lastRngState = (xororng.State0, xororng.State1);
            propagator = new TilePropagator(model, maskedTopology, propagatorOptions);

            lastStatus = DeBroglie.Resolution.Undecided;

            CheckStatus("Failed to initialize propagator");
            stats.createPropagatorTime += (DateTime.Now - t1).TotalSeconds;
        }


        // Construct the propagator
        internal void Setup()
        {
            progress?.Invoke("Setting up", 0.0f);

            ApplyInitialConstraintsAndSkybox();
            BanBigTiles();
        }

        ITopology GetMaskedTopology(bool includePins)
        {
            var mask = TesseraInitialConstraintHelper.GetMask(grid, topology, options.initialConstraints, includePins);
            return topology.WithMask(mask);
        }


        private void Run()
        {
            CheckStatus("Propagator is not ready to run");

            var t1 = DateTime.Now;
            var lastProgress = DateTime.Now;
            var progressResolution = TimeSpan.FromSeconds(0.1);
            var stepCount = 0;

            // Track changes if we need to step back to last good
            // This is like a poor mans backtracking
            changeTracker = null;
            if (options.failureMode == FailureMode.LastGood || options.failureMode == FailureMode.Minimal)
            {
                changeTracker = propagator.CreateChangeTracker();
            }

            while (propagator.Status == DeBroglie.Resolution.Undecided)
            {
                // Break if cancellation requested
                options.cancellationToken.ThrowIfCancellationRequested();

                // Reset the changeTracker to empty
                if (changeTracker != null)
                {
                    foreach (var _ in changeTracker.GetChangedIndices()) { }
                }

                // Report progress
                if (lastProgress + progressResolution < DateTime.Now)
                {
                    lastProgress = DateTime.Now;
                    progress?.Invoke("Generating", (float)propagator.GetProgress());
                    progressTiles?.Invoke(propagator.ToValueSets<ModelTile>());
                }
                // Actually step
                propagator.Step();

                // Have we gone over the step limit?
                stepCount += 1;
                stats.stepCount += 1;
                if (options.stepLimit > 0 && stepCount >= options.stepLimit)
                {
                    propagator.SetContradiction();
                    CheckStatus("Propagator reached step limit");
                }
            }
            stats.runTime += (DateTime.Now - t1).TotalSeconds;
        }

        public void PostProcess()
        {
            // Attempt to make minimal test case
            if (propagator.Status == DeBroglie.Resolution.Contradiction && changeTracker != null)
            {
                var t1 = DateTime.Now;
                progress?.Invoke("Backtracking to last good state", 1.0f);

                var uncertainModelTile = new ModelTile { };
                var contradictionModelTile = new ModelTile { Offset = new Vector3Int(-1, -1, -1) };
                var recentlyChangedIndices = new HashSet<int>(changeTracker.GetChangedIndices());
                var badModelTiles = propagator.ToValueArray<ModelTile>(uncertainModelTile, contradictionModelTile);
                var inidices = propagator.Topology.GetIndices().Where(i => badModelTiles.Get(i).Tile != null).ToList();

                // The assumption is that if an index was changed at all since the last step, it wasn't known good before that step.
                // Not sure this assmption always holds up? There are other ways of stepping backwards, e.g. using backtrading.
                var goodIndicies = inidices.Where(x => !recentlyChangedIndices.Contains(x)).ToList();

                var badIndicies = inidices.Where(x => recentlyChangedIndices.Contains(x)).ToList();

                void GotoState(List<int> indices)
                {
                    (xororng.State0, xororng.State1) = lastRngState;
                    propagator.Clear();

                    Setup();
                    foreach (var j in indices)
                    {
                        propagator.Topology.GetCoord(j, out var x, out var y, out var z);
                        var status = propagator.Select(x, y, z, new Tile(badModelTiles.Get(j)));
                        if (status == DeBroglie.Resolution.Contradiction)
                        {
                            return;
                        }

                        propagator.StepConstraints();
                        if (propagator.Status == DeBroglie.Resolution.Contradiction)
                        {
                            return;
                        }
                    }
                }

                if (options.failureMode == FailureMode.LastGood)
                {
                    GotoState(goodIndicies);
                }
                else if (options.failureMode == FailureMode.Minimal)
                {
                    progress?.Invoke("Creating minimal failure demonstration", 1.0f);

                    // We sort tiles nearer the contraction first as this can give slightly better results
                    var contradictionLocation = propagator.Topology.GetIndices().Cast<int?>().FirstOrDefault(i => badModelTiles.Get(i.Value).Equals(contradictionModelTile));
                    if (contradictionLocation is int cl)
                    {
                        var contradictionPoint = grid.GetCellCenter(grid.GetCell(cl));
                        goodIndicies = goodIndicies.OrderBy(i => (grid.GetCellCenter(grid.GetCell(i)) - contradictionPoint).magnitude).ToList();
                        badIndicies = badIndicies.OrderBy(i => (grid.GetCellCenter(grid.GetCell(i)) - contradictionPoint).magnitude).ToList();
                    }

                    List<bool> GetContradictionPattern()
                    {
                        return badIndicies.Select(i => propagator.GetPossibleTiles(i).Count == 0).ToList();
                    }

                    var contradictionPattern = GetContradictionPattern();

                    // The order of good before bad is important for reconstructing the original failure accurately
                    var currentIndices = goodIndicies.Concat(badIndicies).ToList();

                    // Remove items from currentIdicies if they don't contribute to the contradiction
                    while (true)
                    {
                        var found = false;
                        var toSearch = currentIndices.ToList();
                        toSearch.Reverse();
                        foreach (var i in toSearch)
                        {
                            // Try removing i
                            var nextIndicies = currentIndices.Where(x => x != i).ToList();
                            GotoState(nextIndicies);

                            // If there's no contradiction, then i is critical and must be kept
                            if (propagator.Status != DeBroglie.Resolution.Contradiction)
                                continue;

                            // Check that it's the *same* contradiction we saw before.
                            // This is important for user consistency, and also because 
                            // minimal test case can otherwise chop thigns down so much they are non-sensical
                            var nextContradictionPattern = GetContradictionPattern();
                            if (!contradictionPattern.SequenceEqual(nextContradictionPattern))
                                continue;

                            // We've got the same result before and after removing i, 
                            // so permanently remove it and move on.
                            currentIndices = nextIndicies;
                            found = true;
                        }
                        // No more indices can be removed from currentIndices
                        if (!found)
                            break;
                    }

                    // Finally, setup propagator into the minimal test case
                    GotoState(currentIndices);

                }
                else
                {
                    throw new Exception();
                }
                stats.postProcessTime += (DateTime.Now - t1).TotalSeconds;
            }
        }


        private void ApplyInitialConstraintsAndSkybox()
        {
            var t1 = DateTime.Now;
            var mask = maskedTopology.Mask;

            var initialConstraintHelper = new TesseraInitialConstraintHelper(propagator, grid, tileModelInfo, palette);

            foreach (var ic in options.initialConstraints)
            {
                initialConstraintHelper.Apply(ic);
                CheckStatus($"Contradiction after setting initial constraint {ic.Name}.");
            }
            CheckStatus("Contradiction after setting initial constraints.");
            stats.initialConstraintsTime += (DateTime.Now - t1).TotalSeconds;


            // Apply skybox (if any)
            if (options.skyBox != null)
            {
                t1 = DateTime.Now;
                var skyBoxFaceDetailsByFaceDir = grid.CellType.GetFaceDirs().ToDictionary(x => x, x => {
                    var ix = grid.CellType.Invert(x);
                    return options.skyBox.faceDetails.FirstOrDefault(f => f.faceDir == x).faceDetails ??
                        // For triangles, try the inverse direction
                        options.skyBox.faceDetails.FirstOrDefault(f => f.faceDir == ix).faceDetails ??
                        throw new Exception($"Couldn't find skybox for direction {x}");
                });
                foreach (var cell in grid.GetCells())
                {
                    foreach (var faceDir in grid.GetValidFaceDirs(cell))
                    {
                        if (!grid.TryMove(cell, faceDir, out var destCell, out var _, out var _) || !grid.InBounds(destCell))
                        {
                            // Edge of grid (unmasked)
                            initialConstraintHelper.FaceConstrain2(cell, faceDir, skyBoxFaceDetailsByFaceDir[faceDir]);
                        }
                    }
                }
                stats.skyboxTime += (DateTime.Now - t1).TotalSeconds;
            }
            CheckStatus("Contradiction after setting initial constraints and skybox.");
        }

        private IEnumerable<(Vector3Int, CellFaceDir)> GetBorders()
        {
            var mask = maskedTopology.Mask;

            // TODO: Could special case for regular grids
            // NB: Grid is pre-masking, so cells next to a mask hole are not on the border
            foreach (var cell in Grid.GetCells())
            {
                if (mask != null && !maskedTopology.ContainsIndex(Grid.GetIndex(cell)))
                    continue;

                foreach (var d in Grid.GetValidFaceDirs(cell))
                {

                    if (!grid.TryMove(cell, d, out var cell2, out var _, out var _) || !grid.InBounds(cell2))
                    {
                        yield return (cell, d);
                    }
                }
            }
        }

        // As big tiles are split into smaller tiles before sending to DeBroglie, 
        // the generation can make big tiles that overhang the boundary of the generation area.
        // This bans such setups.
        private void BanBigTiles()
        {
            var t1 = DateTime.Now;

            var internalAdjacenciesByFaceDir = tileModelInfo.InternalAdjacencies
                .ToLookup(t => t.GridDir, t => t.Src);
            foreach (var (p, faceDir) in GetBorders())
            {
                foreach (var t in internalAdjacenciesByFaceDir[faceDir])
                {
                    if (maskedTopologyWithPins.Mask == null || maskedTopologyWithPins.ContainsIndex(maskedTopologyWithPins.GetIndex(p.x, p.y, p.z)))
                    {
                        propagator.Ban(p.x, p.y, p.z, t);
                    }
                }
            }
            CheckStatus("Contradiction after removing big tiles overlapping edges.");
            stats.banBigTilesTime += (DateTime.Now - t1).TotalSeconds;
        }

        // TODO: This should return via TesseraCompletion rather than logging
        private void CheckStatus(string s)
        {
            if (lastStatus != DeBroglie.Resolution.Contradiction && propagator.Status == DeBroglie.Resolution.Contradiction)
            {
                lastStatus = propagator.Status;
                Debug.LogWarning(s);
            }
        }
    }
}
