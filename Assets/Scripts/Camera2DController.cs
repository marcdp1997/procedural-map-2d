using UnityEngine;

public class Camera2DController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;

    private void Update()
    {
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) move.y += 1f;
        if (Input.GetKey(KeyCode.S)) move.y -= 1f;
        if (Input.GetKey(KeyCode.A)) move.x -= 1f;
        if (Input.GetKey(KeyCode.D)) move.x += 1f;

        transform.position += _moveSpeed * Time.deltaTime * move;
    }
}
