using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Bolt;
using Ludiq.FullSerializer;
using MiscUtil.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Spheres
{
    [ExecuteInEditMode]
    public class Field : MonoBehaviour
    {
        public GameObject Grid;
        public GameObject Spheres; // manual alignment
        public int SphereNumbers;
        private IList<Sphere> _spheres;

        public float MinRadius = .02f;
        public float MaxRadius = .05f;

        public float MinVelocity = .001f;
        public float MaxVelocity = .003f;

        public Mesh InstanceMesh;
        public Material InstanceMaterial;

        private ComputeBuffer _positionBuffer;
        private ComputeBuffer _argsBuffer;
        private int _cachedInstanceCount = -1;
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };


        void Start()
        {
            OnGridUpdate();
        }

        void Update()
        {
            UpdatePositions();
            Draw();
        }

        public static  int GlobalInt;
        private static object _obj = new object();

        private static ManualResetEvent[] handles = new ManualResetEvent[3];
        static void Worker(object p)
        {
            //            Debug.Log( $"Worker: {p}"  );

            for (int i = 0; i < 1000; i++)
            {
//                Interlocked.Increment(ref GlobalInt);
//                lock (_obj)
                {

//                    GlobalInt++;
                    Thread.Sleep( 1 );
                }
            }

            handles[(int) p].Set();
        }
        void OnEnable()
        {
            GlobalInt = 0;
            
            var startTime = Time.realtimeSinceStartup;

//            for( int i = 0; i < 3; i++ )
//            {
//                handles[i] = new ManualResetEvent( false );
//            }

//            var pts = new ParameterizedThreadStart(Worker);
//            var t1 = new Thread( pts );
//            var t2 = new Thread( pts );
//            var t3 = new Thread( pts );

//            t1.Start(1);
//            t2.Start(2);
//            t3.Start(2);

//            ThreadPool.QueueUserWorkItem( Worker, 0 );
//            ThreadPool.QueueUserWorkItem( Worker, 1 );
//            ThreadPool.QueueUserWorkItem( Worker, 2 );

//            var joinTime = Time.realtimeSinceStartup;
//            WaitHandle.WaitAll( handles );
//            t1.Join();
//            t2.Join();
//            t3.Join();

//            var duration = Time.realtimeSinceStartup - startTime;
//            var execDuration = Time.realtimeSinceStartup - joinTime;
//            Debug.Log($"globalIn: {GlobalInt} duration: {duration} joinDuration: {execDuration}");
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
            var grid = Grid.GetComponent<Grid>();

            var camera = Camera.main;
            var pos = camera.transform.position;
            camera.transform.position = new Vector3(grid.Width / 2.0f, grid.Height / 2.0f, pos.z);
        }

        #region Randomize methos

        public void RandomizeSpheres()
        {
            var startTime = Time.realtimeSinceStartup;
            
            var grid = Grid.GetComponent<Grid>();

            // validate input data 
            if( grid.CellX < MaxRadius * 2.0f || grid.CellY < MaxRadius * 2.0f )
            {
                Debug.LogError( $"Input parameters error: circle radius too large, reduce it and try again!" );
	            //return;
            }
            if(!SystemInfo.supportsInstancing)
            {
                Debug.LogError( $"GPU instancing not supported" );
                return;
            }

            // create spheres
            _spheres = Enumerable.Range( 1, SphereNumbers )
                .Select( s => new Sphere(Random.Range( MinRadius, MaxRadius )))
                .ToList();

            foreach( var sphere in _spheres.AsEnumerable() )
            {
                var velocity = new Vector3() {
                    x = Random.Range(-1.0f, 1.0f),
                    y = Random.Range(-1.0f, 1.0f),
                    z = .0f
                };

                sphere.Velocity = Vector3.ClampMagnitude( velocity, Random.Range( MinVelocity, MaxVelocity ));
            }

            // position spheres
            compIntersect = 0;
            compensateCount = 0;
            
            grid.ClearBuckets();
            foreach( var sphere in _spheres )
            {
                if(PositionSphere( sphere, true ))
                    grid.AddSphere( sphere );
            }
            
            Debug.Log($"compIntersect: {compIntersect} compensateCount: {compensateCount}");

            // update instanced buffers
//            UpdateBuffers();
            Debug.Log($"Randomize spheres duration {Time.realtimeSinceStartup - startTime}");

//            PrintIntersections();
        }

        public void PrintIntersections()
        {
            if( _spheres == null ) return;

            var intersectCount = _spheres.Sum( sphere => _spheres.Sum( (s) => sphere.IsIntersect( s ) ? 1 : 0));
            intersectCount -= _spheres.Count; // remove self intersections
            intersectCount /= 2; // remove mirror intersections
            Debug.Log( $"Sphere intersections count: {intersectCount}" );
        }

        public void SyncWithVisualSpheres()
        {
            if( _spheres == null ) return;

            var i = 0;
            foreach( var t in Spheres.transform.GetComponentsInChildren<MeshFilter>() )
                _spheres[i++].Center = t.transform.position;
        }

        private int compIntersect = 0;
        private int compensateCount = 0;

        private bool PositionSphere(Sphere sphere, bool checkOverlaping = true)
        {
            var maxAttempts = 100;
            var grid = Grid.GetComponent<Grid>();

            do {
                var center = sphere.Center;
                center.x = Random.Range( MaxRadius, grid.Width - MaxRadius );
                center.y = Random.Range( MaxRadius, grid.Height - MaxRadius );
                sphere.Center = center;

                if (!checkOverlaping) break;

                if (grid.Intersect(sphere) == null)
                    break;
                continue;

                var intersectSphere = grid.Intersect(sphere);
                if (intersectSphere == null) break;
                
                compIntersect++;
                var d = sphere.Distance(intersectSphere);
                var overlap = d - sphere.Radius - intersectSphere.Radius;
                center.x -= overlap * (center.x - intersectSphere.Center.x) / d;
                center.y -= overlap * (center.y - intersectSphere.Center.y) / d;
                sphere.Center = center;

                if (grid.IsIntersect(sphere)) continue;
                
                compensateCount++;
                break;
            } while( --maxAttempts > 0 );

            return maxAttempts > 0;
        }

        #endregion

        #region Draw methods

        public void Draw()
        {
//            DrawSpheresMono();
//            DrawSpheresMesh();
            DrawSpheresByBuckets();
//            DrawSpheresIndirect();
        }

        public void ClearSpheresMono()
        {
            foreach( var t in Spheres.transform.GetComponentsInChildren<MeshFilter>() )
                DestroyImmediate(t.gameObject);
        }

        public void DrawSpheresMono()
        {
            if( _spheres == null ) return;

            ClearSpheresMono();

            for( var i = 0; i < _spheres.Count; i++ )
            {
                var sphere = _spheres[i];
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"sphere_{i}";
                go.transform.position = sphere.Center;
                go.transform.localScale = new Vector3(2.0f * sphere.Radius, 2.0f * sphere.Radius, .01f);
                go.transform.parent = Spheres.transform;
                go.GetComponent<Renderer>().sharedMaterial.color = Color.white;
            }
        }

        public void DrawSpheresMesh()
        {
            if( _spheres == null ) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            var material = go.GetComponent<Renderer>().sharedMaterial; material.color = Color.red;
            DestroyImmediate(go);

            foreach( var sphere in _spheres )
            {
                var matrix = Matrix4x4.Translate(sphere.Center) *
                             Matrix4x4.Scale(new Vector3(2.0f * sphere.Radius, 2.0f * sphere.Radius, 2.0f * sphere.Radius));

                Graphics.DrawMesh( mesh, matrix, material, 0 );
            }
        }

        public void DrawSpheresByBuckets()
        {
//            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
//            var material = go.GetComponent<Renderer>().sharedMaterial; material.color = Color.red;
//            DestroyImmediate(go);
            
            var grid = Grid.GetComponent<Grid>();
            for (var i = 0; i < grid.Rows * grid.Height; i++)
            {
                if(!(grid._buckets[i] is IList<Sphere> bucket)) continue;
                
                foreach( var sphere in bucket ) 
                {
                    var matrix = Matrix4x4.Translate(sphere.Center) *
                                 Matrix4x4.Scale(new Vector3(2.0f * sphere.Radius, 2.0f * sphere.Radius, 2.0f * sphere.Radius));

                    Graphics.DrawMesh( InstanceMesh, matrix, InstanceMaterial, 0 );
                }
            }
        }

        public void DrawSpheresIndirect()
        {
            if( _spheres == null ) return;

            Graphics.DrawMeshInstancedIndirect(InstanceMesh, 0, InstanceMaterial, new UnityEngine.Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), _argsBuffer);
        }

        private void UpdateBuffers()
        {
            if( _spheres == null ) return;

            if (_argsBuffer == null)
                _argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

            _cachedInstanceCount = _spheres.Count;

            // set positions 
            _positionBuffer?.Release();
            _positionBuffer = new ComputeBuffer(_cachedInstanceCount, 16 );
            var positions = new Vector4[_cachedInstanceCount];
            for( var i = 0; i < _cachedInstanceCount; i++ )
            {
                var sphere = _spheres[i];
                positions[i] = new Vector4(sphere.Center.x, sphere.Center.y, sphere.Center.z, sphere.Radius);
            }
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

        private void UpdatePositions()
        {
            if( _spheres == null ) return;

            var grid1 = Grid.GetComponent<Grid>();

            var collisions = new List<Tuple<Sphere, Sphere>>();

            for (var i = 0; i < _spheres.Count; i++)
            {
                var si = _spheres[i];
                var ci = si.Center;

                ci += si.Velocity * Time.deltaTime;

                // boundary
                if (ci.x - si.Radius < 0)
                {
                    si.Velocity.x *= -1;
                    ci.x = si.Radius;
                }
                else if (ci.x + si.Radius > grid1.Width)
                {
                    si.Velocity.x *= -1;
                    ci.x = grid1.Width - si.Radius;
                }
                if (ci.y - si.Radius < 0)
                {
                    si.Velocity.y *= -1;
                    ci.y = si.Radius;
                }
                else if (ci.y + si.Radius > grid1.Height)
                {
                    si.Velocity.y *= -1;
                    ci.y = grid1.Height - si.Radius;
                }

                // collision
                for( var j = 0; j < i; j++ )
                {
                    var sj = _spheres[j];
                    var cj = sj.Center;

                    if (si.IsIntersect(sj))
                    {
                        collisions.Add( new Tuple<Sphere, Sphere>(si, sj) );

                        var d = si.Distance( sj );
                        var overlap = 0.5f * (d - si.Radius - sj.Radius) / d;

                        ci -= overlap * (ci - cj);
                        cj += overlap * (ci - cj);
                    }

                    sj.Center = cj;
                }

                si.Center = ci;
            }
            
            // update collided circles velocity
            // ref: https://en.wikipedia.org/wiki/Elastic_collision
            foreach(var pair in collisions)
            {
                var s1 = pair.Item1;
                var s2 = pair.Item2;

                var n = (s2.Center - s1.Center).normalized;
                var dot = Vector3.Dot(s1.Velocity - s2.Velocity, n);
                var k = 2.0f * dot / (s1.Radius + s2.Radius);
                s1.Velocity -= k * s2.Radius * n;
                s2.Velocity += k * s1.Radius * n;

//                var d12 = (s1.Center - s2.Center).normalized;
//                var d21 = (s2.Center - s1.Center).normalized;
//                var k = 2.0f / (s1.Radius + s2.Radius);
//                var dot1 = Vector3.Dot(s1.Velocity - s2.Velocity, d12);
//                var dot2 = Vector3.Dot(s2.Velocity - s1.Velocity, d21);

//                s1.Velocity -=  k * s2.Radius * dot1 * d12;
//                s2.Velocity += k * s1.Radius * dot2 * d12;
            }

            UpdateBuffers();
        }

        #endregion
    }

} // namespace Spehres
