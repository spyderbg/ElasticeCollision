using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using Sphere = Spheres.Sphere;
using Random = UnityEngine.Random;
using Collision = Spheres.Sphere.Collision;

namespace Spheres
{
    [ExecuteInEditMode]
    public class Field : MonoBehaviour
    {
        public enum RandomizeMethods
        {
            Simple,
            MoveRight
        }
        public enum RenderMethods
        {
            Mono,
            Mesh,
            ByBuckets,
            Indirect
        }

        public RandomizeMethods RandomizeMethod = RandomizeMethods.MoveRight;
        public RenderMethods RenderMethod = RenderMethods.Mono;
        public GameObject Grid;
        public GameObject Spheres; 
        
        public int SpheresNumber;

        public float MinRadius = .02f;
        public float MaxRadius = .05f;

        public float MinVelocity = .001f;
        public float MaxVelocity = .003f;

        public Mesh InstanceMesh;
        public Material InstanceMaterial;

        public float Speed = 1.0f;

        private Grid _grid;
        private ComputeBuffer _positionBuffer;
        private ComputeBuffer _argsBuffer;
        private int _cachedInstanceCount = -1;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        #region MonoBehaviour
        
        void Start()
        {
            _grid = Grid.GetComponent<Grid>();

            OnGridUpdate();
        }

        void Update()
        {
            if(_grid == null )
                _grid = Grid.GetComponent<Grid>();
        
            // check for recreate
            if ( _grid.SpheresNumber != SpheresNumber )
            {
                RandomizeSpheres();
                return;
            }
            
            Simulate(Speed * Time.deltaTime);
            RenderSpheres();
        }

        void OnDrawGizmos()
        {

#if UNITY_EDITOR
            // Ensure continuous Update calls.
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }
        void OnDisable()
        {
            _positionBuffer?.Release();
            _positionBuffer = null;

            _argsBuffer?.Release();
            _argsBuffer = null;
        }

        private void OnGridUpdate()
        {
            var camera = Camera.main;
            var pos = camera.transform.position;
            camera.transform.position = new Vector3(_grid.Width / 2.0f, _grid.Height / 2.0f, pos.z);
        }
        
        #endregion

        #region Statistics 
        
        public void PrintIntersectionsCount()
        {
            if(!_grid.HasSpheres) return;

            Debug.Log( message: $"Sphere intersections count: {_grid.IntersectCount()}" );
        }
        
        #endregion
        
        #region Randomize methos
        
        private delegate bool PositionSpheres(Sphere sphere, bool checkOverlaping = true);
        
        public void RandomizeSpheres()
        {
            // validate input data 
            if( _grid.CellX < MaxRadius * 2.0f || _grid.CellY < MaxRadius * 2.0f )
            {
//                Debug.LogError( $"Input parameters error: circle radius too large, reduce it and try again!" );
//	            return;
            }

            // create spheres
            PositionSpheres positionMethod;
            switch (RandomizeMethod)
            {
                case RandomizeMethods.Simple: positionMethod = PositionSphereSimple; break;
                case RandomizeMethods.MoveRight: positionMethod = PositionSphereMoveRight; break;
                default:
                    throw new ArgumentOutOfRangeException( $"Spheres randomize method is not specified!!!" );
            }
            
            _grid.ClearBuckets();

            for( var i = 0; i < SpheresNumber; i++ )
            {
                var velocity = new Vector3() {
                    x = Random.Range(-1.0f, 1.0f),
                    y = Random.Range(-1.0f, 1.0f),
                    z = .0f
                };
            
                var sphere = new Sphere();
                sphere.Radius = Random.Range(MinRadius, MaxRadius);
                sphere.Velocity = Vector3.ClampMagnitude( velocity, Random.Range( MinVelocity, MaxVelocity ));
                
                if( !positionMethod( sphere, true )) continue;
                
                _grid.AddSphere( sphere );
            }
        }

