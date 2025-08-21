using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHP = 100;
    private int hp;
    private bool isDead;

    public int MaxHP => maxHP;
    public int CurrentHP => hp;

    // Single event (keep this; remove any other OnDeath lines)
    public event System.Action OnDeath;

    void Awake() => hp = maxHP;

    public void TakeDamage(int dmg)
    {
        // Let skills (e.g., shield) modify damage
        foreach (var mod in GetComponents<NightHunter.combat.IDamageModifier>())
            dmg = mod.ModifyIncomingDamage(dmg);

        if (isDead) return;

        hp -= Mathf.Max(0, dmg);
        if (hp <= 0) Die();
    }

    private void Die()
    {
        if (isDead) return; // safety
        isDead = true;
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}
