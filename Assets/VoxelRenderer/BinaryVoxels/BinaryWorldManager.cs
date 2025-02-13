using UnityEngine;

public class BinaryWorldManager : MonoBehaviour
{
    [SerializeField] Vector3Int size;
    [SerializeField] GameObject chunkPrefab;

    BinaryChunk[] chunks;
    int x = 0, y = 0, z = 0;
    bool isGenerating = true;

    void Start()
    {
        chunks = new BinaryChunk[size.x * size.y * size.z];
    }

    void Update()
    {
        if (!isGenerating) return;

        if (x < size.x && y < size.y && z < size.z)
        {
            int index = x + y * size.y + z * size.x;
            chunks[index] = Instantiate(chunkPrefab, new Vector3(x * 32, y * 32, z * 32), transform.rotation).GetComponent<BinaryChunk>();
            
            z++;
            if (z >= size.z)
            {
                z = 0;
                y++;
                if (y >= size.y)
                {
                    y = 0;
                    x++;
                    if (x >= size.x)
                    {
                        isGenerating = false; // Termina la generación
                    }
                }
            }
        }
    }
}
