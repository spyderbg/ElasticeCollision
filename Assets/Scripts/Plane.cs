﻿using UnityEngine;

namespace Spheres
{
    public class Plane : Collider
    {
        private Vector3 _normal;
        private Vector3 _normalNorm;
        private float _distance;

        public Plane( float a, float b, float c, float d )
        {
            Normal = new Vector3(a, b, c);
            _distance = d;
        }

        public Plane( Vector3 v )
        {
            Normal = new Vector3(v.x, v.y, v.z);
            _distance = 0.0f;
        }

        public Vector3 Normal
        {
            get => _normal;
            set
            {
                _normal = value;
                _normalNorm = _normal.normalized;
            }
        }

        public Vector3 NormalNorm => _normalNorm;

        public float Distance
        {
            get => _distance;
            set => _distance = value;
        }

        public Vector3 Reflect(Vector3 ray) =>
            ray - 2 * Vector3.Dot(ray, _normalNorm) * _normalNorm;
    }
}
