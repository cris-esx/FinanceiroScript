using FicanceiroScript.Dominio.Interfaces.Helpers;
using FinanceiroScript.Dominio.Interfaces.Helpers;
using NLog;
using NLog.Config;
using NLog.Targets;

public class LogHelper : ILogHelper
{
    private readonly IDiretorioHelper _diretorioHelper;
    private static ILogger _logger;

    public LogHelper(IDiretorioHelper diretorioHelper)
    {
        _diretorioHelper = diretorioHelper;
        ConfigureLogging();
    }

    private void ConfigureLogging()
    {
        string diretorioLog = _diretorioHelper.ObterDiretorioResultado();

        if (!Directory.Exists(diretorioLog))
        {
            Directory.CreateDirectory(diretorioLog);
        }

        var config = new LoggingConfiguration();
        var fileTarget = new FileTarget("logfile")
        {
            FileName = Path.Combine(diretorioLog, "log.txt"),
            Layout = "${longdate} ${level} ${message} ${exception}",
            KeepFileOpen = true,
            MaxArchiveFiles = 5
        };

        var consoleTarget = new ConsoleTarget("logconsole")
        {
            Layout = "${longdate} ${level} ${message} ${exception}",
        };

        config.AddTarget(fileTarget);
        config.AddTarget(consoleTarget);

        config.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget);
        config.AddRule(LogLevel.Info, LogLevel.Fatal, consoleTarget);
        LogManager.Configuration = config;

        _logger = LogManager.GetCurrentClassLogger();
    }

    public void LogMensagem(string mensagem)
    {
        _logger.Info(mensagem);
    }
}
