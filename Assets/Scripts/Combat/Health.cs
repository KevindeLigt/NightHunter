using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHP = 100;
    int hp;

    public System.Action OnDeath;

    void Awake() => hp = maxHP;

    public void TakeDamage(int dmg)
    {
        hp -= Mathf.Max(1, dmg);
        if (hp <= 0) Die();
    }

    void Die()
    {
        OnDeath?.Invoke();
        Destroy(gameObject);
    }
}
