using UnityEngine;

namespace Spheres
{
    public class Collision
    {
        public float t; // time of collision
        public Vector3 c; // contact point in the moment of collision
        public Vector3 p; // position in the movement of collision
        public Vector3 v; // direction after collision
        public Sphere sphere;
        public Plane plane; // has to be plane
    }
}
