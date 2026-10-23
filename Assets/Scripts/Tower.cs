using UnityEngine;

public class Tower : MonoBehaviour
{
    public float range = 12f;
    public float damage = 7f;
    public float cooldown = 0.45f;

    float timer;
    float lineTimer;
    LineRenderer lr;

    public void Setup(BuildingData data)
    {
        if (data == null) return;
        range = data.towerRange;
        damage = data.towerDamage;
        cooldown = data.towerCooldown;
    }

    void Awake()
    {
        lr = gameObject.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = 0.12f;
        if (Game.LineMat != null) lr.material = new Material(Game.LineMat);
        else lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = new Color(0.6f, 1f, 1f);
        lr.enabled = false;
    }

    void Update()
    {
        if (Game.I != null && Game.I.lost) return;

        if (lineTimer > 0f)
        {
            lineTimer -= Time.deltaTime;
            if (lineTimer <= 0f) lr.enabled = false;
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        Enemy best = null;
        float bd = range;
        foreach (var e in Enemy.All)
        {
            if (e.state == EState.Flee) continue;   // desertores escapam
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < bd) { bd = d; best = e; }
        }
        if (best == null) return;

        timer = cooldown;
        lr.SetPosition(0, transform.position + Vector3.up * 1f);
        lr.SetPosition(1, best.transform.position);
        lr.enabled = true;
        lineTimer = 0.06f;
        best.hp.Damage(damage, gameObject);
    }
}
