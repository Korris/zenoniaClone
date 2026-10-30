using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    public enum ChatChannel { World, Map, Party, Whisper, System }

    public struct ChatMessage
    {
        public ChatChannel Channel;
        public string Sender;   // null / empty for system lines
        public string Text;
        public float Time;      // Time.unscaledTime when received (collapsed chat fades old lines)
    }

    /// <summary>
    /// The seam between the chat window and the network: the window only calls ChatHub.Send and reads ChatHub.History. A real transport
    /// implements this interface, forwards Send to the server and calls ChatHub.Receive for every incoming line.
    /// </summary>
    public interface IChatTransport
    {
        /// <summary>Sends a line the local player typed on `channel`.</summary>
        void Send(ChatChannel channel, string text);
    }

    /// <summary>Local-only transport: the player's own line comes straight back (what the server would echo).</summary>
    public sealed class LocalChatTransport : IChatTransport
    {
        public void Send(ChatChannel channel, string text) { ChatHub.Receive(channel, ChatHub.LocalName(), text); }
    }

    /// <summary>Chat history (last 40 lines) + the active transport. System lines (quest accepted ...) go to the World tab without a sender.</summary>
    public static class ChatHub
    {
        public const int MaxLines = 40, MaxLength = 120;
        public static IChatTransport Transport = new LocalChatTransport();
        public static readonly List<ChatMessage> History = new List<ChatMessage>();
        /// <summary>Raised for every new line (the window refreshes).</summary>
        public static event Action<ChatMessage> Received;

        public static void ResetState() { History.Clear(); Transport = new LocalChatTransport(); Received = null; }

        public static string LocalName()
        {
            HeroController h = GameSession.Hero;
            return h != null ? h.Class.Name : "You";
        }

        /// <summary>Called by the chat window on Send: trims, clamps and hands the line to the transport.</summary>
        public static void Send(ChatChannel channel, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            text = text.Trim();
            if (text.Length > MaxLength) text = text.Substring(0, MaxLength);
            Transport.Send(channel, text);
        }

        /// <summary>A line arrived (from the local echo or, later, the network).</summary>
        public static void Receive(ChatChannel channel, string sender, string text)
        {
            var m = new ChatMessage { Channel = channel, Sender = sender, Text = text, Time = Time.unscaledTime };
            History.Add(m);
            if (History.Count > MaxLines) History.RemoveAt(0);
            if (Received != null) Received(m);
        }

        public static void System(string text) { Receive(ChatChannel.System, null, text); }
    }
}
