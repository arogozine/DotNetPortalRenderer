using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using RenderingEngine.Tooling;
using System.Runtime.InteropServices;

namespace SoftwareRenderer
{
    public partial class SoftwareRendererWindow : GameWindow
    {

        // ── OpenGL objects ───────────────────────────────────────────────────────
        private int _vao;        // vertex array object
        private int _vbo;        // vertex buffer (positions + UVs)
        private int _texture;    // 2-D texture that holds the pixel data
        private int _shader;     // linked shader program

        // ── Shader sources ───────────────────────────────────────────────────────
        private const string VertSrc = """
        #version 410 core
        layout(location = 0) in vec2 aPos;
        layout(location = 1) in vec2 aUV;
        out vec2 vUV;
        void main() {
            vUV = aUV;
            gl_Position = vec4(aPos, 0.0, 1.0);
        }
        """;

        // GL_BGRA upload swizzles B↔R automatically on the GPU, so the sampler
        // already returns RGBA — no manual channel swap needed in the shader.
        private const string FragSrc = """
        #version 410 core
        in  vec2 vUV;
        out vec4 fragColor;
        uniform sampler2D uTex;
        void main() {
            fragColor = texture(uTex, vUV);
        }
        """;

        // ── Full-screen quad (NDC positions + UV coords) ─────────────────────────
        // Two triangles covering [-1,+1] × [-1,+1].
        // UV y=0 is the top of the texture; flip V so row-0 of the array maps to
        // the top of the window.
        private static readonly float[] QuadVertices =
        [
           // X      Y     U     V
            -1.0f, -1.0f,  0.0f, 1.0f,   // bottom-left
             1.0f, -1.0f,  1.0f, 1.0f,   // bottom-right
             1.0f,  1.0f,  1.0f, 0.0f,   // top-right
 
            -1.0f, -1.0f,  0.0f, 1.0f,   // bottom-left
             1.0f,  1.0f,  1.0f, 0.0f,   // top-right
            -1.0f,  1.0f,  0.0f, 0.0f,   // top-left
        ];

        private void BuildShader()
        {
            int vert = CompileShader(ShaderType.VertexShader, VertSrc);
            int frag = CompileShader(ShaderType.FragmentShader, FragSrc);

            _shader = GL.CreateProgram();
            GL.AttachShader(_shader, vert);
            GL.AttachShader(_shader, frag);
            GL.LinkProgram(_shader);

            GL.GetProgram(_shader, GetProgramParameterName.LinkStatus, out int ok);
            if (ok == 0)
                throw new Exception($"Shader link error:\n{GL.GetProgramInfoLog(_shader)}");

            GL.DeleteShader(vert);
            GL.DeleteShader(frag);

            GL.UseProgram(_shader);
            GL.Uniform1(GL.GetUniformLocation(_shader, "uTex"), 0);
        }

        private static int CompileShader(ShaderType type, string src)
        {
            int id = GL.CreateShader(type);
            GL.ShaderSource(id, src);
            GL.CompileShader(id);
            GL.GetShader(id, ShaderParameter.CompileStatus, out int ok);
            if (ok == 0)
                throw new Exception($"{type} compile error:\n{GL.GetShaderInfoLog(id)}");

            return id;
        }

        private void BuildQuad()
        {
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer,
                QuadVertices.Length * sizeof(float),
                QuadVertices,
                BufferUsageHint.StaticDraw);

            const int stride = 4 * sizeof(float); // 2 pos + 2 uv

            // layout(location = 0) → position (xy)
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float,
                normalized: false, stride, offset: 0);

            // layout(location = 1) → UV (xy)
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float,
                normalized: false, stride, offset: 2 * sizeof(float));
        }

        private void BuildTexture()
        {
            if (_texture != default)
            {
                GL.DeleteTexture(_texture);
            }

            _texture = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            // No mipmaps needed for a 1:1 pixel display.
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D,
                TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            // Allocate immutable GPU storage
            GL.TexStorage2D(
                TextureTarget2d.Texture2D,
                levels: 1,
                internalformat: SizedInternalFormat.Rgba8,  // GPU stores RGBA8
                width: FramebufferSize.X,
                height: FramebufferSize.Y);

            _lastHeight = FramebufferSize.Y;
            _lastWidth = FramebufferSize.X;
        }

        public void BindTexture(nint bgraPtr)
        {
            // Re-upload pixel data to the texture (TexSubImage2D is faster than
            // TexImage2D because it reuses the already-allocated GPU storage).
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.TexSubImage2D(
                TextureTarget.Texture2D,
                level: 0,
                xoffset: 0,
                yoffset: 0,
                width: _lastWidth,
                height: _lastHeight,
                format: PixelFormat.Bgra,
                type: PixelType.UnsignedByte,
                pixels: (nint)bgraPtr);

            // Draw the full-screen quad
            GL.UseProgram(_shader);
            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        private static void EnableDebugOutput()
        {
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);

            GL.DebugMessageCallback(static (source, type, id, severity, len, msg, ptr) =>
            {
                LogSeverity logSeverity;

                switch (severity)
                {
                    case DebugSeverity.DebugSeverityLow:
                    case DebugSeverity.DontCare:
                        logSeverity = LogSeverity.Debug;
                        break;
                    case DebugSeverity.DebugSeverityNotification:
                        logSeverity = LogSeverity.Info;
                        break;
                    case DebugSeverity.DebugSeverityMedium:
                        logSeverity = LogSeverity.Warning;
                        break;
                    case DebugSeverity.DebugSeverityHigh:
                        logSeverity = LogSeverity.Error;
                        break;
                    default:
                        logSeverity = LogSeverity.Info;
                        break;
                }

                string? msgStr = Marshal.PtrToStringAnsi(msg);
                AsyncLogger.Default.AddLog(logSeverity, $"[OpenGL Debug] Source={source}, Type={type}, ID={id}, Severity={severity}, Msg={msgStr}");
                Console.WriteLine(Environment.StackTrace);

            }, nint.Zero);
        }
    }
}
