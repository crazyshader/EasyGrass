using System;
using UnityEngine;

namespace EasyFramework.Grass.Runtime
{
    public struct BuildMesh : IDisposable
    {
        private Mesh _defaultQuad;
        public Mesh BuildQuad()
        {
            if (_defaultQuad != null)
                return _defaultQuad;

            float width = 1;
            float height = 1;
            var vertCount = 4;
            var triCount = 6;
            var vertices = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var triangles = new int[triCount];

            vertices[0] = new Vector3(-width * 0.5f, height, 0);
            vertices[1] = new Vector3(width * 0.5f, height, 0);
            vertices[2] = new Vector3(width * 0.5f, 0, 0);
            vertices[3] = new Vector3(-width * 0.5f, 0, 0);
            normals[0] = Vector3.up;
            normals[1] = Vector3.up;
            normals[2] = Vector3.forward;
            normals[3] = Vector3.forward;
            uvs[0] = new Vector2(0f, 1f);
            uvs[1] = new Vector2(1f, 1f);
            uvs[2] = new Vector2(1f, 0f);
            uvs[3] = new Vector2(0f, 0f);
            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            triangles[3] = 2;
            triangles[4] = 3;
            triangles[5] = 0;

            _defaultQuad = new Mesh();
            _defaultQuad.vertices = vertices;
            _defaultQuad.normals = normals;
            _defaultQuad.triangles = triangles;
            _defaultQuad.SetUVs(0, uvs);
            return _defaultQuad;
        }

        public void Dispose()
        {
            if (_defaultQuad != null)
            {
                SafeDestroy(_defaultQuad);
            }
        }

        private void SafeDestroy(Mesh mesh)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(mesh);
            else
                UnityEngine.Object.DestroyImmediate(mesh);
        }
    }
}