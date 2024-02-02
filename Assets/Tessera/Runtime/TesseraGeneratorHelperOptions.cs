using DeBroglie.Constraints;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Tessera
{
    internal class TesseraGeneratorHelperOptions
    {
        // Basic configuration
        public IGrid grid;
        public TesseraPalette palette;
        // Model options
        public ModelType modelType;
        public TileModelInfo tileModelInfo;
        public List<TesseraTilemap> samples;
        public Vector3Int overlapSize;
        // Constraints
        public List<ITesseraInitialConstraint> initialConstraints;
        public List<ITileConstraint> constraints;
        public TesseraInitialConstraint skyBox;
        // Run control
        public bool backtrack;
        public int stepLimit;
        public TesseraWfcAlgorithm algorithm;
        public Action<string, float> progress;
        public Action<ITopoArray<ISet<ModelTile>>> progressTiles;
        public XoRoRNG xororng;
        public CancellationToken cancellationToken;
        public FailureMode failureMode;
        public TesseraStats stats;

    }
}
