using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using AOLite.Net;
using SmokeLounge.AOtomation.Messaging.Messages;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.ChatMessages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.GameData;
using AOSharp.Common.SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.Messages.SystemMessages;
using System.Threading.Tasks;
using AOLite.Wrappers;
using System.IO;
using AOSharp.Common.Unmanaged.Imports;
using System.Runtime.InteropServices;
using AOSharp.Common.Helpers;
using AOSharp.Common;
using AOSharp.Bootstrap;
using EasyHook;
using AOSharp.Common.Unmanaged.DataTypes;
using CommandLine;
using AOSharp.Core;
using System.Runtime.ExceptionServices;
using AOLite.Debugging;
using System.Reflection;

namespace AOLite
{
    public class Credentials
    {
        public string Username { get; }
        public string Password { get; }

        public Credentials(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }

    public class ClientConfig
    {
        public Credentials Credentials;
        public string CharacterName;
        public Dimension Dimension;
        public bool UseChat = false;
        public string AOPath;
        public bool AutoReconnect = true;
        public int ReconnectDelay = 30000; //TODO: implement exponential backoff
        public List<string> Plugins;
    }

    public static class Client
    {
        public static string CharacterName { get; internal set; }
        public static int LocalDynelId { get; internal set; }

        public static ClientConfig Config;

        public static bool InPlay => _netSession.InPlay;
        public static bool Connected => _netSession.Connected;

        public static object Render_t { get; private set; }

        private static NetworkSession _netSession;
        private static UpdateLoop _updateLoop;
        private static bool _isFirstPlayshift = true;

        private static N3InterfaceModule N3Interface;
        private static N3ClientEngine Engine;
        private static ResourceDatabase ResourceDatabase;
        private static EngineState _engineState;

        internal static Logger Logger;
        internal static bool LogDeserializationErrors = true;

        private static PluginProxy _pluginProxy;

        public static Action<CharacterSelect> CharacterSelect;

        private static Dictionary<SystemMessageType, Action<SystemMessage>> _sysMsgCallbacks;
        private static Dictionary<N3MessageType, Action<N3Message, byte[]>> _n3MsgCallbacks;
        private static List<CharacterActionType> _blacklistedCharacterActionTypes;

        private static string _localPath;

        public static void Start(ClientConfig config, Logger logger)
        {
            Config = config;
            CharacterName = config.CharacterName;
            Logger = logger;

            if (config.UseChat)
                CreateChatClient();

            Init();
            Run();
        }

        public static void Send(MessageBody msgBody) => _netSession.Send(msgBody);

        public static void Send(Message message) => _netSession.Send(message);

        public static void Disconnect()
        {
            Teardown();
        }

        public static void SuppressDeserializationErrors()
        {
            LogDeserializationErrors = false;
        }

        internal static void CreateChatClient()
        {
            //
        }

        internal static void Init()
        {
            AppDomain.CurrentDomain.SetData("AOLite", true);

            _localPath = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Config.AOPath);

            N3Interface = new N3InterfaceModule();
            new CodeHacks().Install();

            RegisterSystemMessageHandlers();
            RegisterN3MessageHandlers();

            _pluginProxy = new PluginProxy();
        }

        internal static void Run()
        {
            _updateLoop = new UpdateLoop(Update);

            _netSession = new NetworkSession(Logger, _pluginProxy, _sysMsgCallbacks, _n3MsgCallbacks);
            _netSession.Disconnected += ShutdownN3Engine;
            _netSession.Connect();

            _updateLoop.Start();
        }

        internal static void StartN3Engine()
        {
            Logger.Debug("Starting N3Engine");

            _engineState = new EngineState(N3Interface.GetCharID());

            ResourceDatabase = new ResourceDatabase();
            ResourceDatabase.Open($"{Config.AOPath}\\cd_image\\data");

            InstanceManager instanceManager = new InstanceManager();
            instanceManager.InitInfoObj(ResourceDatabase.GetInfoObject());

            Engine = N3Interface.CreateN3Engine(ResourceDatabase);

            // Create renderer (TODO: hack this out)
            Marshal.GetDelegateForFunctionPointer<Render_t.CreateRenderDelegate>(Kernel32.GetModuleHandle("randy31.dll") + 0x24C42)();

            LoadCore();
            SetupHooks();
        }

