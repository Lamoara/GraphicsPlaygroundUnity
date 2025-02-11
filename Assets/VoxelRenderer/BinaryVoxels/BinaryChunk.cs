using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Internal.Execution;
using UnityEngine;

public class BinaryChunk : MonoBehaviour
{
    int[] voxelMap;
    int size;


    struct Face
    {
        int[] vertexIndices;
        Vector3[] vertices;
        Vector3[] normals;

        public Face(int[] indices, Vector3[] verts, Vector3[] norms)
        {
            vertexIndices = indices;
            vertices = verts;
            normals = norms;
        }
    };


    void Start()
    {
        Init();
        InitRandomVoxels();
        CreateMesh();
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
            voxelMap[i] = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }
    }

    void CreateMesh()
    {
        int[][] faces = new int[6][];
        //!TODO poner shader para generar caras visibles
        for (int i = 0; i < faces.Length; i++)
        {
            faces[i] = new int[size];
        }

        List<Face>[] renderFaces = new List<Face>[6];

        //Caras frontales

        int[] meshed = new int[size * size];
        //!TODO Cambiar esto para usar las caras que deberia generar el shader
        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size - 1; x++)
            {
                int i = 0;
                int voxelValue = voxelMap[z * size + x];
                while (i < size)
                    {
                    int mask = 0;

                    // Encuentra el primer bit en 1
                    while (i < size && ((voxelValue >> i) & 1) == 0) 
                    {
                        i++;
                    }
                    int height = 1;
                    // Construye la máscara de los 1s encontrados
                    while (i < size && ((voxelValue >> i) & 1) == 1) 
                    {
                        mask |= 1 << i; // Activa el bit en la posición i
                        i++;
                        height ++;
                    }

                    if (mask == 0) 
                        break;

                    int width = 1;
                    while (x + width < size && (meshed[z * size + x + width] & mask) == 0 && (voxelMap[z * size + x + width] & mask) == mask)
                    {
                        print("Mesheable");
                        meshed[z * size + x + width] |= mask;   
                        width ++;
                    }
                    meshed[z * size + x] |= mask;


                    //!TODO Construir triangulo con width y height
                    print(Convert.ToString(voxelValue, 2));

                    print(Convert.ToString(mask, 2));
                }
            }
        }

    }
}
