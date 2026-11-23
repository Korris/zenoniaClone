using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Playing the moves of a DuelView as an animation (spec: docs/reference/card-gem-ui/duel-flip-animation-spec-chatgpt.md). The server (or the offline host) has already decided everything; this only shows it:
    /// a new move first PLACES its card (230 ms: flies in from the hand side, lands at 150 ms with a small squash, dust), 40 ms later every captured card turns a full 180 degrees (360 ms: lift, turn, owner ring
    /// swaps at 90, flash, sparkles, land), staggered 70 ms apart (up, right, down, left), and only when the last one has landed do the score, the log and the turn unlock (`presented` counts the moves shown so far;
    /// score, log, cards left and the result banner all read it). A card captured sideways turns about its vertical axis (scale X), one captured above / below about its horizontal axis (scale Y). Only the card's
    /// own box animates (never the board cells), and the turning cell is lifted above its neighbours while it moves. A view that jumps (a new duel, a reconnect) is shown at once, without playing.
    /// Particles and the flash are the kit's existing sprites (Sprites/fx/light-skill star and diamond) and plain tints; sounds: none exist for cards yet (whoosh, chime, tap, slap are missing).
    /// </summary>
    public sealed partial class CardDuelBattleView
    {
        public const float PlaceMs = 230f, PlaceGapMs = 40f, FlipMs = 360f, FlipStaggerMs = 70f;
        const float LiftPx = 8f, LiftScale = 0.06f, TiltDegrees = 4f;
        static readonly Color FlashTint = new Color(1f, 0.973f, 0.875f), MineGlow = new Color(0.47f, 0.72f, 0.94f), TheirGlow = new Color(0.9f, 0.42f, 0.47f);
        // neighbour order of the stagger: up, right, down, left
        static readonly int[] DirOffset = { -4, 1, 4, -1 };

        int presented = -1; // moves already on screen (-1 = nothing drawn yet: the next view is shown as it is)
        readonly int[] dispCard = new int[16], dispOwner = new int[16];
        int curMove = -1, curPlace = -1;
        float curStart, curEnd;
        readonly float[] flipStart = new float[16];
        readonly bool[] flipHoriz = new bool[16], flipSwapped = new bool[16], flipRaised = new bool[16];
        readonly int[] flipTo = new int[16], homeSibling = new int[16];
        bool dustDone;

        bool Animating(DuelView v) { return curMove >= 0 || (presented >= 0 && presented < v.Moves.Count); }

        void ResetPresentation()
        {
            presented = -1; curMove = -1; curPlace = -1;
            for (int c = 0; c < 16; c++) { dispCard[c] = DuelBoard.Empty; dispOwner[c] = DuelBoard.Empty; ClearCell(c); }
        }

        void ClearCell(int c)
        {
            flipStart[c] = -1f; flipSwapped[c] = false;
            if (flipRaised[c] && slotRt[c] != null) { slotRt[c].SetSiblingIndex(homeSibling[c]); flipRaised[c] = false; }
            if (cell[c] == null) return;
            Transform t = cell[c].Root;
            t.localScale = Vector3.one; t.localRotation = Quaternion.identity; t.localPosition = cellHome[c];
            if (cardShadow[c] != null) cardShadow[c].color = Color.clear;
            if (cardFlash[c] != null) cardFlash[c].color = Color.clear;
            if (ownerOverlay[c] != null) ownerOverlay[c].color = Color.white;
        }

        /// <summary>A view arrived: show it at once when it is a new duel or a long jump, else the new moves are queued and played by StepPresentation.</summary>
        void OnNewView(DuelView v)
        {
            if (presented < 0 || v.Moves.Count < presented || v.Moves.Count - presented > 4) Snap(v);
            Redraw(v);
        }

        void Snap(DuelView v)
        {
            presented = v.Moves.Count; curMove = -1;
            DuelBoard b = v.Board();
            for (int c = 0; c < 16; c++) { dispCard[c] = b.CardAt(c); dispOwner[c] = b.OwnerAt(c); ClearCell(c); }
        }

        void StepPresentation(DuelView v)
        {
            UpdateParticles();
            for (int guard = 0; guard < 6; guard++) // a move that ends this frame lets the next one start in the same frame
            {
                if (curMove < 0)
                {
                    if (presented < 0 || presented >= v.Moves.Count) return;
                    BeginMove(v, presented);
                }
                float t = (Time.unscaledTime - curStart) * 1000f;
                for (int c = 0; c < 16; c++) AnimateCell(c, t);
                if (t < curEnd) return;
                EndMove(v);
            }
        }

        void BeginMove(DuelView v, int index)
        {
            DuelMove m = v.Moves[index];
            curMove = index; curStart = Time.unscaledTime; curPlace = m.Cell; dustDone = false;
            dispCard[m.Cell] = m.Card; dispOwner[m.Cell] = m.Side;
            int k = 0;
            for (int d = 0; d < 4; d++)
            {
                int target = m.Cell + DirOffset[d];
                bool sideStep = d == 1 || d == 3;
                if (target < 0 || target > 15 || (sideStep && target / 4 != m.Cell / 4) || (m.FlipMask & (1 << target)) == 0) continue;
                flipStart[target] = PlaceMs + PlaceGapMs + k * FlipStaggerMs;
                flipHoriz[target] = sideStep; flipTo[target] = m.Side; flipSwapped[target] = false;
                k++;
            }
            curEnd = k == 0 ? PlaceMs : PlaceMs + PlaceGapMs + (k - 1) * FlipStaggerMs + FlipMs;
            Redraw(v); // the placed card appears (its flips are still the old owner)
            Debug.Log("[ZenoniaProto] duel-anim: move " + (index + 1) + " places " + DuelBoard.Coord(m.Cell) + ", flips " + k);
        }

        void EndMove(DuelView v)
        {
            for (int c = 0; c < 16; c++)
            {
                if (flipStart[c] >= 0f && !flipSwapped[c]) { dispOwner[c] = flipTo[c]; ApplyOwner(c, flipTo[c]); }
                ClearCell(c);
            }
            presented++; curMove = -1; curPlace = -1;
            Redraw(v); // score, log, cards left, valid cells and the turn unlock now
        }

        // ---- one cell at time t (ms since the move began) ----

        void AnimateCell(int c, float t)
        {
            if (dispCard[c] == DuelBoard.Empty) return;
            Transform tr = cell[c].Root;
            Vector3 pos = cellHome[c], scale = Vector3.one;
            float rotZ = 0f, shadow = 0f, flash = 0f;
            if (c == curPlace) PlaceFrame(c, t, ref pos, ref scale, ref shadow, ref flash);
            else if (flipStart[c] >= 0f) FlipFrame(c, t - flipStart[c], ref pos, ref scale, ref rotZ, ref shadow, ref flash);
            else return;
            tr.localPosition = pos; tr.localScale = scale; tr.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            cardShadow[c].color = new Color(0f, 0f, 0f, shadow);
            cardFlash[c].color = new Color(FlashTint.r, FlashTint.g, FlashTint.b, flash);
        }

        void PlaceFrame(int c, float t, ref Vector3 pos, ref Vector3 scale, ref float shadow, ref float flash)
        {
            float s = 1f;
            if (t < 150f) { float k = OutCubic(t / 150f); s = Mathf.Lerp(1.10f, 1f, k); pos += new Vector3(0f, -(1f - k) * 60f, 0f); }
            else if (t < 190f) { float k = (t - 150f) / 40f; s = Mathf.Lerp(1f, 0.95f, k); flash = 0.25f * (1f - k); }
            else if (t < PlaceMs) { s = Mathf.Lerp(0.95f, 1f, (t - 190f) / 40f); if (!dustDone) { dustDone = true; Burst(c, 5, 10f, 18f, 180f, Color.white); } }
            scale = new Vector3(s, s, 1f);
        }

        void FlipFrame(int c, float tf, ref Vector3 pos, ref Vector3 scale, ref float rotZ, ref float shadow, ref float flash)
        {
            if (tf < 0f) return; // waiting for its turn in the stagger
            if (tf >= FlipMs) { if (!flipSwapped[c]) Swap(c); return; }
            if (!flipRaised[c]) { homeSibling[c] = slotRt[c].GetSiblingIndex(); slotRt[c].SetAsLastSibling(); if (fxLayer != null) fxLayer.SetAsLastSibling(); flipRaised[c] = true; } // above its neighbours
            float lift = tf < 50f ? tf / 50f : tf < 310f ? 1f : 1f - OutBack((tf - 310f) / 50f);
            float theta = tf <= 50f ? 0f : tf >= 310f ? 180f : 180f * InOutCubic((tf - 50f) / 260f);
            if (theta >= 90f && !flipSwapped[c]) Swap(c);
            float axis = Mathf.Max(0.04f, Mathf.Abs(Mathf.Cos(theta * Mathf.Deg2Rad))), grow = 1f + LiftScale * lift;
            scale = flipHoriz[c] ? new Vector3(grow * axis, grow, 1f) : new Vector3(grow, grow * axis, 1f);
            pos += new Vector3(0f, LiftPx * lift, 0f);
            rotZ = (flipHoriz[c] ? 1f : -1f) * TiltDegrees * Mathf.Sin(theta * Mathf.Deg2Rad); // fake perspective: a small lean while turning
            if (tf >= 310f) rotZ += 2f * Mathf.Sin((tf - 310f) / 50f * Mathf.PI * 2f); // the little shake on landing
            shadow = 0.12f * Mathf.Sin(Mathf.Clamp01(tf / FlipMs) * Mathf.PI);
            flash = Mathf.Abs(tf - 180f) < 35f ? 0.45f * (1f - Mathf.Abs(tf - 180f) / 35f) : 0f;
            if (flipSwapped[c])
            {
                float glow = Mathf.Clamp01(1f - Mathf.Abs(tf - 240f) / 70f); // the new owner's colour glows softly for about 140 ms
                ownerOverlay[c].color = Color.Lerp(Color.white, flipTo[c] == DuelView.Me ? MineGlow : TheirGlow, glow * 0.8f);
            }
        }

        // the owner changes at the 90 degree mark; the sparkles start there
        void Swap(int c)
        {
            flipSwapped[c] = true;
            dispOwner[c] = flipTo[c];
            ApplyOwner(c, flipTo[c]);
            Burst(c, 8, 12f, 25f, 230f, Color.white);
        }

        static float OutCubic(float x) { x = Mathf.Clamp01(x); return 1f - Mathf.Pow(1f - x, 3f); }
        static float InOutCubic(float x) { x = Mathf.Clamp01(x); return x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f; }
        static float OutBack(float x) { x = Mathf.Clamp01(x) - 1f; return 1f + 0.7f * x * x * x + 0.7f * x * x; } // overshoots about 3 percent

        // ---- sparkles ----

        struct Spark { public Image Img; public float Born, Life; public Vector2 From, To; public bool Live; }
        readonly List<Spark> sparks = new List<Spark>();
        Sprite sparkStar, sparkDiamond;

        void BuildFxLayer(float boardW)
        {
            fxLayer = UiKit.Rect(boardRoot, "FlipFx", 0f, 0f, boardW, BoardH);
            sparkStar = PaperArt.Get("kit/star-full"); // the kit's own gold star and the HUD sparkle (both have alpha, so they show on the cream board)
            sparkDiamond = SpriteOf("Sprites/ui/ro-soft/wanted-sparkle");
        }

        static Sprite softGlow;
        /// <summary>The kit's soft round glow (ui/ro-soft/pet-glow): the shadow under a lifted card (tinted black) and the light on the edge-on card.</summary>
        static Sprite SoftGlow { get { return softGlow != null ? softGlow : (softGlow = SpriteOf("Sprites/ui/ro-soft/pet-glow")); } }

        static Sprite SpriteOf(string path)
        {
            Texture2D t = Resources.Load<Texture2D>(path);
            return t == null ? null : Sprite.Create(t, new Rect(0f, 0f, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        }

        Vector2 CellCentre(int c)
        {
            Rect r = CardDuelBoardLayout.Scaled(CardDuelBoardLayout.Cell(c % 4, c / 4), CardDuelBoardLayout.Scale(BoardH));
            return new Vector2(r.center.x, -r.center.y);
        }

        void Burst(int c, int count, float minR, float maxR, float lifeMs, Color tint)
        {
            if (fxLayer == null || sparkStar == null) return;
            Vector2 centre = CellCentre(c);
            for (int i = 0; i < count; i++)
            {
                float a = (i + Random.value * 0.6f) / count * Mathf.PI * 2f, r = Random.Range(minR, maxR);
                Spark s = Take();
                s.Img.sprite = i % 2 == 0 ? sparkStar : (sparkDiamond != null ? sparkDiamond : sparkStar);
                s.Img.color = tint; s.Img.rectTransform.sizeDelta = Vector2.one * Random.Range(12f, 20f);
                s.Born = Time.unscaledTime; s.Life = lifeMs / 1000f; s.From = centre; s.To = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; s.Live = true;
                s.Img.gameObject.SetActive(true);
                Put(s);
            }
        }

        int takeIndex;
        Spark Take()
        {
            for (int i = 0; i < sparks.Count; i++) if (!sparks[i].Live) { takeIndex = i; return sparks[i]; }
            if (sparks.Count >= 40) { takeIndex = 0; return sparks[0]; } // the pool is full: the oldest one is reused
            Image img = UiKit.Rect(fxLayer, "Spark", 0f, 0f, 12f, 12f).gameObject.AddComponent<Image>();
            img.raycastTarget = false; img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            sparks.Add(new Spark { Img = img });
            takeIndex = sparks.Count - 1;
            return sparks[takeIndex];
        }

        void Put(Spark s) { sparks[takeIndex] = s; }

        void UpdateParticles()
        {
            for (int i = 0; i < sparks.Count; i++)
            {
                Spark s = sparks[i];
                if (!s.Live) continue;
                float k = (Time.unscaledTime - s.Born) / s.Life;
                if (k >= 1f) { s.Live = false; s.Img.gameObject.SetActive(false); sparks[i] = s; continue; }
                Color c = s.Img.color; c.a = 1f - k;
                s.Img.color = c;
                s.Img.rectTransform.anchoredPosition = Vector2.Lerp(s.From, s.To, OutCubic(k));
                s.Img.rectTransform.localScale = Vector3.one * (1f - 0.4f * k);
            }
        }

        static void CenterPivot(RectTransform rt)
        {
            Vector2 size = rt.rect.size;
            Vector2 shift = new Vector2((0.5f - rt.pivot.x) * size.x, (0.5f - rt.pivot.y) * size.y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition += shift; // the card stays where it was; it now turns about its own middle
        }
    }
}
