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
    public static class HexGeometryUtils
    {
        public static HexPrismFaceDir FromSide(int side)
        {
            switch (side)
            {
                case 0: return HexPrismFaceDir.Right;
                case 1: return HexPrismFaceDir.ForwardRight;
                case 2: return HexPrismFaceDir.ForwardLeft;
                case 3: return HexPrismFaceDir.Left;
                case 4: return HexPrismFaceDir.BackLeft;
                case 5: return HexPrismFaceDir.BackRight;
            }
            throw new Exception();
        }

        public static Vector3 GetCellCenter(Vector3Int cell, Vector3 origin, Vector3 tileSize)
        {
            return origin +
                Vector3.up * tileSize.y * cell.y +
                Vector3.right * tileSize.x * cell.x +
                HexPrismFaceDir.ForwardLeft.Forward() * tileSize.x * cell.z;
        }



        private const float eps = 1e-6f;

        internal static bool FindCell(
            Vector3 origin,
            Vector3 tileSize,
            Vector3 tileCenter,
            Matrix4x4 tileLocalToGridMatrix,
            out Vector3Int cell,
            out CellRotation rotation)
        {
            var m = tileLocalToGridMatrix;

            var localPos = m.MultiplyPoint3x4(tileCenter);

            var up = m.MultiplyVector(Vector3.up);
            if (Vector3.Distance(up, Vector3.up) > eps)
            {
                cell = default;
                rotation = default;
                return false;
            }

            var right = m.MultiplyVector(Vector3.right);

            var scale = m.lossyScale;
            var isReflection = false;
            if (scale.x * scale.y * scale.z < 0)
            {
                isReflection = true;
                right.x = -right.x;
            }

            var angle = Mathf.Atan2(right.z, right.x);
            var angleInt = Mathf.RoundToInt(angle / (Mathf.PI / 3));

            rotation = (isReflection ? HexRotation.ReflectX : HexRotation.Identity) * HexRotation.Rotate60(-angleInt);
            return FindCell(origin, tileSize, localPos, out cell);
        }

        internal static bool FindCell(Vector3 origin, Vector3 tileSize, Vector3 position, out Vector3Int cell)
        {
            // Thanks redblobgames

            position -= origin;

            var x = position.x / tileSize.x;
            var z = position.z / tileSize.x;


            var q = (Math.Sqrt(3) / 3 * x + 1f / 3 * z) * Math.Sqrt(3);
            var r = (2f / 3 * z) * Math.Sqrt(3);
            var s = -q - r;

            var rq = (int)Math.Round(q);
            var rr = (int)Math.Round(r);
            var rs = (int)Math.Round(s);

            var x_diff = Math.Abs(rq - q);
            var y_diff = Math.Abs(rr - r);
            var z_diff = Math.Abs(rs - s);

            if (x_diff > y_diff && x_diff > z_diff)
                rq = -rr - rs;
            else if (y_diff > z_diff)
                rr = -rq - rs;
            else
                rs = -rq - rr;

            var y = (int)Mathf.Round(position.y / tileSize.y);

            cell = new Vector3Int(rq, y, rr);
            return true;
        }

        // I regret not using cube co-ordinates in first place.
        // Cube-coords always sum to zero and have anumber of other useful properties
        /*
         * 
         *  0,-1,1   /\   1,-1,0
         *          /  \
         * -1,0,1  |    |   1,0,-1
         *         |    |   
         *  -1,1,0  \  /  0,1,-1
         *           \/
         * */


        public static Vector3Int ToCubeCoords(Vector3Int cell)
        {
            return new Vector3Int(cell.x, -cell.z, -cell.x + cell.z);
        }

        public static Vector3Int FromCubeCords(Vector3Int cc, int y = 0)
        {
            return new Vector3Int(cc.x, y, -cc.y);
        }

        public static Vector3Int CubeRotate(HexRotation rotation, Vector3Int cc)
        {
            if (rotation.IsReflection)
                cc = new Vector3Int(cc.z, cc.y, cc.x);
            switch (rotation.Rotation)
            {
                case 0: return cc;
                case 1: return new Vector3Int(-cc.z, -cc.x, -cc.y);
                case 2: return new Vector3Int(cc.y, cc.z, cc.x);
                case 3: return new Vector3Int(-cc.x, -cc.y, -cc.z);
                case 4: return new Vector3Int(cc.z, cc.x, cc.y);
                case 5: return new Vector3Int(-cc.y, -cc.z, -cc.x);
            }
            throw new Exception();
        }

        public static Vector3Int Rotate(HexRotation rotation, Vector3Int cell)
        {
            var cc = ToCubeCoords(cell);
            cc = CubeRotate(rotation, cc);
            return FromCubeCords(cc, cell.y);
        }
    }
}
