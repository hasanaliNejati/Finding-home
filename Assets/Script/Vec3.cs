using UnityEngine;

namespace Script
{
    public struct Vec3 {
        public float x, y, z;
        public Vec3(float x, float y, float z = 0) { this.x=x; this.y=y; this.z=z; }
        public static implicit operator Vec3(Vector3 v) => new(v.x, v.y, v.z);
        public static implicit operator Vector3(Vec3 v) => new(v.x, v.y, v.z);
    }
}