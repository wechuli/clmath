using System;
using System.Windows;
using System.Windows.Input;
using SharpGL;
using SharpGL.Enumerations;
using SharpGL.SceneGraph;
using SharpGL.WPF;

namespace clmath.viewer
{
    public partial class MainWindow : Window
    {
        public const float lim = 1000;
        public const float res = 0.1f;
        public readonly Component fx;
        private int scaleX = 1;
        private int scaleY = 1;
        private MathContext ctx;
        private Component x;
        
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            fx = Program.ParseFunc(App.Func);
            ctx = new MathContext();
            var key = fx.EnumerateVars()[0];
            ctx.var[key] = x = new Component() { type = Component.Type.Num, arg = (double)0 };
        }

        private void Initialize(object sender, OpenGLRoutedEventArgs args)
        {
        }

        private void Draw(object sender, OpenGLRoutedEventArgs args)
        {
            var gl = args.OpenGL;

            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            //gl.LoadIdentity();
            gl.LineWidth(20f);
            gl.PointSize(20f);

            gl.Color(1.0, 1.0, 1.0);
            
            gl.Begin(BeginMode.Lines);
            gl.Vertex(0,0);
            gl.Vertex(Graph.ActualWidth, Graph.ActualHeight);
            gl.End();

            // y axis
            gl.Begin(BeginMode.Lines);
            gl.Vertex(-lim, 0);
            gl.Vertex(lim, 0);
            gl.End();

            // x axis
            gl.Begin(BeginMode.Lines);
            gl.Vertex(0, -lim);
            gl.Vertex(0, lim);
            gl.End();

            // x > 0 curve
            gl.Begin(BeginMode.Lines);
            gl.Color(1.0, 0.0, 0.0);
            for (x.arg = (double)0; (double)x.arg < lim; x.arg = (double)x.arg + res)
            {
                var y = fx.Evaluate(ctx);
                gl.Vertex((double)x.arg, y);
            }
            gl.End();
            
            // x < 0 curve
            gl.Begin(BeginMode.Lines);
            for (x.arg = (double)0; (double)x.arg > lim; x.arg = (double)x.arg - res)
            {
                var y = fx.Evaluate(ctx);
                gl.Vertex((double)x.arg, y);
            }
            gl.End();

            gl.Flush();
        }

        private void MouseHandler(object sender, MouseEventArgs e)
        {
        }

        private void ClickHandler(object sender, MouseButtonEventArgs e)
        {
        }

        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Application.Current.Shutdown(0);
            if (e.Key == Key.NumPad4)
                scaleX += 1;
            if (e.Key == Key.NumPad1)
                scaleX -= 1;
            if (e.Key == Key.NumPad5)
                scaleY += 1;
            if (e.Key == Key.NumPad2)
                scaleY -= 1;
        }
    }
}