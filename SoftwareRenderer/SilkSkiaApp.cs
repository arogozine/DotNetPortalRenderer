using RenderingEngine;
using RenderingEngine.Models;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;
using System.Diagnostics.CodeAnalysis;

namespace SoftwareRenderer
{
    /// <summary>
    /// Handles Window Logic and a OpenGL surface
    /// </summary>
    internal sealed class SilkSkiaApp : IDisposable
    {
        private IWindow window;
        private GRGlInterface? grgInterface;
        private GRContext? grContext;
        private SKSurface? surface;
        private SKCanvas? canvas;
        private GRBackendRenderTarget? renderTarget;

        private readonly PortalEngine Engine;

        public SilkSkiaApp(Arguments arguments)
        {
            SetupWindow();
            Engine = new PortalEngine(arguments);
        }

        public void Run() => window.Run();

        #region IWindow Events

        [MemberNotNull(nameof(grgInterface), nameof(grContext))]
        private void Window_Load()
        {
            grgInterface = GRGlInterface.Create();
            grContext = GRContext.CreateGl(grgInterface);
            
            SetupRenderAndCanvas();
            SetupKeyEvents();

            Engine.StartTheGameLoop(window.Size.X, window.Size.Y);
        }

        private unsafe void Window_Render(double delta)
        {
            ThrowIfNull(canvas);
            ThrowIfNull(grContext);

            grContext.ResetContext(GRBackendState.All);

            void* bgraPtr = Engine.RenderNextFrame();

            if (bgraPtr == null)
            {
                return;
            }

            var info = new SKImageInfo(window.Size.X, window.Size.Y)
            {
                AlphaType = SKAlphaType.Premul,
                ColorType = SKColorType.Bgra8888,
            };

            SKImage image = SKImage.FromPixels(info, (nint)bgraPtr, info.RowBytes);

            // prevent crash due to minimizing/maximizing window
            if (image == null)
            {
                return;
            }

            canvas.DrawImage(image, 0f, 0f, SKSamplingOptions.Default, null);
            canvas.Flush();

            image.Dispose();
        }

        private void Window_Update(double delta)
        {
            Engine.Update();
        }

        private void Window_Resize(Vector2D<int> size)
        {
            StopTheGameLoop();

            SetupRenderAndCanvas();

            StartTheGameLoop();
        }

        private void Window_Closing()
        {
            StopTheGameLoop();
        }

        #endregion

        #region IInputContext Events

        private void OnKeyUp(IKeyboard keyboard, Key key, int arg3)
        {
            Engine.OnKeyUp(key);
        }

        private void OnKeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            Engine.OnKeyDown(key);
        }

        #endregion

        [MemberNotNull(nameof(window))]
        private void SetupWindow()
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Renderer";
            windowOptions.Size = new Vector2D<int>(800, 450);
            windowOptions.FramesPerSecond = 60.0;
            windowOptions.UpdatesPerSecond = 60.0;
            windowOptions.PreferredStencilBufferBits = 8;
            windowOptions.PreferredBitDepth = new Vector4D<int>(8, 8, 8, 8);

            GlfwWindowing.Use(); // ???

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Render += Window_Render;
            window.Update += Window_Update;
            window.Resize += Window_Resize;
            window.Closing += Window_Closing;
        }


        [MemberNotNull(nameof(renderTarget), nameof(surface), nameof(canvas))]
        private void SetupRenderAndCanvas()
        {
            ThrowIfNull(window);

            renderTarget = new GRBackendRenderTarget(window.Size.X, window.Size.Y, 0, 8, new GRGlFramebufferInfo(0, (uint)SizedInternalFormat.Rgba8));
            surface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Bgra8888);
            canvas = surface.Canvas;
        }

        private void SetupKeyEvents()
        {
            IInputContext input = window.CreateInput();

            foreach (var keyboard in input.Keyboards)
            {
                // Subscribe to key events
                keyboard.KeyDown += OnKeyDown;
                keyboard.KeyUp += OnKeyUp;
            }
        }

        private void StopTheGameLoop()
        {
            Engine.StopTheGameLoop();

            canvas?.Dispose();
            surface?.Dispose();
            renderTarget?.Dispose();

            canvas = null;
            surface = null;
            renderTarget = null;
        }

        private void StartTheGameLoop()
        {
            if (window.Size.X == 0 || window.Size.Y == 0)
            {
                return;
            }

            Engine.StartTheGameLoop(window.Size.X, window.Size.Y);
        }

        private static void ThrowIfNull([NotNull] object? obj)
        {
            if (obj is null) throw new Exception("Window Setup Error");
        }

        public void Dispose() => StopTheGameLoop();
    }
}
