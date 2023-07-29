using System;
using System.Runtime.CompilerServices;

// 1

// var map = new Point[]
// {
//     new Point(0, 100),
//     new Point(1000, 500),
//     new Point(1500, 100),
//     new Point(3000, 100),
//     new Point(5000, 1500),
//     new Point(6999, 1000),
// };
// var lander = new Lander(new Point(2500, 2500), 0, 0, 500, 0, 0);

// 2: map 1

// var map = new Point[]
// {
//     new Point(0, 100),
//     new Point(1000, 500),
//     new Point(1500, 1500),
//     new Point(3000, 1000),
//     new Point(4000, 150),
//     new Point(5500, 150),
//     new Point(6999, 800),
// };
// var lander = new Lander(new Point(2500, 2700), 0, 0, 550, 0, 0);
// var game = new Game(map, lander);
// var ga = new GeneticAlgorithm(game);
// var solution = ga.Train();

// for (var i = 0; i < solution.Moves.Length && !game.HasEnded; i++)
//     game.Play(solution.Moves[i]);
//
// Console.WriteLine(game.State);

string[] inputs;
var surfaceN = int.Parse(Console.ReadLine());

var map = new Point[surfaceN];

for (var i = 0; i < surfaceN; i++)
{
    var x = Console.ReadLine();
    inputs = x.Split(' ');
    // Console.Error.WriteLine(x);

    var landX = int.Parse(inputs[0]);
    var landY = int.Parse(inputs[1]);

    map[i] = new Point(landX, landY);
}

Lander lander;
Game game;
GeneticAlgorithm ga;
Solution solution = null;

for (var i = 0;; i++)
{
    var x = Console.ReadLine();
    inputs = x.Split(' ');
    // Console.Error.WriteLine(x);

    var X = int.Parse(inputs[0]);
    var Y = int.Parse(inputs[1]);
    var hSpeed = int.Parse(inputs[2]);
    var vSpeed = int.Parse(inputs[3]);
    var fuel = int.Parse(inputs[4]);
    var rotate = int.Parse(inputs[5]);
    var power = int.Parse(inputs[6]);

    if (i == 0)
    {
        lander = new Lander(new Point(X, Y), hSpeed, vSpeed, fuel, rotate, power);
        game = new Game(map, lander);
        ga = new GeneticAlgorithm(game);
        solution = ga.Train();
    }

    var move = solution.Moves[i];

    Console.WriteLine($"{move.Angle} {move.Power}");
}

public readonly record struct Move(int Angle, int Power);

public readonly record struct Point(double X, double Y)
{
    public double DistanceTo(Point point)
    {
        var x = (X - point.X) * (X - point.X);
        var y = (Y - point.Y) * (Y - point.Y);

        return Math.Sqrt(x + y);
    }
}

public readonly record struct Line(Point Start, Point End, bool IsFinish);

public class Lander
{
    public const int POWER_MIN = 0;
    public const int POWER_MAX = 4;
    public const int POWER_STEP = 1;
    public const int ANGLE_MIN = -90;
    public const int ANGLE_MAX = 90;
    public const int ANGLE_STEP = 15;

    public Lander(Point position, double horizontalSpeed, double verticalSpeed, int fuel, int angle, int power)
    {
        Position = position;
        Angle = angle;
        HorizontalSpeed = horizontalSpeed;
        VerticalSpeed = verticalSpeed;
        Fuel = fuel;
        Power = power;
    }

    private void AdjustAngle(int angle)
    {
        if (angle is < ANGLE_MIN or > ANGLE_MAX)
            throw new Exception();

        Angle += Math.Clamp(angle - Angle, -ANGLE_STEP, ANGLE_STEP);
    }

    private void AdjustPower(int power)
    {
        if (power is < POWER_MIN or > POWER_MAX)
            throw new Exception();

        if (Fuel <= 0)
        {
            Power = 0;
            return;
        }

        Power += Math.Clamp(power - Power, -POWER_STEP, POWER_STEP);
        Power = Math.Min(Fuel, Power);
        Fuel -= Power;
    }

