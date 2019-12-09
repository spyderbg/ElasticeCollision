using UnityEngine;

namespace Spheres
{
    public class Sphere : Collider
    {
        #region Ctors

        public Sphere() : this( 1.0f, Vector3.zero, Vector3.zero ) {}
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

        public float Diameter => Radius * 2.0f;

        public Vector3 Velocity;

        #endregion

        #region Public methods

        public bool IsPointInside( Vector3 p ) =>
            Mathf.Pow( p.x - Center.x, 2.0f ) + Mathf.Pow( p.y - Center.y, 2.0f ) <= Radius2;

        public bool IsIntersect( Sphere c )
        {
            return Distance2( c ) <= Mathf.Pow( c.Radius + Radius, 2.0f );
        }

        public bool IsIntersectTime( Sphere c, float deltaTime )
        {
            var a = Center - c.Center;
            var a2 = a.x * a.x + a.y * a.y;
            var b = (Velocity - c.Velocity) * deltaTime;
            var b2 = b.x * b.x + b.y * b.y;
            var ab = Vector3.Dot( a, b );
            var ab2 = ab * ab;
            var d2 = a2 - ab2 / b2;
            var dist = Radius + c.Radius;
            var dist2 = dist * dist;
        
            return d2 <= dist2;
        }

        public float IntersectTime(Sphere c, float deltaTime)
        {
            var a = Center - c.Center;
            var a2 = a.x * a.x + a.y * a.y;
            var b = (Velocity - c.Velocity) * deltaTime;
            var b2 = b.x * b.x + b.y * b.y;
            var ab = Vector3.Dot( a, b );
            var ab2 = ab * ab;
            var d2 = a2 - ab2 / b2;
            var dist = Radius + c.Radius;
            var dist2 = dist * dist;

            if (d2 > dist2) return -1.0f;

            return (-ab - Mathf.Sqrt(ab2 - b2 * (a2 - d2))) / b2;
        }

        public float Distance( Sphere c ) =>
            Mathf.Sqrt(Distance2( c ));

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
