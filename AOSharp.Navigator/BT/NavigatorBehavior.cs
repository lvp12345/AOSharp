using AOSharp.Core.Movement;
using AOSharp.Core;
using BehaviourTree.FluentBuilder;
using BehaviourTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Core.UI;
using AOSharp.Pathfinding;
using AOSharp.Core.Inventory;
using SharpNav;

namespace AOSharp.Navigator.BT
{
    internal class NavigatorBehavior
    {
        public enum ElevatorType
        {
            Up,
            Down
        }

        public static Dictionary<int, Dictionary<ElevatorType, Vector3>> FixerGridElevators = new Dictionary<int, Dictionary<ElevatorType, Vector3>>()
        {
            {
                0,
                new Dictionary<ElevatorType, Vector3>
                {
                    { ElevatorType.Up, new Vector3 (307, 1.313771, 67) },
                }
            },
            {
                1,
                new Dictionary<ElevatorType, Vector3>
                {
                    { ElevatorType.Up, new Vector3(313.8423, 12.31377, 66.95627) },
                    { ElevatorType.Down,  new Vector3(300.2563, 12.31375, 66.94968) },
                }
            },
            {
                2,
                new Dictionary<ElevatorType, Vector3>
                {
                    { ElevatorType.Up, new Vector3(313.4414, 22.31378, 64.6021) },
                    { ElevatorType.Down, new Vector3(300.612, 22.31378, 69.21096) },
                }
            },
            {
                3,
                new Dictionary<ElevatorType, Vector3>
                {
                    { ElevatorType.Up, new Vector3(312.2277, 32.31377, 62.54662) },
                    { ElevatorType.Down, new Vector3(301.8001, 32.31377, 71.29952) },
                }
            },
            {
                4,
                new Dictionary<ElevatorType, Vector3>
                {
                    { ElevatorType.Up, new Vector3(310.3886, 42.31377, 61.06009) },
                    { ElevatorType.Down, new Vector3(303.6136, 42.31374, 72.81461) },
                }
            },
            {
                5,
                new Dictionary<ElevatorType, Vector3>
                {
                    { ElevatorType.Up, new Vector3(308.1369, 52.31377, 60.25761) },
                    { ElevatorType.Down, new Vector3(305.7891, 52.31377, 73.68073) },
                }
            },
            {
                6,
                new Dictionary<ElevatorType, Vector3>
                {
                    { ElevatorType.Up, new Vector3(305.7449, 62.31377, 60.24995) },
                    { ElevatorType.Down, new Vector3(308.1276, 62.31377, 73.66639) },
                }
            },
            {
                7,
                new Dictionary<ElevatorType, Vector3>
                {
                    { ElevatorType.Up, new Vector3(303.5269, 72.31377, 61.13066) },
                    { ElevatorType.Down, new Vector3(310.3562, 72.31377, 72.86738) },
                }
            },
            {
                8,
                new Dictionary<ElevatorType, Vector3>
                {
                    { ElevatorType.Up, new Vector3(301.7581, 82.31377, 62.66596) },
                    { ElevatorType.Down, new Vector3(312.1694, 82.31377, 71.39105) },
                }
            },
            {
                9,
                new Dictionary<ElevatorType, Vector3>
                {
                    { ElevatorType.Up, new Vector3(300.5934, 92.31377, 64.73015) },
                    { ElevatorType.Down, new Vector3(313.3604, 92.31377, 69.3892) },
                }
            },
            {
                10,
                new Dictionary<ElevatorType, Vector3>
                {
                    { ElevatorType.Down, new Vector3(313.7806, 102.3138, 67.06682) },
                }
            }
        };

        internal static IBehaviour<NavigatorContext> NavBehavior()
        {
            return FluentBuilder.Create<NavigatorContext>()
                .Sequence("Root")
                    .Subtree(NavigateTasks())
                .End()
                .Build();
        }

        internal static IBehaviour<NavigatorContext> NavigateTasks()
        {
            return FluentBuilder.Create<NavigatorContext>()
                .Sequence("Navigate Tasks")
                    .Condition("Are there any tasks?", c => c.Tasks.Any())
                    .Do("Load Navmesh", LoadNavmesh)
                    .Selector("Transverse Link")
                        .Subtree(TransverseUseOnTerminalLink())
                        .Subtree(TransverseTerminalLink())
                        .Subtree(TransverseTeleporterLink())
                        .Subtree(TransverseZoneBorderLink())
                        .Subtree(MoveToDestination())
                    .End()
                .End()
                .Build();
        }

