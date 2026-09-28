using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageField : MonoBehaviour
{
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

    // Called by whatever spawns the field so shots come from the player.
    public void Init(Transform shooterTransform)
    {
        shooter = shooterTransform;
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
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

        // Guaranteed hit: the field grants the shot, so no aiming or projectile travel.
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
}
