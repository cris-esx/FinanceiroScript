using FinanceiroScript.Dominio.Interfaces.Helpers;
using NLog;
using NLog.Config;
using NLog.Targets;

public class LogHelper
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

        LogManager.Configuration.Variables["diretorioLog"] = diretorioLog;

        string caminhoArquivoConfigNLog = Path.Combine(AppContext.BaseDirectory, "NLog.config");
        Console.WriteLine($"Carregando configuracao NLog de: {caminhoArquivoConfigNLog}");

        if (!File.Exists(caminhoArquivoConfigNLog))
        {
            Console.WriteLine("Arquivo de config NLog nao encontrado. Usando a configuração padrão no LogHelper.");

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
        }
        else
        {
            LogManager.Setup().LoadConfigurationFromFile(caminhoArquivoConfigNLog);
        }

        _logger = LogManager.GetCurrentClassLogger();
    }

    public void LogMessage(string message)
    {
        _logger.Info(message);
    }

    public void LogError(string message, Exception ex)
    {
        _logger.Error(ex, message);
    }
}
