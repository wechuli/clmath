using System.Drawing;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace clmath;

public sealed class GraphWindow : IDisposable
{
    private static readonly DirectoryInfo AssemblyDir;
    public const float lim = 1000;
    public const float res = 0.1f;
    private int scaleX = 1;
    private int scaleY = 1;
    private MathContext ctx;
    private Component x;

    private readonly Component fx;
    private IWindow window { get; }
    private GL gl { get; set; }

    static GraphWindow()
    {
        AssemblyDir = new FileInfo(typeof(GraphWindow).Assembly.Location).Directory!;
    }

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

    private static readonly double[] axies_verts = new double[]
    {
        // x axis
        -0.5, 0, 0,
        0.5, 0, 0,
            
        // y axis
        0, -0.5, 0,
        0, 0.5, 0
    };
    private static readonly uint[] axies_indices = new uint[]
    {
        // x axis
        0, 1,

        // y axis
        2, 3
    };

    private uint ax_vao;
    private uint ax_vbo;
    private uint ax_ebo;
    private uint shaders;

    private unsafe void Load()
    {
        gl = window.CreateOpenGL();
        gl.Enable(EnableCap.DebugOutput);
        gl.DebugMessageCallback((source, type, id, severity, length, message, param) 
            => Console.WriteLine($"source:{source}\ntype:{type}\nid:{id}\nseverity:{severity}\nlength:{length}\nmsg:{Marshal.PtrToStringAnsi(message)}\n"), null);
        
        // shader loading
        var shd_vtx = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(shd_vtx, File.ReadAllText(Path.Combine(AssemblyDir.FullName, "Assets", "vertex.glsl")));
        gl.CompileShader(shd_vtx);
        var shd_frg = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(shd_frg, File.ReadAllText(Path.Combine(AssemblyDir.FullName, "Assets", "fragment.glsl")));
        gl.CompileShader(shd_frg);
        shaders = gl.CreateProgram();
        gl.AttachShader(shaders, shd_vtx);
        gl.AttachShader(shaders, shd_frg);
        gl.LinkProgram(shaders);
        gl.DeleteShader(shd_vtx);
        gl.DeleteShader(shd_frg);
        
        // graph-cross element
        ax_vao = gl.GenVertexArray();
        ax_vbo = gl.GenBuffer();
        ax_ebo = gl.GenBuffer();
        gl.BindVertexArray(ax_vao);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, ax_vbo);
        fixed(double* ax_vtx_ptr = &axies_verts[0])
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(axies_verts.Length * sizeof(double)), ax_vtx_ptr, GLEnum.StaticDraw);
        
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ax_ebo);
        fixed(uint* ax_vtx_ptr = &axies_indices[0])
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(axies_indices.Length * sizeof(uint)), ax_vtx_ptr, GLEnum.StaticDraw);
        
        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Double, false, 1, null);
        gl.EnableVertexAttribArray(0);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        //gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        gl.BindVertexArray(0);
    }

    private unsafe void Render(double obj)
    {
        gl.Clear(16640U); // color & depth buffer
        
        gl.UseProgram(shaders);
        gl.BindVertexArray(ax_vao);
        gl.DrawElements(PrimitiveType.Lines, 2, DrawElementsType.UnsignedInt, 0);
        
        gl.Flush();
    }

    public void Dispose()
    {
        gl.Dispose();
        window.Dispose();
    }
}