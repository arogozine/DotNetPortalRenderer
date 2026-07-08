using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using RenderingEngine;
using SoftwareRendererModels;
using System.Diagnostics;

namespace SoftwareRenderer
{
    internal enum RenderState : byte
    {
        InitialLoad = 0,
        Normal = 1,
        BeingResized = 2,
        Minimized = 3
    }

    public partial class SoftwareRendererWindow : GameWindow
    {
        private const int _defaultWidth = 1280;
        private const int _defaultHeight = 720;

        private readonly PortalEngine Engine;

        private int _lastWidth = -1;
        private int _lastHeight = -1;
        private RenderState _renderState;

        public SoftwareRendererWindow(Arguments arguments, GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Engine = new PortalEngine(arguments);
        }

        public static SoftwareRendererWindow CreateNew(Arguments arguments)
        {
            return new SoftwareRendererWindow(
                arguments,
                new GameWindowSettings
                {
                    UpdateFrequency = 60,
                    Win32SuspendTimerOnDrag = false
                },
                new NativeWindowSettings
                {
                    Title = "Software Renderer",
                    ClientSize = (_defaultWidth, _defaultHeight),
                    Profile = ContextProfile.Core,
                    Flags = ContextFlags.ForwardCompatible,
                    API = ContextAPI.OpenGL,
                    APIVersion = new(4, 1)
                }
            );
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.LoadBindings(new GLFWBindingsContext());
            EnableDebugOutput();

            BuildShader();
            BuildQuad();

            _renderState = RenderState.InitialLoad;

            StartTheGameLoop();
        }

        protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
        {
            // Minimized State
            if (e.Width == 0 || e.Height == 0)
            {
                base.OnFramebufferResize(e);
                _renderState = RenderState.Minimized;
                return;
            }

            // Coming back from minimized state
            if (_renderState == RenderState.Minimized)
            {
                base.OnFramebufferResize(e);
                _renderState = RenderState.Normal;

                // No need to re-create if same width & height
                if (_lastHeight == e.Height && _lastWidth == e.Width)
                {
                    return;
                }
            }

            GL.Viewport(0, 0, e.Width, e.Height);

            _renderState = RenderState.BeingResized;

            base.OnFramebufferResize(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            float scale = (float)(args.Time * 60.0);
            Engine.Update(scale);

            base.OnUpdateFrame(args);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            // 1. Don't render if minimized
            // 2. On initial load, build texture, then start normal rendering
            // 3. On normal behavior, copy pointer via TexSubImage2D
            // 4. On resize, render one frame with old resolution, then re-build texture after buffer swap

            if (_renderState == RenderState.Minimized)
            {
                return;
            }

            // Initial Load - Build Texture
            if (_renderState == RenderState.InitialLoad)
            {
                BuildTexture();
                _renderState = RenderState.Normal;
                return;
            }

            Debug.Assert(Engine.Renderer != null);

            nint bgraPtr = Engine.Renderer.WaitForRenderedFrame();

            if (bgraPtr == nint.Zero)
            {
                Debug.Fail("BGRA pointer is zero");
                return;
            }

            BindTexture(bgraPtr);
            SwapBuffers();

            if (_renderState == RenderState.BeingResized)
            {
                StopTheGameLoop();
                StartTheGameLoop();

                BuildTexture();
                _renderState = RenderState.Normal;
                return;
            }

            Engine.Renderer.RequestNextFrame();
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            Engine.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            Engine.OnKeyUp(e);
        }

        private void StartTheGameLoop()
        {
            Engine.StartRenderingThread(FramebufferSize.X, FramebufferSize.Y);
        }

        private void StopTheGameLoop()
        {
            Engine.StopRenderingThread();
        }

        protected override void OnUnload()
        {
            StopTheGameLoop();

            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteTexture(_texture);
            GL.DeleteProgram(_shader);

            base.OnUnload();
        }
    }
}
