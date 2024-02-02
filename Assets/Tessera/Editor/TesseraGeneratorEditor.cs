using UnityEditor;
using UnityEngine;
using System;
using UnityEditorInternal;
using UnityEditor.IMGUI.Controls;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace Tessera
{
    [CustomEditor(typeof(TesseraGenerator))]
    public class TesseraGeneratorEditor : Editor
    {
        private const string ChangeBounds = "Change Bounds";

        private class CustomHandle : BoxBoundsHandle
        {
            public TesseraGenerator generator;

            protected override Bounds OnHandleChanged(
                PrimitiveBoundsHandle.HandleDirection handle,
                Bounds boundsOnClick,
                Bounds newBounds)
            {
                // Enforce minimum size for bounds
                // And ensure it is property quantized
                switch (handle)
                {
                    case HandleDirection.NegativeX:
                    case HandleDirection.NegativeY:
                    case HandleDirection.NegativeZ:
                        newBounds.min = Vector3.Min(newBounds.min, newBounds.max - generator.tileSize);
                        newBounds.min = Round(newBounds.min - newBounds.max) + newBounds.max;
                        break;
                    case HandleDirection.PositiveX:
                    case HandleDirection.PositiveY:
                    case HandleDirection.PositiveZ:
                        newBounds.max = Vector3.Max(newBounds.max, newBounds.min + generator.tileSize);
                        newBounds.max = Round(newBounds.max - newBounds.min) + newBounds.min;
                        break;
                }
                Undo.RecordObject(generator, ChangeBounds);

                generator.bounds = newBounds;

                return newBounds;
            }

            Vector3 Round(Vector3 m)
            {
                m.x = generator.tileSize.x * ((int)Math.Round(m.x / generator.tileSize.x));
                m.y = generator.tileSize.y * ((int)Math.Round(m.y / generator.tileSize.y));
                m.z = generator.tileSize.z * ((int)Math.Round(m.z / generator.tileSize.z));
                return m;
            }
        }

        private const string GenerateTiles = "Generate tiles";

        private bool gridShapeToggle = true;
        private bool generationOptionsToggle = true;
        private bool modelOptionsToggle = true;

        private ReorderableList reorderableTileList;

        SerializedProperty list;
        private GUIStyle headerBackground;
        int controlId;

        int selectorIndex = -1;

        const int k_fieldPadding = 2;
        const int k_elementPadding = 5;
        // This can be queried with EditorGUI.GetPropertyHeight. May equal EditorGUIUtility.singleLineHeight?
        const int k_propertyHeight = 18;


        CustomHandle h = new CustomHandle();


        private void OnEnable()
        {
            list = serializedObject.FindProperty("tiles");

            reorderableTileList = new ReorderableList(serializedObject, list, true, false, true, true);

            reorderableTileList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                //Debug.Log("drawElementCallback");
                SerializedProperty targetElement = list.GetArrayElementAtIndex(index);
                if (targetElement.hasVisibleChildren)
                    rect.xMin += 10;
                var tileProperty = targetElement.FindPropertyRelative("tile");
                var weightProperty = targetElement.FindPropertyRelative("weight");
                var tile = (TesseraTileBase)tileProperty.objectReferenceValue;
                var tileName = tile?.gameObject.name ?? "None";

                var tileRect = rect;
                tileRect.height = EditorGUI.GetPropertyHeight(tileProperty);
                var weightRect = rect;
                weightRect.yMin = tileRect.yMax + k_fieldPadding;
                weightRect.height = EditorGUI.GetPropertyHeight(weightProperty);
                EditorGUI.PropertyField(tileRect, tileProperty);
                EditorGUI.PropertyField(weightRect, weightProperty);
            };

            /*
            reorderableTileList.elementHeightCallback = (int index) =>
            {
                Debug.Log("elementHeightCallback");
                SerializedProperty targetElement = list.GetArrayElementAtIndex(index);
                var tileProperty = targetElement.FindPropertyRelative("tile");
                var weightProperty = targetElement.FindPropertyRelative("weight");
                return EditorGUI.GetPropertyHeight(tileProperty) + k_fieldPadding + EditorGUI.GetPropertyHeight(weightProperty) + k_elementPadding;
            };
            */
            reorderableTileList.elementHeight = k_propertyHeight + k_fieldPadding + k_propertyHeight + k_elementPadding;

            reorderableTileList.drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                var styleHighlight = GUI.skin.FindStyle("MeTransitionSelectHead");
                if (focused == false)
                    return;
                //rect.height = reorderableTileList.elementHeightCallback(index);
                rect.height = reorderableTileList.elementHeight;
                GUI.Box(rect, GUIContent.none, styleHighlight);
            };

            reorderableTileList.onAddCallback = l =>
            {
                ++reorderableTileList.serializedProperty.arraySize;
                reorderableTileList.index = reorderableTileList.serializedProperty.arraySize - 1;
                list.GetArrayElementAtIndex(reorderableTileList.index).FindPropertyRelative("weight").floatValue = 1.0f;
                selectorIndex = reorderableTileList.index;
                controlId = EditorGUIUtility.GetControlID(FocusType.Passive);
                EditorGUIUtility.ShowObjectPicker<TesseraTileBase>(this, true, null, controlId);
            };

            var generator = target as TesseraGenerator;

            h.center = generator.center;
            h.size = Vector3.Scale(generator.tileSize, generator.size);
            h.generator = generator;
        }

        public override void OnInspectorGUI()
        {
            var generator = target as TesseraGenerator;

            this.headerBackground = this.headerBackground ?? (GUIStyle)"RL Header";
            serializedObject.Update();

            GridShapeGUI();
            GenerationOptionsGUI();
            ModelOptionsGUI();
            TileListGUI();
            serializedObject.ApplyModifiedProperties();

            Validate();

            EditorUtility.ClearProgressBar();

            var clearable = !generator.GetTileOutput().IsEmpty;

            GUI.enabled = clearable;

            if (GUILayout.Button("Clear"))
            {
                Clear();
            }

            GUI.enabled = true;

            if (GUILayout.Button(clearable ? "Regenerate" : "Generate"))
            {
                // Undo the last generation
                Undo.SetCurrentGroupName(GenerateTiles);
                if (clearable)
                {
                    Clear();
                }

                Generate(generator);
            }
        }

        private static Texture2D s_WarningIcon;


        internal static Texture2D warningIcon
        {
            get
            {
                return s_WarningIcon ?? (s_WarningIcon = (Texture2D)EditorGUIUtility.IconContent("console.warnicon").image);
            }
        }

        internal static bool HelpBoxWithButton(string message, MessageType type)
        {
            return HelpBoxWithButton(message, "Fix it!", type);
        }

        internal static bool HelpBoxWithButton(string message, string buttonMessage, MessageType type)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(new GUIContent(message, warningIcon));
            var r = GUILayout.Button(buttonMessage);
            EditorGUILayout.EndVertical();
            return r;
        }

        private void SetReadable(GameObject go, Mesh mesh)
        {
            if (mesh == null) return;
            if (mesh.isReadable) return;
            var path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Unable to find asset for a mesh on {go}");
                return;
            }
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogWarning($"Unable to find model importer for asset {path}");
                return;
            }
            Debug.Log($"Updating import settings for asset {path}");
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        private void SetTangents(GameObject go, Mesh mesh)
        {
            if (mesh == null) return;
            var path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"Unable to find asset for a mesh on {go}");
                return;
            }
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogWarning($"Unable to find model importer for asset {path}");
                return;
            }
            Debug.Log($"Updating import settings for asset {path}");
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.SaveAndReimport();
        }

        // Should mirror TesseraGenerator.Validate
        private void Validate()
        {
            var generator = target as TesseraGenerator;

            var allTiles = generator.tiles.Select(x => x.tile).Where(x => x != null);
            if (generator.surfaceMesh != null)
            {
                if (generator.surfaceMesh.GetTopology(0) != MeshTopology.Quads && generator.CellType is CubeCellType)
                {
                    if (HelpBoxWithButton($"Mesh topology {generator.surfaceMesh.GetTopology(0)} is not supported with cubes. You need to select \"Keep Quads\" in the import options.", MessageType.Warning))
                    {
                        var path = AssetDatabase.GetAssetPath(generator.surfaceMesh);
                        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                        importer.keepQuads = true;
                        importer.SaveAndReimport();
                    }
                }
                if (generator.surfaceMesh.GetTopology(0) != MeshTopology.Triangles && generator.CellType is TrianglePrismCellType)
                {
                    if (HelpBoxWithButton($"Mesh topology {generator.surfaceMesh.GetTopology(0)} is not supported with triangles. You need to deselect \"Keep Quads\" in the import options.", MessageType.Warning))
                    {
                        var path = AssetDatabase.GetAssetPath(generator.surfaceMesh);
                        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                        importer.keepQuads = false;
                        importer.SaveAndReimport();
                    }
                }
                if (!generator.surfaceMesh.isReadable)
                {
                    if (HelpBoxWithButton($"Surface mesh needs to be readable.", MessageType.Warning))
                    {
                        SetReadable(generator.gameObject, generator.surfaceMesh);
                    }
                }
                //if (!generator.surfaceSmoothNormals && generator.surfaceMesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent))
                if (generator.surfaceSmoothNormals && generator.surfaceMesh.tangents.Length == 0)
                {
                    if (HelpBoxWithButton($"Surface mesh needs tangents to calculate smoothed normals. You need to select \"Calculate\" the tangent field of the import options.", MessageType.Warning))
                    {
                        SetTangents(generator.gameObject, generator.surfaceMesh);
                    }
                }
                var unreadable = allTiles.Where(tile => tile.GetComponentsInChildren<MeshFilter>().Any(mf => !mf.sharedMesh.isReadable)).ToList();
                if (unreadable.Count > 0)
                {
                    if (HelpBoxWithButton($"Some tiles have meshes that are not readable. They will not be transformed to fit the mesh. E.g {unreadable.First().name}", MessageType.Warning))
                    {
                        foreach (var tile in allTiles)
                        {
                            foreach (var mf in tile.GetComponentsInChildren<MeshFilter>())
                            {
                                SetReadable(mf.gameObject, mf.sharedMesh);
                            }
                            foreach (var mc in tile.GetComponentsInChildren<MeshCollider>())
                            {
                                SetReadable(mc.gameObject, mc.sharedMesh);
                            }
                        }
                    }
                }
                if (generator.filterSurfaceSubmeshTiles)
                {
                    for (var i = 0; i < generator.surfaceSubmeshTiles.Count; i++)
                    {
                        if (generator.surfaceSubmeshTiles[i].tiles.Count == 0)
                        {
                            EditorGUILayout.HelpBox($"Submesh {i} is filtered to zero tiles. Generation is impossible", MessageType.Warning);
                        }
                    }
                }

                return;
            }

            if (generator.GetComponent<ITesseraTileOutput>() is TesseraMeshOutput)
            {
                var unreadable = allTiles.Where(tile => tile.GetComponentsInChildren<MeshFilter>().Any(mf => !mf.sharedMesh.isReadable)).ToList();
                if (unreadable.Count > 0)
                {
                    if (HelpBoxWithButton($"Some tiles have meshes that are not readable. They will not be added to the mesh output. E.g {unreadable.First().name}", MessageType.Warning))
                    {
                        foreach (var tile in allTiles)
                        {
                            foreach (var mf in tile.GetComponentsInChildren<MeshFilter>())
                            {
                                SetReadable(mf.gameObject, mf.sharedMesh);
                            }
                            foreach (var mc in tile.GetComponentsInChildren<MeshCollider>())
                            {
                                SetReadable(mc.gameObject, mc.sharedMesh);
                            }
                        }
                    }
                }
            }
            var cellTypes = generator.GetCellTypes();
            if (cellTypes.Count > 1)
            {
                EditorGUILayout.HelpBox($"You cannot mix tiles of multiple cell types, such as {string.Join(", ", cellTypes.Select(x => x.GetType().Name))}.\n", MessageType.Warning);
            }

            var missizedTiles = generator.GetMissizedTiles().ToList();
            if (missizedTiles.Count > 0)
            {
                if (HelpBoxWithButton($"Some tiles do not have the same tileSize as the generator, {generator.tileSize}, this can cause unexpected behaviour.\n" +
                    "NB: Big tiles should still share the same value of tileSize\n" +
                    "Affected tiles:\n" +
                    string.Join("\n", missizedTiles), "Resize tiles", MessageType.Warning))
                {
                    foreach (var tile in missizedTiles)
                    {
                        tile.tileSize = generator.tileSize;
                    }
                }
            }

            var palette = generator.tiles.Select(x => x.tile?.palette).FirstOrDefault();
            var wrongPaletteTiles = allTiles.Where(x => x.palette != palette).ToList();
            if (wrongPaletteTiles.Count > 0)
            {
                if (HelpBoxWithButton($"Some tiles do not all have the same palette. E.g. {wrongPaletteTiles.First().name}", "Fix it", MessageType.Warning))
                {
                    foreach (var tile in wrongPaletteTiles)
                    {
                        tile.palette = palette;
                    }
                }
            }


        }

        private void Clear()
        {
            var generator = target as TesseraGenerator;
            var tileOutput = generator.GetTileOutput();
            tileOutput.ClearTiles(new UnityEditorEngineInterface(ShouldRecordUndo(generator), GenerateTiles));
        }

        private bool ShouldRecordUndo(TesseraGenerator generator)
        {
            return generator.recordUndo && generator.surfaceMesh == null;
        }

        // Wraps generation with a progress bar and cancellation button.
        private void Generate(TesseraGenerator generator)
        {
            var cts = new CancellationTokenSource();
            string progressText = "";
            float progress = 0.0f;

            var tileOutput = generator.GetTileOutput();

            // Mirrors private method TesseraGenerator.HandleComplete
            void OnComplete(TesseraCompletion completion)
            {
                completion.LogErrror();
                if (!completion.success && generator.failureMode == FailureMode.Cancel)
                {
                    return;
                }

                if (tileOutput != null)
                {
                    tileOutput.UpdateTiles(completion, new UnityEditorEngineInterface(ShouldRecordUndo(generator), GenerateTiles));
                }
            }

            var enumerator = generator.StartGenerate(new TesseraGenerateOptions
            {
                onComplete = OnComplete,
                progress = (t, p) => { progressText = t; progress = p; },
                cancellationToken = cts.Token
            });

            var last = DateTime.Now;
            // Update progress this frequently.
            // Too fast and it'll slow down generation.
            var freq = TimeSpan.FromSeconds(0.1);
            try
            {
                while (enumerator.MoveNext())
                {
                    var a = enumerator.Current;
                    if (last + freq < DateTime.Now)
                    {
                        last = DateTime.Now;
                        if (EditorUtility.DisplayCancelableProgressBar("Generating", progressText, progress))
                        {
                            cts.Cancel();
                            EditorUtility.ClearProgressBar();
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            EditorUtility.ClearProgressBar();
            GUIUtility.ExitGUI();
        }

        [DrawGizmo(GizmoType.Selected)]
        static void DrawGizmo(TesseraGenerator generator, GizmoType gizmoType)
        {
            if (generator.surfaceMesh != null)
            {
                var tf = generator.transform;
                if (generator.GetComponent<TesseraMeshOutput>() is var mo && mo?.targetMeshFilter != null)
                {
                    tf = mo.targetMeshFilter.transform;
                }
                var m = tf.localToWorldMatrix;
                var verticies = generator.surfaceMesh.vertices;
                var normals = generator.surfaceMesh.normals;
                var tileHeight = generator.tileSize.y;
                var meshOffset = generator.surfaceOffset;
                var layerCount = generator.size.y;
                for (var submesh = 0; submesh < generator.surfaceMesh.subMeshCount; submesh++)
                {
                    var indices = generator.surfaceMesh.GetIndices(submesh);
                    var meshTopology = generator.surfaceMesh.GetTopology(submesh);
                    if (meshTopology == MeshTopology.Quads)
                    {
                        for (var i = 0; i < indices.Length; i += 4)
                        {
                            var v1 = verticies[indices[i + 0]];
                            var v2 = verticies[indices[i + 1]];
                            var v3 = verticies[indices[i + 2]];
                            var v4 = verticies[indices[i + 3]];
                            var n1 = normals[indices[i + 0]];
                            var n2 = normals[indices[i + 1]];
                            var n3 = normals[indices[i + 2]];
                            var n4 = normals[indices[i + 3]];
                            v1 = v1 + (meshOffset - 0.5f * tileHeight) * n1;
                            v2 = v2 + (meshOffset - 0.5f * tileHeight) * n2;
                            v3 = v3 + (meshOffset - 0.5f * tileHeight) * n3;
                            v4 = v4 + (meshOffset - 0.5f * tileHeight) * n4;
                            v1 = m.MultiplyPoint3x4(v1);
                            v2 = m.MultiplyPoint3x4(v2);
                            v3 = m.MultiplyPoint3x4(v3);
                            v4 = m.MultiplyPoint3x4(v4);
                            Gizmos.DrawLine(v1, v2);
                            Gizmos.DrawLine(v2, v3);
                            Gizmos.DrawLine(v3, v4);
                            Gizmos.DrawLine(v4, v1);
                        }
                    }
                    else if (meshTopology == MeshTopology.Triangles)
                    {
                        for (var i = 0; i < indices.Length; i += 3)
                        {
                            var v1 = verticies[indices[i + 0]];
                            var v2 = verticies[indices[i + 1]];
                            var v3 = verticies[indices[i + 2]];
                            var n1 = normals[indices[i + 0]];
                            var n2 = normals[indices[i + 1]];
                            var n3 = normals[indices[i + 2]];
                            v1 = v1 + (meshOffset - 0.5f * tileHeight) * n1;
                            v2 = v2 + (meshOffset - 0.5f * tileHeight) * n2;
                            v3 = v3 + (meshOffset - 0.5f * tileHeight) * n3;
                            v1 = m.MultiplyPoint3x4(v1);
                            v2 = m.MultiplyPoint3x4(v2);
                            v3 = m.MultiplyPoint3x4(v3);
                            Gizmos.DrawLine(v1, v2);
                            Gizmos.DrawLine(v2, v3);
                            Gizmos.DrawLine(v3, v1);
                        }
                    }
                }
            }
            else if (generator.CellType is HexPrismCellType)
            {
                var tf = generator.transform;
                if (generator.GetComponent<TesseraMeshOutput>() is var mo && mo?.targetMeshFilter != null)
                {
                    tf = mo.targetMeshFilter.transform;
                }
                var m = tf.localToWorldMatrix;
                var dx = generator.size.x * generator.tileSize.x * HexPrismFaceDir.Right.Forward();
                var dz = generator.size.z * generator.tileSize.x * HexPrismFaceDir.ForwardLeft.Forward();
                var dy = generator.size.y * generator.tileSize.y * HexPrismFaceDir.Up.Forward();
                var v1 = generator.origin + 0.5f * generator.tileSize.x * HexPrismFaceDir.BackLeft.Forward() + 0.5f * generator.tileSize.y * HexPrismFaceDir.Down.Forward();
                var v2 = v1 + dx;
                var v3 = v2 + dz;
                var v4 = v1 + dz;
                var v5 = v1 + dy;
                var v6 = v2 + dy;
                var v7 = v3 + dy;
                var v8 = v4 + dy;
                v1 = m.MultiplyPoint3x4(v1);
                v2 = m.MultiplyPoint3x4(v2);
                v3 = m.MultiplyPoint3x4(v3);
                v4 = m.MultiplyPoint3x4(v4);
                v5 = m.MultiplyPoint3x4(v5);
                v6 = m.MultiplyPoint3x4(v6);
                v7 = m.MultiplyPoint3x4(v7);
                v8 = m.MultiplyPoint3x4(v8);
                Gizmos.DrawLine(v1, v2);
                Gizmos.DrawLine(v2, v3);
                Gizmos.DrawLine(v3, v4);
                Gizmos.DrawLine(v4, v1);
                Gizmos.DrawLine(v5, v6);
                Gizmos.DrawLine(v6, v7);
                Gizmos.DrawLine(v7, v8);
                Gizmos.DrawLine(v8, v5);
                Gizmos.DrawLine(v1, v5);
                Gizmos.DrawLine(v2, v6);
                Gizmos.DrawLine(v3, v7);
                Gizmos.DrawLine(v4, v8);
            }
            else if (generator.CellType is TrianglePrismCellType)
            {
                var tf = generator.transform;
                if (generator.GetComponent<TesseraMeshOutput>() is var mo && mo?.targetMeshFilter != null)
                {
                    tf = mo.targetMeshFilter.transform;
                }
                var m = tf.localToWorldMatrix;
                var dx = generator.size.x * generator.tileSize.x * (TrianglePrismFaceDir.ForwardRight.Forward() + TrianglePrismFaceDir.BackRight.Forward()) / 2;
                var dz = generator.size.z * generator.tileSize.x * (TrianglePrismFaceDir.ForwardRight.Forward() + TrianglePrismFaceDir.Forward.Forward());
                var dy = generator.size.y * generator.tileSize.y * TrianglePrismFaceDir.Up.Forward();
                var v1 = generator.origin + generator.tileSize.x * TrianglePrismFaceDir.BackLeft.Forward() + 0.5f * generator.tileSize.y * HexPrismFaceDir.Down.Forward();
                var v2 = v1 + dx;
                var v3 = v2 + dz;
                var v4 = v1 + dz;
                var v5 = v1 + dy;
                var v6 = v2 + dy;
                var v7 = v3 + dy;
                var v8 = v4 + dy;
                v1 = m.MultiplyPoint3x4(v1);
                v2 = m.MultiplyPoint3x4(v2);
                v3 = m.MultiplyPoint3x4(v3);
                v4 = m.MultiplyPoint3x4(v4);
                v5 = m.MultiplyPoint3x4(v5);
                v6 = m.MultiplyPoint3x4(v6);
                v7 = m.MultiplyPoint3x4(v7);
                v8 = m.MultiplyPoint3x4(v8);
                Gizmos.DrawLine(v1, v2);
                Gizmos.DrawLine(v2, v3);
                Gizmos.DrawLine(v3, v4);
                Gizmos.DrawLine(v4, v1);
                Gizmos.DrawLine(v5, v6);
                Gizmos.DrawLine(v6, v7);
                Gizmos.DrawLine(v7, v8);
                Gizmos.DrawLine(v8, v5);
                Gizmos.DrawLine(v1, v5);
                Gizmos.DrawLine(v2, v6);
                Gizmos.DrawLine(v3, v7);
                Gizmos.DrawLine(v4, v8);
            }
        }

        protected virtual void OnSceneGUI()
        {
            var generator = target as TesseraGenerator;

            if (Event.current.type == EventType.MouseDown)
            {
                mouseDown = true;
            }
            if (Event.current.type == EventType.MouseUp)
            {
                mouseDown = false;
            }

            if (generator.surfaceMesh == null && generator.CellType is CubeCellType)
            {
                EditorGUI.BeginChangeCheck();
                Handles.matrix = generator.gameObject.transform.localToWorldMatrix;
                h.DrawHandle();
                Handles.matrix = Matrix4x4.identity;
                if (EditorGUI.EndChangeCheck())
                {
                }

                if (!mouseDown)
                {
                    h.center = generator.center;
                    h.size = Vector3.Scale(generator.tileSize, generator.size);
                }
            }
            if (generator.surfaceMesh == null && generator.CellType is SquareCellType)
            {
                EditorGUI.BeginChangeCheck();
                Handles.matrix = generator.gameObject.transform.localToWorldMatrix;
                h.DrawHandle();
                Handles.matrix = Matrix4x4.identity;
                if (EditorGUI.EndChangeCheck())
                {
                }

                if (!mouseDown)
                {
                    h.center = generator.center;
                    h.size = Vector3.Scale(generator.tileSize, generator.size);
                }
            }
        }

        private static GUILayoutOption miniButtonWidth = GUILayout.Width(20f);
        private bool mouseDown;

        private void Vector2IntField(SerializedProperty property, GUIContent guiContent = null)
        {
            guiContent = guiContent ?? new GUIContent(property.displayName, property.tooltip);
            var v3 = property.vector3IntValue;
            var v2 = new Vector2Int(v3.x, v3.y);
            v2 = EditorGUILayout.Vector2IntField(guiContent, v2);
            property.vector3IntValue = new Vector3Int(v2.x, v2.y, 0);
        }

        private void Vector2Field(SerializedProperty property, GUIContent guiContent = null)
        {
            guiContent = guiContent ?? new GUIContent(property.displayName, property.tooltip);
            var v3 = property.vector3Value;
            var v2 = new Vector2(v3.x, v3.y);
            v2 = EditorGUILayout.Vector2Field(guiContent, v2);
            property.vector3Value = new Vector3(v2.x, v2.y, 0);
        }

        private void GridShapeGUI()
        {
            var generator = target as TesseraGenerator;
            var cellType = generator.CellType;

            gridShapeToggle = EditorGUILayout.BeginFoldoutHeaderGroup(gridShapeToggle, "Grid Shape");
            if (gridShapeToggle)
            {
                if (serializedObject.FindProperty("surfaceMesh").objectReferenceValue != null)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_size").FindPropertyRelative("y"), new GUIContent("Layers", "Number of stacked copies of the supplied mesh."));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tileSize").FindPropertyRelative("y"), new GUIContent("Tile Height", "Height in units of each layer"));
                }
                else if (cellType is HexPrismCellType)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_center"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_size"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tileSize").FindPropertyRelative("x"), new GUIContent("Hex Tile Size", "Size measured is distance from center to center."));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tileSize").FindPropertyRelative("y"), new GUIContent("Tile Height", "Height in units of each layer"));
                }
                else if (cellType is TrianglePrismCellType)
                {

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_center"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_size"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tileSize").FindPropertyRelative("x"), new GUIContent("Triangle Tile Size", "Size measured is distance from center to center."));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tileSize").FindPropertyRelative("y"), new GUIContent("Tile Height", "Height in units of each layer"));
                }
                else
            if (cellType is CubeCellType)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_center"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_size"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tileSize"));
                }
                else if (cellType is SquareCellType)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_center"));
                    Vector2IntField(serializedObject.FindProperty("m_size"));
                    Vector2Field(serializedObject.FindProperty("tileSize"));
                }
                else
                {
                    throw new Exception();
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("skyBox"));
                if (cellType is CubeCellType || cellType is TrianglePrismCellType)
                {

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("surfaceMesh"));
                    var surfaceMesh = (Mesh)serializedObject.FindProperty("surfaceMesh").objectReferenceValue;
                    if (surfaceMesh != null)
                    {
                        EditorGUI.indentLevel++;
                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.Label("Surface Settings", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("surfaceOffset"), new GUIContent("Offset", "Height in units above the mesh that the first layer starts at."));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("surfaceSmoothNormals"), new GUIContent("Smooth Normals", "Controls how normals are treated for meshes deformed to fit the surfaceMesh."));
                        if (surfaceMesh.subMeshCount > 1)
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("filterSurfaceSubmeshTiles"), new GUIContent("Filter By Submesh", "If true, filters which tiles appear on each material (submesh) of the surface mesh."));

                            if (serializedObject.FindProperty("filterSurfaceSubmeshTiles").boolValue)
                            {
                                var surfaceSubmeshTiles = serializedObject.FindProperty("surfaceSubmeshTiles");
                                var currentSize = surfaceSubmeshTiles.arraySize;
                                if (currentSize != surfaceMesh.subMeshCount)
                                {
                                    // Update array
                                    surfaceSubmeshTiles.arraySize = surfaceMesh.subMeshCount;
                                    var tiles = generator.tiles.Select(x => x.tile).ToList();
                                    for (var i = currentSize; i < surfaceMesh.subMeshCount; i++)
                                    {
                                        // Initialize with a full set of tiles.
                                        var tilesProperty = surfaceSubmeshTiles.GetArrayElementAtIndex(i).FindPropertyRelative("tiles");
                                        tilesProperty.arraySize = tiles.Count;
                                        for (var j = 0; j < tiles.Count; j++)
                                        {
                                            tilesProperty.GetArrayElementAtIndex(j).objectReferenceValue = tiles[j];
                                        }
                                    }
                                }
                                for (var i = 0; i < surfaceMesh.subMeshCount; i++)
                                {
                                    var tilesProperty = surfaceSubmeshTiles.GetArrayElementAtIndex(i).FindPropertyRelative("tiles");
                                    new TileListUtil(generator, $"Submesh {i} filter", tilesProperty, allowSingleTile: false).Draw();
                                }
                            }
                        }
                        GUILayout.EndVertical();
                        EditorGUI.indentLevel--;
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void GenerationOptionsGUI()
        {
            var generator = target as TesseraGenerator;
            var cellType = generator.CellType;

            generationOptionsToggle = EditorGUILayout.BeginFoldoutHeaderGroup(generationOptionsToggle, "Generation");

            if (generationOptionsToggle)
            {

                EditorGUILayout.PropertyField(serializedObject.FindProperty("seed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("retries"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("backtrack"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stepLimit"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("algorithm"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("recordUndo"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("failureMode"));
                if (generator.failureMode != FailureMode.Cancel)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("uncertaintyTile"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("contradictionTile"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleUncertainyTile"));
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void ModelOptionsGUI()
        {
            var generator = target as TesseraGenerator;

            modelOptionsToggle = EditorGUILayout.BeginFoldoutHeaderGroup(modelOptionsToggle, "Model");
            if (modelOptionsToggle)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("modelType"));
                if (generator.modelType == ModelType.Overlapping || generator.modelType == ModelType.Adjacent)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("samples"));
                }
                if (generator.modelType == ModelType.Overlapping)
                {
                    // 90% of the time, users don't care to change the axes separately, so hide it from them
                    if (generator.overlapSize.x == generator.overlapSize.y && generator.overlapSize.x == generator.overlapSize.z)
                    {
                        var size = EditorGUILayout.IntField("Overlap Size", generator.overlapSize.x);
                        if (size != generator.overlapSize.x)
                        {
                            serializedObject.FindProperty("overlapSize").vector3IntValue = new Vector3Int(size, size, size);
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("overlapSize"));
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void TileListGUI()
        {
            if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == controlId)
            {
                if (selectorIndex >= 0)
                {
                    var tileObject = (GameObject)EditorGUIUtility.GetObjectPickerObject();
                    var tile = tileObject.GetComponent<TesseraTile>();
                    list.GetArrayElementAtIndex(selectorIndex).FindPropertyRelative("tile").objectReferenceValue = tile;
                }
            }
            if (Event.current.commandName == "ObjectSelectorClosed" && EditorGUIUtility.GetObjectPickerControlID() == controlId)
            {
                selectorIndex = -1;
            }

            list.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(list.isExpanded, new GUIContent("Tiles"));

            if (list.isExpanded)
            {
                var r1 = GUILayoutUtility.GetLastRect();

                reorderableTileList.DoLayoutList();

                var r2 = GUILayoutUtility.GetLastRect();

                var r = new Rect(r1.xMin, r1.yMax, r1.width, r2.yMax - r1.yMax);

                if (r.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.DragUpdated)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        Event.current.Use();
                    }
                    else if (Event.current.type == EventType.DragPerform)
                    {
                        for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                        {
                            var t = (DragAndDrop.objectReferences[i] as TesseraTileBase) ?? (DragAndDrop.objectReferences[i] as GameObject)?.GetComponent<TesseraTileBase>();
                            if (t != null)
                            {
                                ++reorderableTileList.serializedProperty.arraySize;
                                reorderableTileList.index = reorderableTileList.serializedProperty.arraySize - 1;
                                list.GetArrayElementAtIndex(reorderableTileList.index).FindPropertyRelative("weight").floatValue = 1.0f;
                                list.GetArrayElementAtIndex(reorderableTileList.index).FindPropertyRelative("tile").objectReferenceValue = t;
                            }
                        }
                        Event.current.Use();
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && reorderableTileList.index >= 0)
            {
                list.DeleteArrayElementAtIndex(reorderableTileList.index);
                if (reorderableTileList.index >= list.arraySize - 1)
                {
                    reorderableTileList.index = list.arraySize - 1;
                }
            }
        }
    }
}