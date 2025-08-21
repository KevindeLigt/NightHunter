using UnityEngine;
using System;

namespace NightHunter.combat
{
    public static class CurrencyWallet
    {
        const string Key = "NH_BloodMoney";
        public static int Balance { get; private set; }
        public static event Action<int> OnChanged;

        [RuntimeInitializeOnLoadMethod]
        static void Load()
        {
            Balance = PlayerPrefs.GetInt(Key, 0);
        }

        public static void Add(int amount)
        {
            if (amount <= 0) return;
            Balance += amount;
            PlayerPrefs.SetInt(Key, Balance);
            PlayerPrefs.Save();
            OnChanged?.Invoke(Balance);
        }

        public static bool Spend(int amount)
        {
            if (amount <= 0) return true;
            if (Balance < amount) return false;
            Balance -= amount;
            PlayerPrefs.SetInt(Key, Balance);
            PlayerPrefs.Save();
            OnChanged?.Invoke(Balance);
            return true;
        }
    }
}
