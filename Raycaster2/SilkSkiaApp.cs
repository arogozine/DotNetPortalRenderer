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
    internal sealed class SilkSkiaApp : IDisposable
    {
        private IWindow window;
        private GRGlInterface? grgInterface;
        private GRContext? grContext;
        private SKSurface? surface;
        private SKCanvas? canvas;
        private GRBackendRenderTarget? renderTarget;

        private readonly PortalEngine Engine;

        public SilkSkiaApp()
        {
            SetupWindow();
            Engine = new PortalEngine();
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

            BGRA[]? bytes = Engine.RenderNextFrame();

            if (bytes is null || bytes.Length == 0)
            {
                return;
            }

            // we avoid using bitmap as it internally creates an SKImage
            // and copies needlessly
            SKImage image;
            fixed (BGRA* bgraPtr = &bytes[0])
            {
                var info = new SKImageInfo(window.Size.X, window.Size.Y)
                {
                    AlphaType = SKAlphaType.Premul,
                    ColorType = SKColorType.Bgra8888,
                };

                image = SKImage.FromPixels(info, (nint)bgraPtr, info.RowBytes);
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

            renderTarget?.Dispose();
            surface?.Dispose();
        }

        private void StartTheGameLoop()
        {
            Engine.StartTheGameLoop(window.Size.X, window.Size.Y);
        }

        private static void ThrowIfNull([NotNull] object? obj)
        {
            if (obj is null) throw new Exception("Window Setup Error");
        }

        public void Dispose() => StopTheGameLoop();
    }
}
