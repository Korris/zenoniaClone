// Chat input for the WebGL build: a real HTML <input> laid over the Unity chat field, so the browser's own text input
// (Vietnamese Telex / VNI via Unikey or EVKey, IME, paste, emoji) works. C# side: Assets/Scripts/WebChatInput.cs.
//
// Keys: Unity listens to the keyboard on the page and calls preventDefault on every key it handles, which blocked the ASCII letters
// from ever reaching the input (only IME / Unikey text got in) and let the game see them. So a capture listener on `window`,
// registered when the framework loads (before Unity registers its own), takes every key event aimed at the open chat input:
// it handles Enter / Escape / Tab itself and stops the event there, so Unity never sees it and the browser types the character.
// Results go back by SendMessage(<object>, OnWebChatSubmit / OnWebChatCancel / OnWebChatTab); the text is URI-encoded
// (plain ASCII) because SendMessage cut multi-byte UTF-8 strings short.
var ZenChatLib = {
  $ZenChat: {
    el: null,
    isOpen: function () { return ZenChat.el !== null && ZenChat.el.getAttribute('data-open') === '1'; },
    reply: function (method, value) {
      var target = ZenChat.el.getAttribute('data-obj');
      if (typeof SendMessage === 'function') SendMessage(target, method, value);
      else if (Module.SendMessage) Module.SendMessage(target, method, value);
    },
    // hide the input and give the keyboard back to the game canvas (else the next Enter goes nowhere)
    hide: function () {
      var el = ZenChat.el;
      if (!el) return;
      el.setAttribute('data-open', '');
      el.value = '';
      el.style.display = 'none';
      el.blur();
      if (Module.canvas) Module.canvas.focus();
    },
    onKey: function (e) {
      if (!ZenChat.isOpen() || e.target !== ZenChat.el) return;
      e.stopImmediatePropagation(); // Unity's listeners never run: the game ignores the key, the browser still types it
      if (e.type !== 'keydown') return; // keyup is not taken: Unity must see the Enter that opened the chat go up again
      if (e.key === 'Enter' && !e.isComposing) {
        e.preventDefault();
        var v = ZenChat.el.value;
        ZenChat.hide();
        ZenChat.reply('OnWebChatSubmit', encodeURIComponent(v));
      } else if (e.key === 'Escape') {
        e.preventDefault();
        ZenChat.hide();
        ZenChat.reply('OnWebChatCancel', '');
      } else if (e.key === 'Tab') {
        e.preventDefault();
        ZenChat.reply('OnWebChatTab', '');
      }
    },
    init: function () {
      if (typeof window === 'undefined' || window.__zenChatKeys) return;
      window.__zenChatKeys = true;
      ['keydown', 'keypress'].forEach(function (t) { window.addEventListener(t, ZenChat.onKey, true); });
    },
    create: function () {
      var el = document.createElement('input');
      el.id = 'zen-chat-input';
      el.type = 'text';
      el.maxLength = 120;
      el.autocomplete = 'off';
      el.spellcheck = false;
      el.style.cssText = 'position:fixed;z-index:20;box-sizing:border-box;border:0;outline:0;margin:0;padding:0 12px;' +
        'background:rgba(8,14,28,0.92);color:#fff;border-radius:6px;font-family:Arial,Helvetica,sans-serif;display:none;';
      document.body.appendChild(el);
      el.addEventListener('blur', function () { // clicked away: same as Escape
        if (!ZenChat.isOpen()) return;
        ZenChat.hide();
        ZenChat.reply('OnWebChatCancel', '');
      });
      ZenChat.el = el;
    }
  },
  $ZenChat__postset: 'ZenChat.init();',

  ZenChatOpen: function (objPtr, placeholderPtr, x, y, w, h, fontPx) {
    ZenChat.init();
    if (!ZenChat.el) ZenChat.create();
    var el = ZenChat.el;
    // Unity screen pixels (origin top-left here) -> CSS pixels of the page
    var canvas = Module.canvas, r = canvas.getBoundingClientRect();
    var sx = r.width / canvas.width, sy = r.height / canvas.height;
    el.setAttribute('data-obj', UTF8ToString(objPtr));
    el.placeholder = UTF8ToString(placeholderPtr);
    el.style.left = (r.left + x * sx) + 'px';
    el.style.top = (r.top + y * sy) + 'px';
    el.style.width = (w * sx) + 'px';
    el.style.height = (h * sy) + 'px';
    el.style.fontSize = Math.max(12, fontPx * sy) + 'px';
    el.style.display = 'block';
    el.setAttribute('data-open', '1');
    setTimeout(function () { if (ZenChat.isOpen()) el.focus(); }, 0);
  },

  ZenChatClose: function () {
    if (ZenChat.isOpen()) ZenChat.hide();
  }
};
autoAddDeps(ZenChatLib, '$ZenChat');
mergeInto(LibraryManager.library, ZenChatLib);
