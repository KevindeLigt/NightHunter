using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [SerializeField] float decayPerSec = 6f;   // how fast it settles
    [SerializeField] float maxPosOffset = 0.05f;
    [SerializeField] float maxRotOffset = 1.5f; // degrees

    float trauma;                 // 0..1
    Vector3 basePos;
    Quaternion baseRot;

    void Awake() { basePos = transform.localPosition; baseRot = transform.localRotation; }

    public void Kick(float intensity = 0.2f, float duration = 0.07f)
    {
        // simple “trauma” model, duration contributes to intensity
        trauma = Mathf.Clamp01(trauma + intensity + duration * 0.5f);
    }

    void LateUpdate()
    {
        if (trauma <= 0f) return;

        float t = trauma * trauma; // non-linear falloff
        Vector3 pos = new Vector3(
            (Mathf.PerlinNoise(Time.time * 17f, 0f) - 0.5f),
            (Mathf.PerlinNoise(0f, Time.time * 19f) - 0.5f),
            (Mathf.PerlinNoise(Time.time * 23f, Time.time * 29f) - 0.5f)
        ) * (maxPosOffset * t);

        Vector3 rot = new Vector3(
            (Mathf.PerlinNoise(Time.time * 31f, 0f) - 0.5f),
            (Mathf.PerlinNoise(0f, Time.time * 37f) - 0.5f),
            (Mathf.PerlinNoise(Time.time * 41f, Time.time * 43f) - 0.5f)
        ) * (maxRotOffset * t);

        transform.localPosition = basePos + pos;
        transform.localRotation = baseRot * Quaternion.Euler(rot);

        trauma = Mathf.MoveTowards(trauma, 0f, decayPerSec * Time.deltaTime);
        if (trauma <= 0f) { transform.localPosition = basePos; transform.localRotation = baseRot; }
    }
}
