using System.Collections.Generic;
using UnityEngine;

namespace NightHunter.combat
{
    public static class SkillLibrary
    {
        static bool _loaded;
        static readonly Dictionary<SkillId, SkillData> _byId = new Dictionary<SkillId, SkillData>();

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            var assets = Resources.LoadAll<SkillData>("Skills");
            foreach (var s in assets)
            {
                if (!s || s.id == SkillId.None) continue;
                if (_byId.ContainsKey(s.id)) { Debug.LogWarning($"[SkillLibrary] Duplicate SkillId {s.id}"); continue; }
                _byId.Add(s.id, s);
            }
            Debug.Log($"[SkillLibrary] Loaded {_byId.Count} skills.");
        }

        public static SkillData Get(SkillId id)
        {
            EnsureLoaded();
            SkillData v; _byId.TryGetValue(id, out v);
            return v;
        }
    }
}
