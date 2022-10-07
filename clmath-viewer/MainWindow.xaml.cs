using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace clmath.viewer
{
    public partial class MainWindow : Window
    {
        public const float res = 1.0f;
        public readonly Component fx;
        private int scaleX = 75;
        private int scaleY = 100;
        private MathContext ctx;
        private Component x;
        
        public MainWindow()
        {
            DataContext = this;
            fx = Program.ParseFunc(App.Func);
            ctx = new MathContext();
            var key = fx.EnumerateVars()[0];
            ctx.var[key] = x = new Component() { type = Component.Type.Num, arg = (double)0 };

            InitializeComponent();
            DrawGraph();
        }

        private bool graphInit;
        private Line lineX;
        private Line lineY;
        private readonly List<Line> lines = new();
        private int lineIndex;

        private void SetLine(double x1, double y1, double x2, double y2)
        {
            if (!HasValue(x1) || !HasValue(x2) || !HasValue(y1) || !HasValue(y2))
                return;
            if (lineIndex >= lines.Count)
            {
                var line = new Line() { Stroke = Brushes.Red, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 };
                Graph.Children.Add(line);
                lines.Add(line);
            }
            else
            {
                var line = lines[lineIndex];
                line.X1 = x1;
                line.Y1 = y1;
                line.X2 = x2;
                line.Y2 = y2;
                lineIndex += 1;
            }
        }
        
        public static bool HasValue(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private void DrawGraph()
        {
            //Graph.Children.Clear();
            var xc = Width / 2;
            var yc = Height / 2;

            if (!graphInit)
            {
                // x axis
                Graph.Children.Add(lineX = new Line()
                {
                    Stroke = Brushes.Azure,
                    X1 = 0,
                    Y1 = yc,
                    X2 = Width,
                    Y2 = yc
                });
                
                // y axis
                Graph.Children.Add(lineY = new Line()
                {
                    Stroke = Brushes.Azure,
                    X1 = xc,
                    Y1 = 0,
                    X2 = xc,
                    Y2 = Height
                });
                graphInit = true;
            }
            else
            {
                lineX.X2 = Width;
                lineX.Y1 = yc;
                lineX.Y2 = yc;
                lineY.Y2 = Height;
                lineY.X1 = xc;
                lineY.X2 = xc;
            }

            lineIndex = 0;
            double px, py = px = -Width - Width;
            double step = res / Math.Sqrt(scaleX) * 2.5;
            for (x.arg = -xc; (double)x.arg < Width; x.arg = (double)x.arg + step)
            {
                var y = fx.Evaluate(ctx) * -1;
                SetLine(
                    px * scaleX + xc,
                    py * scaleY + yc,
                    (double)x.arg * scaleX + xc,
                    y * scaleY + yc
                );
                px = (double)x.arg;
                py = y;
            }

            // cleanup unused lines
            var array = lines.GetRange(lineIndex, lines.Count-lineIndex).ToArray();
            foreach (var line in array)
            {
                lines.Remove(line);
                Graph.Children.Remove(line);
            }
        }

        private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawGraph();
        }

        private void MainWindow_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
        }
    }
}