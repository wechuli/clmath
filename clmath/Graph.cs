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

    private void Resize(Vector2D<int> s)
    {
        // Adjust the viewport to the new window size
        gl.Viewport(s);
    }
    
    private uint ax_vbo;
    private uint ax_vao;
    private uint shaders;

    private const string shader_vtx = "#version 400\n"
                                      + "in vec3 vp;"
                                      + "void main() {"
                                      + "  gl_Position = vec4(vp, 1.0);"
                                      + "}";

    private const string shader_frag = "#version 400\n"
                                       + "out vec4 frag_colour;"
                                       + "void main() {"
                                       + "  frag_colour = vec4(0.5, 0.0, 0.5, 1.0);"
                                       + "}";

    private unsafe void Load()
    {
        gl = window.CreateOpenGL();
        
        double[] ax_vtx = new double[]
        {
            0, -0.5, 0,
            1, -0.5, 1
        };
        /*
        double[] ax_vtx = {
            0.0,  0.5,  0.0,
            0.5, -0.5,  0.0,
            -0.5, -0.5,  0.0
        };
        */

        ax_vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, ax_vbo);
        fixed(double* ax_vtx_ptr = &ax_vtx[0])
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(ax_vtx.Length * sizeof(double)), ax_vtx_ptr, GLEnum.StaticDraw);

        ax_vao = gl.GenVertexArray();
        gl.BindVertexArray(ax_vao);
        gl.EnableVertexAttribArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, ax_vbo);
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Double, false, 0, null);

        var shd_vtx = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(shd_vtx, shader_vtx);
        gl.CompileShader(shd_vtx);
        var shd_frg = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(shd_frg, shader_frag);
        gl.CompileShader(shd_frg);

        shaders = gl.CreateProgram();
        gl.AttachShader(shaders, shd_vtx);
        gl.AttachShader(shaders, shd_frg);
        gl.LinkProgram(shaders);
    }

    private unsafe void Render(double obj)
    {
        gl.Clear(16640U); // color & depth buffer
        //gl.ClearColor(Color.Black);
        
        gl.UseProgram(shaders);
        gl.BindVertexArray(ax_vao);
        gl.DrawArrays(PrimitiveType.Lines, 0, 3);
    }

    public void Dispose()
    {
        gl.Dispose();
        window.Dispose();
    }
}