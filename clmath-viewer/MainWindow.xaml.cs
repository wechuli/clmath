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
        private int scaleX = 100;
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

        private void DrawGraph()
        {
            Graph.Children.Clear();
            var xc = Width / 2;
            var yc = Height / 2;
            
            // x axis
            Graph.Children.Add(new Line()
            {
                Stroke = Brushes.Azure,
                X1 = 0,
                Y1 = yc,
                X2 = Width,
                Y2 = yc
            });
            
            // y axis
            Graph.Children.Add(new Line()
            {
                Stroke = Brushes.Azure,
                X1 = xc,
                Y1 = 0,
                X2 = xc,
                Y2 = Height
            });

            double px, py = px = -Width - Width;
            for (x.arg = -xc; (double)x.arg < Width; x.arg = (double)x.arg + res)
            {
                var y = fx.Evaluate(ctx);
                Graph.Children.Add(new Line()
                {
                    Stroke = Brushes.Red,
                    X1 = px * scaleX + xc,
                    Y1 = py * scaleY + yc,
                    X2 = (double)x.arg * scaleX + xc,
                    Y2 = y * scaleY + yc
                });
                px = (double)x.arg;
                py = y;
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