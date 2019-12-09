using UnityEngine;

namespace Spheres
{
    public class Plane : Collider
    {
        private Vector3 _normal;
        private float _distance;

        public Plane( float a, float b, float c, float d )
        {
            _normal = new Vector3(a, b, c);
            _distance = d;
        }

        public Vector3 Normal
        {
            get => _normal;
            set => _normal = value;
        }

        public float Distance
        {
            get => _distance;
            set => _distance = value;
        }
    }
}
