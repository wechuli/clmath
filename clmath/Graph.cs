using System.Drawing;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace clmath;

public sealed class GraphWindow : IDisposable
{
    public const float lim = 1000;
    public const float res = 0.1f;
    private int scaleX = 1;
    private int scaleY = 1;
    private MathContext ctx;
    private Component x;

    private readonly Component fx;
    private IWindow window { get; }
    private GL gl { get; set; }

    public GraphWindow(Component fx)
    {
        this.fx = fx;
        this.window = Window.Create(WindowOptions.Default);
        
        ctx = new MathContext();
        var key = fx.EnumerateVars()[0];
        ctx.var[key] = x = new Component() { type = Component.Type.Num, arg = (double)0 };
        
        window.Load += Load;
        window.FramebufferResize += Resize;
        window.Render += Render;
        window.Run();
    }

    private void Load()
    {
        gl = window.CreateOpenGL();
    }

    private void Resize(Vector2D<int> s)
    {
        // Adjust the viewport to the new window size
        gl.Viewport(s);
    }

    private void Render(double obj)
    {
        gl.ClearColor(Color.White);
        gl.Clear((uint) ClearBufferMask.ColorBufferBit);
        
        // x axis
        gl.BeginQuery(GLEnum.Lines, 1);
        gl.vertex
        gl.EndQuery(GLEnum.Lines);
    }

    public void Dispose()
    {
        gl.Dispose();
        window.Dispose();
    }
}