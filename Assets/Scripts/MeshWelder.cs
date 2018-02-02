using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
	Originally created by Bunny83.
	Was streamlined and modified a bit by Terrev to better fit the needs of this project,
	then overhauled by grappigegovert for major speed improvements.
	Bunny83's original version:
	https://www.dropbox.com/s/u0wfq42441pkoat/MeshWelder.cs?dl=0
	Which was posted here:
	http://answers.unity3d.com/questions/1382854/welding-vertices-at-runtime.html
*/

namespace B83.MeshHelper
{
    public enum EVertexAttribute
    {
        Position = 0x0001,
        Normal = 0x0002,
        UV1 = 0x0010,
    }

    public class Vertex
    {
        public Vector3 pos;
        public Vector3 normal;
        public Vector2 uv1;

        public Vertex(Vector3 aPos)
        {
            pos = aPos;
        }

        public override bool Equals(object obj)
        {
            Vertex other = obj as Vertex;
            if (other != null)
            {
                return other.pos.Equals(pos) && other.normal.Equals(normal) && other.uv1.Equals(uv1);
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = pos.x.GetHashCode();
                hashCode = (hashCode * 397) ^ pos.y.GetHashCode();
                hashCode = (hashCode * 397) ^ pos.z.GetHashCode();
                hashCode = (hashCode * 397) ^ normal.x.GetHashCode();
                hashCode = (hashCode * 397) ^ normal.z.GetHashCode();
                hashCode = (hashCode * 397) ^ normal.y.GetHashCode();
                return hashCode;
            }
        }
    }

    public class MeshWelder
    {
        Vertex[] vertices;
        Dictionary<Vertex, List<int>> newVerts;
        int[] map;

        EVertexAttribute m_Attributes;
        public CustomMesh customMesh;

        private bool HasAttr(EVertexAttribute aAttr)
        {
            return (m_Attributes & aAttr) != 0;
        }

        private void CreateVertexList()
        {
            var Positions = customMesh.vertices;
            var Normals = customMesh.normals;
            var Uv1 = customMesh.uv;
            m_Attributes = EVertexAttribute.Position;
            if (Normals != null && Normals.Length > 0) m_Attributes |= EVertexAttribute.Normal;
            if (Uv1 != null && Uv1.Length > 0) m_Attributes |= EVertexAttribute.UV1;

            vertices = new Vertex[Positions.Length];
            for (int i = 0; i < Positions.Length; i++)
            {
                var v = new Vertex(Positions[i]);
                v.normal = Normals[i];
                if (HasAttr(EVertexAttribute.UV1)) v.uv1 = Uv1[i];
                vertices[i] = v;
            }
        }

        private void RemoveDuplicates()
        {
            newVerts = new Dictionary<Vertex, List<int>>(vertices.Length);
            for(int i = 0; i < vertices.Length; i++)
            {
                Vertex v = vertices[i];
                List<int> originals;
                if (newVerts.TryGetValue(v, out originals))
                {
                    originals.Add(i);
                }
                else
                {
                    newVerts.Add(v, new List<int> {i});
                }
            }
        }

        private void AssignNewVertexArrays()
        {
            map = new int[vertices.Length];
            customMesh.vertices = new Vector3[newVerts.Count];
            customMesh.normals = new Vector3[newVerts.Count];
            if (HasAttr(EVertexAttribute.UV1))
                customMesh.uv = new Vector2[newVerts.Count];
            int i = 0;
            foreach (KeyValuePair<Vertex, List<int>> kvp in newVerts)
            {
                foreach (int index in kvp.Value)
                {
                    map[index] = i;
                }
                customMesh.vertices[i] = kvp.Key.pos;
                customMesh.normals[i] = kvp.Key.normal;
                if (HasAttr(EVertexAttribute.UV1))
                    customMesh.uv[i] = kvp.Key.uv1;
                i++;
            }
        }

        private void RemapTriangles()
        {
            int[] tris = customMesh.triangles;
            for (int i = 0; i < tris.Length; i++)
            {
                tris[i] = map[tris[i]];
            }
            customMesh.triangles = tris;
        }

        public void Weld(bool hasUV)
        {
            CreateVertexList();
            RemoveDuplicates();
            AssignNewVertexArrays();
            RemapTriangles();
        }
    }
}
