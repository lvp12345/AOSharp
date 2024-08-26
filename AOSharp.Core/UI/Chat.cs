using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.GameData;
using System.Linq;
using SmokeLounge.AOtomation.Messaging.Messages;
using AOSharp.Common.SharedEventArgs;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.Messages.ChatMessages;

namespace AOSharp.Core.UI
{
    public static class Chat
    {
        private static Dictionary<string, Action<string, string[], ChatWindow>> _customCommands = new Dictionary<string, Action<string, string[], ChatWindow>>();
        private static ConcurrentQueue<(string, ChatColor)> _messageQueue = new ConcurrentQueue<(string, ChatColor)>();
        public static EventHandler<GroupMessageEventArgs> GroupMessageReceived;
        public static Action<string> FeedbackReceived;

        public static Action<PrivateGroupMessage> PrivateGroupMessageReceived;
        public static Action<PrivateGroupInviteMessage> PrivateGroupInviteReceived;

        public static void RegisterCommand(string command, Action<string, string[], ChatWindow> callback)
        {
            if(!_customCommands.ContainsKey(command))
                _customCommands.Add(command, callback);
        }

        internal static void Update()
        {
            try
            {
                while (_messageQueue.TryDequeue(out (string text, ChatColor color) msg))
                    GamecodeUnk.AppendSystemText(0, msg.text, msg.color);
            }
            catch (Exception e)
            {
                WriteLine($"This shouldn't happen pls report (Chat): {e.Message}");
            }
}

        private static void OnUnknownCommand(IntPtr pWindow, string command)
        {
            ChatWindow chatWindow = new ChatWindow(pWindow);
            string[] commandParts = command.Remove(0, 1).Trim().Split(' ');

            if (_customCommands.ContainsKey(commandParts[0]))
                _customCommands[commandParts[0]]?.Invoke(commandParts[0], commandParts.Skip(1).ToArray(), chatWindow);
            else
                chatWindow.WriteLine($"No chat command or script named \"{commandParts[0]}\" available.", ChatColor.LightBlue);
        }

        private static void OnGroupMessage(GroupMessageEventArgs args)
        {
            GroupMessageReceived?.Invoke(null, args);
        }

        internal static void OnFormatFeedback(FormatFeedbackMessage feedbackMsg)
        {
            FeedbackReceived?.Invoke(feedbackMsg.FormattedMessage);
        }

        public static void WriteLine(object obj, ChatColor color = ChatColor.Gold)
        {
            WriteLine((obj == null) ? "null" : obj.ToString(), color);
        }

        public static void WriteLine(string text, ChatColor color = ChatColor.Gold)
        {
            _messageQueue.Enqueue((text, color));
        }

        public static void InvitePrivateGroup(uint recipient)
        {
            Network.Send(new PrivateGroupInviteMessage
            {
                Sender = recipient
            });
        }

        public static void AcceptPrivateGroupInvite(uint recipient)
        {
            Network.Send(new PrivateGroupInviteAcceptMessage
            {
                Sender = recipient
            });
        }

        public static void SendPrivateGroupMessage(uint channelId, string msg)
        {
            Network.Send(new PrivateGroupMessage
            {
                ChannelId = channelId,
                Sender = (uint)Game.ClientInst,
                Text = msg,
            });
        }

        public static void SendPrivateMessage(uint recipient, string message)
        {
            Network.Send(new PrivateMsgMessage()
            {
                Sender = recipient,
                Text = message,
                Unk1 = 0x0001,
            });

            Chat.WriteLine($"To [{recipient}]: {message}", ChatColor.LightBlue);
        }

        public static void SendVicinityMessage(string text, VicinityMessageType messageType = VicinityMessageType.Vicinity)
        {
            //Tbh I don't really understand the difference here but w/e we get our result.
            TextMessageType type = messageType == VicinityMessageType.Vicinity || messageType == VicinityMessageType.Me ? 
                TextMessageType.Say : messageType == VicinityMessageType.Shout ? TextMessageType.Shout : TextMessageType.Whisper;


            Network.Send(new TextMessage()
            {
                TextMessageType = type,
                Text = text,
                Range = (TextMessageRange)messageType
            });
        }

        internal static void OnChatMessage(ChatMessageBody chatMsg)
        {
            if (chatMsg is PrivateGroupMessage privGroupMsg)
                PrivateGroupMessageReceived?.Invoke(privGroupMsg);
            else if (chatMsg is PrivateGroupInviteMessage privGroupInvMsg)
                PrivateGroupInviteReceived?.Invoke(privGroupInvMsg);
        }
    }

    public enum VicinityMessageType
    {
        Vicinity = 0,
        Whisper = 1,
        Shout = 2,
        Me = 3
    }
}