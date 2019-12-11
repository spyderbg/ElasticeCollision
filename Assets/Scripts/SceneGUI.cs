using Spheres;
using UnityEditor;
using UnityEngine;
using Grid = Spheres.Grid;
using Plane = Spheres.Plane;

[CustomEditor(typeof(Field))]
public class SceneGUI : Editor
{
    private static bool _isGridVisible = true;
    private GUIStyle _statBgrStyle = null;

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
        var statX = 550.0f;
        var statY = 10.0f;
        var statW = 300.0f;
        var statLine = 18.0f;

        if(_statBgrStyle == null)
        {
            _statBgrStyle = new GUIStyle();
            _statBgrStyle.normal.background = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.9f));
        }

        var rect = new Rect(statX, statY + statLine, statW, statLine);
        foreach(Sphere si in grid.Spheres)
            rect.y += statLine - 2.0f;

        GUI.color = Color.white;

        GUI.Box( new Rect(statX, statY, statW, rect.y + statLine - statY ), "Positions:", _statBgrStyle );

        rect = new Rect(statX, statY + statLine, statW, statLine);
        foreach(Sphere si in grid.Spheres)
        {
            GUI.Label( rect, $"p:{si.Center} v:{si.Velocity} r:({si.Radius})" );
            rect.y += statLine - 2.0f;
        }

        Handles.EndGUI();
    }

    private Texture2D MakeTexture( int w, int h, Color color )
    {
        var pix = new Color[w * h];
        for(int i = 0; i < pix.Length; i++)
            pix[i] = color;

        var tex = new Texture2D(w, h);
        tex.SetPixels( pix );
        tex.Apply();
        return tex;
    }

}
