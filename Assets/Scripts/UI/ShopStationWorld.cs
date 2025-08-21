using System.Collections;
using UnityEngine;

namespace NightHunter.combat
{
    [RequireComponent(typeof(Collider))]
    public class ShopStationWorld : MonoBehaviour
    {
        public ShopUI ui;                 // world-space ShopUI on the canvas
        public CanvasGroup canvasGroup;   // same canvas' CanvasGroup
        public KeyCode interactKey = KeyCode.E;
        public bool requirePress = false; // false = auto show in trigger; true = press to toggle
        public float fadeSeconds = 0.15f;

        int insideCount; bool visible;

        void Reset() { GetComponent<Collider>().isTrigger = true; }
        void Awake() { if (!canvasGroup && ui) canvasGroup = ui.GetComponent<CanvasGroup>(); HideImmediate(); }

        void HideImmediate()
        {
            if (!canvasGroup) return;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = canvasGroup.blocksRaycasts = false;
            if (ui) ui.Close();
        }

        void SetVisible(bool v)
        {
            visible = v;
            StopAllCoroutines();
            StartCoroutine(FadeTo(v ? 1f : 0f));
            if (ui) { if (v) ui.Open(); else ui.Close(); }
        }

        IEnumerator FadeTo(float target)
        {
            if (!canvasGroup) yield break;
            float t = 0f, start = canvasGroup.alpha;
            while (t < fadeSeconds)
            {
                t += Time.deltaTime;
                float a = (fadeSeconds <= 0f) ? 1f : t / fadeSeconds;
                canvasGroup.alpha = Mathf.Lerp(start, target, a);
                yield return null;
            }
            canvasGroup.alpha = target;
            bool on = target > 0.99f;
            canvasGroup.interactable = on;
            canvasGroup.blocksRaycasts = on;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            insideCount++;
            if (!requirePress) SetVisible(true);
        }
        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            insideCount = Mathf.Max(0, insideCount - 1);
            if (insideCount == 0) SetVisible(false);
        }
        void Update()
        {
            if (!requirePress || insideCount <= 0) return;
            if (Input.GetKeyDown(interactKey)) SetVisible(!visible);
        }
    }
}

