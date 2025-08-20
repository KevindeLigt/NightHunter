using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class KnockbackReceiver : MonoBehaviour
{
    [SerializeField] float drag = 8f; // higher = stops faster
    [SerializeField] float mass = 1f;

    CharacterController cc;
    Vector3 impact;

    void Awake() { cc = GetComponent<CharacterController>(); }

    public void AddImpact(Vector3 direction, float force)
    {
        direction.y = 0f; direction.Normalize();
        impact += direction * (force / Mathf.Max(0.01f, mass));
    }

    void Update()
    {
        if (impact.sqrMagnitude > 0.0001f)
        {
            cc.Move(impact * Time.deltaTime);
            impact = Vector3.MoveTowards(impact, Vector3.zero, drag * Time.deltaTime);
        }
    }
}