        private bool PositionSphereSimple(Sphere sphere, bool checkOverlaping = true)
        {
            var maxAttempts = 100;
            
            do {
                var center = sphere.Center;
                center.x = Random.Range( MaxRadius, _grid.Width - MaxRadius );
                center.y = Random.Range( MaxRadius, _grid.Height - MaxRadius );
                sphere.Center = center;
            } while( checkOverlaping && _grid.IsIntersect(sphere) && --maxAttempts > 0 );

            return maxAttempts > 0;
        }

        private bool PositionSphereMoveRight(Sphere sphere, bool checkOverlaping = true)
        {
            var maxAttempts = 10;
            
            do {
                var center = sphere.Center;
                center.x = Random.Range( MaxRadius, _grid.Width - MaxRadius );
                center.y = Random.Range( MaxRadius, _grid.Height - MaxRadius );
                sphere.Center = center;

                if (!checkOverlaping) break;

                for (;;)
                {
                    if (_grid.IsIntersect(sphere))
                    {
                        center.x += sphere.Radius;
                        if (center.x > _grid.Width - sphere.Radius)
                            break;
                        sphere.Center = center;
                    }
                    else
                    {
                        return true;
                    }
                }
            } while( --maxAttempts > 0 );

            return maxAttempts > 0;
        }
        
        #endregion

        #region Render methods

        public void ClearSphereObjects()
        {
            var spheres = Spheres.transform.GetComponentsInChildren<MeshFilter>();
            if (spheres == null) return;

            foreach (var t in spheres)
                DestroyImmediate(t.gameObject);
        }

        public void RenderSpheres()
        {
            if (!_grid.HasSpheres) return;
            
            switch (RenderMethod)
            {
                case RenderMethods.Mono: RenderSpheresMono(); break;
                case RenderMethods.Mesh: RenderSpheresMesh(); break;
                case RenderMethods.ByBuckets: RenderSpheresByBuckets(); break;
                case RenderMethods.Indirect: RenderSpheresIndirect(); break;
                default:
                    throw new ArgumentOutOfRangeException($"Spheres render mehtod is not specified!!!");
            }
        }

        private void SyncWithVisualSpheres()
        {
            if(!_grid.HasSpheres) return;

//            var i = 0;
//            foreach( var t in Spheres.transform.GetComponentsInChildren<MeshFilter>() )
//                _spheres[i++].Center = t.transform.position;
        }

        private void SyncVisualSpheres()
        {
            if(!_grid.HasSpheres) return;

            var idx = 0;
            var go = Spheres.transform.GetComponentsInChildren<MeshFilter>();
            foreach( Sphere si in _grid.Spheres )
                go[idx++].transform.position = si.Center;
        }

        private void RenderSpheresMono()
        {
            if (_grid.SpheresNumber != Spheres.transform.childCount)
            {
                // destroy sphere gameObjects
                ClearSphereObjects();

                // recreate spheres
                var idx = 1;
                foreach( Sphere si in _grid.Spheres )
                {
                    var go = GameObject.CreatePrimitive( PrimitiveType.Sphere );
                    go.name = $"sphere_{idx++}";
                    go.transform.position = si.Center;
                    go.transform.localScale = new Vector3( si.Diameter, si.Diameter, si.Diameter );
                    go.transform.parent = Spheres.transform;
                    go.GetComponent<Renderer>().sharedMaterial.color = Color.white;
                }
            }

            SyncVisualSpheres();
        }

        private void RenderSpheresMesh()
        {
            // create mesh
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            var material = go.GetComponent<Renderer>().sharedMaterial; 
            material.color = Color.red;
            DestroyImmediate(go);

            // render spheres
            foreach( Sphere s in _grid.Spheres )
            {
                var matrix = Matrix4x4.Translate(s.Center) *
                             Matrix4x4.Scale(new Vector3(s.Diameter, s.Diameter, s.Diameter));

                try {
                    Graphics.DrawMesh( mesh, matrix, material, 0 );
                }
                catch (Exception e) {
                    Debug.Log($"RenderSpheresMesh::DrawMesh Exception: {e}");
                }
            }
        }

