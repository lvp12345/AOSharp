using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Core.Inventory;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using AOSharp.Core.UI;
using AOSharp.Common.Helpers;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.ChatMessages;

namespace AOSharp.Core
{
    public static class Network
    {
        public static EventHandler<byte[]> PacketReceived;
        public static EventHandler<N3Message> N3MessageReceived;
        public static Action<SystemMessage> SystemMessageReceived;
        public static Action<PingMessage> PingMessageReceived;
        public static EventHandler<byte[]> PacketSent;
        public static EventHandler<N3Message> N3MessageSent;
        public static EventHandler<ChatMessageBody> ChatMessageReceived;

        private static ConcurrentQueue<byte[]> _rawInboundPacketQueue = new ConcurrentQueue<byte[]>();
        private static ConcurrentQueue<Message> _inboundMessageQueue = new ConcurrentQueue<Message>();
        private static ConcurrentQueue<byte[]> _rawOutboundPacketQueue = new ConcurrentQueue<byte[]>();
        private static ConcurrentQueue<Message> _outboundMessageQueue = new ConcurrentQueue<Message>();
        private static ConcurrentQueue<ChatMessage> _inboundChatMessageQueue = new ConcurrentQueue<ChatMessage>();

        private static Dictionary<N3MessageType, Action<N3Message>> n3MsgCallbacks = new Dictionary<N3MessageType, Action<N3Message>>
        {
            { N3MessageType.KnubotOpenChatWindow, NpcDialog.OnKnubotOpenChatWindow },
            { N3MessageType.KnubotAnswerList, NpcDialog.OnKnubotAnswerList },
            { N3MessageType.CharacterAction, OnCharacterAction },
            { N3MessageType.TemplateAction, OnTemplateAction },
            { N3MessageType.GenericCmd, OnGenericCmd },
            { N3MessageType.CharInPlay, OnCharInPlay },
            { N3MessageType.QuestAlternative, OnQuestAlternative },
            { N3MessageType.AOTransportSignal, OnAoTransportSignal },
            { N3MessageType.InfromPlayer, OnInfromPlayer },
            { N3MessageType.FormatFeedback, OnFormatFeedback},
            { N3MessageType.Trade, Trade.OnTradeMessage },
            { N3MessageType.Inspect, OnInspect},
            { N3MessageType.SendScore, OnSendScore },
            { N3MessageType.OrgServer, OnOrgServer }
        };

        public static void Send(MessageBody message)
        {
            byte[] packet = PacketFactory.Create(message);

            if (packet == null)
                return;

            Send(packet);
        }

        public static void Send(ChatMessageBody message)
        {
            byte[] packet = ChatPacketFactory.Create(message);

            if (packet == null)
                return;

            SendChat(packet);
        }

        public static unsafe void Send(byte[] payload)
        {
            IntPtr pClient = Client_t.GetInstanceIfAny();

            if (pClient == IntPtr.Zero)
                return;

            IntPtr pConnection = *(IntPtr*)(pClient + 0x84);

            if (pConnection == IntPtr.Zero)
                return;

            Connection_t.Send(pConnection, 0, payload.Length, payload);
        }

        public static unsafe void SendChat(byte[] payload)
        {
            IntPtr pChatServerInterface = *(IntPtr*)(Kernel32.GetProcAddress(Kernel32.GetModuleHandle("GUI.dll"), "?s_pcInstance@ChatGUIModule_c@@0PAV1@A") + 0x18);

            if (pChatServerInterface == IntPtr.Zero)
                return;
            
            IntPtr pChatServerUnk = *(IntPtr*)(pChatServerInterface + 0x30);

            if (pChatServerUnk == IntPtr.Zero)
                return;

            int chatSocket = *(int*)(pChatServerUnk + 0x78);

            Ws2_32.send(chatSocket, Marshal.UnsafeAddrOfPinnedArrayElement(payload, 0), payload.Length, 0);
        }

