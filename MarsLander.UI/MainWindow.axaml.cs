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
    private readonly Lander lander;
    private readonly Game game;
    private readonly Line finish;

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
        lander = new Lander(new Point(2500, 2700), 0, 0, 550, 0, 0);
        game = new Game(map, lander);
        finish = game.Map.First(x => x.IsFinish);

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
            Stroke = game.State switch
            {
                GameState.Landed => Brushes.LimeGreen,
                GameState.Crashed => Brushes.Red,
                _ => Brushes.White,
            },
            StrokeThickness = 1,
            Width = 40,
            Height = 40,
        };
        Canvas.SetLeft(player, lander.Position.X * kx);
        Canvas.SetTop(player, Canvas.Bounds.Height - lander.Position.Y * ky - player.Height / 2);

        Canvas.Children.Add(ground);
        Canvas.Children.Add(finishLine);
        Canvas.Children.Add(player);
    }

    private async Task PlayAll()
    {
        do
        {
            game.Play(new Move(0, 0));
            Render();

            await Task.Delay(100);
        } while (!game.HasEnded);
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

    private async void PlayAll_OnClick(object? sender, RoutedEventArgs e)
        => await PlayAll();
}