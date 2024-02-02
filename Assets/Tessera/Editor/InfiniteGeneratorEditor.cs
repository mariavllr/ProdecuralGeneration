using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    [CustomEditor(typeof(InfiniteGenerator))]
    class InfiniteGeneratorEditor : Editor
    {
        bool chunksFoldout = true;

        public override void OnInspectorGUI()
        {
            var u = target as InfiniteGenerator;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("generator"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("parallelism"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("seed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("watchedColliders"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("scanInterval"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxInstantiatePerUpdate"));
            chunksFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(chunksFoldout, "Chunk Bounds");
            if (chunksFoldout)
            {
                EditorGUI.indentLevel++;
                foreach (var x in new[] { "X", "Y", "Z" })
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty($"infinite{x}"));
                    if (!(x == "X" ? u.infiniteX : x == "Y" ? u.infiniteY : u.infiniteZ))
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(serializedObject.FindProperty($"min{x}Chunk"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty($"max{x}Chunk"));
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("chunkCleanupType"));
            if(u.chunkCleanupType != ChunkCleanupType.None)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cleanupInterval"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chunkPersistTime"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxCleanupPerUpdate"));

            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
