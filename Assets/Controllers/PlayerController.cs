using UnityEngine;

public class PlayerController : MonoBehaviour
{
    float speed;
    float inputX, inputY;

    void Start()
    {

    }

    void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(inputX, inputY) * speed * Time.deltaTime;
        
        transform.Translate(movement);
    }

}