        private void RenderSpheresByBuckets()
        {
            if(!SystemInfo.supportsInstancing)
            {
                Debug.LogError( $"GPU instancing not supported" );
                return;
            }

            // create mesh
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            var material = go.GetComponent<Renderer>().sharedMaterial; 
            material.color = Color.blue;
            material.enableInstancing = true;
            DestroyImmediate(go);

            // render spheres
            var matrices = new Matrix4x4[10];
            foreach( IList<Sphere> bucket in _grid.Buckets )
            {
                var idx = 0;
                if(matrices.Length < bucket.Count)
                    matrices = new Matrix4x4[bucket.Count];

                foreach (var sphere in bucket)
                {
                    matrices[idx++] = Matrix4x4.Translate(sphere.Center) *
                                      Matrix4x4.Scale(new Vector3(sphere.Diameter, sphere.Diameter, sphere.Diameter));
                }

                try {
                    Graphics.DrawMeshInstanced(mesh, 0, material, matrices, bucket.Count);
                }
                catch (Exception e) {
                    Debug.Log($"RenderSpheresByBuckets::DrawMesh Exception: {e}");
                }
            }
        }

        private void RenderSpheresIndirect()
        {
            if(!_grid.HasSpheres) return;

            if(!SystemInfo.supportsInstancing)
            {
                Debug.LogError( $"GPU instancing not supported" );
                return;
            }

            UpdateBuffers();
            Graphics.DrawMeshInstancedIndirect(InstanceMesh, 0, InstanceMaterial, new UnityEngine.Bounds(Vector3.zero, new Vector3(10.0f, 10.0f, 10.0f)), _argsBuffer);
        }

        private void UpdateBuffers()
        {
            if(!_grid.HasSpheres) return;

            if (_argsBuffer == null)
                _argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

            _cachedInstanceCount = _grid.SpheresNumber;

            // set positions 
            _positionBuffer?.Release();
            _positionBuffer = new ComputeBuffer(_cachedInstanceCount, 16);

            var idx = 0;
            var positions = new Vector4[_cachedInstanceCount];
            foreach( Sphere s in _grid.Spheres )
                positions[idx++] = new Vector4(s.Center.x, s.Center.y, s.Center.z, s.Diameter);

            _positionBuffer.SetData( positions );
            InstanceMaterial.SetBuffer("positionBuffer", _positionBuffer);

            // indirect args
            if (InstanceMesh != null) {
                args[0] = (uint)InstanceMesh.GetIndexCount(0);
                args[1] = (uint)_cachedInstanceCount;
                args[2] = (uint)InstanceMesh.GetIndexStart(0);
                args[3] = (uint)InstanceMesh.GetBaseVertex(0);
            }
            else {
                args[0] = args[1] = args[2] = args[3] = 0;
            }
            _argsBuffer.SetData(args);
        }

        #endregion

        #region Movement methods

        private void Simulate(float deltaTime)
        {
            if(!_grid.HasSpheres) return;
            
            
//            UpdatePositions(deltaTime);
//            UpdatePositions2( deltaTime );
//            UpdatePositions3( deltaTime );
//            _grid.UpdatePositions(deltaTime);

//            UpdateBuffers();
        }

