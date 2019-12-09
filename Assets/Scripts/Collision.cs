using UnityEngine;

namespace Spheres
{
    public class Collision
    {
        public float t; // time of collision
        public Vector3 c; // center point in the moment of collision
        public Vector3 p; // position in the movement of collision
        public Vector3 v; // velocity after collision
        public Collider collider1;
        public Collider collider2;
    }
}
