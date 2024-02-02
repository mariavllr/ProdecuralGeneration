using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{
    public class TrianglePrismCellDrawingType : ICellDrawingType
    {
        public static TrianglePrismCellDrawingType Instance = new TrianglePrismCellDrawingType();

        public bool Is2D => false;

        private bool IsUpDown(CellFaceDir dir) => ((TrianglePrismFaceDir)dir).IsUpDown();

        private Vector3 GetForward(CellFaceDir dir) => ((TrianglePrismFaceDir)dir).Forward();


        public void GetFaceCenterAndNormal(Vector3Int offset, CellFaceDir dir, Vector3 center, Vector3 tileSize, out Vector3 faceCenter, out Vector3 faceNormal)
        {
            faceNormal = GetForward(dir);
            faceCenter = TrianglePrismGeometryUtils.GetCellCenter(offset, center, tileSize) + 0.5f * faceNormal * (IsUpDown(dir) ? tileSize.y : tileSize.x);
        }

        public Vector3[] GetFaceVertices(Vector3Int offset, CellFaceDir dir, Vector3 center, Vector3 tileSize)
        {
            var up = ((TrianglePrismFaceDir)dir).Up();
            GetFaceCenterAndNormal(offset, dir, center, tileSize, out var faceCenter, out var forward);
            var right = Vector3.Cross(forward, up);

            var innerRadius = 0.5f * tileSize.x;
            var outerRadius = tileSize.x;
            var side = Mathf.Sqrt(3) * tileSize.x;

            if (IsUpDown(dir))
            {
                if(TrianglePrismGeometryUtils.PointsUp(offset))
                {
                    var v1 = faceCenter + outerRadius * up;
                    var v2 = faceCenter - innerRadius * up + 0.5f * side * right;
                    var v3 = faceCenter - innerRadius * up - 0.5f * side * right;
                    return new[] { v1, v2, v3, };
                }
                else
                {
                    var v1 = faceCenter - outerRadius * up;
                    var v2 = faceCenter + innerRadius * up + 0.5f * side * right;
                    var v3 = faceCenter + innerRadius * up - 0.5f * side * right;
                    return new[] { v1, v2, v3, };
                }
            }
            else
            {
                return SquareFaceDrawingUtils.GetFaceVertices(faceCenter, 0.5f * up * tileSize.y, 0.5f * Mathf.Sqrt(3) * tileSize.x * right);
            }
        }

        public Vector3[] GetSubFaceVertices(Vector3Int offset, CellFaceDir dir, SubFace subface, Vector3 center, Vector3 tileSize)
        {
            var up = ((TrianglePrismFaceDir)dir).Up();
            GetFaceCenterAndNormal(offset, dir, center, tileSize, out var faceCenter, out var forward);
            var right = Vector3.Cross(forward, up);

            var innerRadius = 0.5f * tileSize.x;
            var outerRadius = tileSize.x;
            var side = Mathf.Sqrt(3) * tileSize.x;

            var t = 1 / 3.0f;


            if (IsUpDown(dir))
            {
                var (subfaceType, i1, i2) = Unpack(offset, subface);

                var upness = dir == (CellFaceDir)TrianglePrismFaceDir.Up ? 1 : -1;
                var pointsUp = TrianglePrismGeometryUtils.PointsUp(offset);

                switch (subfaceType)
                {
                    case SubFaceType.Center:
                        {
                            if (pointsUp)
                            {
                                var v1 = faceCenter + t * outerRadius * up;
                                var v2 = faceCenter - t * innerRadius * up + t * 0.5f * side * right;
                                var v3 = faceCenter - t * innerRadius * up - t * 0.5f * side * right;
                                return new[] { v1, v2, v3, };
                            }
                            else
                            {
                                var v1 = faceCenter - t * outerRadius * up;
                                var v2 = faceCenter + t * innerRadius * up + t * 0.5f * side * right;
                                var v3 = faceCenter + t * innerRadius * up - t * 0.5f * side * right;
                                return new[] { v1, v2, v3, };
                            }
                        }
                    case SubFaceType.Vertex:
                        {
                            var v1 = t * 0.5f * side * up + t * innerRadius * right;
                            var v2 = t * 0.5f * side * up + innerRadius * right;
                            var v3 = 0.5f * side * up + innerRadius * right;
                            var v4 = -t * 0.5f * side * up + innerRadius * right;

                            var r = Quaternion.Euler(0, -upness * (60 * i1 - 150), 0);
                            var r2 = Quaternion.Euler(0, -upness * (60 * (2 + i1) - 150), 0);
                            return new[] {
                                faceCenter + r * v1,
                                faceCenter + r * v2,
                                faceCenter + r * v3,
                                faceCenter + r2 * v4,
                            };
                        }
                    case SubFaceType.Edge:
                        {
                            var v1 = -t * 0.5f * side * up + t * innerRadius * right;
                            var v2 = +t * 0.5f * side * up + t * innerRadius * right;
                            var v3 = +t * 0.5f * side * up + innerRadius * right;
                            var v4 = -t * 0.5f * side * up + innerRadius * right;
                            var r = Quaternion.Euler(0, -upness * (60 * i1 - 30), 0);
                            return new[] {
                                faceCenter + r * v1,
                                faceCenter + r * v2,
                                faceCenter + r * v3,
                                faceCenter + r * v4,
                            };
                        }
                    default:
                        throw new Exception();
                }
            }
            else
            {
                return SquareFaceDrawingUtils.GetSubFaceVertices(subface, faceCenter, 0.5f * up * tileSize.y, 0.5f * Mathf.Sqrt(3) * tileSize.x * right);
            }
        }

        enum SubFaceType
        {
            Center,
            Vertex,
            Edge,
        }

        // TODO: Could hex/cube drawing also be simplified via a similar Unpack
        // Reutrns the subface type, and the vertices associated with it.
        private (SubFaceType, int, int) Unpack(Vector3Int offset, SubFace subface)
        {
            var pointsUp = TrianglePrismGeometryUtils.PointsUp(offset);

            if (subface == SubFace.TriangleCenter)
            {
                return (SubFaceType.Center, 0, 0);
            }
            if (pointsUp)
            {
                switch (subface)
                {
                    case SubFace.TriangleBottom:
                        return (SubFaceType.Edge, 5, 1);
                    case SubFace.TriangleTopRight:
                        return (SubFaceType.Edge, 1, 3);
                    case SubFace.TriangleTopLeft:
                        return (SubFaceType.Edge, 3, 5);
                    case SubFace.TriangleTop:
                        return (SubFaceType.Vertex, 3, 0);
                    case SubFace.TriangleBottomLeft:
                        return (SubFaceType.Vertex, 5, 0);
                    case SubFace.TriangleBottomRight:
                        return (SubFaceType.Vertex, 1, 0);
                    default:
                        throw new Exception();
                }
            }
            else
            {
                switch (subface)
                {
                    case SubFace.TriangleBottom:
                        return (SubFaceType.Vertex, 0, 0);
                    case SubFace.TriangleTopRight:
                        return (SubFaceType.Vertex, 2, 0);
                    case SubFace.TriangleTopLeft:
                        return (SubFaceType.Vertex, 4, 0);
                    case SubFace.TriangleTop:
                        return (SubFaceType.Edge, 2, 4);
                    case SubFace.TriangleBottomLeft:
                        return (SubFaceType.Edge, 4, 0);
                    case SubFace.TriangleBottomRight:
                        return (SubFaceType.Edge, 0, 2);
                    default:
                        throw new Exception();
                }
            }
        }

        private Vector3 Locate(Vector3Int offset, CellFaceDir faceDir, SubFace subface)
        {
            return Locate(offset, faceDir, subface, out var _, out var _);
        }

        private Vector3 Locate(Vector3Int offset, CellFaceDir faceDir, SubFace subface, out Vector3 faceCenter, out Vector3 forward)
        {
            GetFaceCenterAndNormal(offset, faceDir, Vector3.zero, Vector3.one, out faceCenter, out forward);
            var up = ((TrianglePrismFaceDir)faceDir).Up();
            var right = Vector3.Cross(forward, up);

            var p = faceCenter;
            switch(subface)
            {
                case SubFace.TopLeft:     p += ( 1) * up * 0.5f + (-1) * right * Mathf.Sqrt(3) / 2; break;
                case SubFace.Top:         p += ( 1) * up * 0.5f + ( 0) * right * Mathf.Sqrt(3) / 2; break;
                case SubFace.TopRight:    p += ( 1) * up * 0.5f + ( 1) * right * Mathf.Sqrt(3) / 2; break;
                case SubFace.Left:        p += ( 0) * up * 0.5f + (-1) * right * Mathf.Sqrt(3) / 2; break;
                case SubFace.Center:      p += ( 0) * up * 0.5f + ( 0) * right * Mathf.Sqrt(3) / 2; break;
                case SubFace.Right:       p += ( 0) * up * 0.5f + ( 1) * right * Mathf.Sqrt(3) / 2; break;
                case SubFace.BottomLeft:  p += (-1) * up * 0.5f + (-1) * right * Mathf.Sqrt(3) / 2; break;
                case SubFace.Bottom:      p += (-1) * up * 0.5f + ( 0) * right * Mathf.Sqrt(3) / 2; break;
                case SubFace.BottomRight: p += (-1) * up * 0.5f + ( 1) * right * Mathf.Sqrt(3) / 2; break;
            }
            var pointsUp = TrianglePrismGeometryUtils.PointsUp(offset);
            if (pointsUp)
            {
                switch(subface)
                {
                    case SubFace.TriangleBottomLeft: p += -0.5f * up - right * Mathf.Sqrt(3) / 2; break;
                    case SubFace.TriangleBottom: p += -0.5f * up; break;
                    case SubFace.TriangleBottomRight: p += -0.5f * up + right * Mathf.Sqrt(3) / 2; break;
                    case SubFace.TriangleTopLeft: p += 0.25f * up - right * Mathf.Sqrt(3) / 4;break;
                    case SubFace.TriangleTop: p += 1f * up;break;
                    case SubFace.TriangleTopRight: p += 0.25f * up + right * Mathf.Sqrt(3) / 4;break;
                }
            }
            else
            {

                switch (subface)
                {
                    case SubFace.TriangleTopLeft: p += 0.5f * up - right * Mathf.Sqrt(3) / 2; break;
                    case SubFace.TriangleTop: p += 0.5f * up; break;
                    case SubFace.TriangleTopRight: p += 0.5f * up + right * Mathf.Sqrt(3) / 2; break;
                    case SubFace.TriangleBottomLeft: p += -0.25f * up - right * Mathf.Sqrt(3) / 4; break;
                    case SubFace.TriangleBottom: p += -1f * up; break;
                    case SubFace.TriangleBottomRight: p += -0.25f * up + right * Mathf.Sqrt(3) / 4; break;
                }
            }
            return p;
        }

        private bool Near(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b) < 1e-6;
        }

        public bool IsAffected(Vector3Int parentOffset, CellFaceDir parentFaceDir, SubFace parentSubface, Vector3Int childOffset, CellFaceDir childFaceDir, SubFace childSubface, PaintMode paintMode)
        {
            switch(paintMode)
            {
                case PaintMode.Pencil:
                    return parentOffset == childOffset && parentFaceDir == childFaceDir && parentSubface == childSubface;
                case PaintMode.Face:
                case PaintMode.Add:
                    return childOffset == parentOffset && childFaceDir == parentFaceDir;
                case PaintMode.Remove:
                    return childOffset == parentOffset;
                case PaintMode.Edge:
                    var v1 = Locate(parentOffset, parentFaceDir, parentSubface, out var faceCenter, out var forward);
                    var v2 = Locate(childOffset, childFaceDir, childSubface, out var childFaceCenter, out var childForward);

                    var d = v1 - v2;
                    var towardsEdge = v1 - faceCenter;
                    var alongEdge = Vector3.Cross(forward, towardsEdge);

                    return
                        // v2 must line on an infinite line defined by the edge
                        Math.Abs(Vector3.Dot(d, towardsEdge)) < 1e-6 &&
                        Math.Abs(Vector3.Dot(d, forward)) < 1e-6 &&
                        // And it must be on a face that borders the edge
                        Math.Abs(Vector3.Dot(childForward, alongEdge)) < 1e-6 &&
                        Math.Abs(Vector3.Dot(faceCenter - childFaceCenter, alongEdge)) < 1e-6;

                case PaintMode.Vertex:
                    return Near(Locate(parentOffset, parentFaceDir, parentSubface), Locate(childOffset, childFaceDir, childSubface));
                default:
                    return false;
            }
            throw new Exception();
        }

        public RaycastCellHit Raycast(Ray ray, Vector3Int offset, Vector3 center, Vector3 tileSize, float? minDistance, float? maxDistance)
        {
            var bestT1 = 0f;
            var bestT2 = float.PositiveInfinity;
            CellFaceDir? bestFace = null;

            void TestFace(TrianglePrismFaceDir faceDir)
            {
                GetFaceCenterAndNormal(offset, (CellFaceDir)(faceDir), center, tileSize, out var faceCenter, out var forward);
                var d1 = Vector3.Dot(faceCenter - ray.origin, forward);
                var d2 = Vector3.Dot(ray.direction, forward);
                var t = d1 / d2;
                if (d2 < 0)
                {
                    if (t > bestT1)
                    {
                        bestT1 = t;
                        bestFace = (CellFaceDir)(faceDir);
                    }
                }
                else
                {
                    if(t < bestT2)
                    {
                        bestT2 = t;
                    }
                }
            }

            var pointsUp = TrianglePrismGeometryUtils.PointsUp(offset);


            TestFace(TrianglePrismFaceDir.Up);
            TestFace(TrianglePrismFaceDir.Down);
            if (pointsUp)
            {
                TestFace(TrianglePrismFaceDir.Back);
                TestFace(TrianglePrismFaceDir.ForwardRight);
                TestFace(TrianglePrismFaceDir.ForwardLeft);
            }
            else
            {
                TestFace(TrianglePrismFaceDir.Forward);
                TestFace(TrianglePrismFaceDir.BackLeft);
                TestFace(TrianglePrismFaceDir.BackRight);
            }
            if (bestFace == null)
            {
                return null;
            }
            if(bestT2 < bestT1)
            {
                return null;
            }
            if(bestT1 > maxDistance)
            {
                return null;
            }
            if(bestT1 < minDistance)
            {
                return null;
            }

            {
                var up = ((TrianglePrismFaceDir)bestFace.Value).Up();
                GetFaceCenterAndNormal(offset, bestFace.Value, center, tileSize, out var faceCenter, out var forward);
                var right = Vector3.Cross(forward, up);

                var point = ray.origin + bestT1 * ray.direction;

                var p = point - faceCenter;

                var isUpDown = IsUpDown(bestFace.Value);

                return new RaycastCellHit
                {
                    dir = bestFace.Value,
                    point = point,
                    
                    subface = new Vector2(
                        Vector3.Dot(p, right) / (isUpDown ? tileSize.x * 0.5f : tileSize.x * Mathf.Sqrt(3) / 2),
                        Vector3.Dot(p, up) / (isUpDown ? tileSize.x * 0.5f : tileSize.y * 0.5f)
                        ),
                };
            }
        }

        public SubFace RoundSubFace(Vector3Int offset, CellFaceDir dir, Vector2 subface, PaintMode paintMode)
        {
            if(IsUpDown(dir))
            {
                var pointsUp = TrianglePrismGeometryUtils.PointsUp(offset);
                if (paintMode == PaintMode.Vertex)
                {
                    if (pointsUp)
                    {
                        var angle = Mathf.Atan2(subface.y, subface.x);
                        var angleInt = Mathf.RoundToInt((angle) / (Mathf.PI * 2 / 3));
                        if (angleInt < 0)
                        {
                            angleInt += 3;
                        }
                        return (SubFace)((int)(SubFace.TriangleBottomRight) + 2 * angleInt);
                    }
                    else
                    {
                        var angle = Mathf.Atan2(subface.y, subface.x);
                        var angleInt = Mathf.RoundToInt((angle + Mathf.PI / 3) / (Mathf.PI * 2 / 3));
                        if (angleInt < 0)
                        {
                            angleInt += 3;
                        }
                        return (SubFace)((int)(SubFace.TriangleBottom) + 2 * angleInt);

                    }
                }
                else
                {
                    // Probably more efficient ways to do this
                    var angle = Mathf.Atan2(subface.y, subface.x);
                    var angleInt = Mathf.RoundToInt((angle + Mathf.PI * (pointsUp ? -5 : -7) / 6) / (Mathf.PI * 2 / 3));
                    subface = Quaternion.Euler(0, 0, -120 * angleInt + (pointsUp ? 120 + 90 : 150 )) * subface;
                    if (angleInt < 0)
                    {
                        angleInt += 3;
                    }
                    if (paintMode == PaintMode.Edge)
                    {
                        return (SubFace)((int)(pointsUp ? SubFace.TriangleBottom : SubFace.TriangleBottomRight) + 2 * ((angleInt + 2) % 3));
                    }
                    if (subface.x < 1 / 3f)
                    {
                        return SubFace.TriangleCenter;
                    }

                    var t = 1 / Mathf.Sqrt(3);
                    if (pointsUp)
                    {
                        if (subface.y > t)
                        {
                            return (SubFace)((int)(SubFace.TriangleBottomRight) + 2 * ((angleInt + 2) % 3));
                        }
                        if (subface.y < -t)
                        {
                            return (SubFace)((int)(SubFace.TriangleBottomRight) + 2 * ((angleInt + 1) % 3));
                        }
                    }
                    else
                    {
                        if (subface.y > t)
                        {
                            return (SubFace)((int)(SubFace.TriangleBottom) + 2 * ((angleInt + 0) % 3));
                        }
                        if (subface.y < -t)
                        {
                            return (SubFace)((int)(SubFace.TriangleBottom) + 2 * ((angleInt + 2) % 3));
                        }
                    }
                    return (SubFace)((int)(pointsUp ? SubFace.TriangleBottom : SubFace.TriangleBottomRight) + 2 * ((angleInt + 2) % 3) );
                }

            }
            else
            {
                return SquareFaceDrawingUtils.RoundSubFace(subface, paintMode);
            }
        }

        public IEnumerable<SubFace> GetSubFaces(Vector3Int offset, CellFaceDir dir)
        {
            if(IsUpDown(dir))
            {
                yield return SubFace.TriangleBottom;
                yield return SubFace.TriangleBottomRight;
                yield return SubFace.TriangleTopRight;
                yield return SubFace.TriangleTop;
                yield return SubFace.TriangleTopLeft;
                yield return SubFace.TriangleBottomLeft;
                yield return SubFace.TriangleCenter;
            }
            else
            {
                yield return SubFace.BottomLeft;
                yield return SubFace.Bottom;
                yield return SubFace.BottomRight;
                yield return SubFace.Left;
                yield return SubFace.Center;
                yield return SubFace.Right;
                yield return SubFace.TopLeft;
                yield return SubFace.Top;
                yield return SubFace.TopRight;
            }
        }

        public void SetSubFaceValue(FaceDetails faceDetails, SubFace subface, int color)
        {
            switch (subface)
            {
                case SubFace.BottomLeft: faceDetails.bottomLeft = color; return;
                case SubFace.Bottom: faceDetails.bottom = color; return;
                case SubFace.BottomRight: faceDetails.bottomRight= color; return;
                case SubFace.Left: faceDetails.left = color; return;
                case SubFace.Center: faceDetails.center = color; return;
                case SubFace.Right: faceDetails.right = color; return;
                case SubFace.TopLeft: faceDetails.topLeft = color; return;
                case SubFace.Top: faceDetails.top = color; return;
                case SubFace.TopRight: faceDetails.topRight = color; return;
                case SubFace.TriangleTop: faceDetails.top = color; return;
                case SubFace.TriangleTopRight: faceDetails.topRight = color; return;
                case SubFace.TriangleTopLeft: faceDetails.topLeft = color; return;
                case SubFace.TriangleBottom: faceDetails.bottom = color; return;
                case SubFace.TriangleBottomLeft: faceDetails.bottomLeft = color; return;
                case SubFace.TriangleBottomRight: faceDetails.bottomRight = color; return;
                case SubFace.TriangleCenter: faceDetails.center = color; return;
            }
            throw new Exception();
        }

        public int GetSubFaceValue(FaceDetails faceDetails, SubFace subface)
        {
            switch (subface)
            {
                case SubFace.BottomLeft: return faceDetails.bottomLeft;
                case SubFace.Bottom: return faceDetails.bottom;
                case SubFace.BottomRight: return faceDetails.bottomRight;
                case SubFace.Left: return faceDetails.left;
                case SubFace.Center: return faceDetails.center;
                case SubFace.Right: return faceDetails.right;
                case SubFace.TopLeft: return faceDetails.topLeft;
                case SubFace.Top: return faceDetails.top;
                case SubFace.TopRight: return faceDetails.topRight;
                case SubFace.TriangleTop: return faceDetails.top;
                case SubFace.TriangleTopRight: return faceDetails.topRight;
                case SubFace.TriangleTopLeft: return faceDetails.topLeft;
                case SubFace.TriangleBottom: return faceDetails.bottom;
                case SubFace.TriangleBottomLeft: return faceDetails.bottomLeft;
                case SubFace.TriangleBottomRight: return faceDetails.bottomRight;
                case SubFace.TriangleCenter: return faceDetails.center;
            }
            throw new Exception($"{subface}");
        }

        public Vector3Int Move(Vector3Int offset, CellFaceDir dir)
        {
            return offset + ((TrianglePrismFaceDir)dir).OffsetDelta();
        }

    }
}