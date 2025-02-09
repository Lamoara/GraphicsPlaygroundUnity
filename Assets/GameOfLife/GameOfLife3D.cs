using System.Collections.Generic;
using UnityEngine;

public class GameOfLife3D : MonoBehaviour
{
    [SerializeField] int width = 30, height = 30, depth = 30;
    [SerializeField] float cubeSize = 1f;
    [SerializeField] ComputeShader behaviourShader, cleanUpShader;
    [SerializeField] Material voxelMaterial;

    private ComputeBuffer cellsBuffer, cellsNextBuffer, resultBuffer;
    private int behaviourKernelHandle, cleanUpKernelHandle;
    private Mesh voxelMesh;
    private int[] cells;
    private List<Matrix4x4> matrices = new List<Matrix4x4>();

    void Start()
    {
        voxelMesh = GenerateVoxelMesh();
        InitializeBuffers();
        InitializeCells();
    }

    void Update()
    {
        int groupsX = Mathf.CeilToInt(width / 5.0f);
        int groupsY = Mathf.CeilToInt(height / 5.0f);
        int groupsZ = Mathf.CeilToInt(depth / 5.0f);

        
        behaviourShader.SetBuffer(behaviourKernelHandle, "cells", cellsBuffer);
        behaviourShader.SetBuffer(behaviourKernelHandle, "cellsNext", cellsNextBuffer);

        behaviourShader.Dispatch(behaviourKernelHandle, groupsX, groupsY, groupsZ);

        // Intercambiar buffers
        ComputeBuffer temp = cellsBuffer;
        cellsBuffer = cellsNextBuffer;
        cellsNextBuffer = temp;

        cleanUpShader.SetBuffer(cleanUpKernelHandle, "cells", cellsBuffer);
        cleanUpShader.SetBuffer(cleanUpKernelHandle, "result", resultBuffer);

        cleanUpShader.Dispatch(behaviourKernelHandle, groupsX, groupsY, groupsZ);

        resultBuffer.GetData(cells);

        matrices.Clear();

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == 1)
            {
                int x = i % width;
                int y = (i / width) % height;
                int z = i / (width * height);

                matrices.Add(Matrix4x4.TRS(
                    new Vector3(x * cubeSize, y * cubeSize, z * cubeSize),
                    Quaternion.identity,
                    Vector3.one * cubeSize
                ));
            }
        }

        int batchSize = 1023;
        for (int i = 0; i < matrices.Count; i += batchSize)
        {
            int count = Mathf.Min(batchSize, matrices.Count - i);
            Graphics.DrawMeshInstanced(voxelMesh, 0, voxelMaterial, matrices.GetRange(i, count).ToArray());
        }
    }

    Mesh GenerateVoxelMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0),
            new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(0, 1, 1), new Vector3(1, 1, 1),
        };

        int[] triangles = new int[]
        {
            0, 2, 1, 1, 2, 3, // Front
            5, 7, 4, 4, 7, 6, // Back
            0, 4, 2, 2, 4, 6, // Left
            1, 3, 5, 5, 3, 7, // Right
            2, 6, 3, 3, 6, 7, // Top
            0, 1, 4, 4, 1, 5  // Bottom
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void InitializeBuffers()
    {
        int totalCells = width * height * depth;
        cellsBuffer = new ComputeBuffer(totalCells, sizeof(int));
        cellsNextBuffer = new ComputeBuffer(totalCells, sizeof(int));
        resultBuffer = new ComputeBuffer(totalCells, sizeof(int));

        behaviourKernelHandle = behaviourShader.FindKernel("CSMain");

        behaviourShader.SetInt("width", width);
        behaviourShader.SetInt("height", height);
        behaviourShader.SetInt("depth", depth);
        behaviourShader.SetBuffer(behaviourKernelHandle, "cells", cellsBuffer);
        behaviourShader.SetBuffer(behaviourKernelHandle, "cellsNext", cellsNextBuffer);


        cleanUpKernelHandle = cleanUpShader.FindKernel("CSMain");

        cleanUpShader.SetInt("width", width);
        cleanUpShader.SetInt("height", height);
        cleanUpShader.SetInt("depth", depth);
        cleanUpShader.SetBuffer(cleanUpKernelHandle, "cells", cellsBuffer);
        cleanUpShader.SetBuffer(cleanUpKernelHandle, "result", resultBuffer);
    }

    void InitializeCells()
    {
        int totalCells = width * height * depth;
        cells = new int[totalCells];

        for (int i = 0; i < totalCells; i++)
        {
            cells[i] = Random.value > 0.5f ? 1 : 0;
        }
        cellsBuffer.SetData(cells);
    }

    void OnDestroy()
    {
        cellsBuffer.Release();
        cellsNextBuffer.Release();
        resultBuffer.Release();
    }
}