        private void UpdatePositions(float deltaTime)
        {
            deltaTime = .20f;

//            var collisions = new List<Tuple<Sphere, Sphere>>();

            foreach( Sphere si in _grid.Spheres )
            {
//                var si = _spheres[i];
//                var ci = si.Center;
//                var vi = si.Velocity;// * deltaTime;
                // collision
//                for( var j = 0; j < i; j++ )
//                {
//                    var sj = _spheres[j];
//                    var cj = sj.Center;
//
//                    if (si.IsIntersect(sj))
//                    {
////                        collisions.Add( new Tuple<Sphere, Sphere>(si, sj) );
//                        si.Collisions.Add(sj);
//
//                        var d = si.Distance( sj );
//                        var overlap = 0.5f * (d - si.Radius - sj.Radius) / d;
//
//                        ci -= overlap * (ci - cj);
//                        cj += overlap * (ci - cj);
//                    }
//
//                    sj.Center = cj;
//                }

//                for( var j = 0; j < _spheres.Count; j++ )
//                {
//                    var sj = _spheres[j];
//                    var cj = sj.Center;
//                    var vj = sj.Velocity * deltaTime;
                    
//                    if(si == sj) continue;

                    // prevent overlapping
//                    if (si.IsIntersect(sj))
//                    {
//                        var d = si.Distance( sj );
//                        var overlap = 0.5f * (d - si.Radius - sj.Radius) / d;

//                        ci -= overlap * (ci - cj);
//                        cj += overlap * (ci - cj);
//                    }

                    // find time ot collision
//                    if( si.IsIntersectTime( sj, deltaTime ) )
//                    {
//                        vi *= -1.0f;
//                        vj *= -1.0f;
//                    }
//                    var t = si.IsIntersectTime(sj, deltaTime);
//                    if(0.0f <= t && t <= 1.0f)
//                    {
//                        ci += t * vi;
//                        cj += t * vj;
                        
//                        collisions.Add( new Tuple<Sphere, Sphere>(si, sj) );
//                    } else {
//                    }

//                    sj.Center = cj;
//                    sj.Velocity = vj;
//                }


                var ci = si.Center;
                var vi = si.Velocity;
                var vm = si.Velocity.magnitude;

                var max = _grid.Width + _grid.Height;
                var slope_offset = 2.8f;
                var left = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
                var right = new Vector4(-1.0f, 0.0f, 0.0f, _grid.Width);
                var top = new Vector4(0.0f, -1.0f, 0.0f, _grid.Height);
                var bottom = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);

                var slope1 = new Vector4(1.0f, 1.0f, 0.0f, slope_offset);
                var slope2 = new Vector4(-1.0f, -1.0f, 0.0f, max - slope_offset);
                var slope3 = new Vector4(-1.0f, 1.0f, 0.0f, _grid.Width - slope_offset);
                var slope4 = new Vector4(1.0f, -1.0f, 0.0f, _grid.Height - slope_offset);

                var planes = new Vector4[] {left, right, top, bottom, slope1, slope2, slope3, slope4 };

                var collisions = new List<Collision>();

                for( var q = 0; q < 100; q++ )
                {
                    collisions.Clear();

                    foreach( var plane in planes )
                    {
                        var lprim = plane - new Vector4( 0.0f, 0.0f, 0.0f, si.Radius );
                        var lcDot = Vector4.Dot( lprim, new Vector4( ci.x, ci.y, ci.z, 1.0f ) );
                        var lvDot = Vector4.Dot( lprim, new Vector4( vi.x, vi.y, vi.z, 0.0f ) );
                        if(Math.Abs( lvDot ) < float.Epsilon) continue;

                        var t = -(lcDot / lvDot);
                        if( .0f < t && t < 1.0f )
                        {
                            var n = new Vector3( plane.x, plane.y, plane.z ).normalized;
                            var r = vi - 2 * Vector3.Dot( vi, n ) * n;
                            collisions.Add( new Collision()
                            {
                                t = t,
                                p = ci + t * vi,
                                c = ci + t * vi - si.Radius * new Vector3( lprim.x, lprim.y, lprim.z ),
                                v = r * (1 - t)
                            } );

                            Debug.Log( $"contact point: ({ci.x}, {ci.y}, {ci.z}) test({ci})" );
                        }
                    }

                    if( !collisions.Any() )
                    {
                        ci += vi;
                        break;
                    }

                    var col = collisions.OrderBy( c => c.t ).ElementAt( 0 );
                    ci = col.p;
                    vi = col.v;
                    if( (Math.Abs( vi.magnitude ) > float.Epsilon) )
                        si.Velocity = vi.normalized * vm;
                    else
                        break;
                }

                si.Center = ci;
//                si.Velocity = vi / deltaTime;
                /*
                if (ci.x < 0)
                {
//                    vi.x *= -1;
                    si.Velocity.x *= -1.0f;
                    ci.x = 0.0f;
                }
                else if (ci.x > _grid.Width)
                {
//                    vi.x *= -1;
                    si.Velocity.x *= -1.0f;
                    ci.x = _grid.Width;
                }

                if (ci.y < 0)
                {
//                    vi.y *= -1;
                    si.Velocity.y *= -1.0f;
                    ci.y = 0.0f;
                }
                else if (ci.y > _grid.Height)
                {
//                    vi.y *= -1;
                    si.Velocity.y *= -1.0f;
                    ci.y = _grid.Height;
                }
                */
//                si.Center = ci;
//                si.Velocity = vi / deltaTime;
            }

