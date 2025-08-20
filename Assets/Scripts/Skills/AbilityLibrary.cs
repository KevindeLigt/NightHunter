using System.Collections.Generic;
using UnityEngine;

namespace NightHunter.combat
{
    public static class AbilityLibrary
    {
        static bool _loaded;
        static readonly Dictionary<SkillId, SkillSpec> _byId = new();

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            var specs = Resources.LoadAll<SkillSpec>("Skills");
            foreach (var s in specs)
            {
                if (!s || s.id == SkillId.None) continue;
                if (_byId.ContainsKey(s.id)) { Debug.LogWarning($"[AbilityLibrary] Duplicate SkillId {s.id}"); continue; }
                _byId.Add(s.id, s);
            }
            Debug.Log($"[AbilityLibrary] Loaded {_byId.Count} skills.");
        }

        public static SkillSpec Get(SkillId id)
        {
            EnsureLoaded();
            _byId.TryGetValue(id, out var spec);
            return spec;
        }
    }
}
