using DeBroglie.Rot;
using DeBroglie.Topo;
using System;
using UnityEngine;

namespace Tessera
{
    public static class SquareFaceDirExtensions
    {
        public static int GetSide(this SquareFaceDir faceDir)
        {
            switch (faceDir)
            {
                case SquareFaceDir.Right: return 0;
                case SquareFaceDir.Up: return 1;
                case SquareFaceDir.Left: return 2;
                case SquareFaceDir.Down: return 3;
            }
            throw new Exception($"{faceDir} is not a valid value for SquareFaceDir");
        }

        /// <returns>The normal vector for a given face.</returns>
        public static Vector3Int Forward(this SquareFaceDir faceDir)
        {
            switch (faceDir)
            {
                case SquareFaceDir.Left: return Vector3Int.left;
                case SquareFaceDir.Right: return Vector3Int.right;
                case SquareFaceDir.Up: return Vector3Int.up;
                case SquareFaceDir.Down: return Vector3Int.down;
            }
            throw new Exception($"{faceDir} is not a valid value for SquareFaceDir");
        }

        /// <returns>Returns the face dir with the opposite normal vector.</returns>
        public static SquareFaceDir Inverted(this SquareFaceDir faceDir)
        {

            switch (faceDir)
            {
                case SquareFaceDir.Left: return SquareFaceDir.Right;
                case SquareFaceDir.Right: return SquareFaceDir.Left;
                case SquareFaceDir.Up: return SquareFaceDir.Down;
                case SquareFaceDir.Down: return SquareFaceDir.Up;
            }
            throw new Exception($"{faceDir} is not a valid value for SquareFaceDir");
        }

        /// <summary>
        /// Convert from Tessera enum to DeBroglie enum.
        /// </summary>
        internal static Direction ToDirection(this SquareFaceDir faceDir)
        {
            switch (faceDir)
            {
                case SquareFaceDir.Left: return Direction.XMinus;
                case SquareFaceDir.Right: return Direction.XPlus;
                case SquareFaceDir.Up: return Direction.YPlus;
                case SquareFaceDir.Down: return Direction.YMinus;
            }
            throw new Exception($"{faceDir} is not a valid value for SquareFaceDir");
        }
    }
}