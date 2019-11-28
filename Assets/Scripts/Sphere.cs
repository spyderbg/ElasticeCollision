using UnityEngine;

namespace Spheres
{
    public class Sphere
    {
        #region Ctors
        public Sphere( float radius ) : this( radius, Vector3.zero, Vector3.zero ) {}
        public Sphere(float radius, Vector3 center) : this(radius, center, Vector3.zero) {}
        public Sphere(float radius, Vector3 center, Vector3 velocity) 
        {
            Center = center;
            Radius = radius;
            Velocity = velocity;
        }

        #endregion

        #region Properties

        private Vector3 _center;
        public Vector3 Center
        {
            get => _center;
            set
            {
                _center = value;
                UpdateBounds();
            }
        }

        private Bounds _bounds;
        public Bounds Bounds => _bounds;

        private float _radius;
        public float Radius
        {
            get => _radius;
            set { _radius = value; Radius2 = _radius * _radius; }
        }

        public float Radius2 { get; private set; }

        public Vector3 Velocity;

        #endregion

        #region Public methods

        public bool IsPointInside( Vector3 p ) =>
            Mathf.Pow( p.x - Center.x, 2.0f ) + Mathf.Pow( p.y - Center.y, 2.0f ) <= Radius2;

        public bool IsIntersect( Sphere c ) =>
             Distance2(c) <= Mathf.Pow( c.Radius + Radius, 2.0f );

        public float Distance( Sphere c ) =>
            Mathf.Sqrt(Mathf.Pow( c.Center.x - Center.x, 2.0f ) + Mathf.Pow( c.Center.y - Center.y, 2.0f ));

        public float Distance2( Sphere c ) =>
            Mathf.Pow( c.Center.x - Center.x, 2.0f ) + Mathf.Pow( c.Center.y - Center.y, 2.0f );



        #endregion

        #region Private methods

        private void UpdateBounds()
        {
            _bounds.min.x = _center.x - Radius;
            _bounds.min.y = _center.y - Radius;
            _bounds.min.z = 0.0f;
            _bounds.max.x = _center.x + Radius;
            _bounds.max.y = _center.y + Radius;
            _bounds.max.z = 0.0f;
        }

        #endregion
    }
}
