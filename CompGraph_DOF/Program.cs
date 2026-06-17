namespace CompGraph_DOF;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        try
        {
            using var app = new DofApplication();
            app.Run();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
