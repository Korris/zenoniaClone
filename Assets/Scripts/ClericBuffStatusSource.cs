using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// How long a hero's Spirit Blessing (slot 0, +ATK) / Divine Aegis (slot 1, +DEF) really has left, for the maintained status picture
    /// (ClericBuffStatusFx), on every side:
    ///  - offline / authority: the hero's own BuffState (HeroBuffs);
    ///  - client, local hero: the server's SelfState buff list (HeroBuffs.ActiveFor);
    ///  - client, any other hero (party or not): the server's AllyBuff event (NetEventBus.AllyBuff, sent to everyone near the hero when the buff
    ///    is applied / refreshed) gives the server-clock end time.
    /// A dead hero has nothing. Buffs are matched by their source skill, so another class's +ATK buff never shows the Cleric picture.
    /// </summary>
    public static class ClericBuffStatusSource
    {
        public const int Atk = 0, Def = 1;
        static readonly Dictionary<HeroController, float[]> NetEnd = new Dictionary<HeroController, float[]>();
        static readonly List<BuffView> Views = new List<BuffView>(6);
        static byte atkSource = BuffSource.None, defSource = BuffSource.None;
        static bool resolved;

        static void Resolve()
        {
            if (resolved) return;
            resolved = true;
            foreach (SkillDef d in SkillTable.ForClass(RpgSession.ClassId(HeroClass.Cleric)))
            {
                if (d.Id == ClericBuffFx.BlessingId) atkSource = BuffSource.Pack(d.Class, d.Index);
                else if (d.Id == ClericBuffFx.AegisId) defSource = BuffSource.Pack(d.Class, d.Index);
            }
        }

        /// <summary>Atk / Def for a packed buff source, -1 for any other skill's buff.</summary>
        public static int SlotOf(byte source)
        {
            Resolve();
            return source == BuffSource.None ? -1 : source == atkSource ? Atk : source == defSource ? Def : -1;
        }

        /// <summary>Client: an AllyBuff event played at server time `serverMs` (the buff runs `msLeft` more on the server; 0 = removed).</summary>
        public static void OnNet(HeroController h, byte source, uint msLeft, double serverMs)
        {
            int slot = SlotOf(source);
            if (h == null || slot < 0) return;
            float[] end;
            if (!NetEnd.TryGetValue(h, out end)) NetEnd[h] = end = new float[2];
            end[slot] = msLeft > 0 ? (float)serverMs + msLeft : 0f;
            if (msLeft > 0) ClericBuffStatusFx.Ensure(h, ClericBuffStatusFx.HoldForCastMs); // the cast replay (same batch) plays the receive burst first
        }

        /// <summary>ms left of the hero's Atk / Def Cleric buff (0 = none).</summary>
        public static float LeftMs(HeroController h, int slot)
        {
            if (h == null || h.IsDead) return 0f;
            if (NetMode.IsClient && h != LocalPlayer.Hero)
            {
                float[] end;
                return NetEnd.TryGetValue(h, out end) && end[slot] > 0f ? Mathf.Max(0f, end[slot] - NetClient.ServerNowMs) : 0f;
            }
            Resolve();
            byte want = slot == Atk ? atkSource : defSource;
            HeroBuffs.ActiveFor(h, Views); // authority: the real state; client local hero: the server's list
            for (int i = 0; i < Views.Count; i++) if (Views[i].Source == want && Views[i].LeftMs > 0f) return Views[i].LeftMs;
            return 0f;
        }

        /// <summary>Map change (ProxySpawner.ClearForTransfer): the other heroes' end times belong to the old map.</summary>
        public static void Clear() { NetEnd.Clear(); }
    }
}
