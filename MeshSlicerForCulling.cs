// Copyright (c) 2018 Archy Piragkov. All Rights Reserved.  Licensed under the MIT license
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


namespace Artics.EditorUtils
{
    #region Slicer
    /// <summary>
    /// Slices big meshes into smaller parts to get better performance with occlusion culling.
    ///
    /// I wrote this component for my project and I think it can be useful for your projects too :) Currently, it supports only slicing by X and Y axes. It's don't separates triangles if they are in different parts, so use it only for high poly meshes Maybe I'll improve in the future.

    /// Using is simple.
    /// 1. Drag here gameobject which contains MeshFilter object of mesh which you want to slice.
    /// 2. Set a default material for new parts
    /// 3. Set number of parts by vertical and horizontal
    /// 4. Press thew "Slice bounded" button
    ///
    ///You can also use second methotd, but it's rather a crutch. I just wrote it for the test and decided to left him.
    ///
    ///To test bounds of any mesh - drag it gameobject to BoundsVisualizer and press visualize bounds.
    /// </summary>
    public class MeshSlicerForCulling : MonoBehaviour
    {
        #region Variables

        public GameObject Container;
        public Material DefaultMaterial;

        public int Parts;
        public int Limit;

        public GameObject BoundsVisualizer;

        protected Bounds[] Bounds;
        protected Bounds VisualizeBounds;

        #endregion

        #region BoundsMethod         

        [NaughtyAttributes.Button("Slice bounded")]
        public void SliceBounded()
        {
            transform.position = Container.transform.position;

            GameObject newParent = new GameObject($"[Sliced] {Container.name}");
            newParent.transform.position = Container.transform.position;
            var baseMesh = Container.GetComponent<MeshFilter>().sharedMesh;

            var meshCache = new MeshCache() { Instance = baseMesh, Vertices = baseMesh.vertices, Triangles = baseMesh.triangles, Normals = baseMesh.normals, UVs = baseMesh.uv };

            //bounds  preparations
            int boundsParts = Parts * Parts;
            var boundsArr = new Bounds[boundsParts];

            baseMesh.RecalculateBounds();
            var meshBounds = baseMesh.bounds;

            Vector3 size = meshBounds.size / Parts;
            size.z = meshBounds.size.z;

            //bounds calculations
            for (int x = 0; x < Parts; x++)
                for (int y = 0; y < Parts; y++)
                    boundsArr[x * Parts + y] = new Bounds(meshBounds.min + new Vector3(size.x * x, size.y * y, 0) + size * 0.5f, size);

            Bounds = boundsArr;

            //triangle-bounds mask
            var trianglesMask = new int[meshCache.Triangles.Length];

            for (int i = 0; i < trianglesMask.Length; i++)
                trianglesMask[i] = -1;

            //creatig meshes
            for (int i = 0; i < boundsParts; i++)
            {
                FindstrianglesInBounds(boundsArr[i], i, trianglesMask, meshCache);
                CreateMeshFromBoundMask(i, trianglesMask, meshCache, newParent.transform);
            }

        }

        public void FindstrianglesInBounds(Bounds bounds, int id, int[] trianglesMask, MeshCache cache)
        {
            for (int i = 0; i < trianglesMask.Length; i += 3)
                if (trianglesMask[i] != -1 && bounds.Contains(cache.Vertices[cache.Triangles[i]]) || bounds.Contains(cache.Vertices[cache.Triangles[i + 1]]) || bounds.Contains(cache.Vertices[cache.Triangles[i + 2]]))
                    trianglesMask[i] = trianglesMask[i + 1] = trianglesMask[i + 2] = id;
        }


        public void CreateMeshFromBoundMask(int id, int[] boundsMask, MeshCache cache, Transform parent = null)
        {
            var boundedTriangles = new List<int>();

            for (int i = 0; i < boundsMask.Length; i++)
                if (boundsMask[i] == id)
                    boundedTriangles.Add(cache.Triangles[i]);

            if (boundedTriangles.Count == 0)
            {
                Debug.LogWarning($"Sector:{id} - no triangles found");
                return;
            }

            int TrianglesPartAmmount = boundedTriangles.Count;
            var vertexesDictionary = new Dictionary<int, Vector3>();
            var triangles = cache.Triangles;
            var vertices = cache.Vertices;

            for (int i = 0; i < TrianglesPartAmmount; i++)
                vertexesDictionary[boundedTriangles[i]] = vertices[boundedTriangles[i]];

            //new vertices
            var newVertices = vertexesDictionary.Values.ToArray();

            //new triangles
            var keysList = vertexesDictionary.Keys.ToList();
            var newTriangles = new int[TrianglesPartAmmount];

            for (int i = 0; i < TrianglesPartAmmount; i++)
                newTriangles[i] = keysList.IndexOf(boundedTriangles[i]);

            int verticesPartAmmount = keysList.Count;
            //uv
            var oldUV = cache.UVs;
            var newUV = new Vector2[verticesPartAmmount];

            for (int i = 0; i < verticesPartAmmount; i++)
                newUV[i] = oldUV[keysList[i]];

            //normals
            var oldNormals = cache.Normals;
            var newNormals = new Vector3[verticesPartAmmount];

            for (int i = 0; i < verticesPartAmmount; i++)
                newNormals[i] = oldNormals[keysList[i]];

            //creating GameObject

            var child = new GameObject($"Sector {id}");
            child.transform.parent = parent;
            child.transform.localPosition = Vector3.zero;

            var newMesh = new Mesh();
            newMesh.vertices = newVertices;
            newMesh.triangles = newTriangles;
            newMesh.uv = newUV;
            newMesh.normals = newNormals;

            newMesh.RecalculateBounds();


            child.AddComponent<MeshFilter>().mesh = newMesh;
            child.AddComponent<MeshRenderer>().material = DefaultMaterial;
        }