            // update collided circles velocity
            // ref: https://en.wikipedia.org/wiki/Elastic_collision

            //            foreach(var pair in collisions)
            //            {
            //                var (s1, s2) = (Tuple<Sphere, Sphere>) pair;

            //                var n = (s2.Center - s1.Center).normalized;
            //                var dot = Vector3.Dot(s1.Velocity - s2.Velocity, n);
            //                var k = 2.0f * dot / (s1.Radius + s2.Radius);
            //                s1.Velocity -= k * s2.Radius * n;
            //                s2.Velocity += k * s1.Radius * n;

            //                var d12 = (s1.Center - s2.Center).normalized;
            //                var d21 = (s2.Center - s1.Center).normalized;
            //                var k = 2.0f / (s1.Radius + s2.Radius);
            //                var dot1 = Vector3.Dot(s1.Velocity - s2.Velocity, d12);
            //                var dot2 = Vector3.Dot(s2.Velocity - s1.Velocity, d21);

            //                s1.Velocity -=  k * s2.Radius * dot1 * d12;
            //                s2.Velocity += k * s1.Radius * dot2 * d12;
            //            }

            //            for (var c = 0; c < _grid.Columns; c++)
            //                _grid.CollisionsWorker(c);

            //            _grid.VelocityUpdateWorker(new Range(0, _grid.Rows * _grid.Columns));

            //            _grid.BucketUpdateWorker(new Range(0, _grid.Rows * _grid.Columns));
        }

