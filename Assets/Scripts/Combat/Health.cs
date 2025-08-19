using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHP = 100;
    int hp;

    public System.Action OnDeath;
    public int MaxHP => maxHP;
    public int CurrentHP => hp;


    void Awake() => hp = maxHP;

    public void TakeDamage(int dmg)
    {
        // Let skills modify incoming damage (shield, etc.)
        foreach (var mod in GetComponents<NightHunter.combat.IDamageModifier>())
            dmg = mod.ModifyIncomingDamage(dmg);

        // allow full block: use Max(0, dmg) not Max(1, dmg)
        hp -= Mathf.Max(0, dmg);
        if (hp <= 0) Die();
    }

    void Die()
    {
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}
