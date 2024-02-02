using DeBroglie.Rot;
using DeBroglie.Topo;
using System;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Geometric calculations specific to cube shaped tiles
    /// </summary>
    internal static class CubeGeometryUtils
    {
        /// <summary>
        /// Is p in a rect between the origin and size
        /// </summary>
        internal static bool InBounds(Vector3Int p, Vector3Int size)
        {
            if (p.x < 0) return false;
            if (p.x >= size.x) return false;
            if (p.y < 0) return false;
            if (p.y >= size.y) return false;
            if (p.z < 0) return false;
            if (p.z >= size.z) return false;

            return true;
        }

        /// <summary>
        /// Rotates v about the y-axis by r.
        /// </summary>
        internal static Vector3Int Rotate(Rotation r, Vector3Int v)
        {
            (v.x, v.z) = TopoArrayUtils.SquareRotateVector(v.x, v.z, r);
            return v;
        }

        /// <summary>
        /// Rotates v about the y-axis by r.
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
                    return new Vector3(-v.z, v.y, v.x);
                case 2 * 90:
                    return new Vector3(-v.x, v.y, -v.z);
                case 3 * 90:
                    return new Vector3(v.z, v.y, -v.x);
            }
            throw new Exception();
        }

        public static CubeRotation ToCubeRotation(Rotation r)
        {

            var o = CubeRotation.Identity;
            if (r.ReflectX)
            {
                o = CubeRotation.ReflectX;
            }
            for (var i = 0; i < r.RotateCw; i += 90)
            {
                o = CubeRotation.RotateXZ * o;
            }
            return o;
        }

        /// <summary>
        /// Given a cube normal vector, converts it to the FaceDir enum
        /// </summary>
        internal static CubeFaceDir FromNormal(Vector3Int v)
        {
            if (v.x == 1) return CubeFaceDir.Right;
            if (v.x == -1) return CubeFaceDir.Left;
            if (v.y == 1) return CubeFaceDir.Up;
            if (v.y == -1) return CubeFaceDir.Down;
            if (v.z == 1) return CubeFaceDir.Forward;
            if (v.z == -1) return CubeFaceDir.Back;

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
            for (var i = 0; i < r.RotateCw / 90; i++)
            {
                c.RotateCw();
            }
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
        internal static (CubeFaceDir, FaceDetails) RotateBy(CubeFaceDir faceDir, FaceDetails faceDetails, MatrixInt3x3 rotator)
        {
            var rotatedFaceDirForward = rotator.Multiply(faceDir.Forward());
            var rotatedFaceDirUp = rotator.Multiply(faceDir.Up());
            var rotatedFaceDirRight = rotator.Multiply(Vector3.Cross(faceDir.Forward(), faceDir.Up()));
            var rotatedFaceDir = FromNormal(rotatedFaceDirForward);
            var trueUp = rotatedFaceDir.Up();
            var trueForward = rotatedFaceDirForward; // =  rotatedFaceDir.Forward();
            var trueRight = Vector3.Cross(trueForward, trueUp);
            // Find the rotation that will map rotatedFaceDirUp to trueUp
            // and rotatedFaceDirRight to trueRight
            var dot = Vector3.Dot(rotatedFaceDirUp, trueUp);
            var cross = Vector3.Dot(rotatedFaceDirUp, trueRight);
            Rotation faceRot;
            if (dot == 1)
            {
                faceRot = new Rotation();
            }
            else if (dot == -1)
            {
                faceRot = new Rotation(180);
            }
            else if (cross == 1)
            {
                faceRot = new Rotation(270);
            }
            else if (cross == -1)
            {
                faceRot = new Rotation(90);
            }
            else
            {
                throw new Exception();
            }
            if (Vector3.Dot(Vector3.Cross(rotatedFaceDirForward, rotatedFaceDirUp), rotatedFaceDirRight) < 0)
            {
                faceRot = new Rotation(360 - faceRot.RotateCw, true);
            }
            faceRot = faceRot.Inverse();
            var rotatedFaceDetails = RotateBy(faceDetails, faceRot);

            return (rotatedFaceDir, rotatedFaceDetails);
        }

        internal static bool FindCell(
            Vector3 origin,
            Vector3 tileSize,
            Vector3 tileCenter, 
            Matrix4x4 tileLocalToGridMatrix, 
            out Vector3Int cell, 
            out CellRotation rotation)
        {
            var m = tileLocalToGridMatrix;

            Vector3Int Rotate(Vector3Int v)
            {
                var v1 = m.MultiplyVector(v);
                var v2 = new Vector3Int((int)Math.Round(v1.x), (int)Math.Round(v1.y), (int)Math.Round(v1.z));

                return v2;
            }

            // True if v is a unit vector along an axis
            bool Ok(Vector3Int v)
            {
                return Math.Abs(v.x) + Math.Abs(v.y) + Math.Abs(v.z) == 1;
            }

            var rotatedRight = Rotate(Vector3Int.right);
            var rotatedUp = Rotate(Vector3Int.up);
            var rotatedForward = Rotate(new Vector3Int(0, 0, 1));

            if (Ok(rotatedRight) && Ok(rotatedUp) && Ok(rotatedForward))
            {

                rotation = CubeRotation.FromMatrixInt(new MatrixInt3x3
                {
                    col1 = rotatedRight,
                    col2 = rotatedUp,
                    col3 = rotatedForward,
                });

                var localPos = m.MultiplyPoint3x4(tileCenter);

                return FindCell(origin, tileSize, localPos, out cell);
            }
            else
            {
                cell = default;
                rotation = default;
                return false;
            }
        }

        internal static bool FindCell(Vector3 origin, Vector3 tileSize, Vector3 position, out Vector3Int cell)
        {
            position -= origin;
            var x = (int)Mathf.Round(position.x / tileSize.x);
            var y = (int)Mathf.Round(position.y / tileSize.y);
            var z = (int)Mathf.Round(position.z / tileSize.z);
            cell = new Vector3Int(x, y, z);
            return true;
        }
    }
}
