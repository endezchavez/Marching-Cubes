using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshGenerator))]
public class MeshGeneratorEditor : Editor
{
    MeshGenerator meshGenerator;
    Editor terrainEditor;

    public override void OnInspectorGUI()
    {
        using (var check = new EditorGUI.ChangeCheckScope())
        {
            base.OnInspectorGUI();
            
        }

        if(GUILayout.Button("Generate Terrain"))
        {
            meshGenerator.GenerateTerrain();
        }

        DrawTerrainSettingsEdior(meshGenerator.settings, meshGenerator.OnTerrainSettingsUpdated, ref meshGenerator.terrainSettingsFoldout, ref terrainEditor);

    }

    void DrawTerrainSettingsEdior(Object settings, System.Action onSettingsUpdated, ref bool foldout, ref Editor editor)
    {
        if(settings != null)
        {
            foldout = EditorGUILayout.InspectorTitlebar(foldout, settings);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                if (foldout)
                {
                    CreateCachedEditor(settings, null, ref editor);
                    editor.OnInspectorGUI();

                    if (check.changed)
                    {
                        if(onSettingsUpdated != null)
                        {
                            onSettingsUpdated();
                        }
                    }
                }

            }
        }
        
    }

    private void OnEnable()
    {
        meshGenerator = (MeshGenerator)target;
    }
}
