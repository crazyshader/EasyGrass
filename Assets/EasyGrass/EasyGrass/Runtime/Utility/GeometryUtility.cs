using UnityEngine;

namespace EasyFramework.Grass.Runtime
{
    public class GeometryUtility
    {
        public enum TestPlanesResults
        {
            /// <summary>
            /// The AABB is completely in the frustrum.
            /// </summary>
            Inside = 0,
            /// <summary>
            /// The AABB is partially in the frustrum.
            /// </summary>
            Intersect,
            /// <summary>
            /// The AABB is completely outside the frustrum.
            /// </summary>
            Outside
        }

        /// <summary>
        /// This is crappy performant, but easiest version of TestPlanesAABBFast to use.
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public static TestPlanesResults TestPlanesAABB(Plane[] planes, ref Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;

            return TestPlanesAABBFast(planes, ref min, ref max, true);
        }

        /// <summary>
        /// This is crappy performant, but easiest version of TestPlanesAABBFast to use.
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public static TestPlanesResults TestPlanesAABBFast(Plane[] planes, ref Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;

            return TestPlanesAABBFast(planes, ref min, ref max);
        }

        /// <summary>
        /// This is a faster AABB cull than brute force that also gives additional info on intersections.
        /// Calling Bounds.Min/Max is actually quite expensive so as an optimization you can precalculate these.
        /// http://www.lighthouse3d.com/tutorials/view-frustum-culling/geometric-approach-testing-boxes-ii/
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="boundsMin"></param>
        /// <param name="boundsMax"></param>
        /// <returns></returns>
        public static TestPlanesResults TestPlanesAABBFast(Plane[] planes, ref Vector3 boundsMin, ref Vector3 boundsMax, bool testIntersection = false)
        {
            Vector3 vmin, vmax;
            var testResult = TestPlanesResults.Inside;

            for (int planeIndex = 0; planeIndex < planes.Length; planeIndex++)
            {
                var normal = planes[planeIndex].normal;
                var planeDistance = planes[planeIndex].distance;

                // X axis
                if (normal.x < 0)
                {
                    vmin.x = boundsMin.x;
                    vmax.x = boundsMax.x;
                }
                else
                {
                    vmin.x = boundsMax.x;
                    vmax.x = boundsMin.x;
                }

                // Y axis
                if (normal.y < 0)
                {
                    vmin.y = boundsMin.y;
                    vmax.y = boundsMax.y;
                }
                else
                {
                    vmin.y = boundsMax.y;
                    vmax.y = boundsMin.y;
                }

                // Z axis
                if (normal.z < 0)
                {
                    vmin.z = boundsMin.z;
                    vmax.z = boundsMax.z;
                }
                else
                {
                    vmin.z = boundsMax.z;
                    vmax.z = boundsMin.z;
                }

                var dot1 = normal.x * vmin.x + normal.y * vmin.y + normal.z * vmin.z;
                if (dot1 + planeDistance < 0)
                    return TestPlanesResults.Outside;

                if (testIntersection)
                {
                    var dot2 = normal.x * vmax.x + normal.y * vmax.y + normal.z * vmax.z;
                    if (dot2 + planeDistance <= 0)
                        testResult = TestPlanesResults.Intersect;
                }
            }

            return testResult;
        }

        private static Plane[] _planes = new Plane[6];
        public static Plane[] CalculateFrustumPlanes(Matrix4x4 mat)
        {
            // left
            _planes[0].normal = new Vector3(mat.m30 + mat.m00, mat.m31 + mat.m01, mat.m32 + mat.m02);
            _planes[0].distance = mat.m33 + mat.m03;

            // right
            _planes[1].normal = new Vector3(mat.m30 - mat.m00, mat.m31 - mat.m01, mat.m32 - mat.m02);
            _planes[1].distance = mat.m33 - mat.m03;

            // bottom
            _planes[2].normal = new Vector3(mat.m30 + mat.m10, mat.m31 + mat.m11, mat.m32 + mat.m12);
            _planes[2].distance = mat.m33 + mat.m13;

            // top
            _planes[3].normal = new Vector3(mat.m30 - mat.m10, mat.m31 - mat.m11, mat.m32 - mat.m12);
            _planes[3].distance = mat.m33 - mat.m13;

            // near
            _planes[4].normal = new Vector3(mat.m30 + mat.m20, mat.m31 + mat.m21, mat.m32 + mat.m22);
            _planes[4].distance = mat.m33 + mat.m23;

            // far
            _planes[5].normal = new Vector3(mat.m30 - mat.m20, mat.m31 - mat.m21, mat.m32 - mat.m22);
            _planes[5].distance = mat.m33 - mat.m23;

            // normalize
            for (uint i = 0; i < 6; i++)
            {
                float length = _planes[i].normal.magnitude;
                _planes[i].normal /= length;
                _planes[i].distance /= length;
            }

            return _planes;
        }
    }
}