        internal static IBehaviour<NavigatorContext> NavigateFixerGrid()
        {
            TeleporterLink teleporterLink = null;
            Vector3 elevatorPos = Vector3.Zero;

            return FluentBuilder.Create<NavigatorContext>()
                .Sequence("Fixer Grid")
                    .Condition("Is Teleporter Link?", c => TryConvertTask(c.Tasks.Peek(), out teleporterLink))
                    .Condition("Is Fixer Grid?", c => (PlayfieldId)Playfield.ModelIdentity.Instance == PlayfieldId.FixerGrid)
                    .UntilSuccess("Until Correct Floor")
                        .Selector("Navigate To Correct Floor")
                            .Condition("Is Correct Floor", c => GetNextElevator(teleporterLink, out elevatorPos))
                            .AlwaysFail("")
                                .Do("Move To Next Floor", c => MoveToTransitionSpot(c, teleporterLink, elevatorPos))
                            .End()
                        .End()
                    .End()
                    .Do("Move to exit", c => MoveToTransitionSpot(c, teleporterLink, teleporterLink.TeleporterPos))
                .End()
                .Build();
        }

        internal static IBehaviour<NavigatorContext> PreUseItem_FixerGrid()
        {
            return FluentBuilder.Create<NavigatorContext>()
                .Sequence("Pop FGrid Can")
                    .Condition("Is Fixer Grid Link?", c => c.Tasks.Peek() is FixerGridTerminalLink)
                    .Do("Use FGrid Can", UseFixerGridCan)
                .End()
                .Build();
        }

        internal static IBehaviour<NavigatorContext> TransverseUseOnTerminalLink()
        {
            UseOnTerminalLink terminalLink = null;

            return FluentBuilder.Create<NavigatorContext>()
                .Sequence("Transverse Use On Terminal Link")
                    .Condition("Is Use On Terminal Link?", c => TryConvertTask(c.Tasks.Peek(), out terminalLink))
                    .Do("Move to Terminal", c => MoveToTransitionSpot(c, terminalLink, terminalLink.TerminalPos))
                    .Subtree(PreUseItem_FixerGrid())
                    .Do("Use Terminal", c => UseOnTerminal(c, terminalLink))
                .End()
                .Build();
        }

        internal static IBehaviour<NavigatorContext> TransverseTerminalLink()
        {
            TerminalLink terminalLink = null;

            return FluentBuilder.Create<NavigatorContext>()
                .Sequence("Transverse Terminal Link")
                    .Condition("Is Terminal Link?", c => TryConvertTask(c.Tasks.Peek(), out terminalLink))
                    .Do("Move to Terminal", c => MoveToTransitionSpot(c, terminalLink, terminalLink.TerminalPos))
                    .Do("Use Terminal", c => UseTerminal(c, terminalLink))
                .End()
                .Build();
        }

        internal static IBehaviour<NavigatorContext> TransverseTeleporterLink()
        {
            TeleporterLink teleporterLink = null;

            return FluentBuilder.Create<NavigatorContext>()
                .Sequence("Transverse Teleporter Link")
                    .Condition("Is Teleporter Link?", c => TryConvertTask(c.Tasks.Peek(), out teleporterLink))
                    .Subtree(NavigateFixerGrid())
                    .Do("Move to teleporter", c => MoveToTransitionSpot(c, teleporterLink, teleporterLink.TeleporterPos))
                .End()
                .Build();
        }

        internal static IBehaviour<NavigatorContext> TransverseZoneBorderLink()
        {
            ZoneBorderLink zoneBorderLink = null;

            return FluentBuilder.Create<NavigatorContext>()
                .Sequence("Transverse Zone Border Link")
                    .Condition("Is Zone Border Link?", c => TryConvertTask(c.Tasks.Peek(), out zoneBorderLink))
                    .Do("Move to zone border", c => MoveToTransitionSpot(c, zoneBorderLink, zoneBorderLink.TransitionSpots[0]))
                .End()
                .Build();
        }

        internal static IBehaviour<NavigatorContext> MoveToDestination()
        {
            MoveToTask moveTask = null;

            return FluentBuilder.Create<NavigatorContext>()
                .Sequence("Move To Destination")
                    .Condition("Is MoveTo Task?", c => TryConvertTask(c.Tasks.Peek(), out moveTask))
                    .Do("Move to destination", c => MoveToTransitionSpot(c, moveTask, moveTask.Destination))
                .End()
                .Build();
        }

