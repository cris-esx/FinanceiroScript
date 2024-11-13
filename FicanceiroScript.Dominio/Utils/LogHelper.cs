using FinanceiroScript.Dominio.Interfaces.Helpers;

public class LogHelper
{
    private readonly string logDirectory;
    private readonly IDiretorioHelper _directoryHelper;

    public LogHelper(IDiretorioHelper directoryHelper)
    {
        _directoryHelper = directoryHelper;
        logDirectory = _directoryHelper.GetResultDirectory();
    }

    public void LogMessage(string message)
    {
        string logFilePath = Path.Combine(logDirectory, "Log.txt");

        using (StreamWriter writer = new StreamWriter(logFilePath, append: true))
        {
            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        }
    }
}
