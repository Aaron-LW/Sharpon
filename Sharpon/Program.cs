public static class Program
{
    static void Main(string[] args)
    {
        string fileToOpen = args.Length > 0 ? args[0] : null;
    
        using var game = new Sharpon.Game1(fileToOpen);    
        game.Run();
    
    }
}