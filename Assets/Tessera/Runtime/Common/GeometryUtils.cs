using DeBroglie.Rot;
using UnityEngine;

namespace Tessera
{
    internal static class GeometryUtils
    {
        internal static Quaternion ToQuaternion(Rotation r)
        {
            return Quaternion.Euler(0, -r.RotateCw, 0);
        }

        internal static Matrix4x4 ToMatrix(Rotation r)
        {
            var q = Quaternion.Euler(0, -r.RotateCw, 0);
            return Matrix4x4.TRS(Vector3.zero, q, new Vector3(r.ReflectX ? -1 : 1, 1, 1));
        }

        internal static Bounds Multiply(Matrix4x4 m, Bounds b)
        {
            var bx = Abs(m.MultiplyVector(Vector3.right * b.size.x));
            var by = Abs(m.MultiplyVector(Vector3.up * b.size.y));
            var bz = Abs(m.MultiplyVector(Vector3.forward * b.size.z));
            var c = m.MultiplyPoint3x4(b.center);
            return new Bounds(c, bx + by + bz);
        }

        public static Vector3 Abs(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        public static Vector3 ElementwiseDivide(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }
    }
}
