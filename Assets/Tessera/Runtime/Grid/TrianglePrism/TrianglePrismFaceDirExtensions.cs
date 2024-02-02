using System;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    public static class TrianglePrismFaceDirExtensions
    {
        public static int GetSide(this TrianglePrismFaceDir dir)
        {
            switch (dir)
            {
                case TrianglePrismFaceDir.Back: return 0;
                case TrianglePrismFaceDir.BackRight: return 1;
                case TrianglePrismFaceDir.ForwardRight: return 2;
                case TrianglePrismFaceDir.Forward: return 3;
                case TrianglePrismFaceDir.ForwardLeft: return 4;
                case TrianglePrismFaceDir.BackLeft: return 5;
                
            }
            throw new Exception();
        }

        public static Vector3Int OffsetDelta(this TrianglePrismFaceDir dir)
        {
            switch (dir)
            {
                case TrianglePrismFaceDir.Forward: return new Vector3Int(-1, 0, 1);
                case TrianglePrismFaceDir.Back: return new Vector3Int(1, 0, -1);
                case TrianglePrismFaceDir.Up: return new Vector3Int(0, 1, 0);
                case TrianglePrismFaceDir.Down: return new Vector3Int(0, -1, 0);
                case TrianglePrismFaceDir.ForwardLeft: return new Vector3Int(-1, 0, 0);
                case TrianglePrismFaceDir.BackRight: return new Vector3Int(1, 0, 0);
                case TrianglePrismFaceDir.ForwardRight: return new Vector3Int(1, 0, 0);
                case TrianglePrismFaceDir.BackLeft: return new Vector3Int(-1, 0, 0);
                default:
                    return Vector3Int.zero;
            }
        }

        public static bool IsValid(this TrianglePrismFaceDir dir, Vector3Int offset)
        {
            return dir.IsValid(TrianglePrismGeometryUtils.PointsUp(offset));
        }


        public static bool IsValid(this TrianglePrismFaceDir dir, bool pointsUp)
        {
            switch (dir)
            {
                case TrianglePrismFaceDir.Forward: return !pointsUp;
                case TrianglePrismFaceDir.Back: return pointsUp;
                case TrianglePrismFaceDir.Up: return true;
                case TrianglePrismFaceDir.Down: return true;
                case TrianglePrismFaceDir.ForwardLeft: return pointsUp;
                case TrianglePrismFaceDir.BackRight: return !pointsUp;
                case TrianglePrismFaceDir.ForwardRight: return pointsUp;
                case TrianglePrismFaceDir.BackLeft: return !pointsUp;
                default:
                    throw new Exception();
            }
        }

        public static bool IsUpDown(this TrianglePrismFaceDir dir)
        {
            switch (dir)
            {
                case TrianglePrismFaceDir.Up:
                case TrianglePrismFaceDir.Down:
                    return true;
                default:
                    return false;
            }
            throw new Exception();
        }


        public static Vector3 Up(this TrianglePrismFaceDir dir)
        {
            switch (dir)
            {
                case TrianglePrismFaceDir.Up:
                case TrianglePrismFaceDir.Down:
                    return Vector3.forward;
                default:
                    return Vector3.up;
            }
        }

        public static Vector3 Forward(this TrianglePrismFaceDir dir)
        {
            switch (dir)
            {
                case TrianglePrismFaceDir.Up:
                    return Vector3.up;
                case TrianglePrismFaceDir.Down:
                    return Vector3.down;
                case TrianglePrismFaceDir.Forward:
                    return Quaternion.Euler(0, -60 * 0, 0) * Vector3.forward;
                case TrianglePrismFaceDir.ForwardLeft:
                    return Quaternion.Euler(0, -60 * 1, 0) * Vector3.forward;
                case TrianglePrismFaceDir.BackLeft:
                    return Quaternion.Euler(0, -60 * 2, 0) * Vector3.forward;
                case TrianglePrismFaceDir.Back:
                    return Quaternion.Euler(0, -60 * 3, 0) * Vector3.forward;
                case TrianglePrismFaceDir.BackRight:
                    return Quaternion.Euler(0, -60 * 4, 0) * Vector3.forward;
                case TrianglePrismFaceDir.ForwardRight:
                    return Quaternion.Euler(0, -60 * 5, 0) * Vector3.forward;
            }
            throw new Exception();
        }
    }
}
