using System.Collections.Generic;
using UnityEngine;

public class DamageField : MonoBehaviour
{
    [Header("Cone")]
    [SerializeField] private float range = 1.5f;                    
    [SerializeField, Range(5f, 170f)] private float angle = 60f;    
    [SerializeField] private float height = 2f;
    [SerializeField, Range(4, 32)] private int segments = 12;

    [Header("Shooting")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float shotsPerSecond = 2f;
    [Tooltip("If true, every enemy in the field is shot each tick. If false, only the closest one.")]
    [SerializeField] private bool shootAllEnemies = false;

    [Header("Field")]
    [Tooltip("Seconds before the field disappears. 0 = lasts forever.")]
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private LayerMask enemyLayers = ~0;

    [Header("Visuals (optional)")]
    [Tooltip("A prefab with a LineRenderer, used as a quick bullet tracer.")]
    [SerializeField] private LineRenderer tracerPrefab;
    [SerializeField] private float tracerDuration = 0.05f;
    [SerializeField] private Vector3 muzzleOffset = new Vector3(0f, 1f, 0f);

    private Transform shooter;
    private readonly List<Health> targets = new List<Health>();
    private float nextShotTime;

    public void Init(Transform shooterTransform)
    {
        shooter = shooterTransform;
    }

    private void Awake()
    {
        Mesh cone = BuildConeMesh();

        // Trigger collider shaped like the cone
        var col = GetComponent<MeshCollider>();
        if (col == null) col = gameObject.AddComponent<MeshCollider>();
        col.sharedMesh = cone;
        col.convex = true;
        col.isTrigger = true;

        // Trigger events need a Rigidbody on one of the two objects
        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // If the prefab has a MeshFilter, show the same cone shape
        var mf = GetComponent<MeshFilter>();
        if (mf != null) mf.sharedMesh = cone;
    }

    private void Start()
    {
        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((enemyLayers.value & (1 << other.gameObject.layer)) == 0) return;

        Health h = other.GetComponentInParent<Health>();
        if (h != null && !targets.Contains(h)) targets.Add(h);
    }

    private void OnTriggerExit(Collider other)
    {
        Health h = other.GetComponentInParent<Health>();
        if (h != null) targets.Remove(h);
    }

    private void Update()
    {
        targets.RemoveAll(t => t == null || t.IsDead);

        if (targets.Count == 0 || Time.time < nextShotTime) return;
        nextShotTime = Time.time + 1f / shotsPerSecond;

        if (shootAllEnemies)
        {
            foreach (Health t in targets) Shoot(t);
        }
        else
        {
            Shoot(GetClosest());
        }
    }

    private Health GetClosest()
    {
        Vector3 origin = shooter != null ? shooter.position : transform.position;
        Health best = null;
        float bestDist = float.MaxValue;

        foreach (Health t in targets)
        {
            float d = (t.transform.position - origin).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = t;
            }
        }
        return best;
    }

    private void Shoot(Health target)
    {
        if (target == null) return;

        target.TakeDamage(damage);

        if (tracerPrefab != null)
        {
            Vector3 start = shooter != null ? shooter.position + muzzleOffset : transform.position;
            LineRenderer tracer = Instantiate(tracerPrefab);
            tracer.positionCount = 2;
            tracer.SetPosition(0, start);
            tracer.SetPosition(1, target.transform.position + Vector3.up * 0.5f);
            Destroy(tracer.gameObject, tracerDuration);
        }
    }

    // Wedge-shaped prism: tip at local origin, opening along local +Z.
    private Mesh BuildConeMesh()
    {
        float half = angle * 0.5f * Mathf.Deg2Rad;
        var verts = new List<Vector3>();
        var tris = new List<int>();

        verts.Add(Vector3.zero);                  // 0: tip, bottom
        verts.Add(new Vector3(0f, height, 0f));   // 1: tip, top

        for (int i = 0; i <= segments; i++)
        {
            float a = Mathf.Lerp(-half, half, i / (float)segments);
            Vector3 p = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * range;
            verts.Add(p);                                  // 2 + 2i: arc bottom
            verts.Add(p + new Vector3(0f, height, 0f));    // 3 + 2i: arc top
        }

        for (int i = 0; i < segments; i++)
        {
            int b0 = 2 + 2 * i, t0 = 3 + 2 * i;
            int b1 = 4 + 2 * i, t1 = 5 + 2 * i;

            tris.AddRange(new[] { 0, b1, b0 });      // bottom
            tris.AddRange(new[] { 1, t0, t1 });      // top
            tris.AddRange(new[] { b0, b1, t1 });     // curved wall
            tris.AddRange(new[] { b0, t1, t0 });
        }

        // Two flat side walls
        int lb = 2, lt = 3;
        int rb = 2 + 2 * segments, rt = 3 + 2 * segments;
        tris.AddRange(new[] { 0, lb, lt });
        tris.AddRange(new[] { 0, lt, 1 });
        tris.AddRange(new[] { 0, rt, rb });
        tris.AddRange(new[] { 0, 1, rt });

        var mesh = new Mesh { name = "FieldCone" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}