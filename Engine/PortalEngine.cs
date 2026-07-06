using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using RenderingEngine.Engine;
using RenderingEngine.MapLoader;
using RenderingEngine.Tooling;
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
            const float moveSpeed = 0.5f;
            const float rotSpeed = 0.08f;

            bool shiftHeld = PressedKeys.Contains(Keys.LeftShift) || PressedKeys.Contains(Keys.RightShift);
            float speedMultiplier = shiftHeld ? 2f : 1f;

            if (PressedKeys.Contains(Keys.Up) || PressedKeys.Contains(Keys.W))
            {
                MoveUpDown(moveSpeed * scale * speedMultiplier);
            }

            if (PressedKeys.Contains(Keys.Down) || PressedKeys.Contains(Keys.S))
            {
                MoveUpDown(-moveSpeed * scale * speedMultiplier);
            }

            if (PressedKeys.Contains(Keys.Right) || PressedKeys.Contains(Keys.D))
            {
                Rotate(-rotSpeed * scale * speedMultiplier);
            }

            if (PressedKeys.Contains(Keys.Left) || PressedKeys.Contains(Keys.A))
            {
                Rotate(rotSpeed * scale * speedMultiplier);
            }
        }

        public void OnKeyDown(KeyboardKeyEventArgs keyArg)
        {
            _ = PressedKeys.Add(keyArg.Key);
        }

        public void OnKeyUp(KeyboardKeyEventArgs keyArg)
        {
            _ = PressedKeys.Remove(keyArg.Key);
        }

        private void Rotate(float rotSpeed)
        {
            PlayerLocation.Angle += rotSpeed;
            PlayerLocation.Angle = MathFormulas.ClampAngle(PlayerLocation.Angle);

            PlayerMovement.MovePlayer(PlayerLocation, RenderableState, 0, 0);
        }

        private void MoveUpDown(float acceleration)
        {
            (float sin, float cos) = MathF.SinCos(PlayerLocation.Angle);
            float moveX = cos * 5.5f * acceleration;
            float moveY = sin * 5.5f * acceleration;

            PlayerMovement.MovePlayer(PlayerLocation, RenderableState, moveX, moveY);
        }

        internal PortalPlayerSnapshot PortalPlayerSnapshot()
        {
            (float x, float y, float z) = PlayerLocation.Where;
            (float sin, float cos) = MathF.SinCos(PlayerLocation.Angle);

            PortalPlayerSnapshot snapShot = ObjectPool.PortalPlayerSnapshot;

            snapShot.Sin = sin;
            snapShot.Cos = cos;
            snapShot.X = x;
            snapShot.Y = y;
            snapShot.Z = z;
            snapShot.Velocity = PlayerLocation.Velocity;
            snapShot.Angle = PlayerLocation.Angle;
            snapShot.Sector = PlayerLocation.Sector;
            snapShot.Yaw = PlayerLocation.Yaw;

            return snapShot;
        }

        public void StopRenderingThread()
        {
            Renderer?.StopTheGameLoop();
            Renderer = null;
        }

        [MemberNotNull(nameof(Renderer))]
        public void StartRenderingThread(int width, int height)
        {
            Renderer = new GameRenderingThread(this, LoadedGameState.ResourceType);
            Renderer.StartTheGameLoop(RenderableState, width, height);
        }
    }
}