    public bool Move(Move move)
    {
        AdjustAngle(move.Angle);
        AdjustPower(move.Power);

        var fx = Power * -Math.Sin(Angle * (Math.PI / 180));
        var fy = Power * Math.Cos(Angle * (Math.PI / 180)) - Game.G;

        HorizontalSpeed += fx;
        VerticalSpeed += fy;

        var positionX = Position.X + HorizontalSpeed + fx * 0.5;
        var positionY = Position.Y + VerticalSpeed + fy * 0.5 + Game.G;
        if (positionX is < 0 or > Game.WIDTH || positionY is < 0 or > Game.HEIGHT)
            return false;

        Position = new Point(positionX, positionY);
        return true;
    }

    public Lander Clone()
        => new Lander(Position, HorizontalSpeed, VerticalSpeed, Fuel, Angle, Power);

    public Point Position { get; private set; }

    public int Angle { get; private set; }

    public double HorizontalSpeed { get; private set; }

    public double VerticalSpeed { get; private set; }

    public int Fuel { get; private set; }

    public int Power { get; private set; }
}

public enum GameState
{
    Landing,
    Landed,
    Crashed,
    CrashedOnFinish,
}

public class Game
{
    public const int WIDTH = 7000;
    public const int HEIGHT = 3000;
    public const int LAND_SIZE = 1000;
    public const double G = 3.711;

    private readonly Line[] map;
    private readonly Line finish;
    private readonly Lander initialLander;
    private Lander lander;

    private Point lastLanderPosition;

    public Game(Point[] map, Lander lander)
    {
        (this.map, finish) = GetMap(map);
        this.initialLander = lander;
        this.lander = lander.Clone();
        State = GameState.Landing;
        lastLanderPosition = lander.Position;
    }

    private static (Line[], Line) GetMap(Point[] map)
    {
        var lines = new Line[map.Length - 1];
        var finish = default(Line);

        for (var i = 0; i < map.Length - 1; i++)
        {
            var left = map[i];
            var right = map[i + 1];
            var isFinish = left.Y == right.Y && right.X - left.X >= LAND_SIZE;

            var line = new Line(left, right, isFinish);
            lines[i] = line;
            if (isFinish)
                finish = line;
        }

        return (lines, finish);
    }

    private static bool IsOnSegment(Point p, Point q, Point r)
        => q.X <= Math.Max(p.X, r.X) &&
           q.X >= Math.Min(p.X, r.X) &&
           q.Y <= Math.Max(p.Y, r.Y) &&
           q.Y >= Math.Min(p.Y, r.Y);

    private static int GetOrientation(Point p, Point q, Point r)
    {
        var val = (q.Y - p.Y) * (r.X - q.X) -
                  (q.X - p.X) * (r.Y - q.Y);

        if (val == 0)
            return 0;

        return val > 0 ? 1 : 2;
    }

    private static bool Intersect(Line segment1, (Point, Point) segment2)
    {
        var (p1, q1, _) = segment1;
        var (p2, q2) = segment2;

        var o1 = GetOrientation(p1, q1, p2);
        var o2 = GetOrientation(p1, q1, q2);
        var o3 = GetOrientation(p2, q2, p1);
        var o4 = GetOrientation(p2, q2, q1);

        if (o1 != o2 && o3 != o4)
            return true;

        if (o1 == 0 && IsOnSegment(p1, p2, q1))
            return true;

        if (o2 == 0 && IsOnSegment(p1, q2, q1))
            return true;

        if (o3 == 0 && IsOnSegment(p2, p1, q2))
            return true;

        if (o4 == 0 && IsOnSegment(p2, q1, q2))
            return true;

        return false;
    }

    private IntersectionResult GetIntersection(Point point1, Point point2)
    {
        foreach (var line in map)
        {
            var intersect = Intersect(line, (point1, point2));
            if (!intersect)
                continue;

            if (line.IsFinish)
                return IntersectionResult.Finish;

            return IntersectionResult.Crash;
        }

        return IntersectionResult.None;
    }

