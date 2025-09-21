namespace SoftwareRenderer
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var app = new SilkSkiaApp();
            app.Run();
        }
    }
}
