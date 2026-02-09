using RenderingEngine.Engine;

namespace RenderingEngine
{
    public sealed unsafe class GameEngineLoop : IDisposable
    {
        private readonly PortalEngine Engine;
        private CancellationTokenSource EngineLoopCancellationToken;
        private void* currentFrame = null;
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
                Sprites = Engine.Sprites,
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
            
            return;

            void TaskBody() 
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    StartRenderingSemaphore.Wait(cancellationToken);
                    currentFrame = renderer.DrawFrame(Engine.GetSnapshot());
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

        public void StartTheGameLoop()
        {
            EngineLoopCancellationToken = new CancellationTokenSource();
            MainEngineLoop(EngineLoopCancellationToken.Token);
        }

        public unsafe void* RenderFrame()
        {
            if (engineLoopTask == null)
            {
                return null;
            }

            _ = StartRenderingSemaphore.Release();
            RenderedFrameSemaphore.Wait();

            return currentFrame;
        }

        public void Dispose()
        {
            StopTheGameLoop();
        }
    }
}