        #endregion

        #region PolygonMethod

        [NaughtyAttributes.Button("Slice")]
        public void SimpleSlice()
        {
            GameObject newParent = new GameObject();
            newParent.transform.position = Container.transform.position;

            var baseMesh = Container.GetComponent<MeshFilter>().sharedMesh;

            int facesNum = baseMesh.triangles.Length / 3;
            int partFacesAmmount = facesNum / Parts;
            int TrianglesPartAmmount = partFacesAmmount * 3;

            for (int i = 0; i < Parts - 1; i++)
                CreateMeshFromRange(i * partFacesAmmount, (i + 1) * partFacesAmmount, baseMesh, newParent.transform);

            CreateMeshFromRange(partFacesAmmount * (Parts - 1), facesNum, baseMesh, newParent.transform);
        }

        public void CreateMeshFromRange(int startTriangle, int endTriangle, Mesh baseMesh, Transform parent = null)
        {
            int trianglesPartAmmount = (endTriangle - startTriangle) * 3;
            int startId = startTriangle * 3;
            int endId = endTriangle * 3;
            var vertexesDictionary = new Dictionary<int, Vector3>();
            var triangles = baseMesh.triangles;
            var vertices = baseMesh.vertices;

            for (int i = startId; i < endId; i++)
                vertexesDictionary[triangles[i]] = vertices[triangles[i]];

            //new vertices
            var newVertices = vertexesDictionary.Values.ToArray();

            //new triangles
            var keysList = vertexesDictionary.Keys.ToList();
            var newTriangles = new int[trianglesPartAmmount];

            for (int i = 0; i < trianglesPartAmmount; i++)
                newTriangles[i] = keysList.IndexOf(triangles[startId + i]);

            int verticesPartAmmount = keysList.Count;
            //uv
            var oldUV = baseMesh.uv;
            var newUV = new Vector2[verticesPartAmmount];

            for (int i = 0; i < verticesPartAmmount; i++)
                newUV[i] = oldUV[keysList[i]];

            //normals
            var oldNormals = baseMesh.normals;
            var newNormals = new Vector3[verticesPartAmmount];

            for (int i = 0; i < verticesPartAmmount; i++)
                newNormals[i] = oldNormals[keysList[i]];

            //creating GameObject

            var child = new GameObject();
            child.transform.parent = parent;
            child.transform.localPosition = Vector3.zero;

            var newMesh = new Mesh();
            newMesh.vertices = newVertices;
            newMesh.triangles = newTriangles;
            newMesh.uv = newUV;
            newMesh.normals = newNormals;

            newMesh.RecalculateBounds();

            child.AddComponent<MeshFilter>().mesh = newMesh;
            child.AddComponent<MeshRenderer>().material = DefaultMaterial;
        }

        #endregion

        #region Visualizing

        [NaughtyAttributes.Button("Visualize bounds")]
        public void CalcBoundsVisualizer()
        {
            VisualizeBounds = BoundsVisualizer.GetComponent<MeshFilter>().sharedMesh.bounds;
        }

        private void OnDrawGizmos()
        {
            Vector3 pos = transform.position;

            if (Bounds != null)
                for (int i = 0; i < Bounds.Length; i++)
                    if (i < Limit)
                        Gizmos.DrawWireCube(Bounds[i].center + pos, Bounds[i].size);

            if (BoundsVisualizer != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(VisualizeBounds.center + BoundsVisualizer.transform.position, VisualizeBounds.size);
            }
        }

        #endregion
    }

    #endregion

    #region MeshCache

    public class MeshCache
    {
        public Mesh Instance;
        public int[] Triangles;
        public Vector3[] Vertices;
        public Vector2[] UVs;
        public Vector3[] Normals;
    }

    #endregion
}