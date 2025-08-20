using UnityEngine;
using UnityEngine.UI;
using NightHunter.combat;

public class SkillHUD : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public Image frame;          // optional decorative frame
        public Image icon;           // SkillSpec.icon
        public Image cooldownFill;   // Image Type = Filled / Radial 360
        [HideInInspector] public SkillId current; // internal tracking
    }

    [Header("Refs")]
    [SerializeField] private AbilityRunner abilityRunner;

    [Header("Slots")]
    public SlotUI slot1;
    public SlotUI slot2;
    public SlotUI slot3;

    void Start() { RefreshAll(); }

    void Update()
    {
        if (!abilityRunner) return;

        // update icons if slots changed at runtime
        var slots = abilityRunner.GetSlots(); // [slot1, slot2, slot3]
        if (slot1.current != slots[0] || slot2.current != slots[1] || slot3.current != slots[2])
            RefreshAll();

        // cooldown fills (1 = cooling, 0 = ready)
        UpdateCooldown(slot1, slots[0]);
        UpdateCooldown(slot2, slots[1]);
        UpdateCooldown(slot3, slots[2]);
    }

    void RefreshAll()
    {
        var slots = abilityRunner.GetSlots();
        ApplyIcon(slot1, slots[0]);
        ApplyIcon(slot2, slots[1]);
        ApplyIcon(slot3, slots[2]);
    }

    void ApplyIcon(SlotUI ui, SkillId id)
    {
        ui.current = id;
        var spec = AbilityLibrary.Get(id);

        bool has = (spec != null && id != SkillId.None);
        if (ui.icon)
        {
            ui.icon.enabled = has;
            ui.icon.sprite = has ? spec.icon : null;
            ui.icon.color = Color.white;
        }
        if (ui.cooldownFill)
        {
            ui.cooldownFill.enabled = has;
            ui.cooldownFill.fillAmount = 0f; // start as ready
        }
        if (ui.frame) ui.frame.enabled = true; // keep visible; change if you want empty=hidden
    }

    void UpdateCooldown(SlotUI ui, SkillId id)
    {
        if (!ui.cooldownFill || id == SkillId.None) return;

        float t = abilityRunner.GetCooldown01(id); // 1..0
        ui.cooldownFill.fillAmount = t;

        // dim icon while cooling down
        if (ui.icon) ui.icon.color = (t > 0f) ? new Color(1, 1, 1, 0.5f) : Color.white;
    }
}
