﻿using DeBroglie.Topo;
using DeBroglie.Trackers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Attach this to a TesseraGenerator to run the generator stepwise over several updates,
    /// displaying the changes so far.
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    [RequireComponent(typeof(TesseraGenerator))]
    [AddComponentMenu("Tessera/Animated Generator", 41)]
    public class AnimatedGenerator : MonoBehaviour
    {
        private AnimatedGeneratorState state = AnimatedGeneratorState.Stopped;

        private TesseraGeneratorHelper helper;
        private int stepCount;

        private ISet<ModelTile>[] lastTiles;
        private ChangeTracker changeTracker;
        private float lastStepTime = 0.0f;
        private DateTime baseTime = DateTime.Now;

        private ITesseraTileOutput tileOutput;

        private bool supportsCubes;

        private bool[] hasObject;
        private GameObject[] cubesByIndex;
        private bool firstStep;

        public float secondsPerStep = .0f;

        public float progressPerStep = 1;

        private Task initTask;
        private CancellationTokenSource initCts;

        public enum AnimatedGeneratorState
        {
            Stopped,
            Initializing,
            Running,
            Paused,
        }

        public AnimatedGeneratorState State => state;

        public bool IsStarted => state != AnimatedGeneratorState.Stopped;

        /// <summary>
        /// If true, use threading to avoid stalling Unity.
        /// (ignored on WebGL builds)
        /// </summary>
        public bool multithread = true;

        /// <summary>
        /// Game object to show in cells that have yet to be fully solved.
        /// </summary>
        public GameObject uncertaintyTile;

        /// <summary>
        /// If true, the uncertainty tiles shrink as the solver gets more certain.
        /// </summary>
        public bool scaleUncertainyTile = true;

        public void StartGeneration()
        {
            if(state != AnimatedGeneratorState.Stopped)
                StopGeneration();


            tileOutput?.ClearTiles(UnityEngineInterface.Instance);

            var generator = GetComponent<TesseraGenerator>();
            var currentHelper = helper = generator.CreateTesseraGeneratorHelper();
            stepCount = 0;
            if (multithread && Application.platform != RuntimePlatform.WebGLPlayer)
            {
                initCts = new CancellationTokenSource();
                initTask = Task.Run(() =>
                {
                    currentHelper.Init();
                    currentHelper.CreatePropagator();
                    changeTracker = helper.Propagator.CreateChangeTracker();
                    currentHelper.Setup();
                }, initCts.Token);
                state = AnimatedGeneratorState.Initializing;
            }
            else
            {
                currentHelper.Init();
                currentHelper.CreatePropagator();
                changeTracker = helper.Propagator.CreateChangeTracker();
                currentHelper.Setup();
                OnInitialized();
            }
        }

        private void OnInitialized()
        {
            var generator = GetComponent<TesseraGenerator>();
            lastTiles = new ISet<ModelTile>[helper.Propagator.Topology.IndexCount];
            baseTime = DateTime.Now;
            lastStepTime = GetTime();
            tileOutput = generator.GetTileOutput(true);
            if (!tileOutput.SupportsIncremental)
            {
                throw new Exception($"Output {tileOutput} does not support animations");
            }
            supportsCubes = generator.GetComponent<ITesseraTileOutput>() == null;
            hasObject = new bool[helper.Propagator.Topology.IndexCount];
            cubesByIndex = new GameObject[helper.Propagator.Topology.IndexCount];
            firstStep = true;
            state = AnimatedGeneratorState.Running;
        }


        public void ResumeGeneration()
        {
            switch(state)
            {
                case AnimatedGeneratorState.Stopped:
                    throw new Exception("Generation must be started first.");
                case AnimatedGeneratorState.Running:
                case AnimatedGeneratorState.Initializing:
                    return;
                case AnimatedGeneratorState.Paused:
                    state = AnimatedGeneratorState.Running;
                    return;
            }
        }

        public void PauseGeneration()
        {
            switch (state)
            {
                case AnimatedGeneratorState.Initializing:
                    throw new Exception("Cannot pause.");
                case AnimatedGeneratorState.Running:
                    state = AnimatedGeneratorState.Paused;
                    return;
                case AnimatedGeneratorState.Stopped:
                case AnimatedGeneratorState.Paused:
                    return;
            }
        }

        public void StopGeneration()
        {
            tileOutput?.ClearTiles(UnityEngineInterface.Instance);
            initCts?.Cancel();
            tileOutput = null;
            if (helper != null)
            {
                foreach (var i in helper.Propagator.Topology.GetIndices())
                {
                    ClearCube(i);
                }
            }
            state = AnimatedGeneratorState.Stopped;
        }

        private void Update()
        {
            Step();
        }

        void ClearCube(int i)
        {
            if (cubesByIndex[i] != null)
            {
                ClearCube(cubesByIndex[i]);
            }
            cubesByIndex[i] = null;
        }

        void ClearCube(GameObject cube)
        {
            DestroyImmediate(cube);
        }

        GameObject CreateCube(TesseraGenerator generator, IGrid grid, Vector3Int p)
        {
            //var c = Instantiate(uncertaintyTile, generator.transform.TransformPoint(grid.GetCellCenter(p)), Quaternion.identity, generator.transform);
            var trs = TRS.World(generator.transform) * grid.GetTRS(p);
            var c = Instantiate(uncertaintyTile, trs.Position, trs.Rotation, generator.transform);
            c.transform.localScale = trs.Scale;

            return c;

        }

        public void Step()
        {
            if(state == AnimatedGeneratorState.Initializing)
            {
                if(initTask.IsCompleted)
                {
                    OnInitialized();
                }
                return;
            }

            if (state != AnimatedGeneratorState.Running)
                return;

            if (gameObject == null)
            {
                StopGeneration();
            }

            if (GetTime() < lastStepTime + secondsPerStep) return;

            var generator = GetComponent<TesseraGenerator>();

            var propagator = helper.Propagator;
            var topology = propagator.Topology;
            var grid = helper.Grid;

            for (var i = 0; i < progressPerStep; i++)
            {
                if (propagator.Status != DeBroglie.Resolution.Undecided)
                    break;

                propagator.Step();

                // Have we gone over the step limit?
                // TODO: Cleanup?
                stepCount += 1;
                var stepLimit = generator.stepLimit;
                if (stepLimit > 0 && stepCount >= stepLimit)
                {
                    propagator.SetContradiction();
                }
            }
            lastStepTime = GetTime();

            var mask = topology.Mask ?? Enumerable.Range(0, topology.IndexCount).Select(x => true).ToArray();

            var updateTiles = new Dictionary<Vector3Int, ModelTile>();

            var tileCount = helper.Propagator.TileModel.Tiles.Count();
            const float MinScale = 0.0f;
            const float MaxScale = 1.0f;

            //foreach (var i in topology.GetIndices())
            foreach (var i in changeTracker.GetChangedIndices())
            {
                // Skip indices that are masked out or already processed
                if (!mask[i])
                    continue;

                var before = lastTiles[i];
                var after = propagator.GetPossibleValues<ModelTile>(i);

                // Skip if nothing has changed
                var hasChanged = before == null || !before.SetEquals(after);
                if (!firstStep && !hasChanged)
                    continue;


                if (after.Count == 1)
                {
                    topology.GetCoord(i, out var x, out var y, out var z);
                    var p = new Vector3Int(x, y, z);

                    var modelTile = after.Single();
                    updateTiles[p] = modelTile;
                    ClearCube(i);
                    hasObject[i] = true;
                }
                else if(hasChanged)
                {
                    var p = grid.GetCell(i);

                    // Draw cube
                    ClearCube(i);
                    if (uncertaintyTile != null && supportsCubes)
                    {
                        var c = cubesByIndex[i] = CreateCube(generator, grid, p);
                        if (scaleUncertainyTile)
                        {
                            var scale = (MaxScale - MinScale) * after.Count / tileCount + MinScale;
                            c.transform.localScale = c.transform.localScale * scale;
                        }
                    }

                    // Remove object
                    if (hasObject[i])
                    {
                        updateTiles[p] = new ModelTile();
                        hasObject[i] = false;
                    }
                }
                lastTiles[i] = after;
            }

            var success = propagator.Status != DeBroglie.Resolution.Contradiction;

            var completion = new TesseraCompletion
            {
                success = success,
                tileData = updateTiles,
                isIncremental = true,
                grid = grid,
                gridTransform = TRS.World(generator.transform),
            };

            tileOutput.UpdateTiles(completion, UnityEngineInterface.Instance);
            firstStep = false;


            if (propagator.Status != DeBroglie.Resolution.Undecided)
            {
                if(propagator.Status == DeBroglie.Resolution.Contradiction)
                {
                    var contradictionTile = new ModelTile { };
                    var result = propagator.ToValueArray<ModelTile?>(contradiction: contradictionTile);
                    var contradictionLocation = DeBroglieUtils.GetContradictionLocation(result, grid);
                    completion.LogErrror();
                }

                state = AnimatedGeneratorState.Stopped;
                PauseGeneration();
            }
        }

        private float GetTime()
        {
            if (Application.isPlaying)
            {
                return Time.time;
            }
            else
            {
                return (float)(DateTime.Now - baseTime).TotalSeconds;
            }
        }
    }
}
