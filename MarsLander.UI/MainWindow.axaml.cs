using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using ALine = Avalonia.Controls.Shapes.Line;

namespace MarsLander.UI;

public partial class MainWindow : Window
{
    private readonly Point[] map;
    private readonly Game game;
    private readonly Line finish;
    private readonly GeneticAlgorithm ga;

    public MainWindow()
    {
        map = new Point[]
        {
            new Point(0, 100),
            new Point(1000, 500),
            new Point(1500, 1500),
            new Point(3000, 1000),
            new Point(4000, 150),
            new Point(5500, 150),
            new Point(6999, 800),
        };
        game = new Game(map, new Lander(new Point(2500, 2700), 0, 0, 550, 0, 0));
        finish = game.Finish;
        ga = new GeneticAlgorithm(game);

        InitializeComponent();
    }

    private void Render()
    {
        var kx = Canvas.Bounds.Width / Game.WIDTH;
        var ky = Canvas.Bounds.Height / Game.HEIGHT;

        Canvas.Children.Clear();

        Canvas.Background = Brushes.Black;

        var ground = new Polyline
        {
            Stroke = Brushes.Red
        };
        foreach (var p in map)
        {
            var x = p.X * kx;
            var y = Canvas.Bounds.Height - p.Y * ky;

            ground.Points.Add(new Avalonia.Point(x, y));
        }

        var finishLine = new ALine
        {
            Stroke = Brushes.Red,
            StrokeThickness = 5,
            StartPoint = new Avalonia.Point(finish.Start.X * kx, Canvas.Bounds.Height - finish.Start.Y * ky),
            EndPoint = new Avalonia.Point(finish.End.X * kx, Canvas.Bounds.Height - finish.End.Y * ky),
        };

        var player = new Ellipse
        {
            Fill = new LinearGradientBrush
            {
                GradientStops =
                {
                    new GradientStop(Colors.White, 0.3),
                    new GradientStop(Colors.Blue, 1)
                }
            },
            Stroke = game.State switch
            {
                GameState.Landed => Brushes.LimeGreen,
                GameState.Crashed or GameState.CrashedOnFinish => Brushes.Red,
                _ => Brushes.White,
            },
            StrokeThickness = 1,
            Width = 20,
            Height = 40,
            RenderTransform = new RotateTransform(-game.Lander.Angle)
        };
        Canvas.SetLeft(player, game.Lander.Position.X * kx);
        Canvas.SetTop(player, Canvas.Bounds.Height - game.Lander.Position.Y * ky - player.Height / 2);

        Canvas.Children.Add(ground);
        Canvas.Children.Add(finishLine);
        Canvas.Children.Add(player);
    }

    private async Task PlayAll(Move[] moves)
    {
        for (var i = 0; i < moves.Length && !game.HasEnded; i++)
        {
            game.Play(moves[i]);
            Render();

            await Task.Delay(100);
        }

        await Console.Error.WriteLineAsync($"{game.State} - {game.Lander.Angle} - {game.Lander.HorizontalSpeed} - {game.Lander.VerticalSpeed}");
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        Render();
    }

    protected override void OnResized(WindowResizedEventArgs e)
    {
        base.OnResized(e);

        Render();
    }

    private async void PlayAll_OnClick(object sender, RoutedEventArgs e)
        => await PlayAll(
            Enumerable.Range(0, 200)
                .Select(_ => new Move(0, 0))
                .ToArray());

    private async void Train_OnClick(object sender, RoutedEventArgs e)
    {
        var solution = ga.Train();

        await PlayAll(solution.Moves);
    }
}