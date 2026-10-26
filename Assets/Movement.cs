using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [SerializeField] float speed = 2f;
    [SerializeField] float distance = 3f;

    private Vector3 target;

    void Start()
    {
        // Set the initial target destination
        target = transform.position;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * distance;
        transform.position = target + Vector3.left * offset;
    }

    void FlipCharacter()
    {
        // Optional: Rotates 180 degrees on the Y axis to face the moving direction
        transform.Rotate(0f, 180f, 0f);
    }
}