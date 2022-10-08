using System.Collections.Immutable;
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
    private const int maxFuncs = 7;
    private readonly MathContext[] ctx;
    private double scaleX = 3.5;
    private double scaleY = 1.5;
    private Component[] x;

    private readonly List<Component> fx;
    private IWindow window { get; }
    private GL gl { get; set; }

    static GraphWindow()
    {
        AssemblyDir = new FileInfo(typeof(GraphWindow).Assembly.Location).Directory!;
    }

    public GraphWindow(params Component[] fx)
    {
        if (fx.Length > maxFuncs)
            throw new Exception("Invalid amount of functions");
        
        this.fx = fx.ToList();
        this.window = Window.Create(WindowOptions.Default);

        var fxn = fx.Length;
        ctx = new MathContext[fxn];
        curves_vao = new uint[fxn];
        curves_vbo = new uint[fxn];
        curves_vtx_count = new uint[fxn];
        x = new Component[fxn];
        for (int i = 0; i < fxn; i++)
        {
            ctx[i] = new MathContext();
            var key = fx[i].EnumerateVars()[0];
            ctx[i].var[key] = x[i] = new Component() { type = Component.Type.Num, arg = (double)0 };
        }
        
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
    private readonly uint[] curves_vao;
    private readonly uint[] curves_vbo;
    private readonly uint[] curves_vtx_count;
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
        ax_vao = gl.GenVertexArray();
        ax_vbo = gl.GenBuffer();
        gl.BindVertexArray(ax_vao);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, ax_vbo);
        fixed (double* ax_vtx_ptr = &axies_verts[0])
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(axies_verts.Length * sizeof(double)), ax_vtx_ptr,
                GLEnum.StaticDraw);

        gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Double, false, 0, null);
        gl.EnableVertexAttribArray(0);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        gl.BindVertexArray(0);
        gl.Flush();
    }

    private unsafe void InitGraphCurve()
    {
        const double step = 0.1;
        
        for (int i = 0; i < fx.Count; i++)
        {
            List<Vector2D<double>> curve = new();
            var lim = scaleX + 1;
            for (x[i].arg = -scaleX; (double)x[i].arg! < lim; x[i].arg = (double)x[i].arg! + step)
            {
                var y = fx[i].Evaluate(ctx[i]);
                curve.Add(new Vector2D<double>((double)x[i].arg!, y));
            }

            var curve_verts = curve.SelectMany(v => new[] { v.X / scaleX, v.Y / scaleY }).ToArray();
            curves_vtx_count[i] = Convert.ToUInt32(curve_verts.Length);

            curves_vao[i] = gl.GenVertexArray();
            curves_vbo[i] = gl.GenBuffer();
            gl.BindVertexArray(curves_vao[i]);

            gl.BindBuffer(BufferTargetARB.ArrayBuffer, curves_vbo[i]);
            fixed (double* cv_vtx_ptr = &curve_verts[0])
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(curve_verts.Length * sizeof(double)), cv_vtx_ptr,
                    GLEnum.StaticDraw);

            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Double, false, 0, null);
            gl.EnableVertexAttribArray(0);

            gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            gl.BindVertexArray(0);
            gl.Flush();
        }
    }

    private void Render(double obj)
    {
        gl.Clear(16640U); // color & depth buffer
        
        gl.UseProgram(shaders);
        
        gl.BindVertexArray(ax_vao);
        gl.ColorMask(true, true, true, true);
        gl.DrawArrays(PrimitiveType.Lines, 0, 4);

        for (int i = 0; i < fx.Count; i++)
        {
            gl.BindVertexArray(curves_vao[i]);
            switch (i)
            {
                case 0:
                    gl.ColorMask(true, false, false, true);
                    break;
                case 1:
                    gl.ColorMask(false, true, false, true);
                    break;
                case 2:
                    gl.ColorMask(false, false, true, true);
                    break;
                case 3:
                    gl.ColorMask(true, true, false, true);
                    break;
                case 4:
                    gl.ColorMask(true, false, true, true);
                    break;
                case 5:
                    gl.ColorMask(false, true, true, true);
                    break;
                case 6:
                    gl.ColorMask(true, true, true, true);
                    break;
            }
            gl.DrawArrays(PrimitiveType.LineStrip, 0, curves_vtx_count[i] / 2);
            gl.Flush();
        }
    }

    public void Dispose()
    {
        gl.Dispose();
        window.Dispose();
    }
}
