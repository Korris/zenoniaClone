using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Sun shadow of one standing unit (hero, monster, pet) in the 2.5D prototype: every figure sprite of the unit (body, gear, head, hair) is mirrored
    /// each frame into a soft black silhouette lying flat on the ground, rotated toward the sun's fall direction and squashed (View25D.SunShadow*).
    /// The unit's own round contact shadow stays under its feet. On the listed 2.5D maps (View25D.UsesFakeSun) no silhouette: the FakeSun blob is the shadow. Drawn at View25D.ShadowOrder: above the ground, below telegraphs and every unit.
    /// </summary>
    public sealed class View25DDropShadow
    {
        readonly Transform root, holder;
        readonly List<SpriteRenderer> src = new List<SpriteRenderer>(8), dst = new List<SpriteRenderer>(8);
        readonly Material mat;
        Transform roundShadow;
        int refreshIn;

        public Transform Root { get { return root; } }

        public View25DDropShadow(Transform unit, Material shadowMat)
        {
            root = unit;
            mat = shadowMat;
            holder = new GameObject("Shadow25D").transform;
            holder.SetParent(unit, false);
            roundShadow = unit.Find("Shadow");
        }

        /// <summary>Figure sprites: under "Lean" (heroes), "Body" (monsters) or the unit's own renderer (pets). Glow / additive layers cast nothing.</summary>
        void Gather()
        {
            refreshIn = 30;
            src.Clear();
            Transform figure = root.Find("Lean") ?? root.Find("Body");
            if (figure != null) figure.GetComponentsInChildren(true, src);
            else { var own = root.GetComponent<SpriteRenderer>(); if (own != null) src.Add(own); }
            for (int i = src.Count - 1; i >= 0; i--)
            {
                Material m = src[i].sharedMaterial;
                string sh = m != null && m.shader != null ? m.shader.name : "";
                if (sh.Contains("Additive") || sh.Contains("Aura") || sh.Contains("Shine") || sh.Contains("Light")) src.RemoveAt(i);
            }
            while (dst.Count < src.Count)
            {
                var go = new GameObject("S");
                go.transform.SetParent(holder, false);
                var r = go.AddComponent<SpriteRenderer>();
                r.sharedMaterial = mat;
                r.sortingOrder = View25D.ShadowOrder;
                dst.Add(r);
            }
            for (int i = src.Count; i < dst.Count; i++) dst[i].enabled = false;
        }

        /// <summary>Keeps the unit standing (billboard), its round shadow flat, and copies the current figure frame into the sun shadow.</summary>
        public void Tick(Quaternion tilt, Quaternion untilt, float scale)
        {
            if (root.rotation != tilt) root.rotation = tilt;
            if (scale > 0f) root.localScale = new Vector3(scale, scale, 1f); // perspective size compensation (View25D.UnitScale); 0 = leave (pets scale themselves)
            if (roundShadow != null) roundShadow.localRotation = untilt;
            if (View25D.UsesFakeSun) // listed 2.5D maps: the unit's FakeSun ground blob is its shadow, no silhouette copy
            {
                if (holder.gameObject.activeSelf) holder.gameObject.SetActive(false);
                return;
            }
            if (!holder.gameObject.activeSelf) holder.gameObject.SetActive(true);
            holder.localRotation = untilt * Quaternion.Euler(0f, 0f, View25D.SunShadowAngle);
            holder.localScale = new Vector3(1f, View25D.SunShadowLength, 1f);
            if (--refreshIn <= 0) Gather();
            Matrix4x4 toRoot = root.worldToLocalMatrix;
            for (int i = 0; i < src.Count; i++)
            {
                SpriteRenderer s = src[i], d = dst[i];
                if (s == null) { refreshIn = 0; d.enabled = false; continue; }
                bool show = s.enabled && s.sprite != null && s.gameObject.activeInHierarchy && s.color.a > 0.05f;
                d.enabled = show;
                if (!show) continue;
                d.sprite = s.sprite;
                d.flipX = s.flipX;
                d.flipY = s.flipY;
                Matrix4x4 m = toRoot * s.transform.localToWorldMatrix;
                Vector3 p = m.GetColumn(3);
                Transform t = d.transform;
                t.localPosition = new Vector3(p.x, p.y, 0f);
                t.localRotation = m.rotation;
                t.localScale = m.lossyScale;
                Color tint = View25D.ShadowTint;
                d.color = new Color(tint.r, tint.g, tint.b, View25D.SunShadowAlpha * s.color.a);
            }
        }

        /// <summary>Flag off / another map: the unit lies back on the x/y plane and the sun shadow goes away.</summary>
        public void Remove()
        {
            if (root != null)
            {
                root.rotation = Quaternion.identity;
                if (root.GetComponent<HeroPet>() == null) root.localScale = Vector3.one;
                if (roundShadow != null) roundShadow.localRotation = Quaternion.identity;
            }
            if (holder != null) Object.Destroy(holder.gameObject);
        }
    }
}
