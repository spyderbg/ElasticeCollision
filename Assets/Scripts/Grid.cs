using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml;
using Common;
using UnityEditor.UI;
using UnityEngine;

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
        private AutoResetEvent[] aHandles;

        #region Overwrite methods

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
            handles = new ManualResetEvent[ThreadsNum];
            aHandles = new AutoResetEvent[ThreadsNum];
        }

        void OnDisable()
        {
            if( handles != null )
            {
                foreach (var h in handles)
                    h.Set();
                handles = null;
            }

            if( aHandles != null )
            {
                foreach (var h in aHandles)
                    h.Set();
                aHandles = null;
            }
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

            var bucket = _buckets[GetHash(x, y)];

//             bucket != null
//                   && ((IList<Sphere>)bucket).Any(sphere.IsIntersect);
            
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

            ((IList<Sphere>)bucket).Add(sphere);
        }

        public void UpdatePositions()
        {
            var columnsPerThread = Columns / ThreadsNum;

            for (var i = 0; i <= columnsPerThread; i++)
            {
                for (var t = 0; t < ThreadsNum; t++)
                {
                    var c = i * columnsPerThread + t;
                    if( c < Columns )
                    {
                        // thread 't' updates column 'c'
                        ThreadPool.QueueUserWorkItem( Worker, c );
                    }
                }

                // wait all threads
                WaitHandle.WaitAll( aHandles );
            }
        }

        #endregion

        #region Private methods

        private int GetHash( int x, int y, int z = 0)
            => x + Columns * y;

        private int GetHash(Vector3Int p)
            => GetHash(p.x % Columns, p.y % Rows, 0);

        private int GetHash(Vector3 position)
            => GetHash((int)(position.x % Columns), (int)(position.y % Rows), 0);

        private void Worker(object column)
        {
            const short xr = 0b011011011;
            const short xl = 0b110110110;
            const short yt = 0b111111000;
            const short yb = 0b000111111;

            var x = (int)column;

            for( var y = 0; y < Rows; y++ )
            {
                var bucket = (IList<Sphere>)_buckets[GetHash(x, y)];
                if(bucket == null) continue;

                // update all spheres in current bucket
                foreach( var sphere in bucket )
                {
                    var mask = (sphere.Velocity.x < 0 ? xl : xr) & (sphere.Velocity.y < 0 ? yb : yt);

                    UpdateCollisions(sphere, x, y);
                    if((mask & 1 << 8) == 0) UpdateCollisions(sphere, x - 1, y + 1, 0);
                    if((mask & 1 << 7) == 0) UpdateCollisions(sphere, x, y + 1, 0);
                    if((mask & 1 << 6) == 0) UpdateCollisions(sphere, x + 1, y + 1, 0);
                    if((mask & 1 << 5) == 0) UpdateCollisions(sphere, x - 1, y, 0);
                    if((mask & 1 << 3) == 0) UpdateCollisions(sphere, x + 1, y, 0);
                    if((mask & 1 << 2) == 0) UpdateCollisions(sphere, x - 1, y - 1, 0);
                    if((mask & 1 << 1) == 0) UpdateCollisions(sphere, x, y - 1, 0);
                    if((mask & 1) == 0) UpdateCollisions(sphere, x + 1, y - 1, 0);
                }
            }





        }

        #endregion
    }

} // namespace Spehres