        public static BehaviourStatus LoadNavmesh(NavigatorContext context)
        {
            if (!SMovementController.IsLoaded())
            {
                SMovementController.Set();
            }

            if (!context.NavmeshCache.TryGetValue((PlayfieldId)Playfield.ModelIdentity.Instance, out NavMesh navmesh))
            {
                string navMeshPath = $"{System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}\\NavMeshes\\{Playfield.ModelIdentity.Instance}.nav";

                if (!SNavMeshSerializer.LoadFromFile(navMeshPath, out navmesh))
                {
                    //TODO: Throw exception
                    Chat.WriteLine($"Unable to load Nav mesh for {Playfield.ModelIdentity.Instance}");
                    Stop(context);
                    return BehaviourStatus.Failed;
                }

                context.NavmeshCache.Add((PlayfieldId)Playfield.ModelIdentity.Instance, navmesh);
            }

            if (!SMovementController.NavAgent.HasPathfinder || SMovementController.NavAgent.NavMesh != navmesh)
            {
                Chat.WriteLine($"Loading navmesh for {Playfield.ModelIdentity.Instance}");
                SMovementController.LoadNavmesh(navmesh, true);
            }

            return BehaviourStatus.Succeeded;
        }

        public static BehaviourStatus MoveToTransitionSpot(NavigatorContext context, NavigatorTask task, Vector3 transitionPos)
        {
            if (!IsTaskValid(context, task))
                return BehaviourStatus.Succeeded;

            if (Vector3.Distance(DynelManager.LocalPlayer.Position, transitionPos) < 1f && !SMovementController.IsNavigating())
            {
                if (task is MoveToTask)
                {
                    context.Tasks.Dequeue();

                    if (!context.Tasks.Any())
                        context.Navigator.DestinationReachedCallback?.Invoke();
                }

                return BehaviourStatus.Succeeded;
            }

            if (SMovementController.IsNavigating())
                return BehaviourStatus.Running;

            SMovementController.SetNavDestination(transitionPos);
            return BehaviourStatus.Running;
        }

        public static BehaviourStatus UseFixerGridCan(NavigatorContext context)
        {
            if (Inventory.Find("Data Receptacle", out _))
                return BehaviourStatus.Succeeded;

            if (DynelManager.LocalPlayer.Cooldowns.TryGetValue(Stat.FirstAid, out _))
                return BehaviourStatus.Running;

            if (Inventory.Find("Nano Can: Instant Fixer Grid Conversion", out Item nanoCan))
            {
                nanoCan.Use();

                return BehaviourStatus.Succeeded;
            }
            else
            {
                return BehaviourStatus.Failed;
            }
        }

        public static BehaviourStatus UseOnTerminal(NavigatorContext context, UseOnTerminalLink terminalLink)
        {
            if (DynelManager.Find(terminalLink.TerminalName, out SimpleItem terminal) && Inventory.Find(terminalLink.ItemName, out Item useItem))
            {

                useItem.UseOn(terminal.Identity);
                Chat.WriteLine($"Using {useItem.Slot} on {terminal.Identity}");

                return BehaviourStatus.Succeeded;
            }
            else
            {
                return BehaviourStatus.Failed;
            }
        }

        public static BehaviourStatus UseTerminal (NavigatorContext context, TerminalLink terminalLink)
        {
            if (DynelManager.Find(terminalLink.TerminalName, out SimpleItem terminal))
            {
                terminal.Use();
                return BehaviourStatus.Succeeded;
            }
            else
            {
                return BehaviourStatus.Failed;
            }
        }

        public static void Stop(NavigatorContext context)
        {
            context.Tasks.Clear();
        }

        private static bool IsTaskValid(NavigatorContext context, NavigatorTask task)
        {
            return context.Tasks.Contains(task);
        }

        public static bool TryConvertTask<T>(NavigatorTask task, out T convertedTask) where T : NavigatorTask
        {
            convertedTask = task as T;
            return task is T;
        }

        public static bool GetNextElevator(TeleporterLink teleporterLink, out Vector3 elevatorPos)
        {
            int currentFloor = GetFgridFloor(DynelManager.LocalPlayer.Position);
            int desiredFloor = GetFgridFloor(teleporterLink.TeleporterPos);

            if (currentFloor == desiredFloor)
            {
                elevatorPos = Vector3.Zero;
                return true;
            }

            elevatorPos = FixerGridElevators[currentFloor][currentFloor < desiredFloor ? ElevatorType.Up : ElevatorType.Down];
            return false;
        }

        public static int GetFgridFloor(Vector3 pos)
        {
            return (int)(pos.Y / 10f);
        }
    }
}
