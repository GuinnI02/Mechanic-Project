using UnityEngine;
using UnityEngine.InputSystem;

public class FieldPlacer : MonoBehaviour
{
    [SerializeField] private DamageField fieldPrefab;
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float maxPlaceDistance = 15f;
    [SerializeField] private float cooldown = 3f;

    private float nextPlaceTime;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;
        if (!Keyboard.current.fKey.wasPressedThisFrame) return;

        Debug.Log("F pressed");

        if (Time.time < nextPlaceTime) { Debug.Log("On cooldown"); return; }

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red, 3f);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayers))
        {
            Debug.Log("Ray hit no ground");
            return;
        }

        float dist = Vector3.Distance(transform.position, hit.point);
        if (dist > maxPlaceDistance) { Debug.Log("Too far: " + dist); return; }

        DamageField field = Instantiate(fieldPrefab, hit.point, Quaternion.identity);
        field.Init(transform);
        nextPlaceTime = Time.time + cooldown;
        Debug.Log("Field placed");
    }
}
