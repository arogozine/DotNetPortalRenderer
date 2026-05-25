using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using RenderingEngine.Engine;
using RenderingEngine.MapGen;
using RenderingEngine.Models;

namespace RenderingEngine
{
    public sealed class PortalEngine
    {
        public Player Player { get; private set; }

        internal Sector[] Sectors { get; private set; }
        internal RenderableSprite[] Sprites { get; private set; }

        internal Arguments Arguments { get; }

        public int Width { get; set; }

        public int Height { get; set; }

        public PortalEngine(Arguments arguments)
        {
            this.Arguments = arguments;
            (Player, Sectors, Sprites) = MapLoader.LoadData(arguments);
        }

        private readonly HashSet<Keys> PressedKeys = [];

        public void Update()
        {
            foreach (Keys key in PressedKeys)
            {
                OnKey(key);
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

        private void OnKey(Keys key)
        {
            const float moveSpeed = 0.5f;
            const float rotSpeed = 0.08f;

            switch (key)
            {
                case Keys.Up:
                case Keys.W:
                    MoveUpDown(moveSpeed);
                    break;
                case Keys.Down:
                case Keys.S:
                    MoveUpDown(-moveSpeed);
                    break;
                case Keys.Right:
                case Keys.D:
                    Rotate(-rotSpeed);
                    break;
                case Keys.Left:
                case Keys.A:
                    Rotate(rotSpeed);
                    break;
            }
        }

        private void Rotate(float rotSpeed)
        {
            Player.Angle += rotSpeed;
            Player.Angle = MathFormulas.ClampAngle(Player.Angle);

            PlayerMovement.MovePlayer(Player, Sectors, 0, 0);
        }

        private void MoveUpDown(float acceleration)
        {
            (float sin, float cos) = MathF.SinCos(Player.Angle);
            float moveX = cos * 5.5f * acceleration;
            float moveY = sin * 5.5f * acceleration;

            PlayerMovement.MovePlayer(Player, Sectors, moveX, moveY);
        }

        internal PortalPlayerSnapshot GetSnapshot()
        {
            return new PortalPlayerSnapshot(
                Player.Where,
                Player.Velocity,
                Player.Angle,
                Player.Yaw,
                Player.Sector
            );
        }

        private GameEngineLoop? mtRenderer = null;

        public void StopTheGameLoop()
        {
            mtRenderer?.StopTheGameLoop();
            mtRenderer = null;
        }

        [MemberNotNull(nameof(mtRenderer))]
        public void StartTheGameLoop(int width, int height)
        {
            Width = width;
            Height = height;
            mtRenderer = new GameEngineLoop(this);
            mtRenderer.StartTheGameLoop();
        }

        public unsafe void* RenderNextFrame()
        {
            if (mtRenderer == null)
            {
                return (void*)null;
            }

            return mtRenderer.RenderFrame();
        }
    }
}
