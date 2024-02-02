using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    public static class HexPrismFaceDirExtensions
    {
        public static int GetSide(this HexPrismFaceDir dir)
        {
            switch (dir)
            {
                case HexPrismFaceDir.Right: return 0;
                case HexPrismFaceDir.ForwardRight: return 1;
                case HexPrismFaceDir.ForwardLeft: return 2;
                case HexPrismFaceDir.Left: return 3;
                case HexPrismFaceDir.BackLeft: return 4;
                case HexPrismFaceDir.BackRight: return 5;
            }
            throw new Exception();
        }

        public static bool IsUpDown(this HexPrismFaceDir dir)
        {
            switch (dir)
            {
                case HexPrismFaceDir.Up:
                case HexPrismFaceDir.Down:
                    return true;
                default:
                    return false;
            }
            throw new Exception();
        }

        public static Vector3 Up(this HexPrismFaceDir dir)
        {
            switch (dir)
            {
                case HexPrismFaceDir.Up:
                case HexPrismFaceDir.Down:
                    return Vector3.forward;
                default:
                    return Vector3.up;
            }
        }

        public static Vector3 Forward(this HexPrismFaceDir dir)
        {
            switch (dir)
            {

                case HexPrismFaceDir.Up:
                    return Vector3.up;
                case HexPrismFaceDir.Down:
                    return Vector3.down;
                default:
                    return Quaternion.Euler(0, -60 * dir.GetSide(), 0) * Vector3.right;
            }
        }

        public static Vector3Int ForwardInt(this HexPrismFaceDir dir)
        {
            switch (dir)
            {
                case HexPrismFaceDir.Left: return Vector3Int.left;
                case HexPrismFaceDir.Right: return Vector3Int.right;
                case HexPrismFaceDir.Up: return Vector3Int.up;
                case HexPrismFaceDir.Down: return Vector3Int.down;
                case HexPrismFaceDir.ForwardRight: return new Vector3Int(1, 0, 1);
                case HexPrismFaceDir.ForwardLeft: return new Vector3Int(0, 0, 1);
                case HexPrismFaceDir.BackRight: return new Vector3Int(0, 0, -1);
                case HexPrismFaceDir.BackLeft: return new Vector3Int(-1, 0, -1);
                default:
                    throw new Exception();

            }
        }
    }
}
