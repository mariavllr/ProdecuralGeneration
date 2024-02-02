using DeBroglie.Rot;
using DeBroglie.Topo;
using System;
using UnityEngine;

namespace Tessera
{
    public static class CubeFaceDirExtensions
    {
        /// <returns>Returns (0, 1, 0) vector for most faces, and returns (0, 0, 1) for the top/bottom faces.</returns>
        public static Vector3Int Up(this CubeFaceDir faceDir)
        {
            switch (faceDir)
            {
                case CubeFaceDir.Left:
                case CubeFaceDir.Right:
                case CubeFaceDir.Forward:
                case CubeFaceDir.Back:
                    return Vector3Int.up;
                case CubeFaceDir.Up:
                case CubeFaceDir.Down:
                    return new Vector3Int(0, 0, 1);
            }
            throw new Exception();
        }

        /// <returns>The normal vector for a given face.</returns>
        public static Vector3Int Forward(this CubeFaceDir faceDir)
        {
            switch (faceDir)
            {
                case CubeFaceDir.Left: return Vector3Int.left;
                case CubeFaceDir.Right: return Vector3Int.right;
                case CubeFaceDir.Up: return Vector3Int.up;
                case CubeFaceDir.Down: return Vector3Int.down;
                case CubeFaceDir.Forward: return new Vector3Int(0, 0, 1);
                case CubeFaceDir.Back: return new Vector3Int(0, 0, -1);
            }
            throw new Exception();
        }

        /// <returns>Returns the face dir with the opposite normal vector.</returns>
        public static CubeFaceDir Inverted(this CubeFaceDir faceDir)
        {

            switch (faceDir)
            {
                case CubeFaceDir.Left: return CubeFaceDir.Right;
                case CubeFaceDir.Right: return CubeFaceDir.Left;
                case CubeFaceDir.Up: return CubeFaceDir.Down;
                case CubeFaceDir.Down: return CubeFaceDir.Up;
                case CubeFaceDir.Forward: return CubeFaceDir.Back;
                case CubeFaceDir.Back: return CubeFaceDir.Forward;
            }
            throw new Exception();
        }

        /// <summary>
        /// Convert from Tessera enum to DeBroglie enum.
        /// </summary>
        internal static Direction ToDirection(this CubeFaceDir faceDir)
        {
            switch (faceDir)
            {
                case CubeFaceDir.Left: return Direction.XMinus;
                case CubeFaceDir.Right: return Direction.XPlus;
                case CubeFaceDir.Up: return Direction.YPlus;
                case CubeFaceDir.Down: return Direction.YMinus;
                case CubeFaceDir.Forward: return Direction.ZPlus;
                case CubeFaceDir.Back: return Direction.ZMinus;
            }
            throw new Exception();
        }
    }
}