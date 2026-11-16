using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The "throw a Pet Ball" prompt (docs/pet-system-design.md 6, 8: pick a monster, tap the ball icon, the server checks). Two buttons above the Talk prompt, shown while the
    /// hero has a catchable target: the monster it hit in the last 10 s at half HP or less that can become a pet (not a boss), or a fallen boss whose capture window is open
    /// (PetCaptureService, NetEventType.PetCapturable) within 4 tiles. Normal ball on the left, rare ball on the right when the account has one. The buttons only ask
    /// (InvOp.PetCapture with the monster's NetId); the answer is the server's toast.
    /// </summary>
    public sealed class CaptureMarker : MonoBehaviour
    {
        static CaptureMarker instance;
        static ushort corpseId; static Vector2 corpseFeet; static float corpseUntil; static bool corpseWanted;

        RectTransform root;
        Button normal, rare;
        Text normalLabel, rareLabel;
        ushort targetId;
        float rareArmedUntil; // ChatGPT Lần 9: a Rare Ball (100 %) needs a second tap within 3 s, so a stray tap in a combo cannot spend it

        public static void Create(Transform canvas)
        {
            instance = UiKit.Fill(canvas, "CaptureMarker").gameObject.AddComponent<CaptureMarker>();
            instance.Build();
        }

        /// <summary>A boss just fell: a ball may be thrown at `id` for `ms` (from the server's event, or straight from PetCaptureService offline).</summary>
        public static void OpenCorpse(ushort id, Vector2 feet, uint ms, bool wanted) { corpseId = id; corpseFeet = feet; corpseUntil = Time.unscaledTime + ms / 1000f; corpseWanted = wanted; }

        void Build()
        {
            root = UiWinKit.Anchored(transform, "Balls", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 0f, 126f, 330f, 42f);
            normal = UiWinKit.ArtButton(root, "Normal", "btn-gold", 0f, 0f, 160f, 42f, "Pet Ball", 15, new Color(0.25f, 0.12f, 0.02f), () => Throw(false));
            normalLabel = normal.GetComponentInChildren<Text>();
            rare = UiWinKit.ArtButton(root, "Rare", "btn-navy", 170f, 0f, 160f, 42f, "Rare Ball", 15, Color.white, () => Throw(true));
            rareLabel = rare.GetComponentInChildren<Text>();
            root.gameObject.SetActive(false);
        }

        void Throw(bool rareBall)
        {
            if (targetId == 0) return;
            if (rareBall && Time.unscaledTime > rareArmedUntil) { rareArmedUntil = Time.unscaledTime + 3f; return; } // first tap arms, the label asks to tap again
            rareArmedUntil = 0f;
            PetActions.Send(InvOp.PetCapture, (byte)(rareBall ? 1 : 0), targetId.ToString());
        }

        /// <summary>The monster a ball could go to right now, 0 for none. Client-side estimate only: the server decides.</summary>
        ushort FindTarget(HeroController hero, out bool boss)
        {
            boss = false;
            if (corpseUntil > Time.unscaledTime && Vector2.Distance(hero.Position, corpseFeet) <= PetRules.CaptureRangeUnits) { boss = true; return corpseId; }
            Monster m = Monster.LastHitByMe;
            if (m == null || m.IsDying || m.IsBoss || Time.time - Monster.LastHitByMeAt > PetRules.CaptureHitMemoryMs / 1000f) return 0;
            if (m.Hp > m.MaxHp * PetRules.CaptureHpLine || Vector2.Distance(hero.Position, m.Center) > PetRules.CaptureRangeUnits) return 0;
            return PetCaptureService.PetOf(m) != null ? m.NetId : (ushort)0;
        }

        void Update()
        {
            HeroController hero = GameSession.Hero;
            PlayerState p = PetActions.Me;
            AccountState a = p != null ? p.Account : null;
            bool boss;
            targetId = hero != null && !hero.IsDead && a != null && !UiWindows.ModalOpen && !InventoryPanel.IsOpen ? FindTarget(hero, out boss) : (ushort)0;
            bool show = targetId != 0 && (a.balls > 0 || a.rareBalls > 0);
            root.gameObject.SetActive(show);
            if (!show) return;
            normal.gameObject.SetActive(a.balls > 0);
            rare.gameObject.SetActive(a.rareBalls > 0);
            normalLabel.text = "Pet Ball  x" + a.balls;
            rareLabel.text = Time.unscaledTime <= rareArmedUntil ? "Tap again to throw" : "Rare Ball  x" + a.rareBalls;
            ((RectTransform)rare.transform).anchoredPosition = new Vector2(a.balls > 0 ? 170f : 0f, 0f);
        }
    }
}