        internal static void ShutdownN3Engine()
        {
            if (Engine != null)
            {
                Logger.Debug("N3Engine shutting down");
                Engine?.Close();
                Engine = null;
            }

            N3Interface?.Shutdown();
        }

        private static void LoadCore()
        {
            _pluginProxy = new PluginProxy();
            _pluginProxy.LoadCore(_localPath + "\\AOSharp.Core.dll");
        }

        private static void LoadPlugins()
        {
            foreach (string pluginPath in Config.Plugins)
                _pluginProxy.LoadPlugin(pluginPath);

            _pluginProxy.RunPluginInitializations();
        }

        private static void SetupHooks()
        {
            Hooker.CreateHook("Gamecode.dll",
                        "?ToClientN3Message@n3EngineClientAnarchy_t@@UBEXABVIdentity_t@@PAVACE_Data_Block@@@Z",
                        new N3EngineClientAnarchy_t.ToClientN3MessageDelegate(N3EngineClientAnarchy_ToClientN3MessageDelegate_Hook));

            Hooker.CreateHook("Connection.dll",
                        "?Send@Connection_t@@QAEHIIPBX@Z",
                        new Connection_t.DSend(Connection_t_Send_Hook));
        }

        private static int Connection_t_Send_Hook(IntPtr pConnection, uint unk, int len, byte[] buf)
        {
            _netSession.SendDatablock(buf);
            return 0;
        }

        private static unsafe void N3EngineClientAnarchy_ToClientN3MessageDelegate_Hook(IntPtr pThis, ref Identity identity, IntPtr pDataBlock)
        {
            var datablock = *(ACEDataBlock*)pDataBlock;

            byte[] managedBlock = new byte[datablock.BlockSize + 16];
            managedBlock[3] = 0xA;

            Marshal.Copy(datablock.Data, managedBlock, 16, datablock.BlockSize);

            _netSession.SendDatablock(managedBlock);
        }

        internal static void Teardown()
        {
            _updateLoop?.Stop();
            _netSession.Disconnect();
            Logger.Dispose();
        }

        internal static void Update(float deltaTime)
        {
            _netSession.Update();

            //IPCChannel.UpdateInternal();

            if (Engine == null)
                return;

            if (!InPlay)
                return;

            TickEngine(deltaTime);
        }

        [HandleProcessCorruptedStateExceptions]
        internal static void TickEngine(float deltaTime)
        {
            try
            {
                _engineState.AddTick(deltaTime);
                Engine.RunEngine(deltaTime);
                _pluginProxy.Update(deltaTime);
            }
            catch(AccessViolationException e)
            {
                Logger.Error(e.Message);

                DumpFinalEngineState();

                string path = $"{_localPath}\\AOLiteCrash_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}.dat";
                Console.WriteLine($"Saving engine state to {path}");
                _engineState.SaveState(path);

                Config.AutoReconnect = false;

                Disconnect();
            }
        }

        private static void DumpFinalEngineState()
        {
            int i = 1;
            foreach(var description in _engineState.DescribeBlock(_engineState.TickBlocks.Peek()))
                Logger.Debug($"{i++}. {description}");
        }

        public static void SendMessageToEngine(byte[] dataBlock)
        {
            _engineState.AddDataBlock(dataBlock);
            N3Interface.ProcessMessage(dataBlock);
        }

        internal static void SelectCharacter(int id)
        {
            Send(new SelectCharacterMessage
            {
                CharacterId = id
            });

            N3Interface.SetCharID(id);
            LocalDynelId = id;
        }