        private void UpdatePositions2( float deltaTime )
        {
            foreach( Sphere si in _grid.Spheres )
            {
                var ci = si.Center;
                var vi = si.Velocity * deltaTime;
                var vm = si.Velocity.magnitude;

                var max = _grid.Width + _grid.Height;
                var slope_offset = 0.0f;
                var left = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
                var right = new Vector4(-1.0f, 0.0f, 0.0f, _grid.Width);
                var top = new Vector4(0.0f, -1.0f, 0.0f, _grid.Height);
                var bottom = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);

                var slope1 = new Vector4(1.0f, 1.0f, 0.0f, slope_offset); // \
                var slope2 = new Vector4(-1.0f, -1.0f, 0.0f, max - slope_offset); // \
                var slope3 = new Vector4(-1.0f, 1.0f, 0.0f, _grid.Width - slope_offset);  // /
                var slope4 = new Vector4(1.0f, -1.0f, 0.0f, _grid.Height - slope_offset); // /

                var planes = new Vector4[] {/*left, right, top, bottom,*/ slope1, slope2, slope3, slope4 };

                var collisions = new List<Collision>();

                foreach (var plane in planes)
                {
                    var lprim = plane - new Vector4(0.0f, 0.0f, 0.0f, si.Radius);
                    var lcDot = Vector4.Dot(lprim, new Vector4(ci.x, ci.y, ci.z, 1.0f));
                    var lvDot = Vector4.Dot(lprim, new Vector4(vi.x, vi.y, vi.z, 0.0f));
                    if (Math.Abs(lvDot) < float.Epsilon) continue;

                    var t = -(lcDot / lvDot);
                    if( t <= .0f || 1.0f <= t) continue;

                    var n = new Vector3(plane.x, plane.y, plane.z).normalized;
                    var r = vi - 2 * Vector3.Dot(vi, n) * n;
                    collisions.Add(new Collision()
                    {
                        t = t,
                        p = ci + t * vi,
                        c = ci + t * vi - si.Radius * new Vector3(lprim.x, lprim.y, lprim.z),
                        v = r * (1 - t)
                    });

                    Debug.Log($"contact point 2: ({ci}) slope({plane})");
                }

                if (collisions.Any()) {
                    var col = collisions.OrderBy(c => c.t).ElementAt(0);
                    ci = col.p;
                    vi = col.v;
                    if ((Math.Abs(vi.magnitude) > float.Epsilon))
                        si.Velocity = vi.normalized * vm;
                } else {
                    ci += vi;
                }

                si.Center = ci;
            }
        }

        private void UpdatePositions3( float deltaTime )
        {
            foreach( Sphere si in _grid.Spheres )
            {
                var ci = si.Center;
                var vi = si.Velocity * deltaTime;
                var vim = si.Velocity.magnitude;

                var planes = new Vector4[]
                {
                    new Vector4(1.0f, 0.0f, 0.0f, 0.0f), // left
                    new Vector4(-1.0f, 0.0f, 0.0f, _grid.Width), // right
                    new Vector4(0.0f, -1.0f, 0.0f, _grid.Height), // top
                    new Vector4(0.0f, 1.0f, 0.0f, 0.0f) // bottom
                };

                si.Collisions.Clear();
                // collide with borders
                foreach (var plane in planes)
                {
                    var lprim = plane - new Vector4(0.0f, 0.0f, 0.0f, si.Radius);
                    var lcDot = Vector4.Dot(lprim, new Vector4(ci.x, ci.y, ci.z, 1.0f));
                    var lvDot = Vector4.Dot(lprim, new Vector4(vi.x, vi.y, vi.z, 0.0f));
                    if (Math.Abs(lvDot) < float.Epsilon) continue;

                    var t = -(lcDot / lvDot);
                    if( t <= .0f || 1.0f <= t) continue;

                    var n = new Vector3(plane.x, plane.y, plane.z).normalized;
                    var r = vi - 2 * Vector3.Dot(vi, n) * n;
                    si.Collisions.Add(new Collision()
                    {
                        t = t,
                        p = ci + t * vi,
                        c = ci + t * vi - si.Radius * new Vector3(lprim.x, lprim.y, lprim.z),
                        v = r * (1 - t)
                    });

                    Debug.Log($"contact point 2: ({ci}) slope({plane})");
                }

                continue;
                // collide with spheres
                foreach( Sphere sj in _grid.Spheres )
                {
                    var cj = sj.Center;
                    var vj = sj.Velocity * deltaTime;
                    var vjm = sj.Velocity.magnitude;

                    if( si == sj ) continue;

                    // find time of collision
                    var a = ci - cj;
                    var a2 = a.x * a.x + a.y * a.y;
                    var b = vi - vj;
                    var b2 = b.x * b.x + b.y * b.y;
                    var ab = Vector3.Dot( a, b );
                    var ab2 = ab * ab;
                    var d2 = a2 - ab2 / b2;
                    var dist = si.Radius + sj.Radius;
                    var dist2 = dist * dist;

                    if( d2 > dist2 ) continue;

                    var t = (-ab - Mathf.Sqrt(ab2 - b2 * (a2 - d2))) / b2;

                    if( t < .0f || 1.0f < t )
                    {
                        Debug.LogError($"Sphere collision time out of [0, 1] t({t})");
                        continue;
                    };

                    var n = (cj - ci).normalized;
                    var dot = Vector3.Dot(vi - vj, n);
                    var k = 2.0f * dot / (si.Radius + sj.Radius);

                    si.Collisions.Add(new Collision()
                    {
                        t = t,
                        p = cj + t * vi,
                        c = cj, // incorrect
                        v = vj + k * si.Radius *n,
                        collider = sj
                    });
                }
            }

            foreach( Sphere si in _grid.Spheres )
            {
                var ci = si.Center;
                var vi = si.Velocity * deltaTime;
                var vim = si.Velocity.magnitude;
                var col = si.Collisions.OrderBy( c => c.t ).ElementAt( 0 );

                if( si.Collisions.Any() )
                {
                    ci = col.p;
                    vi = col.v;
                    if( (Math.Abs( vi.magnitude ) > float.Epsilon) )
                        si.Velocity = vi.normalized * vim;
                }
                else
                {
                    ci += vi;
                }

                si.Center = ci;
            }
        }

        #endregion
    }

} // namespace Spehres
