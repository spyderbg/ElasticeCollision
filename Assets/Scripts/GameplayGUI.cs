using System.Collections;
using System.Collections.Generic;
using Spheres;
using UnityEngine;

public class GameplayGUI : MonoBehaviour
{
    public void OnGUI()
    {
        var field = FindObjectOfType<Field>();

        // Randomize button
        if (GUI.Button( new Rect(10.0f, 10.0f, 100.0f, 50.0f), "Randomize"))
        {
            field.RandomizeSpheres();
            field.Draw();
        }
    }
}
