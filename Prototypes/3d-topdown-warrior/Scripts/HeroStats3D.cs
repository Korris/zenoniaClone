using UnityEngine;

/// <summary>
/// Hero HP / MP for the prototype HUD. Slime hits arrive through Hud3D.Pop(hurt: true) and call Hurt(). MP regenerates all the time,
/// HP only after a few seconds without being hit. At 0 HP the hero is "revived" to full after a short pause (no death flow in the prototype).
/// </summary>
public class HeroStats3D : MonoBehaviour
{
    public static HeroStats3D I;
    public int Level = 40, MaxHp = 3600, MaxMp = 260;
    public float MpRegen = 4f, HpRegenPct = 0.03f, HpRegenDelay = 4f, ReviveDelay = 2f;
    public float Hp, Mp;
    float lastHurt = -99f, downAt = -1f;

    void Awake()
    {
        I = this; Hp = MaxHp; Mp = MaxMp;
        // capture aid: --mp 30 starts with 30 MP (to see the "not enough MP" slots), --mpregen 0 stops regeneration
        var a = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(a, "--mp"); if (i >= 0 && i + 1 < a.Length) float.TryParse(a[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out Mp);
        i = System.Array.IndexOf(a, "--mpregen"); if (i >= 0 && i + 1 < a.Length) float.TryParse(a[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out MpRegen);
    }

    /// <summary>Damage from a monster (already shown as a red number by Hud3D).</summary>
    public static void Hurt(int amount)
    {
        if (I == null || I.downAt >= 0f) return;
        I.Hp = Mathf.Max(0f, I.Hp - amount);
        I.lastHurt = Time.time;
        if (I.Hp <= 0f) I.downAt = Time.time;
    }

    /// <summary>Spends `cost` MP if there is enough; false (nothing spent) otherwise.</summary>
    public static bool Spend(int cost)
    {
        if (I == null) return true;
        if (I.Mp < cost) return false;
        I.Mp -= cost;
        return true;
    }

    public static bool HasMp(int cost) { return I == null || I.Mp >= cost; }

    void Update()
    {
        Mp = Mathf.Min(MaxMp, Mp + MpRegen * Time.deltaTime);
        if (downAt >= 0f) { if (Time.time - downAt >= ReviveDelay) { Hp = MaxHp; downAt = -1f; } return; }
        if (Time.time - lastHurt >= HpRegenDelay) Hp = Mathf.Min(MaxHp, Hp + MaxHp * HpRegenPct * Time.deltaTime);
    }
}
