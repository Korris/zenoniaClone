using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>Painted row of a skill's art (same indices as SkillVfxSet); None = no sprite in this phase.</summary>
    public enum ChoreoRow { None = -1, Cast = 0, Loop = 1, Impact = 2 }

    /// <summary>Where a phase is drawn: at the aim point, on the caster (follows it), flying caster to aim point, or dropping from the sky onto the aim point.</summary>
    public enum ChoreoAnchor { Target, Caster, Travel, Sky }

    public enum ChoreoParticle { Embers, Debris, Dust }

    public struct ChoreoBurst { public ChoreoParticle Kind; public int Count; public uint Color; }

    /// <summary>Expanding shock ring: radius in tiles, life in ms, 0xRRGGBB.</summary>
    public struct ChoreoRing { public float Radius; public int Ms; public uint Color; }

    /// <summary>
    /// One phase of a skill choreography (engine-free data; times in ms from cast start, sizes in tiles of 16 units, colours 0xRRGGBB).
    /// Everything a phase owns starts at From and ends at To: the painted sprite row (frames F0..F1 spread over the phase, or cycled when Loop),
    /// an additive glow copy, ground telegraph / decal, particle bursts (or a stream over the phase), rings, flash, hit-stop, shake, punch-zoom, sound cue.
    /// </summary>
    public sealed class ChoreoPhase
    {
        public int From, To;
        public ChoreoAnchor At = ChoreoAnchor.Target;
        public float OffX, OffY, SkyTiles = 7f;          // offset from the anchor (tiles, y up); starting height of a Sky drop
        public ChoreoRow Row = ChoreoRow.None; public int F0, F1; public float Width = 2f; public bool Loop, PerHit; public float SpriteAlpha = 1f;   // normal-blend sprite opacity (low for bubbles / ground blasts that overlap the caster)
        public uint GlowColor; public float GlowAlpha;
        public uint TeleColor; public float TeleR0, TeleR1; public bool TeleLine;   // line: length TeleR1 tiles, 1 tile wide, along the aim
        public uint DecalColor; public float DecalR0, DecalR1, DecalAlpha = 0.4f;
        public List<ChoreoBurst> Bursts = new List<ChoreoBurst>(); public bool Stream;
        public List<ChoreoRing> Rings = new List<ChoreoRing>();
        public int HitStopMs, ShakeMs, FlashMs, PunchMs; public float Shake, Punch; public uint FlashColor; public string Sfx;

        public ChoreoPhase(int from, int to) { From = from; To = to; }

        public ChoreoPhase Spr(ChoreoRow row, int f0, int f1, float widthTiles) { Row = row; F0 = f0; F1 = f1; Width = widthTiles; return this; }
        public ChoreoPhase Anchor(ChoreoAnchor a) { At = a; return this; }
        public ChoreoPhase Off(float x, float y) { OffX = x; OffY = y; return this; }
        public ChoreoPhase Alpha(float a) { SpriteAlpha = a; return this; }
        public ChoreoPhase Cycle() { Loop = true; return this; }
        public ChoreoPhase Hits() { PerHit = true; return this; }
        public ChoreoPhase Glow(uint rgb, float alpha) { GlowColor = rgb; GlowAlpha = alpha; return this; }
        public ChoreoPhase Tele(uint rgb, float r0, float r1) { TeleColor = rgb; TeleR0 = r0; TeleR1 = r1; return this; }
        public ChoreoPhase Line(uint rgb, float length) { TeleColor = rgb; TeleR0 = TeleR1 = length; TeleLine = true; return this; }
        public ChoreoPhase Decal(uint rgb, float r0, float r1, float alpha = 0.4f) { DecalColor = rgb; DecalR0 = r0; DecalR1 = r1; DecalAlpha = alpha; return this; }
        public ChoreoPhase Embers(int n, uint rgb) { return Burst(ChoreoParticle.Embers, n, rgb); }
        public ChoreoPhase Debris(int n) { return Burst(ChoreoParticle.Debris, n, 0); }
        public ChoreoPhase Dust(int n, uint rgb = 0) { return Burst(ChoreoParticle.Dust, n, rgb); }
        public ChoreoPhase Trail() { Stream = true; return this; }   // particles spread over the phase instead of one burst at From
        public ChoreoPhase Ring(float radiusTiles, int ms, uint rgb) { Rings.Add(new ChoreoRing { Radius = radiusTiles, Ms = ms, Color = rgb }); return this; }
        public ChoreoPhase HitStop(int ms) { HitStopMs = ms; return this; }
        public ChoreoPhase Quake(int ms, float amount) { ShakeMs = ms; Shake = amount; return this; }
        public ChoreoPhase Flare(uint rgb, int ms) { FlashColor = rgb; FlashMs = ms; return this; }
        public ChoreoPhase Zoom(float amount, int ms) { Punch = amount; PunchMs = ms; return this; }
        public ChoreoPhase Sound(string cue) { Sfx = cue; return this; }

        ChoreoPhase Burst(ChoreoParticle kind, int n, uint rgb) { Bursts.Add(new ChoreoBurst { Kind = kind, Count = n, Color = rgb }); return this; }
    }

    /// <summary>Full choreography of one skill. Projectile skills fly TravelTiles at max range (the timeline is shifted earlier when the hit is closer). Sustain skills end through OnBreak / OnExpire.</summary>
    public sealed class Choreo
    {
        public string ClassId, SkillId;
        public int TotalMs;
        public float TravelTiles;                        // > 0 = projectile: phases anchored Travel fly this far
        public List<ChoreoPhase> Phases = new List<ChoreoPhase>(), OnBreak = new List<ChoreoPhase>(), OnExpire = new List<ChoreoPhase>();

        public Choreo(string classId, string skillId, int totalMs, float travelTiles = 0f) { ClassId = classId; SkillId = skillId; TotalMs = totalMs; TravelTiles = travelTiles; }

        public ChoreoPhase Add(int from, int to) { var p = new ChoreoPhase(from, to); Phases.Add(p); return p; }
        public ChoreoPhase Break(int from, int to) { var p = new ChoreoPhase(from, to); OnBreak.Add(p); return p; }
        public ChoreoPhase Expire(int from, int to) { var p = new ChoreoPhase(from, to); OnExpire.Add(p); return p; }
    }
}
