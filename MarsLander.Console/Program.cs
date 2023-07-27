// 1

var map = new Point[]
{
    new Point(0, 100),
    new Point(1000, 500),
    new Point(1500, 100),
    new Point(3000, 100),
    new Point(5000, 1500),
    new Point(6999, 1000),
};
var lander = new Lander(new Point(2500, 2500), 0, 0, 500, 0, 0);

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
var game = new Game(map, lander);
do
{
    if (lander.VerticalSpeed <= -40)
        game.Play(new Move(0, 4));
    else
        game.Play(new Move(0, 0));
} while (!game.HasEnded);

Console.WriteLine(game.State);

public readonly record struct Move(int Angle, int Power);

public readonly record struct Point(double X, double Y);

public readonly record struct Line(Point Start, Point End, bool IsFinish);

public class Lander
{
    private const int POWER_MIN = 0;
    private const int POWER_MAX = 4;
    private const int POWER_STEP = 1;
    private const int ANGLE_MIN = -90;
    private const int ANGLE_MAX = 90;
    private const int ANGLE_STEP = 15;

    public Lander(Point position, int horizontalSpeed, int verticalSpeed, int fuel, int angle, int power)
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

    public void Move(Move move)
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
            throw new Exception();

        Position = new Point(positionX, positionY);
    }

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
    Crashed
}

public class Game
{
    public const int WIDTH = 7000;
    public const int HEIGHT = 3000;
    public const int LAND_SIZE = 1000;
    public const double G = 3.711;

    private readonly Line[] map;
    private readonly Lander lander;

    private Point lastLanderPosition;

    public Game(Point[] map, Lander lander)
    {
        this.map = GetMap(map);
        this.lander = lander;
        State = GameState.Landing;
        lastLanderPosition = lander.Position;
    }

    private static Line[] GetMap(Point[] map)
    {
        var lines = new Line[map.Length - 1];

        for (var i = 0; i < map.Length - 1; i++)
        {
            var left = map[i];
            var right = map[i + 1];
            var isFinish = left.Y == right.Y && right.X - left.X >= LAND_SIZE;

            lines[i] = new Line(left, right, isFinish);
        }

        return lines;
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
        lander.Move(move);

        var intersection = GetIntersection(lastLanderPosition, lander.Position);
        if (intersection == IntersectionResult.Crash)
        {
            State = GameState.Crashed;
        }
        else if (intersection == IntersectionResult.Finish)
        {
            if (lander.Angle == 0 &&
                Math.Abs(Math.Round(lander.VerticalSpeed)) <= 40 &&
                Math.Abs(Math.Round(lander.HorizontalSpeed)) <= 20)
            {
                State = GameState.Landed;
            }
            else
            {
                State = GameState.Crashed;
            }
        }
    }

    public GameState State { get; private set; }

    public bool HasEnded => State != GameState.Landing;

    public Line[] Map => map;

    private enum IntersectionResult
    {
        None,
        Crash,
        Finish
    }
}