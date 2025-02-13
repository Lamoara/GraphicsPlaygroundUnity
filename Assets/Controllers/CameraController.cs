using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 5f; // Velocidad de movimiento
    public float lookSpeed = 2f; // Velocidad de rotación con el ratón
    public float upSpeed = 3f; // Velocidad de subida/bajada

    private float pitch = 0f; // Rotación en el eje X (arriba/abajo)
    private float yaw = 0f;   // Rotación en el eje Y (izquierda/derecha)

    void Start()
    {
        // Bloquear y esconder el ratón al iniciar
        Cursor.lockState = CursorLockMode.Locked; // Bloquea el cursor en el centro de la pantalla
        Cursor.visible = false; // Hace invisible el cursor
    }

    void Update()
    {
        // Movimiento con WASD y espacio/shift para subir/bajar
        float horizontal = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float vertical = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        float upDown = 0f;

        if (Input.GetKey(KeyCode.Space))
        {
            upDown = upSpeed * Time.deltaTime; // Subir
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            upDown = -upSpeed * Time.deltaTime; // Bajar
        }

        // Movimiento en el espacio
        transform.Translate(horizontal, upDown, vertical);

        // Obtener entrada del ratón para rotar la cámara
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
        yaw += mouseX;
        pitch -= mouseY; // Invertir el eje Y para que se sienta más natural

        // Limitar la rotación en el eje X (para evitar volcarse hacia atrás)
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        // Aplicar rotaciones
        transform.eulerAngles = new Vector3(pitch, yaw, 0f);
    }
}
