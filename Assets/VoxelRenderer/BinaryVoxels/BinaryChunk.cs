using System.Collections.Generic;
using UnityEngine;

public class BinaryChunk : MonoBehaviour
{
    int[] voxelMap;
    int size;


    struct Face
    {
        int[] vertexIndices;
        Vector3[] vertex;
        Vector3[] normals;
    };


    void Start()
    {
        Init();
        InitRandomVoxels();
    }
    void Init()
    {
       size = sizeof(int) * 8;
        voxelMap = new int[size * size];
    }

    void InitRandomVoxels()
    {
        for (int i= 0; i < voxelMap.Length; i++)
        {
            voxelMap[i] = Random.Range(0, int.MaxValue);
        }
    }

    void CreateMesh()
    {
        int[][] faces = new int[6][];
        for (int i = 0; i < faces.Length; i++)
        {
            faces[i] = new int[size];
        }

        List<Face>[] renderFaces = new List<Face>[6];


    }
}
