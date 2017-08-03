using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
	Originally created by Bunny83, modified to better fit the needs of this project + speed improvements
	Could still probably be made to run faster...
	http://answers.unity3d.com/questions/1382854/welding-vertices-at-runtime.html
	https://www.dropbox.com/s/u0wfq42441pkoat/MeshWelder.cs?dl=0
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
    }
	
    public class MeshWelder
    {
	
        Vertex[] vertices;
        List<Vertex> newVerts;
        int[] map;
		
        EVertexAttribute m_Attributes;
        public CustomMesh customMesh;
		
        private bool HasAttr(EVertexAttribute aAttr)
        {
            return (m_Attributes & aAttr) != 0;
        }
		
        private bool Compare(Vertex v1, Vertex v2)
        {
			// Saved y for last because there's likely to be tons of verts with the same y value but not as many with x and z
			if (v1.pos.x != v2.pos.x) return false;
			if (v1.pos.z != v2.pos.z) return false;
			if (v1.pos.y != v2.pos.y) return false;
			
			if (v1.normal.x != v2.normal.x) return false;
			if (v1.normal.y != v2.normal.y) return false;
			if (v1.normal.z != v2.normal.z) return false;
			return true;
        }
		
        private bool CompareWithUV(Vertex v1, Vertex v2)
        {
			// Saved y for last because there's likely to be tons of verts with the same y value but not as many with x and z
			if (v1.pos.x != v2.pos.x) return false;
			if (v1.pos.z != v2.pos.z) return false;
			if (v1.pos.y != v2.pos.y) return false;
			
			if (v1.normal.x != v2.normal.x) return false;
			if (v1.normal.y != v2.normal.y) return false;
			if (v1.normal.z != v2.normal.z) return false;
			
			if (v1.uv1.x != v2.uv1.x) return false;
			if (v1.uv1.y != v2.uv1.y) return false;
            return true;
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
            map = new int[vertices.Length];
            newVerts = new List<Vertex>();
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                bool dup = false;
                for (int i2 = 0; i2 < newVerts.Count; i2++)
                {
                    if (Compare(v, newVerts[i2]))
                    {
                        map[i] = i2;
                        dup = true;
                        break;
                    }
                }
                if (!dup)
                {
                    map[i] = newVerts.Count;
                    newVerts.Add(v);
                }
            }
        }
        private void RemoveDuplicatesWithUV()
        {
            map = new int[vertices.Length];
            newVerts = new List<Vertex>();
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                bool dup = false;
                for (int i2 = 0; i2 < newVerts.Count; i2++)
                {
                    if (CompareWithUV(v, newVerts[i2]))
                    {
                        map[i] = i2;
                        dup = true;
                        break;
                    }
                }
                if (!dup)
                {
                    map[i] = newVerts.Count;
                    newVerts.Add(v);
                }
            }
        }
        private void AssignNewVertexArrays()
        {
            customMesh.vertices = newVerts.Select(v => v.pos).ToArray();
            customMesh.normals = newVerts.Select(v => v.normal).ToArray();
            if (HasAttr(EVertexAttribute.UV1))
                customMesh.uv = newVerts.Select(v => v.uv1).ToArray();
        }
		
        private void RemapTriangles()
        {
            var tris = customMesh.triangles;
            for (int i = 0; i < tris.Length; i++)
            {
                tris[i] = map[tris[i]];
            }
            customMesh.triangles = tris;
        }
        public void Weld(bool hasUV)
        {
            CreateVertexList();
			if (hasUV)
				RemoveDuplicatesWithUV();
			else
				RemoveDuplicates();
            RemapTriangles();
            AssignNewVertexArrays();
        }
    }
}
