using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    /// <summary>
    /// Handles upgrading users from Tessera to TesseraPro
    public static class DummyUpgradeToTesseraPro
    {
        [MenuItem("Tools/Tessera/Upgrade Scene to Tessera Pro")]
        public static void UpgradeAll()
        {
            var replacements = new[]
            {
                (typeof(Dummy_1582759212), "TesseraPalette.cs"),
                (typeof(Dummy_296730116), "TesseraTile.cs"),
                (typeof(Dummy_3092287824), "TesseraGenerator.cs"),
                (typeof(Dummy_423707718), "TesseraVolume.cs"),
                (typeof(Dummy_1784719773), "TesseraPinned.cs"),
                (typeof(Dummy_573662364), "TesseraSquareTile.cs"),
                (typeof(Dummy_2268203068), "TesseraMultipassGenerator.cs"),
            };
            foreach(var (dummyType, replacementScriptPath) in replacements)
            {
                var replacementScript = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Tessera/" + replacementScriptPath);
                foreach (var dummy in Resources.FindObjectsOfTypeAll(dummyType))
                {
                    Debug.LogWarning($"Updating {dummy.name}");
                    using (var serializedObject = new SerializedObject(dummy))
                    {
                        var scriptProperty = serializedObject.FindProperty("m_Script");
                        scriptProperty.objectReferenceValue = replacementScript;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }
    }
}