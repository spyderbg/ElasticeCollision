using Spheres;
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

        // Randomize button
        if (GUI.Button( new Rect(10.0f, 10.0f, 100.0f, 50.0f), "Randomize"))
        {
            field.RandomizeSpheres();
            field.Draw();
        }

        // Grid visible button
        if( GUI.Button( new Rect( 120.0f, 10.0f, 100.0f, 50.0f ), _isGridVisible ? "Hide Grid" : "Show Grid" ) )
        {
            _isGridVisible = !_isGridVisible;

            var grid = field.transform.GetComponentInChildren<Grid>(true);
            grid.gameObject.SetActive( _isGridVisible );
        }

        if( GUI.Button( new Rect( 230.0f, 10.0f, 100.0f, 50.0f ), "Count intersec.") )
        {
            field.PrintIntersections();
        }

        if( GUI.Button( new Rect( 340.0f, 10.0f, 100.0f, 50.0f ), "Sync spheres") )
        {
            field.SyncWithVisualSpheres();
        }

        if( GUI.Button( new Rect( 450.0f, 10.0f, 100.0f, 50.0f ), "Clear") )
        {
            field.ClearSpheresMono();
        }

        Handles.EndGUI();
    }

}
