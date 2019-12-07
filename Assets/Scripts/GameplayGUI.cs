using System.Security.Cryptography.X509Certificates;
using Spheres;
using UnityEngine;

public class GameplayGUI : MonoBehaviour
{
    public float Speed = 1.0f;
    public void OnGUI()
    {
        var field = FindObjectOfType<Field>();

        // Randomize button
        if (GUI.Button( new Rect(10.0f, 10.0f, 100.0f, 50.0f), "Randomize"))
        {
            field.ClearSphereObjects();
            field.RandomizeSpheres();
            field.RenderSpheres();
        }
        
        Speed = GUI.HorizontalSlider(new Rect(120.0f, 20.0f, 100.0f, 40.0f), Speed, 0.1f, 3.0f );
    }
}
