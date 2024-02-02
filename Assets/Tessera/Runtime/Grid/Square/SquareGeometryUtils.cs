using DeBroglie.Rot;
using DeBroglie.Topo;
using System;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Geometric calculations specific to cube shaped tiles
    /// </summary>
    internal static class SquareGeometryUtils
    {
        public static SquareFaceDir FromSide(int side)
        {
            switch (side)
            {
                case 0: return SquareFaceDir.Right;
                case 1: return SquareFaceDir.Up;
                case 2: return SquareFaceDir.Left;
                case 3: return SquareFaceDir.Down;
            }
            throw new Exception();
        }


        /// <summary>
        /// Is p in a rect between the origin and size
        /// </summary>
        internal static bool InBounds(Vector3Int p, Vector2Int size)
        {
            if (p.x < 0) return false;
            if (p.x >= size.x) return false;
            if (p.y < 0) return false;
            if (p.y >= size.y) return false;
            if (p.z != 0) return false;

            return true;
        }

        /// <summary>
        /// Rotates v about the y-axis by r.
        /// </summary>
        internal static Vector3Int Rotate(Rotation r, Vector3Int v)
        {
            (v.x, v.y) = TopoArrayUtils.SquareRotateVector(v.x, v.y, r);
            return v;
        }

        /// <summary>
        /// Rotates v about the z-axis by r.
        /// </summary>
        internal static Vector3 Rotate(Rotation r, Vector3 v)
        {
            if (r.ReflectX)
            {
                v.x = -v.x;
            }
            switch (r.RotateCw)
            {
                case 0 * 90:
                    return new Vector3(v.x, v.y, v.z);
                case 1 * 90:
                    return new Vector3(-v.y, v.x, v.z);
                case 2 * 90:
                    return new Vector3(-v.x, -v.y, v.z);
                case 3 * 90:
                    return new Vector3(v.y, -v.x, v.z);
            }
            throw new Exception();
        }

        public static SquareRotation ToSquareRotation(Rotation r)
        {

            var o = SquareRotation.Identity;
            if (r.ReflectX)
            {
                o = SquareRotation.ReflectX;
            }
            for (var i = 0; i < r.RotateCw; i += 90)
            {
                o = SquareRotation.RotateCCW * o;
            }
            return o;
        }

        /// <summary>
        /// Given a cube normal vector, converts it to the FaceDir enum
        /// </summary>
        internal static SquareFaceDir FromNormal(Vector3Int v)
        {
            if (v.x == 1) return SquareFaceDir.Right;
            if (v.x == -1) return SquareFaceDir.Left;
            if (v.y == 1) return SquareFaceDir.Up;
            if (v.y == -1) return SquareFaceDir.Down;

            throw new Exception();
        }

        public static Vector3 GetCellCenter(Vector3Int cell, Vector3 origin, Vector3 tileSize)
        {
            return origin + Vector3.Scale(tileSize, cell);
        }

        /// <summary>
        /// Returns a new FaceDetails with the paint shuffled around.
        /// Assumes the rotation is about the normal of the face
        /// </summary>
        internal static FaceDetails RotateBy(FaceDetails faceDetails, Rotation r)
        {
            var c = faceDetails.Clone();
            if (r.ReflectX) c.ReflectX();
            for (var i = 0; i < r.RotateCw / 90; i++) c.RotateCw();
            return c;
        }

        /// <summary>
        /// Returns a new FaceDetails with the paint shuffled around.
        /// Assumes the rotation is about the y-axis, and the this
        /// face has the given facing.
        /// </summary>
        internal static FaceDetails RotateBy(FaceDetails faceDetails, Direction direction, Rotation rot)
        {
            if (direction == Direction.YPlus)
            {
                return RotateBy(faceDetails, rot);
            }
            else if (direction == Direction.YMinus)
            {
                return RotateBy(faceDetails, new Rotation(360 - rot.RotateCw, rot.ReflectX));
            }
            else
            {
                if (rot.ReflectX)
                    return RotateBy(faceDetails, new Rotation(0, true));
                else
                    return faceDetails.Clone();
            }
        }

        /// <summary>
        /// Given a FaceDetails on given face of the cube,
        /// rotates the cube, and returns the new face and correctly oriented FaceDetails
        /// </summary>
        internal static (SquareFaceDir, FaceDetails) RotateBy(SquareFaceDir faceDir, FaceDetails faceDetails, SquareRotation rotation)
        {
            var newSide = rotation * faceDir.GetSide();
            var newFaceDetails = faceDetails.Clone();
            if (rotation.IsReflection)
                newFaceDetails.ReflectX();
            return (FromSide(newSide), newFaceDetails);
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

            var forward = m.MultiplyVector(Vector3.forward);
            if (Vector3.Distance(forward, Vector3.forward) > eps)
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
            var angle = Mathf.Atan2(right.y, right.x);
            var angleInt = Mathf.RoundToInt(angle / (Mathf.PI / 2));

            rotation = (isReflection ? SquareRotation.ReflectX : SquareRotation.Identity) * SquareRotation.Rotate90(angleInt);
            return FindCell(origin, tileSize, localPos, out cell);
        }

        internal static bool FindCell(Vector3 origin, Vector3 tileSize, Vector3 position, out Vector3Int cell)
        {
            position -= origin;
            var x = (int)Mathf.Round(position.x / tileSize.x);
            var y = (int)Mathf.Round(position.y / tileSize.y);
            var z = 0;
            cell = new Vector3Int(x, y, z);
            return true;
        }
    }
}
