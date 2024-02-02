using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tessera
{
    public class SquareCellDrawingType : ICellDrawingType
    {
        public static SquareCellDrawingType Instance = new SquareCellDrawingType();

        public bool Is2D => true;

        public Vector3[] GetSubFaceVertices(Vector3Int offset, CellFaceDir dir, SubFace subface, Vector3 center, Vector3 tileSize)
        {
            var u = new DrawingUtil(center, tileSize);

            var forward = ((SquareFaceDir)dir).Forward();
            var right = -Vector3.Cross(Vector3.forward, forward);
            GetFaceCenterAndNormal(offset, dir, center, tileSize, out var faceCenter, out var _);

            (float, float) t;
            switch(subface)
            {
                case SubFace.Left:
                    t = (-1, -1 / 3f);
                    break;
                case SubFace.Center:
                    t = (-1 / 3f, 1 / 3f);
                    break;
                case SubFace.Right:
                    t = (1 / 3f, 1);
                    break;
                default:
                    throw new Exception();
            }

            var v1 = faceCenter;
            var v2 = faceCenter;
            var v3 = faceCenter + u.ScaleByHSize(forward) + t.Item1 * u.ScaleByHSize(right);
            var v4 = faceCenter + u.ScaleByHSize(forward) + t.Item2 * u.ScaleByHSize(right);

            return new[] { v1, v2, v3, v4 };
        }


        public Vector3[] GetFaceVertices(Vector3Int offset, CellFaceDir dir, Vector3 center, Vector3 tileSize)
        {
            var u = new DrawingUtil(center, tileSize);

            var forward = ((SquareFaceDir)dir).Forward();
            var right = -Vector3.Cross(Vector3.forward, forward);
            GetFaceCenterAndNormal(offset, dir, center, tileSize, out var faceCenter, out var _);


            var v1 = faceCenter;
            var v2 = faceCenter;
            var v3 = faceCenter + u.ScaleByHSize(forward) - u.ScaleByHSize(right);
            var v4 = faceCenter + u.ScaleByHSize(forward) + u.ScaleByHSize(right);

            return new[] { v1, v2, v3, v4 };
        }

        public void GetFaceCenterAndNormal(Vector3Int offset, CellFaceDir dir, Vector3 center, Vector3 tileSize, out Vector3 faceCenter, out Vector3 faceNormal)
        {
            var u = new DrawingUtil(center, tileSize);

            faceCenter = center + u.ScaleBySize(offset);
            faceNormal = Vector3.zero;// Unused
        }


        public RaycastCellHit Raycast(Ray ray, Vector3Int offset, Vector3 center, Vector3 tileSize, float? minDistance, float? maxDistance)
        {
            var u = new DrawingUtil(center, tileSize);

            var currentHit = Raycast(center + u.ScaleBySize(offset), u.hsize, ray, out var currentFacePos, out var currentHitPoint);

            // Reject if miss
            if (!currentHit)
            {
                return null;
            }

            // Reject if not in the right distance range
            var d2 = (currentHitPoint - ray.origin).sqrMagnitude;

            if (maxDistance != null && d2 >= maxDistance * maxDistance)
            {
                return null;
            }

            if (minDistance != null && d2 <= minDistance * minDistance)
            {
                return null;
            }

            // Work out which face is inolved

            var px = currentFacePos.x;
            var py = currentFacePos.y;
            var apx = Mathf.Abs(px);
            var apy = Mathf.Abs(py);

            SquareFaceDir faceDir;
            if(apx > apy)
            {
                faceDir = px > 0 ? SquareFaceDir.Right : SquareFaceDir.Left;
            }
            else
            {
                faceDir = py > 0 ? SquareFaceDir.Up : SquareFaceDir.Down;
            }

            var hit = new RaycastCellHit
            {
                dir = (CellFaceDir)faceDir,
                point = currentHitPoint,
            };

            var forward = faceDir.Forward();

            var x = Vector3.Dot(currentFacePos, forward);
            var y = Vector3.Cross(currentFacePos, forward).z;

            hit.subface = new Vector2(x, y / x);

            return hit;
        }

        // Casts a ray at an axis aligned square, and reutrns where in the face and where the hit is.
        private bool Raycast(Vector3 center, Vector3 hsize, Ray ray, out Vector2 facePos, out Vector3 point)
        {
            var dir = ray.direction;
            float t = (center.z - ray.origin.z) / dir.z;

            if (t < 0)
            {
                facePos = default;
                point = default;
                return false;
            }

            point = ray.origin + t * dir;


            var px = (point.x - center.x) / hsize.x;
            var py = (point.y - center.y) / hsize.y;
            facePos = new Vector2(px, py);

            return Mathf.Abs(facePos.x) < 1 && Mathf.Abs(facePos.y) < 1;
        }

        public SubFace RoundSubFace(Vector3Int offset, CellFaceDir dir, Vector2 p2, PaintMode paintMode)
        {
            if (paintMode == PaintMode.Edge)
                return SubFace.Center;
            var sf = SquareFaceDrawingUtils.RoundSubFace(new Vector2(p2.y, 0), paintMode);
            switch(sf)
            {
                case SubFace.TopLeft:
                case SubFace.BottomLeft:
                    return SubFace.Left;
                case SubFace.TopRight:
                case SubFace.BottomRight:
                    return SubFace.Right;
                default:
                    return sf;
            }
        }

        public bool IsAffected(Vector3Int parentOffset, CellFaceDir parentFaceDir, SubFace parentSubface, Vector3Int childOffset, CellFaceDir childFaceDir, SubFace childSubface, PaintMode paintMode)
        {
            /*
            var p2 = SquareFaceDrawingUtils.ToSubFaceVector(parentSubface);
            var p1 = SquareFaceDrawingUtils.ToSubFaceVector(childSubface);

            return childOffset == parentOffset && childFaceDir == parentFaceDir && p1 == p2;
            */
            /*
            var up = ((CubeFaceDir)childFaceDir).Up();
            var forward = ((CubeFaceDir)childFaceDir).Forward();
            var right = Vector3.Cross(forward, up);


            var up2 = ((CubeFaceDir)parentFaceDir).Up();
            var forward2 = ((CubeFaceDir)parentFaceDir).Forward();
            var right2 = Vector3.Cross(forward2, up2);

            Vector3 v1, v2;

            switch (paintMode)
            {
                case PaintMode.Pencil:
                    return childOffset == parentOffset && childFaceDir == parentFaceDir && p1 == p2;
                case PaintMode.Vertex:
                    v1 = childOffset * 2 + forward + right * p1.x + up * p1.y;
                    v2 = parentOffset * 2 + forward2 + right2 * p2.x + up2 * p2.y;
                    return (v1 - v2).sqrMagnitude < 0.01;
                case PaintMode.Edge:
                    var v = forward2 + p2.x * right2 + p2.y * (Vector3)up2;
                    var isX = Math.Abs(Vector3.Dot(v, right));
                    var isY = Math.Abs(Vector3.Dot(v, up));
                    v1 = childOffset * 2 + forward + right * p1.x * isX + (Vector3)up * p1.y * isY;
                    v2 = parentOffset * 2 + forward2 + right2 * p2.x + up2 * p2.y;
                    return (v1 - v2).sqrMagnitude < 0.01;
                case PaintMode.Face:
                case PaintMode.Add:
                    return childOffset == parentOffset && childFaceDir == parentFaceDir;
                case PaintMode.Remove:
                    return childOffset == parentOffset;
            }
            throw new Exception();
            */

            switch (paintMode)
            {
                case PaintMode.Pencil:
                    return parentOffset == childOffset && parentFaceDir == childFaceDir && parentSubface == childSubface;
                case PaintMode.Face:
                case PaintMode.Add:
                    return childOffset == parentOffset && childFaceDir == parentFaceDir;
                case PaintMode.Remove:
                    return childOffset == parentOffset;
                case PaintMode.Vertex:
                    return Canonical(parentOffset, parentFaceDir, parentSubface, paintMode) == Canonical(childOffset, childFaceDir, childSubface, paintMode);
                case PaintMode.Edge:
                    var v1 = Canonical(parentOffset, parentFaceDir, parentSubface, paintMode, out var faceCenter, out var forward);
                    var v2 = Canonical(childOffset, childFaceDir, childSubface, paintMode, out var childFaceCenter, out var childForward);

                    var d = v1 - v2;
                    var towardsEdge = v1 - faceCenter;
                    var alongEdge = Vector3.Cross(Vector3.forward, towardsEdge);

                    return
                        // v2 must line on an infinite line defined by the edge
                        Math.Abs(Vector3.Dot(d, towardsEdge)) < 1e-6 &&
                        Math.Abs(Vector3.Dot(d, forward)) < 1e-6 &&
                        // And it must be on a face that borders the edge
                        Math.Abs(Vector3.Dot(childForward, alongEdge)) < 1e-6 &&
                        Math.Abs(Vector3.Dot(faceCenter - childFaceCenter, alongEdge)) < 1e-6;
            }
            throw new Exception();
        }

        private Vector3 Canonical(Vector3Int offset, CellFaceDir faceDir, SubFace subFace, PaintMode paintMode)
        {
            return Canonical(offset, faceDir, subFace, paintMode, out var _, out var _2);
        }

        private Vector3 Canonical(Vector3Int offset, CellFaceDir faceDir, SubFace subFace, PaintMode paintMode, out Vector3 faceCenter, out Vector3 forward)
        {
            var c2 = Canonical2(subFace);
            forward = ((SquareFaceDir)faceDir).Forward();
            GetFaceCenterAndNormal(offset, faceDir, Vector3.zero, new Vector3(2, 2, 0), out faceCenter, out _);
            var right = -Vector3.Cross(Vector3.forward, forward);
            return faceCenter + forward + right * c2;
        }

        private int Canonical2(SubFace subFace)
        {
            switch (subFace)
            {
                case SubFace.Left: return -1;
                case SubFace.Center: return 0;
                case SubFace.Right: return 1;
            }
            throw new Exception();
        }



        public IEnumerable<SubFace> GetSubFaces(Vector3Int offset, CellFaceDir dir)
        {
            yield return SubFace.Left;
            yield return SubFace.Center;
            yield return SubFace.Right;
        }

        public void SetSubFaceValue(FaceDetails faceDetails, SubFace subface, int value)
        {
            switch (subface)
            {
                case SubFace.Left: faceDetails.left = value; break;
                case SubFace.Center: faceDetails.center = value; break;
                case SubFace.Right: faceDetails.right = value; break;
            }
        }

        public int GetSubFaceValue(FaceDetails faceDetails, SubFace subface)
        {
            switch (subface)
            {
                case SubFace.Left: return faceDetails.left;
                case SubFace.Center: return faceDetails.center;
                case SubFace.Right: return faceDetails.right;
            }
            return 0;
        }

        public Vector3Int Move(Vector3Int offset, CellFaceDir dir)
        {
            return offset + ((SquareFaceDir)dir).Forward();
        }

        private static float Max(Vector3 v)
        {
            return Mathf.Max(v.x, v.y, v.z);
        }

        private static float Min(Vector3 v)
        {
            return Mathf.Min(v.x, v.y, v.z);
        }

        private struct DrawingUtil
        {
            public Vector3 center;
            public Vector3 size;
            public Vector3 hsize;

            public DrawingUtil(Vector3 center, Vector3 tileSize)
            {
                this.center = center;
                this.size = tileSize;
                this.hsize = size * 0.5f;
            }


            public Vector3 ScaleByHSize(Vector3 v)
            {
                return new Vector3(v.x * hsize.x, v.y * hsize.y, v.z * hsize.z);
            }

            public Vector3 ScaleBySize(Vector3 v)
            {
                return new Vector3(v.x * size.x, v.y * size.y, v.z * size.z);
            }
        }
    }
}