namespace NightHunter.combat
{
    public enum SkillId : ushort
    {
        None = 0,
        Adrenaline = 1,   // example: future self-buff (speed/regen) if you want
        Shield = 2,       // <— self shield
        DashSurge = 3,    // <— upgradeable dash
        Bomb = 4, 
    }

    public enum SkillKind : byte
    {
        SelfBuff = 0,     // optional future use
        ShieldSelf = 1,
        DashUpgrade = 2,
        Bomb = 3,
    }
}
