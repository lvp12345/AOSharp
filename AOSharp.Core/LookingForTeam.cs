using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using AOSharp.Common.Helpers;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.Unmanaged.Imports;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.ChatMessages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOSharp.Core
{
    public static class LookingForTeam {

        private static bool completeResults = true;
        private static List<LookingForTeamApplicant> applicants = new List<LookingForTeamApplicant>();

        public static EventHandler<LookingForTeamSearchResultEventArgs> SearchResultReceived;

        public static void Join(string message = "")
        {
            Network.Send(new LftActivateMessage()
            {
                Message = message
            });
        }

        public static void Leave()
        {
            Network.Send(new LftDeactivateMessage());
        }

        public static void Search(LookingForTeamSide side = LookingForTeamSide.Any, LookingForTeamLocation location = LookingForTeamLocation.Anywhere, LookingForTeamProfession profession = LookingForTeamProfession.Any)
        {
            Network.Send(new LftQueryMessage()
            {
                Unknown1 = 0xffffffff,
                Side = (uint)side,
                Location = (uint)location,
                Profession = (uint)profession
            });
        }

        internal static void OnLftQueryResponse(LftQueryResponseMessage message)
        {
            if (completeResults)
            {
                applicants.Clear();
                completeResults = false;
            }

            if (message.Id > 0)
            {
                applicants.Add(new LookingForTeamApplicant(message));
            }
            else
            {
                completeResults = true;
                SearchResultReceived?.Invoke(null, new LookingForTeamSearchResultEventArgs(applicants));
            }
        }
    }

    public class LookingForTeamSearchResultEventArgs : EventArgs
    {
        public List<LookingForTeamApplicant> Applicants { get; }

        public LookingForTeamSearchResultEventArgs(List<LookingForTeamApplicant> applicants)
        {
            Applicants = applicants;
        }
    }

    public class LookingForTeamApplicant
    {
        public Identity Identity { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int PlayfieldId { get; set; }
        public Side Side { get; set; }
        public Profession Profession { get; set; }
        public string Message { get; set; }

        public LookingForTeamApplicant(LftQueryResponseMessage message)
        {
            Identity = new Identity((int)message.Id);
            Name = message.Name;
            Level = (int)message.Level;
            PlayfieldId = (int)message.Playfield;
            Side = (Side)message.Side;
            Profession = (Profession)message.Profession;
            Message = message.Message;
        }
    }

    public enum LookingForTeamLocation : uint
    {
        ThisPlayfield = 0,
        Anywhere = 1,
        RubiKa = 2,
        Shadowlands = 3
    }

    public enum LookingForTeamSide : uint
    {
        Any = 0xffffffff,
        Neutral = Side.Neutral,
        Clan = Side.Clan,
        Omni = Side.OmniTek
    }

    public enum LookingForTeamProfession : uint
    {
        Any = 0xffffffff,
        Soldier = ProfessionFlag.Soldier,
        MartialArtist = ProfessionFlag.MartialArtist,
        Engineer = ProfessionFlag.Engineer,
        Fixer = ProfessionFlag.Fixer,
        Agent = ProfessionFlag.Agent,
        Adventurer = ProfessionFlag.Adventurer,
        Trader = ProfessionFlag.Trader,
        Bureaucrat = ProfessionFlag.Bureaucrat,
        Enforcer = ProfessionFlag.Enforcer,
        Doctor = ProfessionFlag.Doctor,
        NanoTechnician = ProfessionFlag.NanoTechnician,
        MetaPhysicist = ProfessionFlag.MetaPhysicist,
        Keeper = ProfessionFlag.Keeper,
        Shade = ProfessionFlag.Shade
    }
}