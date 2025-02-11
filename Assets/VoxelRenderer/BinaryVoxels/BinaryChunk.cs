using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework.Internal.Execution;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class BinaryChunk : MonoBehaviour
{
    int[] voxelMap;
    int size;

    Mesh mesh;
    [SerializeField] Material mat;

    void Start()
    {
        Init();
        InitRandomVoxels();
        InitMesh();
        CreateMesh();

        List<Vector3> totalVertices = new List<Vector3>(), totalNormals = new List<Vector3>();
        Vector3[] vertices, normals;
        int[] triangles;

        (vertices, normals) = CreateVertices(voxelMap);
        totalVertices.AddRange(vertices);
        totalNormals.AddRange(normals);

        vertices = totalVertices.ToArray();
        normals = totalNormals.ToArray();
        triangles = CreateTriangles(vertices);

        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
    void Init()
    {
        size = sizeof(int) * 8;
        voxelMap = new int[size * size];
    }

    void InitRandomVoxels()
    {
        for (int i= 0; i < voxelMap.Length; i++)
            voxelMap[i] = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
    }

    void InitMesh()
    {
        mesh = new Mesh();
        GetComponent<MeshRenderer>().material = mat;
        GetComponent<MeshFilter>().mesh = mesh;
    }

    (Vector3[], Vector3[]) CreateVertices(int[] faces)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();

        int[] meshed = new int[size * size];
        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size - 1; x++)
            {
                int y = 0;
                int faceValue = faces[z * size + x];
                int meshedValue = meshed[z * size + x];
                while (y < size)
                    {
                    int mask = 0;

                    // Encuentra el primer bit en 1
                    while (y < size && (((faceValue >> y) & 1) == 0 || ((meshedValue >> y) & 1) == 1))
                    {
                        y++;
                    }
                    int height = 0;
                    // Construye la máscara de los 1s encontrados
                    while (y < size && ((faceValue >> y) & 1) == 1 && ((meshedValue >> y) & 1) == 0) 
                    {
                        mask |= 1 << y; // Activa el bit en la posición i
                        height ++;
                        y++;
                    }

                    if (mask == 0) 
                        break;

                    int width = 1;
                    while (x + width < size && (meshed[z * size + x + width] & mask) == 0 && (faces[z * size + x + width] & mask) == mask)
                    {
                        meshed[z * size + x + width] |= mask;   
                        width ++;
                    }
                    meshed[z * size + x] |= mask;

                    vertices.Add(new Vector3(x, y - height, z));
                    vertices.Add(new Vector3(x, y, z));
                    vertices.Add(new Vector3(x + width, y, z));
                    vertices.Add(new Vector3(x + width, y - height, z));

                    for (int i = 0; i < 4; i++)
                        normals.Add(Vector3.back);   
                }
            }
        }

        return (vertices.ToArray<Vector3>(), normals.ToArray<Vector3>());
    }

    int[] CreateTriangles(Vector3[] vertices)
    {
        List<int> triangles = new List<int>();
        for (int i= 0; i < vertices.Length/4; i += 4)
        {
            triangles.Add(i);
            triangles.Add(i + 1);
            triangles.Add(i + 2);
            triangles.Add(i);
            triangles.Add(i + 2);
            triangles.Add(i + 3);
        }

        return triangles.ToArray<int>();
    }

    void CreateMesh()
    {
        int[][] faces = new int[6][];
        //!TODO poner shader para generar caras visibles
        for (int i = 0; i < faces.Length; i++)
        {
            faces[i] = new int[size];
        }

        

    }
}
