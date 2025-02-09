using System;
using Unity.VisualScripting;
using UnityEngine;

public class VoxelWorldController : MonoBehaviour
{
    [SerializeField] GameObject chunkPrefab;
    [SerializeField] int width = 3, height = 3, depth = 3;
    [SerializeField] float spacing = 16f;
    private VoxelMeshRenderer[,,] chunks;

    void Start()
    {
        chunks = new VoxelMeshRenderer[width, height, depth];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector3 position = new Vector3(x * spacing, y * spacing, z * spacing);
                    VoxelMeshRenderer chunk = Instantiate(chunkPrefab, position, Quaternion.identity).GetComponent<VoxelMeshRenderer>();
                    chunks[x, y, z] = chunk;
                }
            }
        }

        foreach (VoxelMeshRenderer chunk in chunks)
        {
            print("Chunk");
            if (chunk != null)
            {
                chunk.InitRandomGrid();
                chunk.Init();
                chunk.UpdateVoxels();
            }
        }
    }
}