    public void Play(Move move)
    {
        if (HasEnded)
            return;

        lastLanderPosition = lander.Position;
        var result = lander.Move(move);
        if (!result)
        {
            State = GameState.Crashed;
            return;
        }

        var intersection = GetIntersection(lastLanderPosition, lander.Position);
        if (intersection == IntersectionResult.Crash)
        {
            State = GameState.Crashed;
        }
        else if (intersection == IntersectionResult.Finish)
        {
            if (lander.Angle == 0 &&
                Math.Abs(lander.VerticalSpeed) <= 40 &&
                Math.Abs(lander.HorizontalSpeed) <= 20)
            {
                State = GameState.Landed;
            }
            else
            {
                State = GameState.CrashedOnFinish;
            }
        }
    }

    public void Reset()
    {
        lander = initialLander.Clone();
        lastLanderPosition = lander.Position;
        State = GameState.Landing;
    }

    public GameState State { get; private set; }

    public bool HasEnded => State != GameState.Landing;

    public Line[] Map => map;

    public Line Finish => finish;

    public Lander Lander => lander;

    private enum IntersectionResult
    {
        None,
        Crash,
        Finish
    }
}

public class Solution
{
    private readonly Move[] moves;

    public Solution()
        : this(GetPopulation())
    {
    }

    private Solution(Move[] moves)
        => this.moves = moves;

    private static int GetNormal(int mean, int stdDev)
    {
        var u1 = 1.0 - Random.Shared.NextDouble();
        var u2 = 1.0 - Random.Shared.NextDouble();
        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        var randNormal = mean + stdDev * randStdNormal;

        return (int)randNormal;
    }

    private static int GetRandomAngle(int prevAngle)
    {
        var angle = prevAngle + GetNormal(0, Lander.ANGLE_STEP);
        angle = Math.Clamp(angle, Lander.ANGLE_MIN, Lander.ANGLE_MAX);

        return angle;
    }

    private static int GetRandomPower(int prevPower)
    {
        var power = prevPower + GetNormal(2, Lander.POWER_STEP);
        power = Math.Clamp(power, Lander.POWER_MIN, Lander.POWER_MAX);

        return power;
    }

    private static Move[] GetPopulation()
    {
        var moves = new Move[GeneticAlgorithm.MOVE_SIZE];
        for (var i = 0; i < moves.Length; i++)
        {
            var prevAngle = 0;
            var prevPower = 0;
            if (i > 0)
            {
                prevAngle = moves[i - 1].Angle;
                prevPower = moves[i - 1].Power;
            }

            var angle = GetRandomAngle(prevAngle);
            var power = GetRandomPower(prevPower);

            moves[i] = new Move(angle, power);
        }

        return moves;
    }

    private double Evaluate(Game game)
    {
        var lander = game.Lander;

        var distancePenalty = 0.0;
        if (game.State is GameState.Crashed or GameState.Landing)
        {
            var landerPosition = lander.Position;
            var finish = game.Finish;

            var distanceToStart = landerPosition.DistanceTo(finish.Start);
            var distanceToEnd = landerPosition.DistanceTo(finish.End);

            distancePenalty = -Math.Min(distanceToStart, distanceToEnd);
        }

        var anglePenalty = 0.0;
        var hSpeedPenalty = 0.0;
        var vSpeedPenalty = 0.0;
        if (game.State is GameState.CrashedOnFinish or GameState.Crashed or GameState.Landing)
        {
            var hSpeed = Math.Abs(lander.HorizontalSpeed);
            var vSpeed = Math.Abs(lander.VerticalSpeed);

            anglePenalty = -Math.Abs(lander.Angle);
            hSpeedPenalty = hSpeed > 20 ? -(hSpeed - 20) : 0;
            vSpeedPenalty = vSpeed > 40 ? -(vSpeed - 40) : 0;
        }

        return //lander.Fuel
            +distancePenalty
            + anglePenalty
            + hSpeedPenalty
            + vSpeedPenalty;
    }

    public void Simulate(Game game)
    {
        foreach (var move in moves)
        {
            game.Play(move);

            if (game.HasEnded)
                break;
        }

        Score += Evaluate(game);

        game.Reset();
    }

    public void ResetScore() => Score = 0;

