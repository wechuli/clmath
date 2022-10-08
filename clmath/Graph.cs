using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace clmath;

public sealed class GraphWindow : IDisposable
{
    private static readonly DirectoryInfo AssemblyDir;
    private readonly MathContext ctx;
    private double scaleX = 3.5;
    private double scaleY = 1.5;
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
        -1, 0, //0,
        1, 0, //0,
            
        // y axis
        0, -1, //0,
        0, 1, //0
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
    //private uint ax_ebo;
    private uint cv_vao;
    private uint cv_vbo;
    //private uint cv_ebo;
    private uint cv_count;
    private uint shaders;

    private void Load()
    {
        gl = window.CreateOpenGL();
        /*
        gl.Enable(EnableCap.DebugOutput);
        gl.DebugMessageCallback((source, type, id, severity, length, message, param) 
            => Console.WriteLine($"source:{source}\ntype:{type}\nid:{id}\nseverity:{severity}\nlength:{length}\nmsg:{Marshal.PtrToStringAnsi(message)}\n"), null);
        */
        
        // shaders
        LoadShaders();
        
        // graphics
        InitGraphCross();
        InitGraphCurve();
    }

    private unsafe void LoadShaders()
    {
        shaders = gl.CreateProgram();

        // vertex shader
        var shd_vtx = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(shd_vtx, File.ReadAllText(Path.Combine(AssemblyDir.FullName, "Assets", "vertex.glsl")));
        gl.CompileShader(shd_vtx);
        gl.AttachShader(shaders, shd_vtx);

        // fragment shader
        var shd_frg = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(shd_frg, File.ReadAllText(Path.Combine(AssemblyDir.FullName, "Assets", "fragment.glsl")));
        gl.CompileShader(shd_frg);
        gl.AttachShader(shaders, shd_frg);

        // cleanup
        gl.LinkProgram(shaders);
        gl.DeleteShader(shd_vtx);
        gl.DeleteShader(shd_frg);
    }

    private unsafe void InitGraphCross()
    {
        // graph-cross element
        ax_vao = gl.GenVertexArray();
        ax_vbo = gl.GenBuffer();
        //ax_ebo = gl.GenBuffer();
        gl.BindVertexArray(ax_vao);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, ax_vbo);
        fixed (double* ax_vtx_ptr = &axies_verts[0])
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(axies_verts.Length * sizeof(double)), ax_vtx_ptr,
                GLEnum.StaticDraw);

        //gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ax_ebo);
        //fixed(uint* ax_idx_ptr = &axies_indices[0])
        //    gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(axies_indices.Length * sizeof(uint)), ax_idx_ptr, GLEnum.StaticDraw);

        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Double, false, 0, null);
        gl.EnableVertexAttribArray(0);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        //gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        gl.BindVertexArray(0);
        gl.Flush();
    }

    private unsafe void InitGraphCurve()
    {
        double step = 0.1;//Math.Sqrt(scaleX) * 2.5;
        List<Vector2D<double>> curve = new();
        var lim = scaleX + 1;
        for (x.arg = -scaleX; (double)x.arg < lim; x.arg = (double)x.arg + step)
        {
            var y = fx.Evaluate(ctx);
            curve.Add(new Vector2D<double>((double)x.arg, y));
        }
        var curve_verts = curve.SelectMany(v => new[] { v.X / scaleX, v.Y / scaleY }).ToArray();
        cv_count = Convert.ToUInt32(curve_verts.Length);

        cv_vao = gl.GenVertexArray();
        cv_vbo = gl.GenBuffer();
        //cv_ebo = gl.GenBuffer();
        gl.BindVertexArray(cv_vao);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, cv_vbo);
        fixed (double* cv_vtx_ptr = &curve_verts[0])
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(curve_verts.Length * sizeof(double)), cv_vtx_ptr,
                GLEnum.StaticDraw);

        //gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, cv_ebo);
        //fixed(uint* cv_idx_ptr = &curve_indices[0])
        //    gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(curve _indices.Length * sizeof(uint)), cv_idx_ptr, GLEnum.StaticDraw);

        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Double, false, 0, null);
        gl.EnableVertexAttribArray(0);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        //gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        gl.BindVertexArray(0);
        gl.Flush();
    }

    private void Render(double obj)
    {
        gl.Clear(16640U); // color & depth buffer
        
        gl.UseProgram(shaders);
        
        gl.BindVertexArray(ax_vao);
        gl.DrawArrays(PrimitiveType.Lines, 0, 4);
        //gl.DrawElements(PrimitiveType.Lines, 2, DrawElementsType.UnsignedInt, 0);
        
        gl.BindVertexArray(cv_vao);
        gl.DrawArrays(PrimitiveType.LineStrip, 0, cv_count / 2);
        //gl.DrawElements(PrimitiveType.Lines, 2, DrawElementsType.UnsignedInt, 0);
        
        gl.Flush();
    }

    public void Dispose()
    {
        gl.Dispose();
        window.Dispose();
    }
}
