using System;
using UnityEditor;
using UnityEngine;

namespace NightHunter.combat
{
    /// Drives WeaponController with a 2-slot loadout: one equipped, one backup.
    public class WeaponLoadout : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private WeaponController controller;

        [Header("Start Loadout")]
        [SerializeField] private WeaponId startEquipped = WeaponId.Pistol;
        [SerializeField] private WeaponId startBackup = WeaponId.None;

        public event Action<WeaponId, WeaponId> OnLoadoutChanged; // (equipped, backup)

        private WeaponId _equipped;
        private WeaponId _backup;

        public WeaponId Equipped => _equipped;
        public WeaponId Backup => _backup;

        void Awake()
        {
            if (!controller)
            {
                controller = GetComponent<WeaponController>();
                if (!controller) { Debug.LogError("[WeaponLoadout] No WeaponController found."); enabled = false; return; }
            }

            // Ensure controller won’t fight us for control
            var so = new SerializedObject(controller);
            var prop = so.FindProperty("manageInternally");
            if (prop != null) { prop.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }

            _equipped = startEquipped;
            _backup = startBackup;

            // Make sure controller “owns” both and has ammo entries
            if (_equipped != WeaponId.None) controller.AddWeapon(_equipped, 0, false);
            if (_backup != WeaponId.None) controller.AddWeapon(_backup, 0, false);

            // Equip the primary
            if (_equipped != WeaponId.None) controller.Equip(_equipped);

            OnLoadoutChanged?.Invoke(_equipped, _backup);
        }

        public void Swap()
        {
            if (_backup == WeaponId.None && _equipped != WeaponId.None) return;

            var temp = _equipped;
            _equipped = _backup;
            _backup = temp;

            if (_equipped != WeaponId.None) controller.Equip(_equipped);
            OnLoadoutChanged?.Invoke(_equipped, _backup);
        }

        /// Pick up a weapon: becomes backup by default. Returns dropped (old backup) if any.
        public WeaponId PickupAsBackup(WeaponId newWeapon, int reserveAmmoToAdd = 0, bool autoSwapToNew = false)
        {
            if (newWeapon == WeaponId.None) return WeaponId.None;

            controller.AddWeapon(newWeapon, reserveAmmoToAdd, false);

            var dropped = _backup;
            _backup = newWeapon;

            if (autoSwapToNew) Swap();

            OnLoadoutChanged?.Invoke(_equipped, _backup);
            return dropped;
        }

        /// Force-equip: new weapon becomes equipped; old equipped moves to backup.
        public WeaponId ForceEquip(WeaponId newWeapon, int reserveAmmoToAdd = 0)
        {
            if (newWeapon == WeaponId.None) return WeaponId.None;

            controller.AddWeapon(newWeapon, reserveAmmoToAdd, false);
            var displaced = _equipped;
            _equipped = newWeapon;
            _backup = displaced;

            controller.Equip(_equipped);
            OnLoadoutChanged?.Invoke(_equipped, _backup);
            return displaced;
        }
    }
}
