using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    [CustomEditor(typeof(TesseraVolume))]
    class TesseraVolumeEditor : Editor
    {
        private TesseraGenerator generator;
        private TileListUtil tileList;

        public override void OnInspectorGUI()
        {
            var volume = (target as TesseraVolume);
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("generator"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("volumeType"), true);

            if (volume.volumeType == VolumeType.TilesetFilter)
            {
                if (volume.generator != generator)
                {
                    generator = volume.generator;
                    tileList = generator == null ? null : new TileListUtil(volume.generator, "Tiles", serializedObject.FindProperty("tiles"));
                }
                if (tileList != null)
                {
                    tileList.Draw();
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tiles"), true);
                }
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("invertArea"), true);
            if (volume.GetComponent<Collider>() == null)
            {
                EditorGUILayout.HelpBox("Add colliders to define the shape of the volume.", MessageType.Warning);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
