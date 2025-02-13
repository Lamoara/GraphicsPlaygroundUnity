using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class BinaryChunk : MonoBehaviour
{
    delegate (Vector3[], Vector3[]) CreateOrientatedVertices(Vector3 origin, Vector3 end);

    [SerializeField] ComputeShader faceCullingShader;
    
    ComputeBuffer frontFacesBuffer, backFacesBuffer, leftFacesBuffer, rightFacesBuffer, upFacesBuffer, downFacesBuffer, voxelMapBuffer, voxelMapRotatedBuffer;

    int[] voxelMap, voxelMapRotated;
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

        int bufferSize = size * size;

        int faceCullingShaderKernel = faceCullingShader.FindKernel("CSMain");

        int[] frontFaces = new int[bufferSize];
        int[] backFaces = new int[bufferSize];
        int[] leftFaces = new int[bufferSize];
        int[] rightFaces = new int[bufferSize];
        int[] upFaces = new int[bufferSize];
        int[] downFaces = new int[bufferSize];
        voxelMapRotated = RotateBits(voxelMap, size);

        frontFacesBuffer = new ComputeBuffer(bufferSize, sizeof(int));
        backFacesBuffer = new ComputeBuffer(bufferSize, sizeof(int));
        leftFacesBuffer = new ComputeBuffer(bufferSize, sizeof(int));
        rightFacesBuffer = new ComputeBuffer(bufferSize, sizeof(int));
        upFacesBuffer = new ComputeBuffer(bufferSize, sizeof(int));
        downFacesBuffer = new ComputeBuffer(bufferSize, sizeof(int));
        voxelMapBuffer = new ComputeBuffer(bufferSize, sizeof(int));
        voxelMapRotatedBuffer = new ComputeBuffer(bufferSize, sizeof(int));

        frontFacesBuffer.SetData(frontFaces);
        backFacesBuffer.SetData(backFaces);
        leftFacesBuffer.SetData(leftFaces);
        rightFacesBuffer.SetData(rightFaces);
        upFacesBuffer.SetData(upFaces);
        downFacesBuffer.SetData(downFaces);
        voxelMapBuffer.SetData(voxelMap);
        voxelMapRotatedBuffer.SetData(voxelMapRotated);

        faceCullingShader.SetBuffer(0, "frontFaces", frontFacesBuffer);
        faceCullingShader.SetBuffer(0, "backFaces", backFacesBuffer);
        faceCullingShader.SetBuffer(0, "leftFaces", leftFacesBuffer);
        faceCullingShader.SetBuffer(0, "rightFaces", rightFacesBuffer);
        faceCullingShader.SetBuffer(0, "upFaces", upFacesBuffer);
        faceCullingShader.SetBuffer(0, "downFaces", downFacesBuffer);
        faceCullingShader.SetBuffer(0, "voxelMap", voxelMapBuffer);
        faceCullingShader.SetBuffer(0, "voxelMapRotated", voxelMapRotatedBuffer);
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

        (vertices, normals) = CreateVertices(backFaces, CreateBackVertices);
        totalVertices.AddRange(vertices);
        totalNormals.AddRange(normals);

        (vertices, normals) = CreateVertices(frontFaces, CreateFrontVertices);
        totalVertices.AddRange(vertices);
        totalNormals.AddRange(normals);

        (vertices, normals) = CreateVertices(leftFaces, CreateLeftSideVertices);
        totalVertices.AddRange(vertices);
        totalNormals.AddRange(normals);

        (vertices, normals) = CreateVertices(rightFaces, CreateRightSideVertices);
        totalVertices.AddRange(vertices);
        totalNormals.AddRange(normals);

        (vertices, normals) = CreateVertices(downFaces, CreateDownVertices);
        totalVertices.AddRange(vertices);
        totalNormals.AddRange(normals);

        (vertices, normals) = CreateVertices(upFaces, CreateUpVertices);
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

    int[] RotateBits(int[] input, int size)
    {
        int[] output = new int[size * size];

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                for (int y = 0; y < size; y++)
                {
                    int bit = (input[x + z * size] >> y) & 1;
                    output[x + y * size] |= bit << z;
                }
            }
        }

        return output;
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
        mesh.indexFormat = IndexFormat.UInt32;
        GetComponent<MeshFilter>().mesh = mesh;
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

    (Vector3[], Vector3[]) CreateVertices(int[] faces, CreateOrientatedVertices vertexFunc)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        Vector3 origin, end;
        Vector3[] generatedVertices, generatedNormals;

        int[] meshed = new int[size * size];

        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
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
                        continue;

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

    (Vector3[], Vector3[]) CreateBackVertices(Vector3 origin, Vector3 end)
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

    (Vector3[], Vector3[]) CreateFrontVertices(Vector3 origin, Vector3 end)
    {
        Vector3[] vertices = new Vector3[4];
        Vector3[] normals = new Vector3[4];

        vertices[3] = new Vector3(origin.x, origin.y, origin.z + 1);
        vertices[2] = new Vector3(origin.x, end.y, origin.z + 1);
        vertices[1] = new Vector3(end.x, end.y, origin.z + 1);
        vertices[0] = new Vector3(end.x, origin.y, origin.z + 1);

        for (int i = 0; i < 4; i++)
            normals[i] = Vector3.back;   

        return (vertices, normals);
    }

    (Vector3[], Vector3[]) CreateRightSideVertices(Vector3 origin, Vector3 end)
    {
        Vector3[] vertices = new Vector3[4];
        Vector3[] normals = new Vector3[4];

        vertices[0] = new Vector3(origin.z + 1, origin.y, origin.x); 
        vertices[1] = new Vector3(origin.z + 1, end.y, origin.x);
        vertices[2] = new Vector3(end.z + 1, end.y, end.x);
        vertices[3] = new Vector3(end.z + 1, origin.y, end.x);

        for (int i = 0; i < 4; i++)
            normals[i] = Vector3.right;

        return (vertices, normals);
    }

    (Vector3[], Vector3[]) CreateLeftSideVertices(Vector3 origin, Vector3 end)
    {
        Vector3[] vertices = new Vector3[4];
        Vector3[] normals = new Vector3[4];

        vertices[3] = new Vector3(origin.z, origin.y, origin.x); 
        vertices[2] = new Vector3(origin.z, end.y, origin.x);
        vertices[1] = new Vector3(end.z, end.y, end.x);
        vertices[0] = new Vector3(end.z, origin.y, end.x);

        for (int i = 0; i < 4; i++)
            normals[i] = Vector3.right;

        return (vertices, normals);
    }

    (Vector3[], Vector3[]) CreateDownVertices(Vector3 origin, Vector3 end)
    {
        Vector3[] vertices = new Vector3[4];
        Vector3[] normals = new Vector3[4];

        vertices[3] = new Vector3(origin.x, origin.z, origin.y);
        vertices[2] = new Vector3(origin.x, origin.z, end.y);
        vertices[1] = new Vector3(end.x, origin.z, end.y);
        vertices[0] = new Vector3(end.x, origin.z, origin.y);

        for (int i = 0; i < 4; i++)
            normals[i] = Vector3.down;   

        return (vertices, normals);
    }

    (Vector3[], Vector3[]) CreateUpVertices(Vector3 origin, Vector3 end)
    {
        Vector3[] vertices = new Vector3[4];
        Vector3[] normals = new Vector3[4];

        vertices[0] = new Vector3(origin.x, origin.z + 1, origin.y);
        vertices[1] = new Vector3(origin.x, origin.z + 1, end.y);
        vertices[2] = new Vector3(end.x, origin.z + 1, end.y);
        vertices[3] = new Vector3(end.x, origin.z + 1, origin.y);

        for (int i = 0; i < 4; i++)
            normals[i] = Vector3.up;   

        return (vertices, normals);
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
        voxelMapRotatedBuffer?.Release();
    }
}
