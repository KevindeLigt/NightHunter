using UnityEngine;
using UnityEngine.UI;
using NightHunter.combat;

public class SkillHUD : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public Image frame;        // optional decorative frame
        public Image icon;         // shows SkillData.icon
        public Image cooldownFill; // Image Type = Filled / Radial 360
        public string keyHint;     // "1", "2", "3" (optional)
        [HideInInspector] public SkillId current;
    }

    [Header("Refs")]
    [SerializeField] private SkillController skills;

    [Header("Slots")]
    public SlotUI slot1;
    public SlotUI slot2;
    public SlotUI slot3;

    void Start()
    {
        ApplyIcons();
    }

    void Update()
    {
        // if slots changed at runtime, refresh icons
        if (slot1.current != skills.slot1 || slot2.current != skills.slot2 || slot3.current != skills.slot3)
            ApplyIcons();

        // update cooldown fills (1 = cooling down, 0 = ready)
        UpdateCooldown(slot1, skills.slot1);
        UpdateCooldown(slot2, skills.slot2);
        UpdateCooldown(slot3, skills.slot3);
    }

    void ApplyIcons()
    {
        ApplyIcon(slot1, skills.slot1);
        ApplyIcon(slot2, skills.slot2);
        ApplyIcon(slot3, skills.slot3);
    }

    void ApplyIcon(SlotUI ui, SkillId id)
    {
        ui.current = id;
        var data = SkillLibrary.Get(id);
        bool has = (data != null && id != SkillId.None);
        if (ui.icon) { ui.icon.enabled = has; ui.icon.sprite = has ? data.icon : null; }
        if (ui.cooldownFill) { ui.cooldownFill.fillAmount = 0f; ui.cooldownFill.enabled = has; }
        if (ui.frame) ui.frame.enabled = true; // keep frame visible even if empty, adjust if you prefer
    }

    void UpdateCooldown(SlotUI ui, SkillId id)
    {
        if (!ui.cooldownFill || id == SkillId.None) return;
        float t = skills.GetCooldown01(id);         // 1..0
        ui.cooldownFill.fillAmount = t;             // radial mask
        if (ui.icon) ui.icon.color = (t > 0f) ? new Color(1, 1, 1, 0.5f) : Color.white; // dim while cooling
    }
}
