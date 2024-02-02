using DeBroglie.Rot;
using DeBroglie.Topo;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    // Contains details about the shape of a cell
    // This is responsible for several things:
    // * Drawing / interacting with a cell in the UI
    // * A topology like interface for dealing with offsets and big tiles. 
    //     The topology uses offsets and facedirs instead of co-ordinates and directions to distinguish it. We call this "tile local space" as opposed to "generator space"
    // * Interpretation of Direction and Rotation
    public interface ICellType
    {
        // Directions

        IEnumerable<CellFaceDir> GetFaceDirs();

        IEnumerable<(CellFaceDir, CellFaceDir)> GetFaceDirPairs();

        CellFaceDir Invert(CellFaceDir faceDir);

        // Rotations

        IList<CellRotation> GetRotations(bool rotatable = true, bool reflectable = true, RotationGroupType rotationGroupType = RotationGroupType.All);

        CellRotation Multiply(CellRotation a, CellRotation b);

        CellRotation Invert(CellRotation a);

        CellRotation GetIdentity();

        CellFaceDir Rotate(CellFaceDir faceDir, CellRotation rotation);

        (CellFaceDir, FaceDetails) RotateBy(CellFaceDir faceDir, FaceDetails faceDetails, CellRotation rot);

        Matrix4x4 GetMatrix(CellRotation cellRotation);

        // Offset topology. This is basically a re-

        // This shares a lot of similarities with IGrid, but it's not as full featured.
        // Notable, rotations are not supported.
        bool TryMove(Vector3Int offset, CellFaceDir dir, out Vector3Int dest);

        IEnumerable<CellFaceDir> FindPath(Vector3Int startOffset, Vector3Int endOffset);

        Vector3 GetCellCenter(Vector3Int offset, Vector3 center, Vector3 tileSize);

        /// <summary>
        /// Note startCell/destCell are actually offsets, but naming is hard...
        /// </summary>
        bool TryMoveByOffset(Vector3Int startCell, Vector3Int startOffset, Vector3Int destOffset, CellRotation rotation, out Vector3Int destCell);


        /// <summary>
        /// Given a shape, and a rotation, finds the translation that puts the rotated shape back on itself,
        /// and applies that translation to each of the offsets in the shape.
        /// Returns null if no such mapping is possible.
        /// </summary>
        IDictionary<Vector3Int, Vector3Int> Realign(ISet<Vector3Int> shape, CellRotation rotation);
    }
}
