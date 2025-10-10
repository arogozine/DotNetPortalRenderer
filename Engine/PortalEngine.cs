using RenderingEngine.Engine;
using RenderingEngine.MapGen;
using RenderingEngine.Models;
using Silk.NET.Input;

namespace RenderingEngine
{
    public sealed class PortalEngine
    {
        public Player Player { get; private set; }
        internal Sector[] Sectors { get; private set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public PortalEngine()
        {
            (Player, Sectors) = MapLoader.LoadData();
        }

        private readonly HashSet<Key> PressedKeys = [];

        public void Update()
        {
            foreach (Key key in PressedKeys)
            {
                OnKey(key);
            }
        }

        public void OnKeyDown(Key key)
        {
            if (!PressedKeys.Add(key))
            {
                PressedKeys.Remove(key);
                return;
            }
        }

        public void OnKeyUp(Key key)
        {
            PressedKeys.Remove(key);
        }

        private void OnKey(Key key)
        {
            const float moveSpeed = 0.5f;
            const float rotSpeed = 0.08f;

            switch (key)
            {
                case Key.Up:
                case Key.W:
                    MoveUpDown(moveSpeed);
                    break;
                case Key.Down:
                case Key.S:
                    MoveUpDown(-moveSpeed);
                    break;
                case Key.Right:
                case Key.D:
                    Rotate(-rotSpeed);
                    break;
                case Key.Left:
                case Key.A:
                    Rotate(rotSpeed);
                    break;
            }
        }

        private void Rotate(float rotSpeed)
        {
            Player.Angle += rotSpeed;
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
        }

        [MemberNotNull(nameof(mtRenderer))]
        public void StartTheGameLoop(int width, int height)
        {
            Width = width;
            Height = height;
            mtRenderer = new GameEngineLoop(this);
            mtRenderer.StartTheGameLoop();
        }

        public BGRA[]? RenderNextFrame()
        {
            return mtRenderer?.RenderFrame();
        }
    }
}