        public static void ProcessMessage(MessageBody message)
        {
            byte[] packet = PacketFactory.Create(message);

            if (packet == null)
                return;

            ProcessMessage(packet);
        }

        public static void ProcessMessage(byte[] dataBlock)
        {
            IntPtr pMessage = MessageProtocol.DataBlockToMessage((uint)dataBlock.Length, dataBlock);
            N3InterfaceModule_t.ProcessMessage(N3InterfaceModule_t.GetInstance(), pMessage);
        }

        internal static void Update()
        {
            try
            {
                while (_rawInboundPacketQueue.TryDequeue(out byte[] packet))
                    PacketReceived?.Invoke(null, packet);

                while (_inboundMessageQueue.TryDequeue(out Message msg))
                    if (msg.Header.PacketType == PacketType.N3Message)
                        OnInboundN3Message((N3Message)msg.Body);
                    else if (msg.Header.PacketType == PacketType.SystemMessage)
                        SystemMessageReceived?.Invoke((SystemMessage)msg.Body);
                    else if (msg.Header.PacketType == PacketType.PingMessage)
                        PingMessageReceived?.Invoke((PingMessage)msg.Body);

                while (_rawOutboundPacketQueue.TryDequeue(out byte[] packet))
                    PacketSent?.Invoke(null, packet);

                while (_outboundMessageQueue.TryDequeue(out Message msg))
                    if (msg.Header.PacketType == PacketType.N3Message)
                        OnOutboundN3Message((N3Message)msg.Body);

                while (_inboundChatMessageQueue.TryDequeue(out ChatMessage msg))
                {
                    Chat.OnChatMessage(msg.Body);
                    ChatMessageReceived?.Invoke(null, msg.Body);

                    if (msg.Header.PacketType == ChatMessageType.LftQueryResponse)
                        LookingForTeam.OnLftQueryResponse((LftQueryResponseMessage)msg.Body);
                }
            }
            catch (Exception e)
            {
                //Chat.WriteLine($"This shouldn't happen pls report (Network): {e.Message}");
            }
        }

        /*
        private static void OnTrade(N3Message n3Msg)
        {
            Trade.OnTradeMessage((TradeMessage)n3Msg);
        }
        */

        private static void OnChatMessage(byte[] packet)
        {
            ChatMessage msg = ChatPacketFactory.Disassemble(packet);

            if (msg == null)
                return;

            _inboundChatMessageQueue.Enqueue(msg);
        }

        private static void OnInboundMessage(byte[] datablock)
        {
            _rawInboundPacketQueue.Enqueue(datablock);

            Message msg = PacketFactory.Disassemble(datablock);

            //Chat.WriteLine(BitConverter.ToString(datablock).Replace("-", ""));

            if (msg == null)
                return;

            _inboundMessageQueue.Enqueue(msg);
        }

        private static void OnOutboundMessage(byte[] datablock)
        {
            _rawOutboundPacketQueue.Enqueue(datablock);

            Message msg = PacketFactory.Disassemble(datablock);

            //Chat.WriteLine(BitConverter.ToString(datablock).Replace("-", ""));

            if (msg == null)
                return;

            _outboundMessageQueue.Enqueue(msg);
        }

        private static void OnInboundN3Message(N3Message n3Msg)
        {
            if (n3MsgCallbacks.ContainsKey(n3Msg.N3MessageType))
                n3MsgCallbacks[n3Msg.N3MessageType].Invoke(n3Msg);

            N3MessageReceived?.Invoke(null, n3Msg);
        }

        private static void OnOutboundN3Message(N3Message n3Msg)
        {
            N3MessageSent?.Invoke(null, n3Msg);
        }

        private static void OnCharInPlay(N3Message n3Msg)
        {
            DynelManager.OnCharInPlay(n3Msg.Identity);
        }

        private static void OnGenericCmd(N3Message n3Msg)
        {
            GenericCmdMessage genericCmdMessage = (GenericCmdMessage)n3Msg;

            if (genericCmdMessage.User != DynelManager.LocalPlayer.Identity)
                return;

            switch (genericCmdMessage.Action)
            {
                case GenericCmdAction.Use:
                    Item.OnUsingItem(genericCmdMessage.Target);
                    break;
            }
        }

