using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("HexMage.Simulator.Tests")]

namespace HexMage.Simulator
{
    public struct Coord : IEquatable<Coord>
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Coord(int x, int y) {
            X = x;
            Y = y;
        }

        public Coord Abs() {
            return new Coord(Math.Abs(X), Math.Abs(Y));
        }

        public int Max() {
            return Math.Max(X, Y);
        }

        public int Min() {
            return Math.Min(X, Y);
        }

        public bool Equals(Coord other) {
            return X == other.X && Y == other.Y;
        }

        public override string ToString() {
            return $"[{X},{Y}]";
        }

        public static Coord operator +(Coord lhs, Coord rhs) {
            return new Coord(lhs.X + rhs.X, lhs.Y + rhs.Y);
        }

        public static Coord operator -(Coord lhs, Coord rhs) {
            return new Coord(lhs.X - rhs.X, lhs.Y - rhs.Y);
        }
    }

    public struct Color
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Color(double x, double y, double z) {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class Ability
    {
        public int Dmg { get; set; }
        public int Cost { get; set; }
        public int Range { get; set; }

        public Ability(int dmg, int cost, int range) {
            Dmg = dmg;
            Cost = cost;
            Range = range;
        }
    }

    public class MobManager
    {
        public List<Mob> Mobs { get; set; } = new List<Mob>();
        public List<Team> Teams { get; set; } = new List<Team>();

        public bool MoveMob(Mob mob, Coord to) {
            // TODO - check that the move is only to a neighbour block
            if (mob.AP > 0) {
                mob.Coord = to;
                mob.AP--;

                return true;
            } else {
                return false;
            }
        }

        public Mob AtCoord(Coord c) {
            return Mobs.FirstOrDefault(mob => Equals(mob.Coord, c));
        }

        public Team AddTeam() {
            var team = new Team();
            Teams.Add(team);
            return team;
        }
    }

    public enum VertexState
    {
        Unvisited,
        Open,
        Closed
    }

    public class Path
    {
        public Coord? Source { get; set; }
        public VertexState State { get; set; }
        public int Distance { get; set; }
        public bool Reachable { get; set; }

        public override string ToString() {
            return $"{Source} - {State} - {Distance}";
        }
    }

    public class Pathfinder
    {
        public Matrix<Path> Paths { get; set; }
        public int Size { get; set; }

        public Pathfinder(int size) {
            Size = size;
            Paths = new Matrix<Path>(size, size);
            Paths.Initialize(() => new Path());
        }

        public IList<Coord> PathTo(Coord target) {
            var result = new List<Coord>();

            Coord current = target;
            result.Add(current);

            Path path = Paths[current];

            // TODO - this is ugly, the hexagonal-map abstraction leaks
            if (path == null) {
                return result;
            }

            int iterations = 1000;
            while (path.Distance > 0 && --iterations > 0) {
                if (path.Source != null) {
                    result.Add(path.Source.Value);
                    path = Paths[path.Source.Value];
                } else {
                    result.Clear();
                    break;
                }
            }

            Debug.Assert(iterations > 0);
            return result;
        }

        public void MoveAsFarAsPossible(MobManager mobManager, Mob mob, IList<Coord> path) {
            int i = path.Count - 1;

            while (mob.AP > 0 && i > 0) {
                mobManager.MoveMob(mob, path[i]);
                i--;
            }
        }

        public int Distance(Coord c) {
            return Paths[c].Distance;
        }

        public void PathfindFrom(Coord start, Map map, MobManager mobManager) {
            var queue = new Queue<Coord>();

            var diffs = new List<Coord> {
                new Coord(-1, 0),
                new Coord(1, 0),
                new Coord(0, -1),
                new Coord(0, 1),
                new Coord(1, -1),
                new Coord(-1, 1),
            };

            foreach (var coord in map.AllCoords) {
                var path = Paths[coord];

                path.Source = null;
                path.Distance = Int32.MaxValue;
                path.Reachable = false;
                path.State = VertexState.Unvisited;
            }

            // TODO - nema byt Y - X?
            var startPath = Paths[start];

            startPath.Distance = 0;
            startPath.State = VertexState.Open;
            startPath.Reachable = true;

            queue.Enqueue(start);

            int iterations = 0;

            while (queue.Count > 0) {
                var current = queue.Dequeue();

                iterations++;

                if (iterations > Size*Size*10 || queue.Count > 1000) {
                    Console.WriteLine("CHYBA, PATHFINDING SE ZASEKL");
                }

                // TODO - opet, nema byt Y a X?
                var p = Paths[current];

                if (p.State == VertexState.Closed) continue;

                p.Reachable = true;
                p.State = VertexState.Closed;

                foreach (var diff in diffs) {
                    Coord neighbour = current + diff;

                    if (IsValidCoord(neighbour)) {
                        Path n = Paths[neighbour];

                        bool notClosed = n.State != VertexState.Closed;
                        bool noWall = map[neighbour] != HexType.Wall;
                        bool noMob = mobManager.AtCoord(neighbour) == null;

                        //if (notClosed && noWall && noMob) {
                        if (notClosed && noWall) {
                            if (n.State == VertexState.Unvisited || n.Distance > p.Distance + 1) {
                                n.Distance = p.Distance + 1;
                                n.Source = current;
                                n.Reachable = true;
                            }

                            n.State = VertexState.Open;
                            queue.Enqueue(neighbour);
                        }
                    }
                }
            }
        }

        public bool IsValidCoord(Coord c) {
            return c.Abs().Max() < Size && c.Min() >= 0;
        }
    }

    public class TurnManager
    {
        public MobManager MobManager { get; set; }
        public List<Mob> TurnOrder { get; set; } = new List<Mob>();
        private int _current = 0;

        public TurnManager(MobManager mobManager) {
            MobManager = mobManager;
        }

        public bool IsTurnDone() {
            return _current >= TurnOrder.Count;
        }

        public void StartNextTurn() {
            TurnOrder.Clear();

            foreach (var mob in MobManager.Mobs) {
                mob.AP = mob.MaxAP;
                if (mob.HP > 0) {
                    TurnOrder.Add(mob);
                }
            }

            _current = 0;

            TurnOrder.Sort((a, b) => a.AP.CompareTo(b.AP));
        }

        public Mob CurrentMob() {
            return TurnOrder[_current];
        }

        public bool MoveNext() {
            if (!IsTurnDone()) _current++;
            return !IsTurnDone();
        }
    }

    public class UsableAbility
    {
        private Ability ability;
        private Mob mob;
        private Mob target;

        public UsableAbility(Mob mob, Mob target, Ability ability) {
            this.mob = mob;
            this.target = target;
            this.ability = ability;
        }

        public void Use() {
            target.HP = Math.Max(0, target.HP - ability.Dmg);

            // TODO - handle negative AP
            mob.AP -= ability.Cost;
        }
    }

    public class GameInstance
    {
        public Map Map { get; set; }
        public MobManager MobManager { get; set; }
        public Pathfinder Pathfinder { get; set; }
        public TurnManager TurnManager { get; set; }
        public int Size { get; set; }

        public GameInstance(int size) {
            Size = size;
            MobManager = new MobManager();
            Pathfinder = new Pathfinder(size);
            TurnManager = new TurnManager(MobManager);
            Map = new Map(size);
        }


        public bool IsFinished() {
#if DEBUG
            Debug.Assert(MobManager.Teams.All(team => team.Mobs.Count > 0));
#endif
            return MobManager.Teams.Any(team => team.Mobs.All(mob => mob.HP == 0));
        }

        public void Refresh() {
            Pathfinder.PathfindFrom(TurnManager.CurrentMob().Coord, Map, MobManager);
        }

        public IList<Ability> UsableAbilities(Mob mob) {
            return mob.Abilities.Where(ability => ability.Cost <= mob.AP).ToList();
        }

        public IList<UsableAbility> UsableAbilities(Mob mob, Mob target) {
            int distance = Pathfinder.Distance(target.Coord);

            return mob.Abilities
                .Where(ability => ability.Range >= distance && mob.AP >= ability.Cost)
                .Select(ability => new UsableAbility(mob, target, ability))
                .ToList();
        }

        public IList<Mob> PossibleTargets(Mob mob) {
            int maxRange = mob.Abilities.Max(ability => ability.Range);

            return MobManager
                .Mobs
                .Where(m => m != mob && Pathfinder.Distance(m.Coord) <= maxRange)
                .ToList();
        }

        public IList<Mob> Enemies(Mob mob) {
            return MobManager.Mobs.Where(enemy => !enemy.Team.Equals(mob.Team)).ToList();
        }
    }

    public interface IPlayer
    {
        bool IsAI();
        void ActionTo(Coord c, GameInstance gameInstance, Mob mob);
        void AnyAction(GameInstance gameInstance, Mob mob);
    }

    public class Team
    {
        public int ID { get; set; }
        public Color Color { get; set; }
        public List<Mob> Mobs { get; set; } = new List<Mob>();
        public IPlayer Player { get; set; }
    }

    public class Mob
    {
        private static int last_id_ = 0;
        public int ID { get; set; }

        public int HP { get; set; }
        public int AP { get; set; }
        public int MaxHP { get; set; }
        public int MaxAP { get; set; }

        public List<Ability> Abilities { get; set; }
        public Team Team { get; set; }
        public Coord Coord { get; set; }
        public static int AbilityCount => 6;

        public Mob(Team team, int maxHp, int maxAp, List<Ability> abilities) {
            Team = team;
            MaxHP = maxHp;
            MaxAP = maxAp;
            Abilities = abilities;
            HP = maxHp;
            AP = maxAp;
            Coord = new Coord(0, 0);
            ID = last_id_++;
        }
    }

    public enum HexType
    {
        Empty,
        Wall,
        Player
    }

    public class Map
    {
        private readonly Matrix<HexType> _hexes;
        public int Size { get; set; }

        public List<Coord> AllCoords {
            get { return _hexes.AllCoords; }
        }

        public Map(int size) {
            Size = size;
            _hexes = new Matrix<HexType>(size, size);
        }


        public HexType this[Coord c] {
            get { return _hexes[c]; }
            set { _hexes[c] = value; }
        }
    }

    public class Generator
    {
        public static Mob RandomMob(Team team, int size, Predicate<Coord> isCoordAvailable) {
            var abilities = new List<Ability>();

            var random = new Random();
            for (int i = 0; i < Mob.AbilityCount; i++) {
                abilities.Add(new Ability(random.Next(1, 10), random.Next(3, 7), 5));
            }

            var mob = new Mob(team, 10, 10, abilities);
            team.Mobs.Add(mob);

            while (true) {
                Coord c = new Coord(random.Next(0, size), random.Next(0, size));
                if (isCoordAvailable(c)) {
                    mob.Coord = c;
                    break;
                }
            }

            return mob;
        }
    }
}