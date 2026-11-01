using System;
using System.Collections;
using System.IO;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --net-bot turret --turret-role caster|watcher (Tools/net-turret-test.ps1): Deploy Turret online through the real key path. The caster learns the skill, walks to monsters, places a turret and lives
    /// through its whole time, recasts mid-life on another spot, then places one by a portal and leaves through it. The watcher is started after the first turret stands, so it arrives late; it walks to the
    /// spot the caster wrote into --shot-path and takes pictures. What each side saw goes into the log as "turret-e2e:" lines. --turret-shots saves PNGs to --shot-path.
    /// </summary>
    public partial class DebugCapture
    {
        const int TurretSkill = 5;
        bool turretShots;
        Vector2 turretWalkTo;
        bool turretWalk, turretPress;
        Vector2 lastTurretSpot;
        float turretPressUntil;

        static void Tu(string line) { Debug.Log("[ZenoniaProto] turret-e2e: " + line); }

        static DeployedTurret TurretOf(ushort owner)
        {
            Zone z = Zone.Main;
            if (z == null) return null;
            for (int i = 0; i < z.Turrets.Count; i++) if (z.Turrets[i] != null && (owner == 0 || z.Turrets[i].Owner == owner)) return z.Turrets[i];
            return null;
        }

        void StartTurretBot()
        {
            string[] a = Environment.GetCommandLineArgs();
            turretShots = Array.IndexOf(a, "--turret-shots") >= 0 && NetMode.RendersFx;
            Directory.CreateDirectory(shotPath);
            InputAdapter.BotHeld = k =>
            {
                HeroController hero = GameSession.Hero;
                if (!turretWalk || hero == null || hero.IsDead) return false;
                Vector2 move = StepToward(hero.Position, turretWalkTo);
                if (move == Vector2.zero) { Vector2 d = turretWalkTo - hero.Position; move = new Vector2(Mathf.Abs(d.x) > 1f ? Mathf.Sign(d.x) : 0f, Mathf.Abs(d.y) > 1f ? Mathf.Sign(d.y) : 0f); }
                return (k == GameKey.Left && move.x < 0f) || (k == GameKey.Right && move.x > 0f) || (k == GameKey.Down && move.y < 0f) || (k == GameKey.Up && move.y > 0f);
            };
            InputAdapter.BotPressed = k => k == GameKey.Skill1 + TurretSkill && turretPress && Time.time < turretPressUntil;
            StartCoroutine(NetLaunchArgs.Arg(a, "--turret-role") == "watcher" ? TurretWatcher() : TurretCaster());
        }

        IEnumerator TurretShot(string name)
        {
            if (!turretShots) yield break;
            yield return new WaitForEndOfFrame();
            Save(Path.Combine(shotPath, name + ".png"));
        }

        void PressTurret() { turretPress = true; turretPressUntil = Time.time + 0.12f; } // short: with --dev-no-cooldowns a held key would cast again

        IEnumerator WalkNear(Func<Vector2?> where, float within, float timeout)
        {
            float start = Time.realtimeSinceStartup;
            HeroController hero = GameSession.Hero;
            while (Time.realtimeSinceStartup - start < timeout)
            {
                Vector2? to = where();
                if (to.HasValue)
                {
                    turretWalkTo = to.Value;
                    if (Vector2.Distance(hero.Position, to.Value) <= within) break;
                    turretWalk = true;
                }
                else { turretWalk = true; turretWalkTo = PatrolSpot(3); }
                yield return null;
            }
            turretWalk = false;
        }

        static Vector2? NearestMonsterSpot()
        {
            // the monster with the most others within range, so a whole life has something to shoot
            Monster best = null;
            int most = -1;
            foreach (Monster m in Monster.All)
            {
                if (m.IsDying) continue;
                int n = 0;
                foreach (Monster o in Monster.All) if (!o.IsDying && (o.Center - m.Center).sqrMagnitude < 70f * 70f) n++;
                if (n > most) { most = n; best = m; }
            }
            return best != null ? best.Center : (Vector2?)null;
        }

        /// <summary>Places a turret and waits for the server's copy; null when none came.</summary>
        IEnumerator PlaceAndWait(DeployedTurret old, string what, Action<DeployedTurret> got)
        {
            ushort me = GameSession.Hero.NetId;
            PressTurret();
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 5f)
            {
                DeployedTurret t = TurretOf(me);
                if (t != null && t != old)
                {
                    Tu("OK " + what + ": turret " + t.NetId + " at " + t.Feet + ", hero at " + GameSession.Hero.Position + ", life left " + t.RemainingMs + " ms, " + (Time.realtimeSinceStartup - start).ToString("F2") + " s after the key");
                    got(t);
                    yield break;
                }
                yield return null;
            }
            Tu("FAIL " + what + ": no turret came");
        }

        IEnumerator TurretCaster()
        {
            HeroController hero = GameSession.Hero;
            ushort me = hero.NetId;
            foreach (int skill in new[] { 0, 0, 0, 1, 1, 1, 3, 3, 3, TurretSkill, TurretSkill, TurretSkill })
            {
                NetClient.SendInvAction(InvOp.SkillLearn, "", (byte)skill);
                yield return new WaitForSeconds(0.25f);
            }
            float t0 = Time.realtimeSinceStartup;
            while (hero.Rpg.Skills[TurretSkill] < 3 && Time.realtimeSinceStartup - t0 < 15f) yield return null;
            SkillDef def = SkillTable.Get("mechanic", TurretSkill);
            SkillCooldowns.DevNone = NetLaunchArgs.DevNoCooldowns;
            Tu("skill " + def.Name + " rank " + hero.Rpg.Skills[TurretSkill] + ", " + def.PctAt(def.Effects[0], hero.Rpg.Skills[TurretSkill]).ToString("F0") + "% per shot; " + SkillDescribe.Description(def, 3));

            yield return WalkNear(NearestMonsterSpot, 30f, 60f);
            int exp0 = hero.Rpg.Exp, level0 = hero.Rpg.Level;
            DeployedTurret first = null;
            yield return PlaceAndWait(null, "first turret", t => first = t);
            if (first == null) { Application.Quit(); yield break; }
            File.WriteAllText(Path.Combine(shotPath, "turret-spot.txt"), first.Feet.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + first.Feet.y.ToString(System.Globalization.CultureInfo.InvariantCulture));
            yield return new WaitForSeconds(0.05f);
            yield return TurretShot("caster-placing");
            yield return new WaitForSeconds(0.1f);
            turretPress = false;
            turretWalkTo = first.Feet + new Vector2(first.Feet.x > 80f ? -36f : 36f, -14f);
            turretWalk = true;
            yield return new WaitForSeconds(1.1f);
            turretWalk = false;
            Tu("walked off the turret: hero at " + hero.Position + ", turret at " + first.Feet + ", " + Vector2.Distance(hero.Position, first.Feet).ToString("F1") + " units apart");
            yield return TurretShot("caster-placed");

            // the whole of its life: when it fires, and when it goes
            float placed = Time.realtimeSinceStartup;
            int shots = DeployedTurret.ShotsDrawn, pictures = 0;
            string times = "";
            while (first != null && TurretOf(me) == first && Time.realtimeSinceStartup - placed < 20f)
            {
                if (DeployedTurret.ShotsDrawn != shots)
                {
                    shots = DeployedTurret.ShotsDrawn;
                    times += " " + (Time.realtimeSinceStartup - placed).ToString("F2");
                    if (pictures < 6) { pictures++; yield return new WaitForSeconds(0.06f); yield return TurretShot("caster-shot-" + pictures); yield return new WaitForSeconds(0.2f); yield return TurretShot("caster-hit-" + pictures); }
                }
                yield return null;
            }
            Tu("first turret gone after " + (Time.realtimeSinceStartup - placed).ToString("F2") + " s; shots at" + times + "; exp " + exp0 + " -> " + hero.Rpg.Exp + ", level " + level0 + " -> " + hero.Rpg.Level + " (the caster did not attack: any gain is the turret's kills, credited to him)");
            yield return new WaitForSeconds(0.08f);
            yield return TurretShot("caster-vanishing");
            yield return new WaitForSeconds(1f);
            yield return TurretShot("caster-gone");

            // recast mid-life from another spot
            yield return WalkNear(NearestMonsterSpot, 60f, 30f);
            DeployedTurret second = null, third = null;
            yield return PlaceAndWait(null, "second turret", t => second = t);
            yield return new WaitForSeconds(0.5f);
            turretPress = false;
            turretWalkTo = hero.Position + new Vector2(hero.Position.x > 400f ? -40f : 40f, 0f);
            turretWalk = true;
            yield return new WaitForSeconds(1.2f);
            turretWalk = false;
            yield return new WaitForSeconds(1.5f);
            Tu("before the recast: turret " + (second != null ? second.Feet.ToString() : "none") + ", " + (TurretOf(me) == second ? "still standing" : "already gone") + ", cooldown left " + hero.SkillCdLeftMs(TurretSkill).ToString("F0") + " ms");
            yield return PlaceAndWait(second, "recast", t => third = t);
            yield return new WaitForSeconds(0.1f);
            yield return TurretShot("caster-recast");
            Zone z = Zone.Main;
            Tu((third != null && TurretOf(me) == third && z.Turrets.Count == 1 ? "PASS" : "FAIL") + " after the recast this client knows " + z.Turrets.Count + " turret(s), standing at " + (third != null ? third.Feet.ToString() : "none"));
            yield return new WaitForSeconds(0.5f);
            turretPress = false;

            // a turret by a portal, then through the portal
            MapPortal[] portals = MapRegistry.Current.Portals;
            string map = MapRegistry.Current.Id;
            EnablePortalBot(); // the walk the portal soak uses
            InputAdapter.BotPressed = k => k == GameKey.Skill1 + TurretSkill && turretPress && Time.time < turretPressUntil;
            float walked = Time.realtimeSinceStartup, closest = float.MaxValue;
            while (closest > 56f && Time.realtimeSinceStartup - walked < 120f)
            {
                closest = float.MaxValue;
                foreach (MapPortal p in portals) closest = Mathf.Min(closest, Vector2.Distance(TileCenter(new Vector2Int(p.Tx, p.Ty)), hero.Position));
                yield return null;
            }
            Tu("near a portal at " + hero.Position + " after " + (Time.realtimeSinceStartup - walked).ToString("F1") + " s");
            DeployedTurret last = null;
            yield return PlaceAndWait(TurretOf(me), "turret by the portal", t => last = t);
            yield return new WaitForSeconds(0.5f);
            turretPress = false;
            float leaving = Time.realtimeSinceStartup;
            System.Func<GameKey, bool> walk = InputAdapter.BotHeld;
            Vector2 lastSpot = hero.Position;
            float lastMoved = Time.realtimeSinceStartup, jiggleUntil = 0f, nextCast = leaving + 5f;
            int jiggleKey = 0;
            GameKey[] sides = { GameKey.Down, GameKey.Left, GameKey.Right, GameKey.Up };
            InputAdapter.BotHeld = k => Time.time < jiggleUntil ? k == sides[jiggleKey % 4] : walk(k);
            while (MapRegistry.Current.Id == map && Time.realtimeSinceStartup - leaving < 90f)
            {
                if (Vector2.Distance(hero.Position, lastSpot) > 3f) { lastSpot = hero.Position; lastMoved = Time.realtimeSinceStartup; }
                else if (Time.realtimeSinceStartup - lastMoved > 1f) { jiggleKey++; jiggleUntil = Time.time + 0.5f; lastMoved = Time.realtimeSinceStartup; }
                if (Time.realtimeSinceStartup > nextCast) { nextCast = Time.realtimeSinceStartup + 5f; PressTurret(); }
                if (TurretOf(me) != null) lastTurretSpot = TurretOf(me).Feet;
                yield return null;
            }
            Tu("left with a turret standing at " + lastTurretSpot + ": hero at " + hero.Position);
            InputAdapter.BotHeld = k => false;
            turretWalk = false;
            yield return new WaitForSeconds(2f);
            Tu((MapRegistry.Current.Id != map && Zone.Main.Turrets.Count == 0 ? "PASS" : "FAIL") + " on map " + MapRegistry.Current.Id + " (was " + map + "), after " + (Time.realtimeSinceStartup - leaving).ToString("F1") + " s, this client knows " + Zone.Main.Turrets.Count + " turret(s)");
            yield return TurretShot("caster-other-map");
            yield return new WaitForSeconds(4f);
            Application.Quit();
        }

        IEnumerator TurretWatcher()
        {
            HeroController hero = GameSession.Hero;
            string spotFile = Path.Combine(shotPath, "turret-spot.txt");
            DeployedTurret seen = null;
            bool lateNoted = false;
            int shots = DeployedTurret.ShotsDrawn, pictures = 0, seenCount = 0, most = 0;
            float started = Time.realtimeSinceStartup, lastLeft = 0f, nearShot = 0f;
            Vector2? fileSpot = null;
            while (Time.realtimeSinceStartup - started < 160f && GameSession.Hero != null)
            {
                DeployedTurret t = TurretOf(0);
                Zone z = Zone.Main;
                if (z != null) most = Mathf.Max(most, z.Turrets.Count);
                if (fileSpot == null && File.Exists(spotFile))
                {
                    try
                    {
                        string[] p = File.ReadAllText(spotFile).Split(' ');
                        fileSpot = new Vector2(float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture));
                    }
                    catch (Exception) { }
                }
                if (t != seen)
                {
                    if (seen != null || (object)seen != null) Tu("watcher: turret gone, it had " + lastLeft.ToString("F0") + " ms of life left when last seen, on the screen of a warrior " + hero.Position);
                    if ((object)seen != null) { yield return new WaitForSeconds(0.08f); yield return TurretShot("watcher-gone-" + seenCount); }
                    seen = t;
                    if (t != null)
                    {
                        seenCount++;
                        Tu("watcher: turret " + t.NetId + " seen at " + t.Feet + " with " + t.RemainingMs + " ms left, owner hero " + t.Owner + (t.RemainingMs < TurretConfig.DurationMs - 1000 ? " (arrived after it was placed)" : ""));
                        if (!lateNoted && t.RemainingMs < TurretConfig.DurationMs - 1000) { lateNoted = true; Tu("PASS a late arrival got a turret that was already standing"); }
                    }
                }
                if (t != null) lastLeft = t.RemainingMs;
                Vector2? go = t != null ? t.Feet : fileSpot;
                if (go.HasValue && Vector2.Distance(hero.Position, go.Value) > 45f) { turretWalkTo = go.Value; turretWalk = true; }
                else turretWalk = false;
                if (t != null && Vector2.Distance(hero.Position, t.Feet) <= 60f)
                {
                    if (Time.realtimeSinceStartup - nearShot > 5f && pictures < 6)
                    {
                        nearShot = Time.realtimeSinceStartup;
                        yield return TurretShot("watcher-seen-" + seenCount + "-" + pictures);
                    }
                    if (DeployedTurret.ShotsDrawn != shots)
                    {
                        shots = DeployedTurret.ShotsDrawn;
                        if (pictures < 8) { pictures++; Tu("watcher: shot drawn, turret faces " + (t.FacesRight ? "right" : "left")); yield return new WaitForSeconds(0.06f); yield return TurretShot("watcher-shot-" + pictures); }
                    }
                }
                shots = DeployedTurret.ShotsDrawn;
                yield return null;
            }
            Tu("watcher: end, saw " + seenCount + " turrets, most at once " + most + ", shots drawn " + DeployedTurret.ShotsDrawn);
            Application.Quit();
        }
    }
}
