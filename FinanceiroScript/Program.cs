using FinanceiroScript.Dominio.Interfaces.Helpers;
using FinanceiroScript.Dominio.Interfaces.Servicos;
using FinanceiroScript.Servicos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                .Build();

            var directoryHelper = host.Services.GetRequiredService<IDiretorioHelper>();
            var logHelper = host.Services.GetRequiredService<LogHelper>();
            logHelper.LogMessage("Program iniciado.");
            var processorService = host.Services.GetRequiredService<INFSeVerificarValidadeNotasServico>();
            processorService.VerificarValidadeNotasFiscais();
        }
    }
}
