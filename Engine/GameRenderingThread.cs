using RenderingEngine.Engine;
using SoftwareRendererModels;

namespace RenderingEngine
{
    public sealed unsafe class GameRenderingThread : IDisposable
    {
        private readonly PortalEngine Engine;
        private CancellationTokenSource EngineLoopCancellationToken;
        private void* currentFrame = null;
        private Task? engineLoopTask = null;

        private readonly SemaphoreSlim StartRenderingSemaphore = new(0, 1);
        private readonly SemaphoreSlim RenderedFrameSemaphore = new(0, 1);

        public GameRenderingThread(PortalEngine engine)
        {
            Engine = engine;
            EngineLoopCancellationToken = new();
        }

        [MemberNotNull(nameof(engineLoopTask))]
        private nint MainEngineLoop(RenderableMap map, int width, int height, CancellationToken cancellationToken)
        {
            var renderer = new PortalRenderer(width, height)
            {
                Sprites = map.Sprites,
                Sectors = map.Sectors
            };

            engineLoopTask = Task.Factory
                .StartNew(TaskBody, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .ContinueWith(static (Task t) =>
                {
                    Debug.WriteLine(t.Exception);
                    Debugger.Break();
                }, TaskContinuationOptions.OnlyOnFaulted);
            
            return (nint)renderer.Buffer;

            void TaskBody() 
            {
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

                while (!cancellationToken.IsCancellationRequested)
                {
                    StartRenderingSemaphore.Wait(cancellationToken);
                    currentFrame = renderer.DrawFrame(Engine.PortalPlayerSnapshot());
                    _ = RenderedFrameSemaphore.Release();
                }
            }
        }

        public void StopTheGameLoop()
        {
            EngineLoopCancellationToken.Cancel();
            EngineLoopCancellationToken.Dispose();
            currentFrame = null;
        }

        public nint StartTheGameLoop(RenderableMap map, int width, int height)
        {
            EngineLoopCancellationToken = new CancellationTokenSource();
            return MainEngineLoop(map, width, height, EngineLoopCancellationToken.Token);
        }

        public nint RenderFrame()
        {
            if (engineLoopTask == null)
            {
                return nint.Zero;
            }

            _ = StartRenderingSemaphore.Release();
            RenderedFrameSemaphore.Wait();

            return (nint)currentFrame;
        }

        public void Dispose()
        {
            StopTheGameLoop();
        }
    }
}
