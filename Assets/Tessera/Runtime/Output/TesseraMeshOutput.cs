using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{

    /// <summary>
    /// Attach this to a TesseraGenerator to output the tiles to a single mesh instead of instantiating them.
    /// > [!Note]
    /// > This class is available only in Tessera Pro
    /// </summary>
    // TODO: Manually handle mesh serialization for undo
    // https://answers.unity.com/questions/607527/is-this-possible-to-apply-undo-to-meshfiltermesh.html
    [RequireComponent(typeof(TesseraGenerator))]
    [AddComponentMenu("Tessera/Tessera Mesh Output", 40)]
    public class TesseraMeshOutput : MonoBehaviour, ITesseraTileOutput
    {
        public MeshFilter targetMeshFilter;

        private Dictionary<Vector3Int, TesseraTileInstance> instances = new Dictionary<Vector3Int, TesseraTileInstance>();
        private HashSet<object> seenMaterials = new HashSet<object>();

        public bool IsEmpty => targetMeshFilter == null || targetMeshFilter.sharedMesh == null;

        public bool SupportsIncremental => true;

        public void ClearTiles(IEngineInterface engine)
        {
            targetMeshFilter.mesh = null;
            instances = new Dictionary<Vector3Int, TesseraTileInstance>();
            seenMaterials = new HashSet<object>();
        }

        private object CanonicalizeMaterial(Material m)
        {
            return m?.ToString() ?? "null";
        }

        // For a given subObject of a tile with a transform, relative to the tile,
        // extract the part of the mesh corresponding to material
        // and return it.
        private CombineInstance? GetCombineInstance(TesseraTileInstance i, GameObject subObject, Matrix4x4 transform, object material)
        {
            var generator = GetComponent<TesseraGenerator>();

            var meshFilter = subObject.GetComponent<MeshFilter>();
            var meshRenderer = subObject.GetComponent<MeshRenderer>();
            if (meshFilter == null)
            {
                return null;
            }
            if (meshRenderer == null)
            {
                throw new Exception($"Expected MeshRenderer to accompany MeshFilter on {subObject}");
            }
            var mesh = meshFilter.sharedMesh;
            if(mesh == null)
            {
                return null;
            }
            var materials = meshRenderer.sharedMaterials.Select(CanonicalizeMaterial).ToList();
            var subMeshIndex = materials.ToList().IndexOf(material);
            if (subMeshIndex < 0)
            {
                return null;
            }

            if (generator.surfaceMesh != null)
            {
                // Make new mesh
                var newMesh = (i.MeshDeformation * transform).Transform(mesh, subMeshIndex);
                return new CombineInstance
                {
                    mesh = newMesh,
                    transform = Matrix4x4.identity,
                    subMeshIndex = 0,
                };
            }
            else
            {
                return new CombineInstance
                {
                    mesh = mesh,
                    transform = Matrix4x4.TRS(i.Position, i.Rotation, i.LocalScale) * transform,
                    subMeshIndex = subMeshIndex,
                };
            }
        }

        private IList<object> GetMaterialsList(IEnumerable<TesseraTileInstance> tileInstances)
        {
            // Work out material indices
            var allMaterials = seenMaterials;
            foreach (var i in tileInstances)
            {
                void GetMaterials(GameObject subObject)
                {
                    var meshFilter = subObject.GetComponent<MeshFilter>();
                    var meshRenderer = subObject.GetComponent<MeshRenderer>();
                    if (meshFilter == null)
                    {
                        return;
                    }
                    if (meshRenderer == null)
                    {
                        throw new Exception($"Expected MeshRenderer to accompany MeshFilter on {subObject}");
                    }
                    var materials = meshRenderer.sharedMaterials;
                    foreach (var m in materials)
                    {
                        allMaterials.Add(CanonicalizeMaterial(m));
                    }

                }
                if (i.Tile.instantiateChildrenOnly)
                {
                    foreach (Transform child in i.Tile.transform)
                    {
                        GetMaterials(child.gameObject);
                    }
                }
                else
                {
                    GetMaterials(i.Tile.gameObject);
                }
            }
            var allMaterialsList = allMaterials.ToList();
            var targetRenderer = targetMeshFilter.GetComponent<MeshRenderer>();
            if (targetRenderer)
            {
                foreach (var material in targetRenderer.sharedMaterials.Reverse())
                {
                    var m = CanonicalizeMaterial(material);
                    var i = allMaterialsList.IndexOf(m);
                    if (i >= 0)
                    {
                        allMaterialsList.RemoveAt(i);
                        allMaterialsList.Insert(0, m);
                    }
                }
            }
            return allMaterialsList;
        }

        private IEnumerable<CombineInstance> GetCombineInstances(TesseraTileInstance i, object material)
        {
            if (i.Tile.instantiateChildrenOnly)
            {
                foreach (Transform child in i.Tile.transform)
                {
                    var ci = GetCombineInstance(i, child.gameObject, Matrix4x4.TRS(child.localPosition, child.localRotation, child.localScale), material);
                    if (ci is CombineInstance combineInstance)
                    {
                        //combineInstance.transform = targetMeshFilter.transform.worldToLocalMatrix * combineInstance.transform;
                        if (combineInstance.mesh.vertexCount > 0)
                        {
                            yield return combineInstance;
                        }
                    }
                }
            }
            else
            {
                var ci = GetCombineInstance(i, i.Tile.gameObject, Matrix4x4.identity, material);
                if (ci != null)
                {
                    yield return ci.Value;
                }
            }
        }

        public void UpdateTiles(TesseraCompletion completion, IEngineInterface engine)
        {
            var allMaterialsList = GetMaterialsList(completion.tileInstances);

            // Update combineMeshInstances
            foreach (var i in completion.tileInstances)
            {
                foreach (var cell in i.Cells)
                {
                    instances.Remove(cell);
                }
                if(i.Tile == null)
                {
                    continue;
                }

                instances[i.Cells.First()] = i;
            }

            // Get combineMeshInstances
            //var combineMeshInstances = instances.ToDictionary(kv => kv.Key, kv => GetCombineInstances(kv.Value, allMaterialsList));

            // Convert combineMeshInstances into a mesh per material
            var allCombine = new List<CombineInstance>();
            foreach (var material in allMaterialsList)
            {
                var combineInstances = new List<CombineInstance>();
                foreach (var kv in instances)
                {
                    combineInstances.AddRange(
                        GetCombineInstances(kv.Value, material).Where(x => x.mesh != null));
                }
                if (combineInstances.Count > 0)
                {
                    var outputMesh = new Mesh();
                    outputMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                    outputMesh.CombineMeshes(combineInstances.ToArray(), true);
                    allCombine.Add(new CombineInstance
                    {
                        mesh = outputMesh,
                        transform = Matrix4x4.identity,
                    });
                }
            }

            // Combine all of those into a single mesh with submeshes
            var finalMesh = new Mesh();
            finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            finalMesh.CombineMeshes(allCombine.ToArray(), false);
            targetMeshFilter.mesh = finalMesh;
        }
    }
        }
