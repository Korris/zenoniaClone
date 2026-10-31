using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Runs the skill objects of one server zone (SkillRun, SkillBolt, SkillGroundZone, SkillMinion, SkillMotion ...) with that zone current. Those classes read Monster.All and
    /// MapRegistry.Current in their Update, and Unity calls Update in no zone order, so the Unity call is turned off (enabled = false, nothing else about the object changes) and this
    /// pump calls the same method once per tick. Heroes, monsters, drops and spawners select their own zone at the top of their Update and are left alone. Only a multi-zone
    /// server has pumps: offline, host and the bench stay on plain Unity updates.
    /// </summary>
    public sealed class ZoneBehaviourPump
    {
        struct Entry { public MonoBehaviour Body; public Action Tick; }

        const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly Dictionary<Type, MethodInfo> UpdateOf = new Dictionary<Type, MethodInfo>();
        static readonly Dictionary<Type, FieldInfo> OwnerFieldOf = new Dictionary<Type, FieldInfo>();

        readonly Transform zoneRoot, worldRoot;
        readonly List<Entry> live = new List<Entry>(32);
        readonly List<MonoBehaviour> scan = new List<MonoBehaviour>(256);

        public ZoneBehaviourPump(Transform zoneRoot, Transform worldRoot) { this.zoneRoot = zoneRoot; this.worldRoot = worldRoot; }

        /// <summary>Once per tick with this zone current: runs the adopted objects, then adopts whatever they spawned.</summary>
        public void Tick()
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                Entry e = live[i];
                if (e.Body == null) { live.RemoveAt(i); continue; }
                try { e.Tick(); }
                catch (Exception ex) { Debug.LogException(ex); } // like Unity: one failing object must not stop the others
            }
            if (live.Count > 0) Adopt();
        }

        /// <summary>Destroys the skill objects `hero` left behind: they would hit this zone's monsters from wherever the hero stands now.</summary>
        public void DestroyOwnedBy(HeroController hero)
        {
            Adopt();
            for (int i = live.Count - 1; i >= 0; i--)
            {
                Entry e = live[i];
                if (e.Body == null) { live.RemoveAt(i); continue; }
                if (OwnerOf(e.Body) != hero) continue;
                UnityEngine.Object.Destroy(e.Body.gameObject);
                live.RemoveAt(i);
            }
        }

        /// <summary>
        /// Takes over every skill object under the zone root that Unity still updates; a hero's cast calls this at once, so Unity never runs what the cast created outside the zone.
        /// The buff ticker serves all zones, so it moves out of the zone instead.
        /// </summary>
        public void Adopt()
        {
            zoneRoot.GetComponentsInChildren(false, scan);
            for (int i = 0; i < scan.Count; i++)
            {
                MonoBehaviour b = scan[i];
                if (b is HeroController || b is Monster || b is LootDrop || b is MonsterSpawner) continue;
                if (b is HeroBuffs) { b.transform.SetParent(worldRoot, false); continue; }
                if (!b.enabled) continue;
                Type t = b.GetType();
                MethodInfo update = UpdateMethod(t);
                if (update == null) continue;
                b.enabled = false;
                live.Add(new Entry { Body = b, Tick = (Action)Delegate.CreateDelegate(typeof(Action), b, update) });
                MethodInfo start = t.GetMethod("Start", AnyInstance, null, Type.EmptyTypes, null); // Unity skips Start on a disabled component
                if (start != null) start.Invoke(b, null);
            }
        }

        static MethodInfo UpdateMethod(Type t)
        {
            MethodInfo m;
            if (!UpdateOf.TryGetValue(t, out m)) UpdateOf[t] = m = t.GetMethod("Update", AnyInstance, null, Type.EmptyTypes, null);
            return m;
        }

        /// <summary>The caster behind a skill object: its SkillContext's body, or the ISkillBody it moves. Null when the type carries neither.</summary>
        static HeroController OwnerOf(MonoBehaviour b)
        {
            Type t = b.GetType();
            FieldInfo f;
            if (!OwnerFieldOf.TryGetValue(t, out f))
            {
                foreach (FieldInfo c in t.GetFields(AnyInstance))
                    if (c.FieldType == typeof(SkillContext) || c.FieldType == typeof(ISkillBody)) { f = c; break; }
                OwnerFieldOf[t] = f;
            }
            if (f == null) return null;
            object v = f.GetValue(b);
            SkillContext ctx = v as SkillContext;
            ISkillBody body = ctx != null ? ctx.Body : v as ISkillBody;
            return body != null ? body.Hero : null;
        }
    }
}
