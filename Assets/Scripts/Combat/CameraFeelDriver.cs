using UnityEngine;

public class CameraFeelDriver : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] float recoilReturn = 12f; // how fast recoil settles
    [SerializeField] float posReturn = 12f; // how fast kickback settles
    [SerializeField] float shakeFreq = 28f;

    Transform pivot;               // we animate THIS, not the camera itself
    Vector3 baseLocalPos = Vector3.zero;
    Quaternion baseLocalRot = Quaternion.identity;
    float baseFov;

    float pitchKick;         // degrees up
    float backKick;          // meters back (local -Z)
    float shakeT, shakeAmp;  // seconds, amplitude
    float fovPunch;

    void Awake()
    {
        if (!cam) cam = GetComponent<Camera>();
        baseFov = cam ? cam.fieldOfView : 60f;

        // Reparent camera under a pivot so we don’t fight mouse-look
        var parent = transform.parent;
        var camPos = transform.localPosition;
        var camRot = transform.localRotation;

        var pivotGO = new GameObject("__CameraFeelPivot");
        pivot = pivotGO.transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = camPos;
        pivot.localRotation = camRot;
        pivot.localScale = Vector3.one;

        transform.SetParent(pivot, false); // camera keeps its own local pos/rot for look
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;

        // decay recoil & position kick
        pitchKick = Mathf.MoveTowards(pitchKick, 0f, recoilReturn * dt);
        backKick = Mathf.MoveTowards(backKick, 0f, posReturn * dt);

        // base pose on pivot
        Vector3 pos = baseLocalPos + new Vector3(0, 0, -backKick);
        Quaternion rot = baseLocalRot;

        // shake
        if (shakeT > 0f)
        {
            shakeT -= dt;
            float n1 = (Mathf.PerlinNoise(Time.time * shakeFreq, 0f) - 0.5f) * 2f;
            float n2 = (Mathf.PerlinNoise(0f, Time.time * shakeFreq) - 0.5f) * 2f;
            pos += new Vector3(n1, n2, 0f) * (shakeAmp * 0.01f);
            rot *= Quaternion.Euler(n2 * shakeAmp, n1 * shakeAmp, 0f);
        }

        // apply to pivot (mouse-look stays on the camera child)
        if (pivot)
        {
            pivot.localPosition = pos;
            pivot.localRotation = rot;
        }

        // FOV punch (decays)
        if (cam)
        {
            fovPunch = Mathf.MoveTowards(fovPunch, 0f, dt * Mathf.Max(8f, cam.fieldOfView));
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, baseFov + fovPunch, dt * 8f);
        }
    }

    public void ShotKick(float upDeg, float back)
    {
        pitchKick += Mathf.Max(0f, upDeg);
        backKick += Mathf.Max(0f, back);
    }
    public void Shake(float amplitude, float duration)
    {
        shakeAmp = Mathf.Max(shakeAmp, amplitude);
        shakeT = Mathf.Max(shakeT, duration);
    }
    public void FovPunch(float amount, float _returnSpeed = 8f)
    {
        fovPunch = Mathf.Max(fovPunch, amount);
    }
}
