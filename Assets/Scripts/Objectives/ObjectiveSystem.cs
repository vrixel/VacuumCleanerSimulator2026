using System;
using System.Collections.Generic;
using UnityEngine;

namespace VCS.Objectives
{
    public class Objective
    {
        public string Id;
        public string Title;
        public string Description;
        public string Event;
        public int Target;
        public int Reward;
        public int Progress;
        public bool Done;
        public bool EverDone;
    }

    /// <summary>
    /// Silly achievements. Progress resets every run; completion is remembered in PlayerPrefs.
    /// Events are plain strings: "absorb:Sock", "absorb:any", "knock", "launch", "bagfull", "trash", "speed", "spin", "clean100".
    /// </summary>
    public class ObjectiveSystem
    {
        readonly List<Objective> all = new List<Objective>();
        readonly Dictionary<string, List<Objective>> byEvent = new Dictionary<string, List<Objective>>();

        public IReadOnlyList<Objective> All => all;
        public event Action<Objective> Completed;

        public int DoneCount
        {
            get { int n = 0; foreach (var o in all) if (o.EverDone) n++; return n; }
        }

        public ObjectiveSystem()
        {
            Add("crumbs", "Crumb Cruncher", "Eat 30 crumbs", "absorb:Crumb", 30, 100);
            Add("dust", "Dust Bunny Hunter", "Catch 20 dust bunnies", "absorb:Dust", 20, 100);
            Add("socks", "Sock Thief", "Steal 8 socks", "absorb:Sock", 8, 150);
            Add("coins", "Piggy Bank", "Find 10 coins", "absorb:Coin", 10, 150);
            Add("bricks", "Barefoot Hero", "Remove 15 toy bricks", "absorb:Brick", 15, 150);
            Add("chair", "Furniture Diet", "Eat a chair", "absorb:Chair", 1, 300);
            Add("couch", "Couch Potato", "Eat the couch", "absorb:Couch", 1, 500);
            Add("toilet", "Royal Flush", "Eat the toilet", "absorb:Toilet", 1, 800);
            Add("launch", "Air Mail", "Blow 5 things across the room", "launch", 5, 200);
            Add("knock", "Domino Day", "Knock over 10 pieces of furniture", "knock", 10, 200);
            Add("bagfull", "Bag Burst", "Fill the bag to the top", "bagfull", 1, 100);
            Add("trash", "Trash Day", "Empty the bag into the bin 3 times", "trash", 3, 200);
            Add("speed", "Speed Demon", "Reach top speed", "speed", 1, 150);
            Add("spin", "Spin Cycle", "Spin 3 times in 2 seconds", "spin", 1, 150);
            Add("eater", "Big Eater", "Eat 200 things", "absorb:any", 200, 300);
            Add("cordwhip", "Cord Whip", "Rewind 80 m of cord", "rewind", 80, 200);
            Add("longreach", "Long Reach", "Find the end of the cord", "taut", 1, 150);
            Add("plughopper", "Plug Hopper", "Use 4 different sockets", "plug", 4, 200);
            Add("spotless", "Spotless", "Clean 100% of the house", "clean100", 1, 1000);
            foreach (var o in all) o.EverDone = PlayerPrefs.GetInt("ach_" + o.Id, 0) == 1;
        }

        void Add(string id, string title, string desc, string ev, int target, int reward)
        {
            var o = new Objective { Id = id, Title = title, Description = desc, Event = ev, Target = target, Reward = reward };
            all.Add(o);
            if (!byEvent.TryGetValue(ev, out var list))
            {
                list = new List<Objective>();
                byEvent[ev] = list;
            }
            list.Add(o);
        }

        public void ResetProgress()
        {
            foreach (var o in all) { o.Progress = 0; o.Done = false; }
        }

        public void Report(string ev, int amount = 1)
        {
            if (!byEvent.TryGetValue(ev, out var list)) return;
            foreach (var o in list)
            {
                if (o.Done) continue;
                o.Progress += amount;
                if (o.Progress < o.Target) continue;
                o.Progress = o.Target;
                o.Done = true;
                if (!o.EverDone)
                {
                    o.EverDone = true;
                    PlayerPrefs.SetInt("ach_" + o.Id, 1);
                    PlayerPrefs.Save();
                }
                Completed?.Invoke(o);
            }
        }

        /// <summary>Fills the buffer with the first unfinished objectives, in list order.</summary>
        public int FillActive(List<Objective> buffer, int max)
        {
            buffer.Clear();
            foreach (var o in all)
            {
                if (o.Done) continue;
                buffer.Add(o);
                if (buffer.Count >= max) break;
            }
            return buffer.Count;
        }
    }
}
