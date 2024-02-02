using DeBroglie;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tessera
{
    public interface ITesseraInitialConstraint
    {
        string Name { get; }
    }

    /// <summary>
    /// Initial constraint objects fix parts of the generation process in places.
    /// Use the utility methods on <see cref="TesseraGenerator"/> to create these objects.
    /// </summary>
    [Serializable]
    public class TesseraInitialConstraint : ITesseraInitialConstraint
    {
        internal string name;

        internal List<OrientedFace> faceDetails;

        internal List<Vector3Int> offsets;

        internal CellRotation rotation;

        // TODO: Is this consistently zero-based
        internal Vector3Int cell;

        public string Name => name;
    }

    public class TesseraVolumeFilter : ITesseraInitialConstraint
    {
        internal string name;

        internal List<TesseraTileBase> tiles;

        internal List<Vector3Int> cells;
        public string Name => name;

        public VolumeType volumeType;
    }

    public class TesseraPinConstraint : ITesseraInitialConstraint
    {
        internal string name;

        internal TesseraTileBase tile;

        internal CellRotation rotation;

        // TODO: Is this consistently zero-based
        internal Vector3Int cell;

        public string Name => name;
    }
}