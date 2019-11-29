using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace Spheres
{
    public class Grid : MonoBehaviour
    {
        public bool DrawGrid = true;
        public float Width;
        public float Height;
        public int Rows;
        public int Columns;

        public float CellX => Width / Columns;
        public float CellY => Height / Rows;

        public Hashtable _buckets = new Hashtable();


        private readonly int ThreadsNum = 4;
        private ManualResetEvent[] handles;
        private ManualResetEvent _critical;

        private Thread _physicsThread;
        private bool _isEnabled;

        private float _deltaTime;

        private GameplayGUI _gameplayGui;
        
        #region Overwrite methods

        private void Awake()
        {
            _critical = new ManualResetEvent(false);
            
            handles = new ManualResetEvent[ThreadsNum];
            for(var i = 0; i < ThreadsNum; i++)
                handles[i] = new ManualResetEvent(false);
        }

        void Start()
        {
            _gameplayGui = FindObjectOfType<GameplayGUI>();
        }
        void Update()
        {
            Interlocked.Exchange(ref _deltaTime,  _gameplayGui.Speed * Time.deltaTime);
        }

        void OnDrawGizmos()
        {
            if (!DrawGrid) return;

            Gizmos.color = Color.white;

            for (var r = 0; r <= Rows; r++)
                Gizmos.DrawLine(new Vector3(.0f, r * CellY, .0f), new Vector3(Width, r * CellY, .0f));

            for (var c = 0; c <= Columns; c++)
                Gizmos.DrawLine(new Vector3(c * CellX, .0f), new Vector3(c * CellX, Height, .0f));
        }

        void OnEnable()
        {
            _isEnabled = true;
            
            _physicsThread = new Thread(UpdateWorker);
//            _physicsThread.Start();
        }

        void OnDisable()
        {
            _isEnabled = false;
            
            foreach (var h in handles)
                h?.Set();
                
            _physicsThread.Abort();
            Thread.Sleep(50);
            
            handles = null;
        }

        #endregion

        #region Public methods 

        public bool IsIntersect(Sphere sphere)
        {
            var c = new Vector3Int(
                (int) (sphere.Center.x % Columns),
                (int) (sphere.Center.y % Rows),
                0);

            Vector3Int[] neighbors = {
                new Vector3Int(-1, 1, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(-1, -1, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, -1, 0),
                new Vector3Int(1, 1, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(1, -1, 0)
            };

            return neighbors.Any(n => IsIntersect(sphere, c + n)); 
//            return IsIntersect( sphere, x, y, 0 )
//                   || IsIntersect(sphere, x - 1, y + 1, 0 )
//                   || IsIntersect(sphere, x - 1, y, 0 )
//                   || IsIntersect(sphere, x - 1, y - 1, 0 )
//                   || IsIntersect(sphere, x, y + 1, 0 )
//                   || IsIntersect(sphere, x, y - 1, 0 )
//                   || IsIntersect(sphere, x + 1, y + 1, 0 )
//                   || IsIntersect(sphere, x + 1, y, 0 )
//                   || IsIntersect(sphere, x + 1, y - 1, 0 );
        }

        public bool IsIntersect(Sphere sphere, Vector3Int pos)
        {
            if( pos.x < 0 || Columns <= pos.x || pos.y < 0 || Rows <= pos.y || pos.z < 0 ) 
                return false;

            var key = GetHash(pos.x, pos.y);
            var bucket = _buckets[key];

            return bucket != null
                   && ((IList<Sphere>)bucket).Any(sphere.IsIntersect);
        }

        public Sphere Intersect(Sphere sphere)
        {
            var c = new Vector3Int(
                (int) (sphere.Center.x % Columns),
                (int) (sphere.Center.y % Rows),
                0);
            
            Vector3Int[] neighbors = {
                new Vector3Int(-1, 1, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(-1, -1, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, -1, 0),
                new Vector3Int(1, 1, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(1, -1, 0)
            };
            
            return neighbors.Select(n => Intersect(sphere, c + n)).FirstOrDefault(s => s != null);
        }
        
        public Sphere Intersect(Sphere sphere, Vector3Int pos)
        {
            if( pos.x < 0 || Columns <= pos.x || pos.y < 0 || Rows <= pos.y || pos.z < 0 ) 
                return null;

            var key = GetHash(pos.x, pos.y);
            var bucket = _buckets[key];
            
            return ((IList<Sphere>) bucket)?.FirstOrDefault(sphere.IsIntersect);
        }
        
        public void UpdateCollisions( Sphere sphere, int x, int y, int z = 0 )
        {
            if( x < 0 || Columns <= x || y < 0 || Rows <= y || z < 0 ) 
                return;
                
            var bucket = _buckets[GetHash(x, y)] as IList<Sphere>;
            if (bucket == null || bucket.Count == 0) return;
            
            var si = sphere;
            var ci = si.Center;
            
            si.Collisions.Clear();

            ci += si.Velocity * _deltaTime;

            // boundary
            if (ci.x - si.Radius < 0) {
                si.Velocity.x *= -1;
                ci.x = si.Radius;
            } else if (ci.x + si.Radius > Width) {
                si.Velocity.x *= -1;
                ci.x = Width - si.Radius;
            } if (ci.y - si.Radius < 0) {
                si.Velocity.y *= -1;
                ci.y = si.Radius;
            } else if (ci.y + si.Radius > Height) {
                si.Velocity.y *= -1;
                ci.y = Height - si.Radius;
            }

            // collision
//            for( var j = 0; j < bucket.Count; j++ )
//            {
//                var sj = bucket[j];
//                var cj = sj.Center;
//                
//                if(si == sj) continue;
//
//                if (si.IsIntersect(sj))
//                {
//                    si.Collisions.Add(sj);
//
//                    var d = si.Distance( sj );
//                    var overlap = 0.5f * (d - si.Radius - sj.Radius) / d;
//
//                    ci -= overlap * (ci - cj);
//                    cj += overlap * (ci - cj);
//                }
//
//                sj.Center = cj;
//            }

            si.Center = ci;
        }
        
        public void ClearBuckets()
        {
            _buckets.Clear();
        }

        public void AddSphere(Sphere sphere)
        {
            var key = GetHash(sphere.Center);
            var bucket = _buckets[key];
            if (bucket == null)
            {
                bucket = new List<Sphere>();
                _buckets[key] = bucket;
            }

            lock(bucket)
                ((IList<Sphere>)bucket).Add(sphere);
        }

        public void RemoveSphere(Sphere sphere)
        {
            // ...
        }

        public void UpdatePositions()
        {
            Debug.Log("UpdatePositions");
            
            var columnsPerThread = Columns / ThreadsNum;

            // found all collisions
            for (var i = 0; i <= columnsPerThread; i++)
            {
                for (var t = 0; t < ThreadsNum; t++)
                {
                    var c = i * columnsPerThread + t;
                    if (c >= Columns) continue;
                    
                    handles[t].Reset();
                    ThreadPool.QueueUserWorkItem( CollisionsWorker, new Tuple<int, int>(t, c) );
//                  CollisionsWorker( c );
                }

                WaitHandle.WaitAll( handles );
            }
            
            // update collided spheres velocity
            var cellsPerThread = Rows * Columns / ThreadsNum;
            var startCell = 0;
//            for (var t = 0; t < ThreadsNum - 1; t++)
//            {
//                ThreadPool.QueueUserWorkItem( VelocityUpdateWorker, new Range(startCell, cellsPerThread ));
//                startCell += cellsPerThread;
//            }
//            ThreadPool.QueueUserWorkItem( VelocityUpdateWorker, new Range( startCell, Rows * Columns - startCell + 1) );
//             VelocityUpdateWorker(new Range( 0, Rows * Columns) );
            
//            WaitHandle.WaitAll( handles );
            
            // update buckets
            startCell = 0;
            foreach (var handle in handles)
                handle.Reset();
            for (var t = 0; t < ThreadsNum - 1; t++)
            {
                ThreadPool.QueueUserWorkItem( BucketUpdateWorker, new Tuple<int, Range>(t, new Range(startCell, cellsPerThread )));
                startCell += cellsPerThread;
            }
            ThreadPool.QueueUserWorkItem( BucketUpdateWorker, new Tuple<int, Range>(ThreadsNum - 1, new Range( startCell, Rows * Columns - startCell + 1)));

//            BucketUpdateWorker( new Range( 0, Rows * Columns) );
            
            WaitHandle.WaitAll( handles );
        }

        #endregion

        #region Private methods

        private int GetHash( int x, int y, int z = 0)
            => x + Columns * y;

        private int GetHash(Vector3Int p)
            => GetHash(p.x % Columns, p.y % Rows, 0);

        private int GetHash(Vector3 position)
            => GetHash((int)(position.x % Columns), (int)(position.y % Rows), 0);

        private int GetHash(Sphere sphere)
            => GetHash(sphere.Center);


        private void UpdateWorker(object data)
        {
            while (_isEnabled)
            {
                Debug.Log("Thread udpate...");
                try
                {
                    UpdatePositions();
                }
                catch (ThreadInterruptedException)
                {
                    Debug.Log("Thread udpate...ThreadInterruptedException");
                }
                catch (ThreadAbortException)
                {
                    Debug.Log("Thread udpate...ThreadAbortException");
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                Thread.Sleep(100);
            }

            Debug.Log($"UpdateWorker finished");
        }

        public void CollisionsWorker(object data)
        {
            const short xr = 0b011011011;
            const short xl = 0b110110110;
            const short yt = 0b111111000;
            const short yb = 0b000111111;

            var (threadId, x) = (Tuple<int, int>) data;

            for( var y = 0; y < Rows; y++ )
            {
                var bucket = (IList<Sphere>)_buckets[GetHash(x, y)];
                if(bucket == null) continue;

                // update all spheres in current bucket
                foreach( var sphere in bucket )
                {
                    var mask = (sphere.Velocity.x < 0 ? xl : xr) & (sphere.Velocity.y < 0 ? yb : yt);

                    UpdateCollisions(sphere, x, y);
                    /*if((mask & 1 << 8) == 0)*/ //UpdateCollisions(sphere, x - 1, y + 1, 0);
                    /*if((mask & 1 << 7) == 0)*/ //UpdateCollisions(sphere, x, y + 1, 0);
                    /*if((mask & 1 << 6) == 0)*/ //UpdateCollisions(sphere, x + 1, y + 1, 0);
                    /*if((mask & 1 << 5) == 0)*/ //UpdateCollisions(sphere, x - 1, y, 0);
                    /*if((mask & 1 << 3) == 0)*/ //UpdateCollisions(sphere, x + 1, y, 0);
                    /*if((mask & 1 << 2) == 0)*/ //UpdateCollisions(sphere, x - 1, y - 1, 0);
                    /*if((mask & 1 << 1) == 0)*/ //UpdateCollisions(sphere, x, y - 1, 0);
                    /*if((mask & 1) == 0)*/      //UpdateCollisions(sphere, x + 1, y - 1, 0);
                }
            }

            handles[threadId].Set();
        }

        public void VelocityUpdateWorker(object data)
        {
            var (threadId, range) = (Tuple<int, Range>) data;
            var startCell = range.@from;
            var endCell = startCell + range.count;

            for (var c = startCell; c < endCell; c++)
            {
                if(!(_buckets[c] is List<Sphere> bucket)) continue;

                foreach (var sphere in bucket)
                {
                    // update velocity
                    if(!sphere.Collisions.Any()) continue;

                    var s1 = sphere;
                    var s2 = sphere.Collisions.OrderBy(s => sphere.Distance(s)).First();

                    var n = (s2.Center - s1.Center).normalized;
                    var dot = Vector3.Dot(s1.Velocity - s2.Velocity, n);
                    var k = 2.0f * dot / (s1.Radius + s2.Radius);
                    s1.Velocity -= k * s2.Radius * n;
                    s2.Velocity += k * s1.Radius * n;
                }
            }
            
            handles[threadId].Set();
        }
        
        public void BucketUpdateWorker(object data)
        {
            var (threadId, range) = (Tuple<int, Range>) data;
            var startCell = range.@from;
            var endCell = startCell + range.count;

            for (var c = startCell; c < endCell; c++)
            {
                if(!(_buckets[c] is List<Sphere> bucket)) continue;

                foreach (var s in bucket)
                    s.Collisions.Clear();
                
                lock (bucket)
                {
                    Debug.Log($"Change sphere bucket: {c}");
                    
                    foreach (var s in bucket.Where(s => c != GetHash(s)))
                        AddSphere(s);
                    bucket.RemoveAll(s => c != GetHash(s));
                }
            }
            
            handles[threadId].Set();
        }
        
        #endregion
    }

} // namespace Spehres
