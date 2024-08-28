using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Misc;
using AOSharp.Core.Movement;
using AOSharp.Core.UI;
using AOSharp.Navigator.BT;
using AOSharp.Pathfinding;
using BehaviourTree;
using Newtonsoft.Json;
using SmokeLounge.AOtomation.Messaging.GameData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Navigator
{
    public class AONavigator
    {
        public Dictionary<PlayfieldId, PlayfieldNode> PlayfieldMap;
        private NavigatorContext _btContext;
        public IBehaviour<NavigatorContext> Behaviour;
        public bool IsNavigating => _btContext.Tasks.Any();
        private AutoResetInterval _internalTick = new AutoResetInterval(100);
        internal Action DestinationReachedCallback;

        public AONavigator() 
        {
            InitPlayfields();

            _btContext = new NavigatorContext(this);
            Behaviour = NavigatorBehavior.NavBehavior();

            Game.TeleportEnded += TeleportEnded;
        }

        private void TeleportEnded(object sender, object e)
        {
            if (!_btContext.Tasks.Any())
                return;

            NavigatorTask task = _btContext.Tasks.Dequeue();

            //Chat.WriteLine($"Dequeuing task: {task} - {task.DstId}");
            //Chat.WriteLine($"Next Task task: {_btContext.Tasks.Peek()} - {_btContext.Tasks.Peek().DstId}");

            if (task.DstId != Playfield.ModelId)
            {
                _btContext.Tasks.Clear();
                return;
            }
        }

        public void Update()
        {
            if (Game.IsZoning)
                return;

            if (!_internalTick.Elapsed)
                return;

            Behaviour.Tick(_btContext);
        }

        public void MoveTo(PlayfieldId id, bool useFGrid = false, bool preferFGrid = true, Action destinationReachedCallback = null)
        {
            if (Playfield.ModelId != id)
            {
                var path = GetPathTo(id, useFGrid, preferFGrid);

                if (path.Count == 0)
                {
                    //TODO Throw no path
                    Chat.WriteLine($"No path to {id}");
                    return;
                }

                foreach (PlayfieldLink link in path)
                {
                    _btContext.Tasks.Enqueue(link);
                }
            }

            DestinationReachedCallback = destinationReachedCallback;
        }


        public void MoveTo(PlayfieldId id, Vector3 pos, bool useFGrid = false, bool preferFGrid = true, Action destinationReachedCallback = null)
        {
            MoveTo(id, useFGrid, preferFGrid, destinationReachedCallback);
            _btContext.Tasks.Enqueue(new MoveToTask(id, pos));
        }

        public void Halt()
        {
            _btContext.Tasks.Clear();
            SMovementController.Halt();
        }

        public List<PlayfieldLink> GetPathTo(PlayfieldId toId, bool useFGrid = false, bool preferFGrid = true)
        {
            return GetPathFromTo((PlayfieldId)Playfield.ModelIdentity.Instance, toId, useFGrid, preferFGrid);
        }

        public List<PlayfieldLink> GetPathFromTo(PlayfieldId fromId, PlayfieldId toId, bool useFGrid = false, bool preferFGrid = true)
        {
            Dictionary<PlayfieldId, float> distance = new Dictionary<PlayfieldId, float>();
            List<PlayfieldId> vertices = new List<PlayfieldId>();
            Stack<PlayfieldId> path = new Stack<PlayfieldId>();
            Dictionary<PlayfieldId, PlayfieldId> previous = new Dictionary<PlayfieldId, PlayfieldId>();

            distance[fromId] = 0;
            vertices.Add(fromId);

            foreach (var pfId in PlayfieldMap.Keys)
            {
                if (pfId == fromId) 
                    continue;

                distance[pfId] = float.MaxValue;
                vertices.Add(pfId);
            }

            while (vertices.Any())
            {
                PlayfieldId current = vertices.OrderBy(x => distance[x]).First();
                vertices.Remove(current);

                if (current == toId)
                {
                    while (true)
                    {
                        path.Push(current);

                        if (!previous.ContainsKey(current))
                            break;

                        current = previous[current];
                    }

                    return GetLinksForPath(path.ToList());
                }

                if (!PlayfieldMap.ContainsKey(current))
                    continue;

                foreach (PlayfieldLink link in PlayfieldMap[current].Links)
                {
                    if (!PlayfieldMap.ContainsKey(link.DstId))
                        continue;

                    float dist = 1;
                    float alt = distance[current] + dist;

                    if (alt < distance[link.DstId])
                    {
                        distance[link.DstId] = alt;
                        previous[link.DstId] = current;
                    }
                }
            }

            return new List<PlayfieldLink>();
        }

        private List<PlayfieldLink> GetLinksForPath(List<PlayfieldId> path)
        {
            List<PlayfieldLink> links = new List<PlayfieldLink>();

            for(int i = 0; i < path.Count - 1; i++)
            {
                PlayfieldMap[path[i]].TryGetLink(path[i + 1], out PlayfieldLink link);
                links.Add(link);
            }

            return links;
        }

        private void InitPlayfields()
        {
            string configPath = $"{System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}\\PlayfieldLinks.json";

            if (!File.Exists(configPath))
                return;

            try
            {
                PlayfieldMap = JsonConvert.DeserializeObject<Dictionary<PlayfieldId, PlayfieldNode>>(File.ReadAllText(configPath));
                Chat.WriteLine($"Navigator Loaded");
            }
            catch (Exception e)
            {
                Chat.WriteLine($"Failed to load PlayfieldLinks.json. The error message was: {e}");
            }
        }
    }
}
