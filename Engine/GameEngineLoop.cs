using RenderingEngine.Engine;
using RenderingEngine.Models;

namespace RenderingEngine
{
    public sealed class GameEngineLoop : IDisposable
    {
        private readonly PortalEngine Engine;
        private CancellationTokenSource EngineLoopCancellationToken;
        private BGRA[]? currentFrame = null;
        private Task? engineLoopTask = null;

        private readonly SemaphoreSlim StartRenderingSemaphore = new(0, 1);
        private readonly SemaphoreSlim RenderedFrameSemaphore = new(0, 1);

        public GameEngineLoop(PortalEngine engine)
        {
            Engine = engine;
            EngineLoopCancellationToken = new();
        }

        [MemberNotNull(nameof(engineLoopTask))]
        private void MainEngineLoop(CancellationToken cancellationToken)
        {
            var renderer = new PortalRenderer(Engine.Width, Engine.Height)
            {
                Player = Engine.Player,
                Sectors = Engine.Sectors
            };

            engineLoopTask = Task.Factory
                .StartNew(TaskBody, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .ContinueWith((Task t) =>
                {
                    Debug.WriteLine(t.Exception);
                    Debugger.Break();
                }, TaskContinuationOptions.OnlyOnFaulted);

            void TaskBody() 
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    StartRenderingSemaphore.Wait(cancellationToken);
                    currentFrame = renderer.DrawFrame(Engine.GetSnapshot());
                    RenderedFrameSemaphore.Release();
                }
            }
        }

        public void StopTheGameLoop()
        {
            EngineLoopCancellationToken.Cancel();
            EngineLoopCancellationToken.Dispose();
            currentFrame = null;
        }

        public void StartTheGameLoop()
        {
            EngineLoopCancellationToken = new CancellationTokenSource();
            MainEngineLoop(EngineLoopCancellationToken.Token);
        }

        public BGRA[]? RenderFrame()
        {
            StartRenderingSemaphore.Release();
            RenderedFrameSemaphore.Wait();
            return currentFrame;
        }

        public void Dispose()
        {
            StopTheGameLoop();
        }
    }
}
