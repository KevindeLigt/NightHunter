using UnityEngine;

namespace NightHunter.combat
{
    [CreateAssetMenu(menuName = "NightHunter/Skills/Skill Spec", fileName = "NewSkillSpec")]
    public class SkillSpec : ScriptableObject
    {
        [Header("Identity")]
        public SkillId id = SkillId.None;
        public string displayName = "New Skill";
        public Sprite icon;

        [Header("Modules")]
        public TargetingSO targeting;
        public DeliverySO delivery;
        public EffectSO[] effects;

        [Header("Timing")]
        public float cooldown = 8f;
    }
}
