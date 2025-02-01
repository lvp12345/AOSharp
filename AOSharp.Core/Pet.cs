using System;
using System.Runtime.InteropServices;
using AOSharp.Core.GameData;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOSharp.Core
{
    public class Pet : PetBase
    {
        public SimpleChar Character => DynelManager.GetDynel<SimpleChar>(Identity);
        public PetType Type => Character != null ? (PetType)Character.GetStat(Stat.PetType) : PetType.Unknown;
        public Pet(Identity identity) : base(identity)
        {
        }

        private void CommandPet(PetCommand command)
        {
            Network.Send(new PetCommandMessage()
            {
                Command = command,
                Pets = new PetBase[1] {this}
            });
        }

        public void Attack(Identity target)
        {
            Targeting.SetTarget(target);
            CommandPet(PetCommand.Attack);
        }

        public void Follow()
        {
            CommandPet(PetCommand.Follow);
        }

        public void Heal(Identity target)
        {
            Targeting.SetTarget(target);
            CommandPet(PetCommand.Heal);
        }

        public void Wait()
        {
            CommandPet(PetCommand.Wait);
        }

        public void Guard()
        {
            CommandPet(PetCommand.Guard);
        }

        public void Behind()
        {
            CommandPet(PetCommand.Behind);
        }
    }

    public static class PetExtensions
    {
        private static void CommandPets(this Pet[] pets, PetCommand command)
        {
            Network.Send(new PetCommandMessage()
            {
                Command = command,
                Pets = pets
            });
        }

        public static void Attack(this Pet[] pets, Identity target)
        {
            Targeting.SetTarget(target);
            pets.CommandPets(PetCommand.Attack);
        }

        public static void Follow(this Pet[] pets)
        {
            pets.CommandPets(PetCommand.Follow);
        }

        public static void Heal(this Pet[] pets, Identity target)
        {
            Targeting.SetTarget(target);
            pets.CommandPets(PetCommand.Heal);
        }

        public static void Wait(this Pet[] pets)
        {
            pets.CommandPets(PetCommand.Wait);
        }

        public static void Guard(this Pet[] pets)
        {
            pets.CommandPets(PetCommand.Guard);
        }

        public static void Behind(this Pet[] pets)
        {
            pets.CommandPets(PetCommand.Behind);
        }
    }
}
