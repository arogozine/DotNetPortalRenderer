using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using RenderingEngine;
using RenderingEngine.Models;
using SkiaSharp;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SoftwareRenderer
{
    public class SkiaWindow : GameWindow
    {
        private const int _defaultWidth = 1280;
        private const int _defaultHeight = 720;

        private SKSurface? _surface;
        private SKCanvas? _canvas;

        private GRGlInterface? _grgInterface;
        private GRContext? _grContext;
        private GRBackendRenderTarget? _renderTarget;

        private readonly PortalEngine Engine;

        public SkiaWindow(Arguments arguments, GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
            Engine = new PortalEngine(arguments);
        }

        public static SkiaWindow CreateNew(Arguments arguments)
        {
            return new SkiaWindow(
                arguments,
                new GameWindowSettings
                {
                    UpdateFrequency = 60,
                    Win32SuspendTimerOnDrag = false
                },
                new NativeWindowSettings
                {
                    Title = "Software Renderer",
                    ClientSize = new OpenTK.Mathematics.Vector2i(_defaultWidth, _defaultHeight),
                    Profile = ContextProfile.Core
                }
            );
        }

        [MemberNotNull(nameof(_grgInterface), nameof(_grContext), nameof(_renderTarget), nameof(_surface), nameof(_canvas))]
        protected override void OnLoad()
        {
            base.OnLoad();

            _grgInterface = GRGlInterface.Create();
            _grContext = GRContext.CreateGl(_grgInterface);
            _renderTarget = new GRBackendRenderTarget(ClientSize.X, ClientSize.Y, 0, 8, new GRGlFramebufferInfo(0, (uint)SizedInternalFormat.Rgba8));
            _surface = SKSurface.Create(_grContext, _renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
            _canvas = _surface.Canvas;

            StartTheGameLoop();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
        }

        protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
        {
            StopTheGameLoop();

            _canvas?.Dispose();
            _surface?.Dispose();
            _renderTarget?.Dispose();

            _renderTarget = new GRBackendRenderTarget(ClientSize.X, ClientSize.Y, 0, 8, new GRGlFramebufferInfo(0, (uint)SizedInternalFormat.Rgba8));
            _surface = SKSurface.Create(_grContext, _renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
            _canvas = _surface.Canvas;

            StartTheGameLoop();

            base.OnFramebufferResize(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            Engine.Update();
            base.OnUpdateFrame(args);
        }

        protected unsafe override void OnRenderFrame(FrameEventArgs e)
        {
            Debug.Assert(_canvas != null);
            Debug.Assert(_grContext != null);

            _grContext.ResetContext(GRBackendState.All);

            void* bgraPtr = Engine.RenderNextFrame();

            if (bgraPtr == null)
            {
                return;
            }

            var info = new SKImageInfo(ClientSize.X, ClientSize.Y)
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

            _canvas.DrawImage(image, 0f, 0f, SKSamplingOptions.Default, null);
            _canvas.Flush();

            image.Dispose();

            SwapBuffers();
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
            /*
            if (window.Size.X == 0 || window.Size.Y == 0)
            {
                return;
            }
            */

            Engine.StartTheGameLoop(ClientSize.X, ClientSize.Y);
        }

        private void StopTheGameLoop()
        {
            Engine.StopTheGameLoop();
        }

        protected override void OnUnload()
        {
            StopTheGameLoop();

            _surface?.Dispose();
            _renderTarget?.Dispose();
            _grContext?.Dispose();
            _grgInterface?.Dispose();

            base.OnUnload();
        }
    }
}
