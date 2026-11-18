using UnityEngine;
using UnityEngine.InputSystem;

public class FieldPlacer : MonoBehaviour
{
    [SerializeField] private DamageField fieldPrefab;
    [SerializeField] private Camera cam;
    [SerializeField] private float cooldown = 3f;
    [SerializeField] private float forwardOffset = 0.25f;

    private float nextPlaceTime;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;
        if (!Keyboard.current.fKey.wasPressedThisFrame) return;
        if (Time.time < nextPlaceTime) return;

        Vector3 dir = GetAimDirection();

        Vector3 pos = transform.position + dir * forwardOffset;
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

        DamageField field = Instantiate(fieldPrefab, pos, rot);
        field.Init(transform);
        nextPlaceTime = Time.time + cooldown;
    }

    // Mouse ray against a flat plane at the player's height: never misses,
    // never snags on walls or enemies. Falls back to facing forward.
    private Vector3 GetAimDirection()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        var plane = new Plane(Vector3.up, transform.position);

        if (plane.Raycast(ray, out float t))
        {
            Vector3 flat = ray.GetPoint(t) - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f) return flat.normalized;
        }

        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        return fwd.sqrMagnitude > 0.001f ? fwd.normalized : Vector3.forward;
    }
}