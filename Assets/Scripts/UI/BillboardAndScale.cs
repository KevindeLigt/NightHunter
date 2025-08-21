using UnityEngine;

public class BillboardAndScale : MonoBehaviour
{
    public Camera cam;
    public float sizeAt1m = 0.08f; // tweak to taste
    public float minScale = 0.4f, maxScale = 2f;

    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;

        Vector3 toCam = transform.position - cam.transform.position;
        transform.rotation = Quaternion.LookRotation(toCam);

        float d = toCam.magnitude;
        float s = Mathf.Clamp(d * sizeAt1m, minScale, maxScale);
        transform.localScale = new Vector3(s, s, s);
    }
}
