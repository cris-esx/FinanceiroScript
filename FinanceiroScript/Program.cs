using FinanceiroScript.Dominio.Interfaces.Helpers;
using FinanceiroScript.Dominio.Interfaces.Servicos;
using FinanceiroScript.Servicos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace FinanceiroScript
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped<INFSeServico, NFSeServico>();
                    services.AddScoped<INFSeVerificarValidadeNotasServico, NFSeVerificarValidadeNotasServico>();
                    services.AddScoped<IDiretorioHelper, DiretorioHelper>();
                    services.AddScoped<LogHelper>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddNLog();
                })
                .Build();

            var directoryHelper = host.Services.GetRequiredService<IDiretorioHelper>();
            var logHelper = host.Services.GetRequiredService<LogHelper>();
            logHelper.LogMessage("Programa iniciado.");

            try
            {
                var validadeNotasServico = host.Services.GetRequiredService<INFSeVerificarValidadeNotasServico>();
                validadeNotasServico.VerificarValidadeNotasFiscais();
            }
            catch (Exception ex)
            {
                logHelper.LogError("Ocorreu um erro", ex);
            }
            finally
            {
                LogManager.Shutdown();
            }
        }
    }
}
