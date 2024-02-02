using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    public abstract class TesseraTileBase : MonoBehaviour
    {
        /// <summary>
        /// Set this to control the colors and names used for painting on the tile.
        /// Defaults to <see cref="TesseraPalette.defaultPalette"/>.
        /// </summary>
        [Tooltip("Set this to control the colors and names used for painting on the tile.")]
        public TesseraPalette palette;

        /// <summary>
        /// A list of outward facing faces.
        /// For a normal cube tile, there are 6 faces. Each face contains adjacency information that indicates what other tiles can connect to it.
        /// It is recommended you only edit this via the Unity Editor, or <see cref="Get(Vector3Int, CellFaceDir)"/> and <see cref="AddOffset(Vector3Int)"/>
        /// </summary>
        public List<OrientedFace> faceDetails;

        /// <summary>
        /// A list of cells that this tile occupies.
        /// For a normal cube tile, this just contains Vector3Int.zero, but it will be more for "big" tiles.
        /// It is recommended you only edit this via the Unity Editor, or <see cref="AddOffset(Vector3Int)"/> and <see cref="RemoveOffset(Vector3Int)"/>
        /// </summary>
        public List<Vector3Int> offsets = new List<Vector3Int>()
        {
            Vector3Int.zero
        };

        /// <summary>
        /// Where the center of tile is.
        /// For big tiles that occupy more than one cell, it's the center of the cell with offset (0, 0, 0). Thie tile may not actually occupy that cell!
        /// </summary>
        [Tooltip("Where the center of tile is.")]
        public Vector3 center = Vector3.zero;

        /// <summary>
        /// The size of one cell in the tile.
        /// NB: This field is only used in the Editor - you must set <see cref="TesseraGenerator.tileSize"/> to match.
        /// </summary>
        [Tooltip("The size of one cell in the tile.")]
        public Vector3 tileSize = Vector3.one;

        /// <summary>
        /// If true, when generating, rotations of the tile will be used.
        /// </summary>
        [Tooltip("If true, when generating, rotations of the tile will be used.")]
        public bool rotatable = true;

        /// <summary>
        /// If true, when generating, reflections in the x-axis will be used.
        /// </summary>
        [Tooltip("If true, when generating, reflections in the x-axis will be used.")]
        public bool reflectable = true;

        /// <summary>
        /// If rotatable is on, specifies what sorts of rotations are used.
        /// </summary>
        [Tooltip("If rotatable is on, specifies what sorts of rotations are used.")]
        public RotationGroupType rotationGroupType = RotationGroupType.XZ;

        /// <summary>
        /// If true, Tessera assumes that the tile paint matches the the symmetry of the tile.
        /// Disable this if there are important details of your tile that the paint doesn't show.
        /// Turning symmetric on can have some performance benefits, and affects the behaviour of the mirror constraint.
        /// </summary>
        [Tooltip("If true, Tessera assumes that the tile paint matches the the symmetry of the tile.\nIf false, assumes every possible rotation is unique and not interchangable.")]
        public bool symmetric = true;

        /// <summary>
        /// If set, when being instantiated by a Generator, only children will get constructed.
        /// If there are no children, then this effectively disables the tile from instantiation.
        /// </summary>
        [Tooltip("If set, when being instantiated by a Generator, only children will get constructed.")]
        public bool instantiateChildrenOnly = false;


        /// <summary>
        /// Finds the face details for a cell with a given offeset.
        /// </summary>
        public FaceDetails Get(Vector3Int offset, CellFaceDir faceDir)
        {
            if(TryGet(offset, faceDir, out var details))
            {
                return details;
            }
            throw new System.Exception($"Couldn't find face at offset {offset} in direction {faceDir}");
        }

        /// <summary>
        /// Finds the face details for a cell with a given offeset.
        /// </summary>
        public bool TryGet(Vector3Int offset, CellFaceDir faceDir, out FaceDetails details)
        {
            try
            {
                details = faceDetails.SingleOrDefault(x => x.offset == offset && x.faceDir == faceDir).faceDetails;
            }
            catch (System.InvalidOperationException)
            {
                Debug.LogWarning($"Dumping offset/faceDir pairs: " + string.Join(",", faceDetails.Select(x => (x.offset, x.faceDir))));
                var faces = faceDetails.Count(x => x.offset == offset && x.faceDir == faceDir);
                throw new System.Exception($"Found {faces} faces with offset {offset}, faceDir {faceDir} (expected exactly one). This indicates the tile {this} has got in an invalid state.");
            }
            return details != null;
        }

        public abstract ICellType CellType { get; }

        /// <summary>
        /// Configures the tile as a "big" tile that occupies several cells.
        /// Keeps <see cref="offsets"/> and <see cref="faceDetails"/> in sync.
        /// </summary>
        public abstract void AddOffset(Vector3Int o);

        /// <summary>
        /// Configures the tile as a "big" tile that occupies several cells.
        /// Keeps <see cref="offsets"/> and <see cref="faceDetails"/> in sync.
        /// </summary>
        public abstract void RemoveOffset(Vector3Int o);
    }
}