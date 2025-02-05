using AOSharp.Common;
using AOSharp.Common.GameData;
using Serilog.Core;
using SmokeLounge.AOtomation.Messaging.Messages.SystemMessages;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Core;
using AOSharp.Bootstrap;
using System.Linq.Expressions;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOLite.Net
{
    public class SessionCookie
    {
        public uint Cookie1;
        public uint Cookie2;
    }

    public class NetworkSession
    {
        private NetworkStateMachine _stateMachine;
        private ZlibTcpClient _tcpClient;
        private MessageSerializer _serializer = new MessageSerializer();

        internal bool InPlay => _stateMachine.IsInState(State.InPlay);
        internal bool Connected => _tcpClient.Connected;
        internal bool Stopped = false;

        internal Action Disconnected;

        private SessionCookie _sessionCookie = null;
        private ushort _messageId = 1;

        private Logger _logger;

        private System.Timers.Timer _reconnectTimer;
        private System.Timers.Timer _pingTimer;

        private PluginProxy _pluginProxy;

        private Dictionary<SystemMessageType, Action<SystemMessage>> _sysMsgCallbacks;
        private Dictionary<N3MessageType, Action<N3Message, byte[]>> _n3MsgCallbacks;
        private Dictionary<SystemMessageType, Action<SystemMessage>> _internalSysMsgCallbacks;
        private Dictionary<N3MessageType, Action<N3Message>> _internalN3MsgCallbacks;

        private static ConcurrentQueue<byte[]> _inboundPacketQueue = new ConcurrentQueue<byte[]>();

        internal NetworkSession(Logger logger, PluginProxy pluginProxy, Dictionary<SystemMessageType, Action<SystemMessage>> sysMsgCallbacks, Dictionary<N3MessageType, Action<N3Message, byte[]>> n3MsgCallbacks)
        {
            _logger = logger;
            _pluginProxy = pluginProxy;
            InitializeStateMachine();
            RegisterInternalSystemMessageHandlers();
            RegisterInternalN3MessageHandlers();

            _sysMsgCallbacks = sysMsgCallbacks;
            _n3MsgCallbacks = n3MsgCallbacks;
        }

        internal void Update()
        {
            while (_inboundPacketQueue.TryDequeue(out byte[] packet))
                ProcessCachedPacket(packet);
        }

        public void Connect()
        {
            _logger.Debug($"Requesting dimension info..");

            try
            {
                DimensionInfo dimensionInfo = Client.Config.Dimension == Dimension.RubiKa ? DimensionInfo.RubiKa : DimensionInfo.RubiKa2019;
                IPEndPoint loginHandlerEndpoint = new IPEndPoint(Dns.GetHostEntry(dimensionInfo.GameServerEndpoint.Host).AddressList[0], dimensionInfo.GameServerEndpoint.Port);
                _stateMachine.Fire(_stateMachine.ConnectTrigger, loginHandlerEndpoint);
            }
            catch (WebException e)
            {
                _logger.Error($"Failed to retrieve dimension info. {e}");
                _stateMachine.Fire(Trigger.FailedToRetreiveDimensionInfo);
            }
        }

        private void Connect(IPEndPoint endpoint)
        {
            _logger.Debug($"Connecting to {endpoint}");

            if (_tcpClient != null && _tcpClient.Connected)
                _tcpClient.Close();

            _tcpClient = new ZlibTcpClient(_logger);
            _tcpClient.Disconnected += OnTcpClientDisconnected;
            _tcpClient.PacketRecv += (p) => _inboundPacketQueue.Enqueue(p);
            _tcpClient.BeginConnect(endpoint.Address, endpoint.Port, ConnectCallback, endpoint);
        }

        private void ConnectCallback(IAsyncResult result)
        {
            try
            {
                _tcpClient.EndConnect(result);
                _stateMachine.Fire(Trigger.OnTcpConnected);
            }
            catch (Exception e)
            {
                IPEndPoint endpoint = result.AsyncState as IPEndPoint;

                _logger.Debug($"Failed to connect to {endpoint}");

                _stateMachine.Fire(Trigger.OnTcpConnectError);
            }
        }

        public void OnTcpClientDisconnected()
        {
            if(InPlay)
                Disconnect();
        }

        public void Disconnect(bool stop = false)
        {
            Stopped = stop;
            _stateMachine.Fire(Trigger.Disconnect);
            Disconnected?.Invoke();
        }

        public void SendDatablock(byte[] datablock)
        {
            using (MemoryStream stream = new MemoryStream(datablock))
            {
                try
                {
                    Send(_serializer.DeserializeDatablock(stream));
                } 
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public void Send(MessageBody messageBody)
        {
            if (messageBody == null)
                return;

            if (messageBody is N3Message n3Message)
                n3Message.Identity = new Identity(IdentityType.SimpleChar, Client.LocalDynelId);

            Message message = new Message
            {
                Body = messageBody,
                Header = new Header
                {
                    PacketType = messageBody.PacketType,
                    Sender = Client.LocalDynelId,
                    Receiver = messageBody.PacketType == PacketType.SystemMessage ? 1 : 2
                }
            };

            Send(message);
        }

        public void Send(Message message)
        {
            message.Header.MessageId = _messageId;

            using (MemoryStream stream = new MemoryStream())
            {
                _serializer.Serialize(stream, message);
                _tcpClient.Send(stream.ToArray());
            }

            _messageId++;

            if (_messageId == 0xFFFF)
                _messageId = 1;
        }

        private void ProcessCachedPacket(byte[] packet)
        {
            try
            {
                Message message = _serializer.Deserialize(packet);

                if (message == null)
                    return;

                if (message.Header.PacketType == PacketType.InitiateCompressionMessage)
                {
                    OnInitiateCompressionMessage();
                }
                else if (message.Header.PacketType == PacketType.PingMessage)
                {
                    Pong(message);
                }
                else if (message.Header.PacketType == PacketType.SystemMessage)
                {
                    SystemMessage sysMsg = (SystemMessage)message.Body;

                    if (_sysMsgCallbacks.TryGetValue(sysMsg.SystemMessageType, out Action<SystemMessage> callback))
                        callback.Invoke(sysMsg);

                    if (_internalSysMsgCallbacks.TryGetValue(sysMsg.SystemMessageType, out Action<SystemMessage> internalCallback))
                        internalCallback.Invoke(sysMsg);
                }
                else if (message.Header.PacketType == PacketType.N3Message)
                {
                    _pluginProxy.DataBlockToMessage(packet);

                    N3Message n3Msg = (N3Message)message.Body;

                    try
                    {
                        if (_n3MsgCallbacks.TryGetValue(n3Msg.N3MessageType, out Action<N3Message, byte[]> callback))
                            callback.Invoke(n3Msg, message.RawPacket);
                        //else
                        //    _logger.Information($"Unregistered message: {n3Msg.N3MessageType}");

                        if (_internalN3MsgCallbacks.TryGetValue(n3Msg.N3MessageType, out Action<N3Message> internalCallback))
                            internalCallback.Invoke(n3Msg);
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Internal Message Handler Failed : {packet.ToHexString()}");
                        _logger.Error(e.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                //HACK!! Our FullCharacter deserializer does not work in all cases so we will just pass an empty FullCharacterMessage back to AOLite's client.
                if ((N3MessageType)((packet[16] << 24) + (packet[17] << 16) + (packet[18] << 8) + packet[19]) == N3MessageType.FullCharacter)
                {
                    FullCharacterMessage emptyFullCharacterMsg = new FullCharacterMessage();
                    _n3MsgCallbacks[N3MessageType.FullCharacter].Invoke(emptyFullCharacterMsg, packet);
                    _internalN3MsgCallbacks[N3MessageType.FullCharacter].Invoke(emptyFullCharacterMsg);
                    return;
                }

                if (!Client.LogDeserializationErrors)
                    return;

                _logger.Error($"Failed to deserialize packet: {packet.ToHexString()}");
                _logger.Error(e.ToString());
            }
        }

        private void Reconnect()
        {
            _tcpClient.Close();
            _sessionCookie = null;
            _messageId = 1;

            if (!Stopped && Client.Config.AutoReconnect)
                Task.Delay(Client.Config.ReconnectDelay).ContinueWith(t => Connect());
            else
                _stateMachine.Fire(Trigger.Stop);
        }

        private void InitializeStateMachine()
        {
            _stateMachine = new NetworkStateMachine(_logger);

            _stateMachine.Configure(State.Idle)
                .Permit(Trigger.Connect, State.Connecting);

            _stateMachine.Configure(State.Disconnected)
                .OnEntry(() => Reconnect())
                .Permit(Trigger.Connect, State.Connecting)
                .Permit(Trigger.Stop, State.Idle)
                .PermitReentry(Trigger.FailedToRetreiveDimensionInfo);

            _stateMachine.Configure(State.Connecting)
                .OnEntryFrom(_stateMachine.ConnectTrigger, (endpoint) => Connect(endpoint))
                .OnEntryFrom(_stateMachine.ConnectErrorTrigger, (endpoint, exception) => Connect(endpoint))
                .Permit(Trigger.OnTcpConnectError, State.Disconnected)
                .Permit(Trigger.Disconnect, State.Disconnected)
                .Permit(Trigger.OnTcpConnected, State.Connected);

            _stateMachine.Configure(State.Connected)
                .OnEntryFrom(Trigger.OnTcpConnected, () =>
                {
                    _tcpClient.BeginReceiving();
                    _stateMachine.Fire(Trigger.ConnectionEstablished);
                })
                .PermitIf(Trigger.ConnectionEstablished, State.Authenticating, () => _sessionCookie == null)
                .PermitIf(Trigger.ConnectionEstablished, State.Zoning, () => !(_sessionCookie == null))
                .Permit(Trigger.OnTcpConnectionError, State.Disconnected)
                .Permit(Trigger.Connect, State.Connecting)
                .Permit(Trigger.Disconnect, State.Disconnected);

            _stateMachine.Configure(State.Authenticating)
                .SubstateOf(State.Connected)
                .OnEntry(() =>
                {
                    Send(new UserLoginMessage
                    {
                        UserName = Client.Config.Credentials.Username,
                        ClientVersion = Client.Config.Dimension == Dimension.RubiKa ? DimensionInfo.RubiKa.Version : DimensionInfo.RubiKa2019.Version
                    });
                })
                .Permit(Trigger.Disconnect, State.Disconnected)
                .Permit(Trigger.FailedToLogin, State.Disconnected);

            _stateMachine.Configure(State.Zoning)
                .SubstateOf(State.Connected)
                .Permit(Trigger.CharInPlay, State.InPlay)
                .Permit(Trigger.Disconnect, State.Disconnected);

            _stateMachine.Configure(State.InPlay)
                .SubstateOf(State.Connected)
                .Ignore(Trigger.CharInPlay)
                .Permit(Trigger.Connect, State.Connecting)
                .Permit(Trigger.Disconnect, State.Disconnected);
        }

        private void Pong(Message pingMsg)
        {
            PingMessage pingBody = (PingMessage)pingMsg.Body;

            Message pongMsg = new Message
            {
                Body = new PingMessage
                {
                    PingMessageType = PingMessageType.Pong,
                    ServerTime = pingBody.ServerTime,
                    UpTime1 = pingBody.UpTime1,
                    UpTime2 = pingBody.UpTime2,
                    Unk2 = pingBody.Unk2,
                },
                Header = new Header
                {
                    PacketType = PacketType.PingMessage,
                    Sender = Client.LocalDynelId,
                    Receiver = pingMsg.Header.Sender
                }
            };

            Send(pongMsg);
        }

        private void OnInitiateCompressionMessage()
        {
            Send(new ZoneLoginMessage
            {
                CharacterId = Client.LocalDynelId,
                Cookie1 = _sessionCookie.Cookie1,
                Cookie2 = _sessionCookie.Cookie2
            });
        }

        private void RegisterInternalSystemMessageHandlers()
        {
            _internalSysMsgCallbacks = new Dictionary<SystemMessageType, Action<SystemMessage>>();

            _internalSysMsgCallbacks.Add(SystemMessageType.ZoneInfo, (msg) =>
            {
                ZoneInfoMessage zoneInfoMsg = (ZoneInfoMessage)msg;

                _sessionCookie = new SessionCookie
                {
                    Cookie1 = zoneInfoMsg.Cookie1,
                    Cookie2 = zoneInfoMsg.Cookie2
                };

                _stateMachine.Fire(_stateMachine.ConnectTrigger, new IPEndPoint(zoneInfoMsg.ServerIpAddress, zoneInfoMsg.ServerPort));
            });

            _internalSysMsgCallbacks.Add(SystemMessageType.ZoneRedirection, (msg) =>
            {
                ZoneRedirectionMessage zoneRedMsg = (ZoneRedirectionMessage)msg;

                _logger.Debug($"ZoneRediction to {zoneRedMsg.ServerIpAddress}:{zoneRedMsg.ServerPort}");

                _stateMachine.Fire(_stateMachine.ConnectTrigger, new IPEndPoint(zoneRedMsg.ServerIpAddress, zoneRedMsg.ServerPort));
            });

            _internalSysMsgCallbacks.Add(SystemMessageType.LoginError, (msg) =>
            {
                LoginErrorMessage loginErrorMsg = (LoginErrorMessage)msg;

                _logger.Debug($"Failed to login: {loginErrorMsg.Error}");

                _stateMachine.Fire(Trigger.FailedToLogin);
            });
        }

        private void RegisterInternalN3MessageHandlers()
        {
            _internalN3MsgCallbacks = new Dictionary<N3MessageType, Action<N3Message>>();

            _internalN3MsgCallbacks.Add(N3MessageType.FullCharacter, (msg) =>
            {
                _stateMachine.Fire(Trigger.CharInPlay);
            });
        }
    }
}
