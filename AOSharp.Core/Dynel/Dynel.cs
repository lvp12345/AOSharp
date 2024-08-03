using System;
using System.Runtime.InteropServices;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Imports;
using AOSharp.Common.Unmanaged.Interfaces;

namespace AOSharp.Core
{
    public class Dynel
    {
        public readonly IntPtr Pointer;

        public unsafe Identity Identity => (*(MemStruct*)Pointer).Identity;

        public CharacterFlags Flags => (CharacterFlags)GetStat(Stat.Flags);

        public unsafe IntPtr VehiclePointer => new IntPtr((*(MemStruct*)Pointer).Vehicle);

        public unsafe Vector3 Position
        {
            get => (*(MemStruct*)Pointer).Vehicle->Position;
            set => (*(MemStruct*)Pointer).Vehicle->Position = value;
        }

        public unsafe Quaternion Rotation
        {
            get => (*(MemStruct*)Pointer).Vehicle->Rotation;
            set => N3Dynel_t.SetRelRot(Pointer, ref value);
        }

        public unsafe Vector3 GlobalPosition => *N3Dynel_t.GetGlobalPos(Pointer);

        public unsafe MovementState MovementState
        {
            get => (*(MemStruct*)Pointer).Vehicle->CharMovementStatus->State;
            set => (*(MemStruct*)Pointer).Vehicle->CharMovementStatus->State = value;
        }

        public unsafe float Runspeed
        {
            get => (*(MemStruct*)Pointer).Vehicle->Runspeed;
            set => (*(MemStruct*)Pointer).Vehicle->Runspeed = value;
        }

        public unsafe float Accel
        {
            get => (*(MemStruct*)Pointer).Vehicle->Accel;
            set => (*(MemStruct*)Pointer).Vehicle->Accel = value;
        }

        public unsafe float Radius => (*(MemStruct*)Pointer).Vehicle->Radius;

        public unsafe bool IsFalling => (*(MemStruct*)Pointer).Vehicle->IsFalling;

        public virtual unsafe bool IsMoving => (*(MemStruct*)Pointer).Vehicle->Velocity > 0f;

        public unsafe float Velocity => (*(MemStruct*)Pointer).Vehicle->Velocity;

        protected unsafe bool IsPathing => (*(MemStruct*)Pointer).Vehicle->PathingDestination != Vector3.Zero;
        protected unsafe Vector3 PathingDestination => (*(MemStruct*)Pointer).Vehicle->PathingDestination;

        public virtual string Name => GetName();
        public Room Room => Playfield.IsDungeon ? new Room(N3Dynel_t.GetZone(Pointer)) : null;

        public bool IsValid => DynelManager.IsValid(this);

        public Dynel(IntPtr pointer)
        {
            Pointer = pointer;
        }
        
        public void Target()
        {
            Targeting.SetTarget(Identity);
        }

        public unsafe int GetStat(Stat stat, int detail = 2)
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine == IntPtr.Zero)
                return 0;

            //Copy identity
            Identity identity = Identity;
            Identity junk = new Identity();

            return N3EngineClientAnarchy_t.GetSkill(pEngine, ref identity, stat, detail, ref junk);
        }

        private string GetName()
        {
            IntPtr pEngine = N3Engine_t.GetInstance();

            if (pEngine == IntPtr.Zero)
                return string.Empty;

            Identity identity = Identity;
            Identity unk = new Identity();

            return Marshal.PtrToStringAnsi(N3EngineClientAnarchy_t.GetName(pEngine, ref identity, ref unk));
        }

        public void Use()
        {
            N3EngineClientAnarchy.UseItem(Identity);
        }

        public float DistanceFrom(Dynel dynel)
        {
            return Vector3.Distance(Position, dynel.Position);
        }

        [StructLayout(LayoutKind.Explicit, Pack = 0)]
        protected unsafe struct MemStruct
        {
            [FieldOffset(0x14)]
            public Identity Identity;

            [FieldOffset(0x50)]
            public Vehicle* Vehicle;
        }

        public override bool Equals(object obj)
        {
            return (obj is Dynel) && Identity.Equals(((Dynel)obj).Identity);
        }

        public override int GetHashCode()
        {
            return Identity.GetHashCode();
        }
    }
}
