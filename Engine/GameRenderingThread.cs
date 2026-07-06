using RenderingEngine.Engine;
using RenderingEngine.Tooling;
using SoftwareRendererModels;

namespace RenderingEngine
{
    public sealed unsafe class GameRenderingThread : IDisposable
    {
        private readonly PortalEngine Engine;
        private readonly GameResourceType _gameResourceType;
        private CancellationTokenSource EngineLoopCancellationToken;
        private nint* currentFrame = null;
        private Task? engineLoopTask = null;

        private readonly SemaphoreSlim StartRenderingSemaphore = new(0, 1);
        private readonly SemaphoreSlim RenderedFrameSemaphore = new(0, 1);

        public GameRenderingThread(PortalEngine engine, GameResourceType gameResourceType)
        {
            Engine = engine;
            EngineLoopCancellationToken = new();
            _gameResourceType = gameResourceType;
        }

        [MemberNotNull(nameof(engineLoopTask), nameof(currentFrame))]
        private void MainEngineLoop(RenderableMap map, int width, int height, CancellationToken cancellationToken)
        {
            PortalRenderer renderer = _gameResourceType == GameResourceType.Doom ?
                new DoomRenderer(width, height) { Sprites = map.Sprites, Sectors = map.Sectors } :
                new BuildRenderer(width, height) { Sprites = map.Sprites, Sectors = map.Sectors };

            engineLoopTask = Task.Factory
                .StartNew(TaskBody, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .ContinueWith(static (Task t) =>
                {
                    AsyncLogger.Default.AddLog(LogSeverity.Error, "Engine Loop Thread Faulted", t.Exception);
                    Debug.WriteLine(t.Exception);
                    Debugger.Break();
                }, TaskContinuationOptions.OnlyOnFaulted);

            currentFrame = (nint*)renderer.Buffer;

            return;

            void TaskBody() 
            {
                Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

                while (!cancellationToken.IsCancellationRequested)
                {
                    StartRenderingSemaphore.Wait(cancellationToken);
                    _ = renderer.DrawFrame(Engine.PortalPlayerSnapshot());
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

        public void StartTheGameLoop(RenderableMap map, int width, int height)
        {
            EngineLoopCancellationToken = new CancellationTokenSource();
            MainEngineLoop(map, width, height, EngineLoopCancellationToken.Token);
            _ = StartRenderingSemaphore.Release();
        }

        public nint WaitForRenderedFrame()
        {
            RenderedFrameSemaphore.Wait();

            if (engineLoopTask == null)
            {
                return nint.Zero;
            }

            return (nint)currentFrame;
        }

        public void RequestNextFrame()
        {
            _ = StartRenderingSemaphore.Release();
        }

        public void Dispose()
        {
            StopTheGameLoop();
        }
    }
}
