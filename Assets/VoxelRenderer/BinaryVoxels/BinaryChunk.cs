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
    delegate (Vector3[], Vector3[]) CreateOrientatedVertices(Vector3 origin, Vector3 end);

    [SerializeField] ComputeShader faceCullingShader;



    ComputeBuffer frontFacesBuffer, backFacesBuffer, leftFacesBuffer, rightFacesBuffer, upFacesBuffer, downFacesBuffer, voxelMapBuffer;

    int[] voxelMap;
    int size;

    Mesh mesh;

    void Start()
    {
        Init();
        InitRandomVoxels();
        InitMesh();
        CreateMesh();
        Render();
        
    }

    void Render()
    {
        List<Vector3> totalVertices = new List<Vector3>(), totalNormals = new List<Vector3>();
        Vector3[] vertices, normals;
        int[] triangles;

        int[] frontFaces = new int[size * size];
        int[] backFaces = new int[size * size];
        int[] leftFaces = new int[size * size];
        int[] rightFaces = new int[size * size];
        int[] upFaces = new int[size * size];
        int[] downFaces = new int[size * size];

        int bufferSize = size * size;

        frontFacesBuffer = new ComputeBuffer(bufferSize, sizeof(int));
        backFacesBuffer = new ComputeBuffer(bufferSize, sizeof(int));
        leftFacesBuffer = new ComputeBuffer(bufferSize, sizeof(int));
        rightFacesBuffer = new ComputeBuffer(bufferSize, sizeof(int));
        upFacesBuffer = new ComputeBuffer(bufferSize, sizeof(int));
        downFacesBuffer = new ComputeBuffer(bufferSize, sizeof(int));
        voxelMapBuffer = new ComputeBuffer(bufferSize, sizeof(int));

        int faceCullingShaderKernel = faceCullingShader.FindKernel("CSMain");

        faceCullingShader.SetInt("size", size);

        int threadGroupsX = Mathf.CeilToInt(size / 8.0f);
        int threadGroupsY = 1;
        int threadGroupsZ = Mathf.CeilToInt(size / 8.0f);

        faceCullingShader.Dispatch(faceCullingShaderKernel, threadGroupsX, threadGroupsY, threadGroupsZ);

        frontFacesBuffer.GetData(frontFaces);
        backFacesBuffer.GetData(backFaces);
        leftFacesBuffer.GetData(leftFaces);
        rightFacesBuffer.GetData(rightFaces);
        upFacesBuffer.GetData(upFaces);
        downFacesBuffer.GetData(downFaces);

        foreach (int e in frontFaces)
            print(e);
    

        (vertices, normals) = CreateVertices(frontFaces, CreateFrontVertices);
        totalVertices.AddRange(vertices);
        totalNormals.AddRange(normals);

        (vertices, normals) = CreateVertices(leftFaces, CreateSideVertices);
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
        GetComponent<MeshFilter>().mesh = mesh;
    }

    (Vector3[], Vector3[]) CreateVertices(int[] faces, CreateOrientatedVertices vertexFunc)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        Vector3 origin, end;
        Vector3[] generatedVertices, generatedNormals;

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

                    int width = 0;
                    while (x + width < size && (meshed[z * size + x + width] & mask) == 0 && (faces[z * size + x + width] & mask) == mask)
                    {
                        meshed[z * size + x + width] |= mask;   
                        width ++;
                    }
                    meshed[z * size + x] |= mask;

                    origin = new Vector3(x, y - height, z);
                    end = new Vector3(x + width, y, z);

                    (generatedVertices, generatedNormals) = vertexFunc(origin, end);

                    vertices.AddRange(generatedVertices);
                    normals.AddRange(generatedNormals);
                }
            }
        }

        return (vertices.ToArray<Vector3>(), normals.ToArray<Vector3>());
    }

    int[] CreateTriangles(Vector3[] vertices)
    {
        List<int> triangles = new List<int>();
        for (int i= 0; i < vertices.Length; i += 4)
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

    (Vector3[], Vector3[]) CreateFrontVertices(Vector3 origin, Vector3 end)
    {
        Vector3[] vertices = new Vector3[4];
        Vector3[] normals = new Vector3[4];

        vertices[0] = new Vector3(origin.x, origin.y, origin.z);
        vertices[1] = new Vector3(origin.x, end.y, origin.z);
        vertices[2] = new Vector3(end.x, end.y, origin.z);
        vertices[3] = new Vector3(end.x, origin.y, origin.z);

        for (int i = 0; i < 4; i++)
            normals[i] = Vector3.back;   

        return (vertices, normals);
    }

    (Vector3[], Vector3[]) CreateSideVertices(Vector3 origin, Vector3 end)
    {
        Vector3[] vertices = new Vector3[4];
        Vector3[] normals = new Vector3[4];

        vertices[0] = new Vector3(origin.z, origin.y, origin.x); 
        vertices[1] = new Vector3(origin.z, end.y, origin.x);
        vertices[2] = new Vector3(end.z, end.y, end.x);
        vertices[3] = new Vector3(end.z, origin.y, end.x);

        // Asignar la normal para cada vértice
        for (int i = 0; i < 4; i++)
            normals[i] = Vector3.right;

        return (vertices, normals);
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

    void OnDestroy()
    {
        DisposeBuffers();
    }

    void DisposeBuffers()
    {
        frontFacesBuffer?.Release();
        backFacesBuffer?.Release();
        leftFacesBuffer?.Release();
        rightFacesBuffer?.Release();
        upFacesBuffer?.Release();
        downFacesBuffer?.Release();
        voxelMapBuffer?.Release();
    }
}