    public (Solution, Solution) Crossover(Solution other)
    {
        const double k1 = 0.8;
        const double k2 = 1 - k1;
        const double mutationChance = 0.01;

        var moves1 = new Move[GeneticAlgorithm.MOVE_SIZE];
        var moves2 = new Move[GeneticAlgorithm.MOVE_SIZE];

        for (var i = 0; i < moves.Length; i++)
        {
            if (Random.Shared.NextDouble() <= mutationChance)
            {
                var prevAngle = 0;
                var prevPower = 0;
                if (i > 0)
                {
                    prevAngle = moves[i - 1].Angle;
                    prevPower = moves[i - 1].Power;
                }

                var angle = GetRandomAngle(prevAngle);
                var power = GetRandomPower(prevPower);

                moves1[i] = new Move(angle, power);
            }
            else
            {
                var angle1 = GetAngle(moves[i].Angle, other.moves[i].Angle);
                var power1 = GetPower(moves[i].Power, other.moves[i].Power);
                moves1[i] = new Move(
                    Math.Clamp(angle1, Lander.ANGLE_MIN, Lander.ANGLE_MAX),
                    Math.Clamp(power1, Lander.POWER_MIN, Lander.POWER_MAX));
            }

            if (Random.Shared.NextDouble() <= mutationChance)
            {
                var prevAngle = 0;
                var prevPower = 0;
                if (i > 0)
                {
                    prevAngle = other.moves[i - 1].Angle;
                    prevPower = other.moves[i - 1].Power;
                }

                var angle = GetRandomAngle(prevAngle);
                var power = GetRandomPower(prevPower);

                moves2[i] = new Move(angle, power);
            }
            else
            {
                var angle2 = GetAngle(other.moves[i].Angle, moves[i].Angle);
                var power2 = GetPower(other.moves[i].Power, moves[i].Power);
                moves2[i] = new Move(
                    Math.Clamp(angle2, Lander.ANGLE_MIN, Lander.ANGLE_MAX),
                    Math.Clamp(power2, Lander.POWER_MIN, Lander.POWER_MAX));
            }
        }

        return (new Solution(moves1), new Solution(moves2));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetAngle(int angle1, int angle2)
            => (int)(k1 * angle1 + k2 * angle2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetPower(int power1, int power2)
            => (int)(k1 * power1 + k2 * power2);
    }

    public Move[] Moves => moves;

    public double Score { get; private set; }
}

public class GeneticAlgorithm
{
    public const int POPULATION_SIZE = 100;
    public const int MOVE_SIZE = 80;
    private const int WINNERS_SIZE = POPULATION_SIZE / 100 * 20;

    private readonly Game game;
    private readonly Solution[] population;

    private bool isInitialized = false;

    public GeneticAlgorithm(Game game)
    {
        this.game = game;

        population = new Solution[POPULATION_SIZE];
    }

    private void GeneratePopulation()
    {
        for (var i = 0; i < population.Length; i++)
            population[i] = new Solution();

        isInitialized = true;
    }

    private void Crossover()
    {
        foreach (var solution in population)
            solution.ResetScore();

        for (var i = WINNERS_SIZE; i < population.Length; i += 2)
        {
            var parent1 = population[Random.Shared.Next(0, WINNERS_SIZE)];
            var parent2 = population[Random.Shared.Next(0, WINNERS_SIZE)];
            while (parent1 == parent2)
                parent2 = population[Random.Shared.Next(0, WINNERS_SIZE)];

            var (child1, child2) = parent1.Crossover(parent2);
            population[i] = child1;
            population[i + 1] = child2;
        }
    }

    private void SimulateAll()
    {
        foreach (var solution in population)
            solution.Simulate(game);
    }

    private Solution Select()
    {
        Array.Sort(population, (l, r) => r.Score.CompareTo(l.Score));

        return population[0];
    }

    public Solution NextGeneration()
    {
        if (!isInitialized)
            GeneratePopulation();
        else
            Crossover();

        SimulateAll();
        var best = Select();

        return best;
    }

    public Solution Train()
    {
        var end = DateTime.UtcNow.AddMilliseconds(950);
        var solution = default(Solution);

        do
        {
            solution = NextGeneration();

            // Console.Error.WriteLine(solution.Score);
        }
        // while (solution.Score < 0);
        while (DateTime.UtcNow < end);

        return solution;
    }
}