using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DensityGenerator))]
public class DensityGeneratorEditor : Editor
{
    DensityGenerator densityGenerator;
    Editor noiseEditor;

    public override void OnInspectorGUI()
    {
        using (var check = new EditorGUI.ChangeCheckScope())
        {
            base.OnInspectorGUI();

        }

        if (GUILayout.Button("Generate Terrain"))
        {
            densityGenerator.UpdateTerrain();
        }

        DrawTerrainSettingsEdior(densityGenerator.settings, densityGenerator.OnNoiseSettingsUpdated, ref densityGenerator.noiseSettingsFoldout, ref noiseEditor);

    }

    void DrawTerrainSettingsEdior(Object settings, System.Action onSettingsUpdated, ref bool foldout, ref Editor editor)
    {
        if (settings != null)
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
                        if (onSettingsUpdated != null)
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
        densityGenerator = (DensityGenerator)target;
    }
}