        private static void OnCharacterAction(N3Message n3Msg)
        {
            CharacterActionMessage charActionMessage = (CharacterActionMessage)n3Msg;

            switch (charActionMessage.Action)
            {
                case CharacterActionType.LeaveTeam:
                    Team.OnMemberLeft(charActionMessage.Target);
                    break;
                case CharacterActionType.QueuePerk:
                    PerkAction.OnPerkQueued();
                    break;
                case CharacterActionType.DuelUpdate:
                    Duel.OnDuelUpdate(charActionMessage.Target, (DuelUpdate)charActionMessage.Parameter1);
                    break;
                case CharacterActionType.SpecialUsed:
                    LocalCooldown.OnSpecialUsed(n3Msg);
                    break;
                case CharacterActionType.SpecialAvailable:
                    LocalCooldown.OnSpecialAvailable(n3Msg);
                    break;
                case CharacterActionType.SpecialUnavailable:
                    LocalCooldown.OnSpecialUnavailable(n3Msg);
                    break;
                //case CharacterActionType.TeamKick:
                //    Team.OnMemberLeft(charActionMessage.Target);
                //    break;

                default:
                    //Chat.WriteLine($"UnhandledCharAction::{charActionMessage.Action} - {charActionMessage.Target} - {charActionMessage.Parameter1} - {charActionMessage.Parameter2} - {charActionMessage.Unknown1} - {charActionMessage.Unknown2}");
                    break;
            }
        }

        private static void OnTemplateAction(N3Message n3Msg)
        {
            TemplateActionMessage templateActionMessage = (TemplateActionMessage)n3Msg;

            switch (templateActionMessage.Unknown2)
            {
                case 3:
                    Item.OnItemUsed(templateActionMessage.ItemLowId, templateActionMessage.ItemHighId, templateActionMessage.Quality, templateActionMessage.Identity);
                    break;
                case 32:
                    PerkAction.OnPerkFinished(templateActionMessage.ItemLowId, templateActionMessage.ItemHighId, templateActionMessage.Quality, templateActionMessage.Identity);
                    break;
            }
        }

        private static void OnInfromPlayer(N3Message n3Msg)
        {
            InfromPlayerMessage infromPlayerMessage = (InfromPlayerMessage)n3Msg;

            if (infromPlayerMessage.UnkIdentity.Type == IdentityType.Battlestation)
                Battlestation.OnBattlestationInvite(infromPlayerMessage.UnkIdentity);
        }

        private static void OnSendScore(N3Message n3Msg)
        {
            SendScoreMessage sendScoreMessage = (SendScoreMessage)n3Msg;
            Battlestation.OnSendScore(sendScoreMessage);
        }

        private static void OnFormatFeedback(N3Message n3Msg)
        {
            Chat.OnFormatFeedback((FormatFeedbackMessage)n3Msg);
        }

        private static void OnQuestAlternative(N3Message n3Msg)
        {
            QuestAlternativeMessage qMsg = (QuestAlternativeMessage)n3Msg;

            Mission.OnRollListChanged(qMsg.MissionSliders, qMsg.MissionDetails);
        }

        private static void OnAoTransportSignal(N3Message n3Msg)
        {
            CityController.OnAOSignalTransportMessage((AOTransportSignalMessage)n3Msg);
        }

        private static void OnInspect(N3Message n3Msg)
        {
            InspectMessage iMsg = (InspectMessage)n3Msg;

            CharacterAction.OnInspected(iMsg.Target, iMsg.Slot);
        }

        private static void OnOrgServer(N3Message n3Msg)
        {
            OrgServerMessage osMsg = (OrgServerMessage)n3Msg;

            if (osMsg.IOrgServerMessage is OrganizationInfo orgInfo)
                Org.InfoReceived.Invoke(orgInfo);
        }
    }
}
