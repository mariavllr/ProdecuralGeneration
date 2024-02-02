using System;
using UnityEngine;

namespace Tessera
{
    /*
     * We measure from center to center, so some dimensions:
     * Height of center above base = 0.5 * size
     * Side of triangle = sqrt(3) * size
     * Height of triangle = 1.5 * size
     */
    /// <summary>
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    public static class TrianglePrismGeometryUtils
    {
        /* Transforms a point so that an equilateral triangle of side sqrt(3) becomes a triangle of sides (1, 1, sqrt(2))
         * 
         * In other words p => Standardize(p / size) maps from triangles to unit squares
         * 
         * y/z
         * ^
         * |
         *  -> x
         *    ______
         *   /\    /     ____
         *  /  \  /  =>  |\ |
         * /____\/       |_\|
         * 
         */
        public static Vector2 Standardize(Vector2 p)
        {
            return new Vector2(
                (p.x - p.y / Mathf.Sqrt(3)) / Mathf.Sqrt(3),
                p.y / 1.5f
                );
        }

        public static Vector2 Unstandardize(Vector2 p)
        {
            return new Vector2(
                p.x * Mathf.Sqrt(3) + p.y * 1.5f / Mathf.Sqrt(3),
                p.y * 1.5f
                );
        }

        public static (Vector2Int, bool, int) Unpack(Vector3Int cell)
        {
            return (new Vector2Int(cell.x >> 1, cell.z), (cell.x & 1) == 0, cell.y);
        }

        public static Vector3Int Pack(Vector2Int tri, bool pointsUp, int y)
        {
            return new Vector3Int(tri.x * 2 + (pointsUp ? 0 : 1), y, tri.y);
        }

        public static Vector3 GetCellCenter(Vector3Int cell, Vector3 origin, Vector3 tileSize)
        {
            var (tri, pointsUp, y) = Unpack(cell);
            var p = Unstandardize(tri) * tileSize.x;
            if(!pointsUp)
            {
                p += new Vector2(Mathf.Sqrt(3) / 2, 1 / 2f) * tileSize.x;
            }
            return origin + new Vector3(p.x, y * tileSize.y, p.y);
        }

        public static bool PointsUp(Vector3Int cell)
        {
            return (cell.x & 1) == 0;
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

            rotation = (isReflection ? TriangleRotation.ReflectX : TriangleRotation.Identity) * TriangleRotation.RotateCCW60(angleInt);
            return FindCell(origin, tileSize, localPos, out cell);
        }


        public static bool FindCell(Vector3 origin, Vector3 tileSize, Vector3 position, out Vector3Int cell)
        {
            position -= origin;
            var p = Standardize(new Vector2(position.x, position.z) / tileSize.x) + new Vector2(1/3f, 1/3f);
            var tri = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
            var pointsUp = (p.x - tri.x + p.y - tri.y) < 1f;
            var y = Mathf.RoundToInt(position.y / tileSize.y);
            cell = Pack(tri, pointsUp, y);
            return true;
        }

        public static TrianglePrismFaceDir FromSide(int side)
        {
            switch (side)
            {
                case 0: return TrianglePrismFaceDir.Back;
                case 1: return TrianglePrismFaceDir.BackRight;
                case 2: return TrianglePrismFaceDir.ForwardRight;
                case 3: return TrianglePrismFaceDir.Forward;
                case 4: return TrianglePrismFaceDir.ForwardLeft;
                case 5: return TrianglePrismFaceDir.BackLeft;
            }
            throw new Exception();
        }

        // I regret not using better co-ordinates in first place.
        // Described here: https://www.boristhebrave.com/2021/05/23/triangle-grids/
        // Except here the origin triangle is 0, 0, 0 and it points up. x+ is up and to the right.
        public static Vector3Int ToTriCoords(Vector3Int cell)
        {
            var (tri, pointsUp, _) = Unpack(cell);
            var x = tri.x;
            var y = tri.y;
            var z = pointsUp ? 0 - x - y : -1 - x - y;
            return new Vector3Int(x, y, z);
        }

        public static Vector3Int CoordRotate(TriangleRotation rotation, Vector3Int coords)
        {
            if (rotation.IsReflection)
                coords = new Vector3Int(coords.z, coords.y, coords.x);
            switch (rotation.Rotation)
            {
                case 0: return coords;
                case 5: return new Vector3Int(-coords.z, -coords.x, -coords.y);
                case 4: return new Vector3Int(coords.y, coords.z, coords.x);
                case 3: return new Vector3Int(-coords.x, -coords.y, -coords.z);
                case 2: return new Vector3Int(coords.z, coords.x, coords.y);
                case 1: return new Vector3Int(-coords.y, -coords.z, -coords.x);
            }
            throw new Exception();
        }

        public static Vector3Int? FromTriCoords(Vector3Int coords, int y = 0)
        {
            var s = (coords.x + coords.y + coords.z);
            s = s < 0 ? s / 3 : (s + 2) / 3;// ceil division
            coords.x -= s;
            coords.y -= s;
            coords.z -= s;
            s = coords.x + coords.y + coords.z;
            bool pointsUp;
            switch(s)
            {
                case -2: return null;
                case -1: pointsUp = false; break;
                case 0: pointsUp = true; break;
                default: throw new Exception();
            }
            var tri = new Vector2Int(coords.x, coords.y);
            return Pack(tri, pointsUp, y);
        }

    }
}
