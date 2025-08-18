using System.Collections.Generic;
using UnityEngine;

namespace NightHunter.combat
{
    public static class WeaponLibrary
    {
        static bool _loaded;
        static readonly Dictionary<WeaponId, WeaponData> _byId = new Dictionary<WeaponId, WeaponData>();

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            var assets = Resources.LoadAll<WeaponData>("Weapons");
            foreach (var wd in assets)
            {
                if (!wd || wd.id == WeaponId.None) continue;
                if (_byId.ContainsKey(wd.id)) { Debug.LogWarning($"[WeaponLibrary] Duplicate id {wd.id}"); continue; }
                _byId.Add(wd.id, wd);
            }
            Debug.Log($"[WeaponLibrary] Loaded {_byId.Count} weapons.");
        }

        public static WeaponData Get(WeaponId id)
        {
            EnsureLoaded();
            WeaponData v; _byId.TryGetValue(id, out v);
            return v;
        }

        public static IReadOnlyDictionary<WeaponId, WeaponData> All
        {
            get { EnsureLoaded(); return _byId; }
        }
    }
}
