using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VoxelMeshRenderer : MonoBehaviour
{
    [SerializeField] int width = 30, height = 30, depth = 30;
    [SerializeField] float cubeSize = 1f;
    [SerializeField] ComputeShader faceCullingShader;


    Mesh mesh;
    List<Vector3> vertices;
    List<int> triangles;
    List<Vector3> normals;
    int[] visibleFaces;
    List<Vector2> uvs;

    int[] voxelGrid;

    ComputeBuffer voxelGridBuffer, visibleFacesBuffer;
    private int faceCullingKernelHandle;

    public int[] VoxelGrid { get => voxelGrid; set => voxelGrid = value; }

    [ContextMenu("Init")]
    public void Init()
    {
        if (voxelGrid == null)
            return;

        InitMesh();
        InitBuffers();
        InitVisibleFaces();

        UpdateVoxels();
    }

    [ContextMenu("Update")]
    public void UpdateVoxels()
    {
        RunFaceCullingShader();
        GenerateTriangles();
        UpdateMesh();
    }

    void InitMesh()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void InitVisibleFaces()
    {
        visibleFaces = new int[width * height * depth];
    }

    public void InitRandomGrid()
    {
        voxelGrid = new int[width * height * depth];
        for (int i = 0; i < voxelGrid.Length; i++)
        {
            voxelGrid[i] = Random.value > 0.7f ? 1 : 0; // 30% bloques sólidos
        }
    }
    void InitBuffers()
    {
        voxelGridBuffer = new ComputeBuffer(voxelGrid.Length, sizeof(int));
        visibleFacesBuffer = new ComputeBuffer(voxelGrid.Length, sizeof(int));


        faceCullingKernelHandle = faceCullingShader.FindKernel("CSMain");

        faceCullingShader.SetBuffer(0, "voxelGrid", voxelGridBuffer);
        faceCullingShader.SetBuffer(0, "visibleFaces", visibleFacesBuffer);
        faceCullingShader.SetInt("width", width);
        faceCullingShader.SetInt("height", height);
        faceCullingShader.SetInt("depth", depth);
    }

    void RunFaceCullingShader()
    {
        voxelGridBuffer.SetData(voxelGrid);
        visibleFacesBuffer.SetData(new int[voxelGrid.Length]);

        int threadGroupsX = Mathf.CeilToInt(width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(height / 8.0f);
        int threadGroupsZ = Mathf.CeilToInt(depth / 8.0f);

        faceCullingShader.Dispatch(faceCullingKernelHandle, threadGroupsX, threadGroupsY, threadGroupsZ);

        visibleFacesBuffer.GetData(visibleFaces);
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        //mesh.uv = uvs.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        triangles = new List<int>();
        vertices = new List<Vector3>();  
        normals = new List<Vector3>();
    }

    void GenerateTriangles()
    {
        triangles = new List<int>();
        vertices = new List<Vector3>();  
        normals = new List<Vector3>();

        // Recorremos todo el grid de voxeles
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int index = x + y * width + z * width * height;

                    if (voxelGrid[index] == 0)
                        continue;

                    int visible = visibleFaces[index];


                    if ((visible & 0x01) != 0) // Cara izquierda
                    {
                        AddFaceIndices(x, y, z, 0); 
                    }
                    if ((visible & 0x02) != 0) // Cara derecha
                    {
                        AddFaceIndices(x, y, z, 1);  
                    }
                    if ((visible & 0x04) != 0) // Cara abajo
                    {
                        AddFaceIndices(x, y, z, 2); 
                    }
                    if ((visible & 0x08) != 0) // Cara arriba
                    {
                        AddFaceIndices(x, y, z, 3);  
                    }
                    if ((visible & 0x10) != 0) // Cara detrás
                    {
                        AddFaceIndices(x, y, z, 4); 
                    }
                    if ((visible & 0x20) != 0) // Cara delante
                    {
                        AddFaceIndices(x, y, z, 5);  
                    }
                }
            }
        }
    }
    void AddFaceIndices(int x, int y, int z, int faceIndex)
    {
        Vector3[] faceVertices = new Vector3[4];
        Vector3 faceNormal = Vector3.zero;

        switch (faceIndex)
        {
            case 0: // Cara izquierda
                faceVertices[0] = new Vector3(x * cubeSize, y * cubeSize, (z + 1) * cubeSize);  // 3
                faceVertices[1] = new Vector3(x * cubeSize, (y + 1) * cubeSize, (z + 1) * cubeSize);  // 2
                faceVertices[2] = new Vector3(x * cubeSize, (y + 1) * cubeSize, z * cubeSize);  // 1
                faceVertices[3] = new Vector3(x * cubeSize, y * cubeSize, z * cubeSize);  // 0
                faceNormal = Vector3.left;  // La normal de la cara izquierda
                break;

            case 1: // Cara derecha
                faceVertices[0] = new Vector3((x + 1) * cubeSize, y * cubeSize, z * cubeSize);  // 0
                faceVertices[1] = new Vector3((x + 1) * cubeSize, (y + 1) * cubeSize, z * cubeSize);  // 1
                faceVertices[2] = new Vector3((x + 1) * cubeSize, (y + 1) * cubeSize, (z + 1) * cubeSize);  // 2
                faceVertices[3] = new Vector3((x + 1) * cubeSize, y * cubeSize, (z + 1) * cubeSize);  // 3
                faceNormal = Vector3.right;  // La normal de la cara derecha
                break;

            case 2: // Cara abajo
                faceVertices[0] = new Vector3(x * cubeSize, y * cubeSize, z * cubeSize);  // 0
                faceVertices[1] = new Vector3((x + 1) * cubeSize, y * cubeSize, z * cubeSize);  // 1
                faceVertices[2] = new Vector3((x + 1) * cubeSize, y * cubeSize, (z + 1) * cubeSize);  // 2
                faceVertices[3] = new Vector3(x * cubeSize, y * cubeSize, (z + 1) * cubeSize);  // 3
                faceNormal = Vector3.down;  // La normal de la cara abajo
                break;

            case 3: // Cara arriba
                faceVertices[0] = new Vector3(x * cubeSize, (y + 1) * cubeSize, z * cubeSize);  // 0
                faceVertices[1] = new Vector3(x * cubeSize, (y + 1) * cubeSize, (z + 1) * cubeSize);  // 1
                faceVertices[2] = new Vector3((x + 1) * cubeSize, (y + 1) * cubeSize, (z + 1) * cubeSize);  // 2
                faceVertices[3] = new Vector3((x + 1) * cubeSize, (y + 1) * cubeSize, z * cubeSize);  // 3
                faceNormal = Vector3.up;  // La normal de la cara arriba
                break;

            case 4: // Cara detrás
                faceVertices[0] = new Vector3(x * cubeSize, (y + 1) * cubeSize, z * cubeSize);  // 3
                faceVertices[1] = new Vector3((x + 1) * cubeSize, (y + 1) * cubeSize, z * cubeSize);  // 2
                faceVertices[2] = new Vector3((x + 1) * cubeSize, y * cubeSize, z * cubeSize);  // 1
                faceVertices[3] = new Vector3(x * cubeSize, y * cubeSize, z * cubeSize);  // 0
                faceNormal = Vector3.back;  // La normal de la cara detrás
                break;

            case 5: // Cara delante
                faceVertices[0] = new Vector3((x + 1) * cubeSize, y * cubeSize, (z + 1) * cubeSize);  // 3
                faceVertices[1] = new Vector3((x + 1) * cubeSize, (y + 1) * cubeSize, (z + 1) * cubeSize);  // 2
                faceVertices[2] = new Vector3(x * cubeSize, (y + 1) * cubeSize, (z + 1) * cubeSize);  // 1
                faceVertices[3] = new Vector3(x * cubeSize, y * cubeSize, (z + 1) * cubeSize);  // 0
                faceNormal = Vector3.forward;  // La normal de la cara delante
                break;
        }


        vertices.AddRange(faceVertices);
        normals.AddRange(new Vector3[] { faceNormal, faceNormal, faceNormal, faceNormal });

        int vertexIndex = vertices.Count - 4;
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    void OnDestroy()
    {
        voxelGridBuffer.Release();
        visibleFacesBuffer.Release();
    }
}
