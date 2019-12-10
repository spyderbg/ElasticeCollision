﻿using Spheres;
using UnityEditor;
using UnityEngine;
using Grid = Spheres.Grid;

[CustomEditor(typeof(Field))]
public class SceneGUI : Editor
{
    private static bool _isGridVisible = true;

    public void OnSceneGUI()
    {
        Handles.BeginGUI();

        var field = FindObjectOfType<Field>();
        var grid = field.transform.GetComponentInChildren<Grid>(true);

        // Randomize button
        if (GUI.Button( new Rect(10.0f, 10.0f, 100.0f, 50.0f), "Randomize"))
        {
            field.ClearSphereObjects();
            field.RandomizeSpheres();
            field.RenderSpheres();
        }

        // Grid visible button
        if( GUI.Button( new Rect( 120.0f, 10.0f, 100.0f, 50.0f ), _isGridVisible ? "Hide Grid" : "Show Grid" ) )
        {
            _isGridVisible = !_isGridVisible;

            grid.gameObject.SetActive( _isGridVisible );
        }

        if( GUI.Button( new Rect( 230.0f, 10.0f, 100.0f, 50.0f ), "Intersections") )
        {
            field.PrintIntersectionsCount();
        }

        if( GUI.Button( new Rect( 340.0f, 10.0f, 100.0f, 50.0f ), "Sync Spheres") )
        {
//            field.SyncWithVisualSpheres();
        }

        field.Speed = GUI.HorizontalSlider(new Rect(20.0f, 70.0f, 100.0f, 40.0f), field.Speed, 0.1f, 10.0f );

        // stats
        GUI.color = Color.white;
        var rect = new Rect(340.0f, 70.0f, 300.0f, 20.0f);
        foreach(Sphere si in grid.Spheres)
        {
//            GUI.Label( rect, $"p:{si.Center} v:{si.Velocity} r:({si.Radius})" );
            rect.y += 18.0f;
        }

        Handles.EndGUI();
    }

}
