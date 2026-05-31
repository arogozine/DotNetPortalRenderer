using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using RenderingEngine.Engine;
using RenderingEngine.MapLoader;
using SoftwareRendererModels;

namespace RenderingEngine
{
    public sealed class PortalEngine
    {
        public FixedGameState LoadedGameState { get; }
        public RenderableMap RenderableState { get; }
        public PlayerLocation PlayerLocation { get; }
        public GameRenderingThread? Renderer { get; private set; }

        public PortalEngine(Arguments arguments)
        {
            LoadedGameState = GameLoader.LoadFixedGameState(arguments);
            RenderableState = GameRenderStateLoader.GenerateRenderableMap(LoadedGameState);
            PlayerLocation = GameRenderStateLoader.GeneratePlayerLocation(LoadedGameState);
        }

        private readonly HashSet<Keys> PressedKeys = [];

        public void Update(float scale)
        {
            foreach (Keys key in PressedKeys)
            {
                OnKey(key, scale);
            }
        }

        public void OnKeyDown(KeyboardKeyEventArgs keyArg)
        {
            if (!PressedKeys.Add(keyArg.Key))
            {
                _ = PressedKeys.Remove(keyArg.Key);
                return;
            }
        }

        public void OnKeyUp(KeyboardKeyEventArgs keyArg)
        {
            _ = PressedKeys.Remove(keyArg.Key);
        }

        private void OnKey(Keys key, float scale)
        {
            const float moveSpeed = 0.5f;
            const float rotSpeed = 0.08f;

            switch (key)
            {
                case Keys.Up:
                case Keys.W:
                    MoveUpDown(moveSpeed * scale);
                    break;
                case Keys.Down:
                case Keys.S:
                    MoveUpDown(-moveSpeed * scale);
                    break;
                case Keys.Right:
                case Keys.D:
                    Rotate(-rotSpeed * scale);
                    break;
                case Keys.Left:
                case Keys.A:
                    Rotate(rotSpeed * scale);
                    break;
            }
        }

        private void Rotate(float rotSpeed)
        {
            PlayerLocation.Angle += rotSpeed;
            PlayerLocation.Angle = MathFormulas.ClampAngle(PlayerLocation.Angle);

            PlayerMovement.MovePlayer(PlayerLocation, RenderableState.Sectors, 0, 0);
        }

        private void MoveUpDown(float acceleration)
        {
            (float sin, float cos) = MathF.SinCos(PlayerLocation.Angle);
            float moveX = cos * 5.5f * acceleration;
            float moveY = sin * 5.5f * acceleration;

            PlayerMovement.MovePlayer(PlayerLocation, RenderableState.Sectors, moveX, moveY);
        }

        internal PortalPlayerSnapshot PortalPlayerSnapshot()
        {
            return new PortalPlayerSnapshot(
                PlayerLocation.Where,
                PlayerLocation.Velocity,
                PlayerLocation.Angle,
                PlayerLocation.Yaw,
                PlayerLocation.Sector
            );
        }

        public void StopRenderingThread()
        {
            Renderer?.StopTheGameLoop();
            Renderer = null;
        }

        [MemberNotNull(nameof(Renderer))]
        public nint StartRenderingThread(int width, int height)
        {
            Renderer = new GameRenderingThread(this);
            return Renderer.StartTheGameLoop(RenderableState, width, height);
        }
    }
}
