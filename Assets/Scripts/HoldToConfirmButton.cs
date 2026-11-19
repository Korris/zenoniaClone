using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// A button that fires only after being held down for Seconds (the second step of deleting a hero): letting go, sliding off or the button turning
    /// non-interactable starts the count again. Progress (0..1) is reported every frame while held so the label can count down.
    /// </summary>
    public sealed class HoldToConfirmButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float Seconds = 2f;
        public bool Interactable = true;
        public Action<float> OnProgress;
        public Action OnConfirmed;
        public Graphic Face;

        bool held, fired;
        float since;

        /// <summary>0 while nobody holds it, up to 1 when it fires.</summary>
        public float Progress { get { return held ? Mathf.Clamp01((Time.unscaledTime - since) / Seconds) : 0f; } }

        public void OnPointerDown(PointerEventData e)
        {
            if (!Interactable || e.button != PointerEventData.InputButton.Left) return;
            held = true; fired = false; since = Time.unscaledTime;
            if (Face != null) Face.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        }

        public void OnPointerUp(PointerEventData e) { Release(); }
        public void OnPointerExit(PointerEventData e) { Release(); }
        void OnDisable() { Release(); }

        void Release()
        {
            held = false;
            if (Face != null) Face.color = Interactable ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.75f);
            if (OnProgress != null) OnProgress(0f);
        }

        void Update()
        {
            if (!held) return;
            if (!Interactable) { Release(); return; }
            float p = Progress;
            if (OnProgress != null) OnProgress(p);
            if (p < 1f || fired) return;
            fired = true;
            held = false;
            if (Face != null) Face.color = Color.white;
            if (OnConfirmed != null) OnConfirmed();
        }
    }
}