        private static void RegisterSystemMessageHandlers()
        {
            _sysMsgCallbacks = new Dictionary<SystemMessageType, Action<SystemMessage>>();

            _sysMsgCallbacks.Add(SystemMessageType.ZoneInfo, (msg) =>
            {
                StartN3Engine();
            });

            _sysMsgCallbacks.Add(SystemMessageType.ServerSalt, (msg) =>
            {
                Send(new UserCredentialsMessage
                {
                    UserName = Config.Credentials.Username,
                    Credentials = LoginEncryption.MakeChallengeResponse(Config.Credentials, ((ServerSaltMessage)msg).ServerSalt)
                });
            });

            _sysMsgCallbacks.Add(SystemMessageType.CharacterList, (msg) =>
            {
                CharacterListMessage charListMsg = (CharacterListMessage)msg;

                var characters = charListMsg.Characters.Select(x => new CharacterSelect.Character
                {
                    Id = x.Id,
                    Name = x.Name
                });

                if (CharacterSelect == null)
                {
                    var desiredChar = characters.FirstOrDefault(x => x.Name == CharacterName);

                    if (desiredChar == null)
                    {
                        Logger.Error($"Could not locate character with name: {CharacterName}.");

                        Logger.Error("Characters on this account:");

                        foreach (LoginCharacterInfo charInfo in charListMsg.Characters)
                            Logger.Error($"\t{charInfo.Name}");

                        return; //TODO: Trigger fatal error state?
                    }

                    desiredChar.Select();
                }
                else
                {
                    CharacterSelect.Invoke(new CharacterSelect
                    {
                        AllowedCharacters = charListMsg.AllowedCharacters,
                        Expansions = (ExpansionFlags)charListMsg.Expansions,
                        Characters = characters.ToList(),
                    });
                }
            });
        }

        private static void RegisterN3MessageHandlers()
        {
            _blacklistedCharacterActionTypes = new List<CharacterActionType>()
            {
                //None at the moment.
            };

            _n3MsgCallbacks = new Dictionary<N3MessageType, Action<N3Message, byte[]>>();

            _n3MsgCallbacks.Add(N3MessageType.PlayfieldAnarchyF, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.SimpleCharFullUpdate, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.Despawn, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.FollowTarget, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.CharInPlay, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.SetStat, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.SetPos, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.TeamMember, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.Attack, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.StopFight, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.HealthDamage, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.AttackInfo, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.SpecialAttackInfo, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.ReflectAttack, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.ShieldAttack, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.Absorb, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.MissedAttackInfo, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.CharSecSpecAttack, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.Stat, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.Buff, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.TeamMemberInfo, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.CastNanoSpell, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.GenericCmd, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.TemplateAction, (msg, raw) => SendMessageToEngine(raw));
            _n3MsgCallbacks.Add(N3MessageType.AddTemplate, (msg, raw) => SendMessageToEngine(raw));

            _n3MsgCallbacks.Add(N3MessageType.WeaponItemFullUpdate, (msg, raw) => SendMessageToEngine(raw)); // Needs long term testing.

            _n3MsgCallbacks.Add(N3MessageType.TeamInvite, (msg, raw) =>
            {
                TeamInviteMessage teamInviteMessage = (TeamInviteMessage)msg;

                TeamRequestEventArgs teamReqArgs = new TeamRequestEventArgs(teamInviteMessage.Requestor);
                Team.TeamRequest?.Invoke(null, teamReqArgs);
            });

            _n3MsgCallbacks.Add(N3MessageType.CharacterAction, (msg, raw) =>
            {
                CharacterActionMessage charActionMessage = (CharacterActionMessage)msg;

                if (_blacklistedCharacterActionTypes.Contains(charActionMessage.Action))
                    return;

                if (charActionMessage.Action == CharacterActionType.TeamRequestInvite)
                {
                    TeamRequestEventArgs teamReqArgs = new TeamRequestEventArgs(charActionMessage.Target);
                    Team.TeamRequest?.Invoke(null, teamReqArgs);
                }
                else
                {
                    SendMessageToEngine(raw);
                }
            });

            _n3MsgCallbacks.Add(N3MessageType.CharDCMove, (msg, raw) => 
            {
                CharDCMoveMessage moveMsg = (CharDCMoveMessage)msg;

                if (DynelManager.LocalPlayer != null && moveMsg.Identity == DynelManager.LocalPlayer.Identity)
                    return;

                SendMessageToEngine(raw);
            });

            _n3MsgCallbacks.Add(N3MessageType.FullCharacter, (msg, raw) =>
            {
                SendMessageToEngine(raw);
                TickEngine(0); //Tick Engine to allow LocalPlayer creation

                Send(new CharInPlayMessage());

                if (_isFirstPlayshift)
                {
                    _isFirstPlayshift = false;
                    LoadPlugins();
                }

                _pluginProxy.TeleportEnded();
            });
        }
    }
}
