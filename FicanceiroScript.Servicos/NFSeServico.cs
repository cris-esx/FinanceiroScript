using System.Text;
using System.Text.RegularExpressions;
using FinanceiroScript.Dominio;
using FinanceiroScript.Dominio.Interfaces.Servicos;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;

namespace FinanceiroScript.Servicos
{
    public class NFSeServico : INFSeServico
    {
        private readonly ILogger<NFSeServico> _logger;

        public NFSeServico(ILogger<NFSeServico> logger)
        {
            _logger = logger;
        }

        public string[]? ObterTodasNFSes(string nfsePDFsPath)
        {
            _logger.LogInformation("Função listar todas as NFSes em PDF iniciada.");

            if (!Directory.Exists(nfsePDFsPath))
            {
                _logger.LogError($"O diretório '{nfsePDFsPath}' não foi encontrado.");
                return null;
            }

            _logger.LogInformation($"Obtendo pdfs do diretorio '{nfsePDFsPath}'.");
            string[] pdfFiles = Directory.GetFiles(nfsePDFsPath, "*.pdf");

            if (pdfFiles.Length == 0)
            {
                _logger.LogWarning($"Nenhum arquivo PDF encontrado.");
                return null;
            }

            return pdfFiles;
        }

        public NFSe ExtrairDadosNFSeDoPdf(Stream pdfStream)
        {
            _logger.LogInformation("Função extrair dados das NFSes dos PDFs iniciada.");
            var nfseData = new NFSe();
            string pdfText = ObterTextoDoPdfStream(pdfStream);

            ExtrairCamposNFSe(pdfText, nfseData);
            ExtrairDadosPessoaJuridica(pdfText, nfseData.Prestador, "Prestador");
            ExtrairDadosPessoaJuridica(pdfText, nfseData.Tomador, "Tomador");

            return nfseData;
        }

        private void ExtrairCamposNFSe(string pdfText, NFSe nfseData)
        {
            var fieldKeys = new List<string>
            {
                "ChaveAcesso", "Numero", "DataCompetencia", "DataEmissao",
                "CodigoServico", "DescricaoServico", "StatusImpostoMunicipal",
                "IncidenciaMunicipal", "ValorServico", "ValorLiquidoNotaFiscal"
            };

            foreach (var fieldKey in fieldKeys)
            {
                if (_mapeamentoComposNFSe.TryGetValue(fieldKey, out var fieldConfig))
                {
                    string pattern = fieldConfig["pattern"];
                    string label = fieldConfig["label"];

                    var fieldValue = ExtrairCampoDoTexto(pdfText, "", new List<string> { label }, pattern);
                    nfseData.GetType().GetProperty(fieldKey)?.SetValue(nfseData, fieldValue);
                }
            }
        }

        private void ExtrairDadosPessoaJuridica(string pdfText, PessoaJuridica pessoa, string prefixoPJ)
        {
            var fieldKeys = new List<string> { "Cnpj", "RazaoSocial", "Email", "Endereco", "Municipio", "Cep" };

            foreach (var fieldKey in fieldKeys)
            {
                if (_mapeamentoComposNFSe.TryGetValue(fieldKey, out var fieldConfig))
                {
                    string padraoValorRegex = fieldConfig["pattern"];
                    string label = fieldConfig["label"];
                    var valorCampo = ExtrairCampoDoTexto(pdfText, prefixoPJ, new List<string> { label }, padraoValorRegex);
                    pessoa.GetType().GetProperty(fieldKey)?.SetValue(pessoa, valorCampo);
                }
            }
        }

        private string? ExtrairCampoDoTexto(string texto, string prefixoPJ, List<string> labels, string padraoValorRegex)
        {
            if (!string.IsNullOrEmpty(prefixoPJ))
            {
                prefixoPJ = $@"(?:{prefixoPJ}[\s\S]*?)";
            }

            foreach (var label in labels)
            {
                var padraoRegexCompleto = $@"(?i){prefixoPJ}(?:{label}\s*[:\-]?\s*)[\s\S]*?{padraoValorRegex}";

                Regex regex = new Regex(padraoRegexCompleto, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                var match = regex.Match(texto);

                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            return null;
        }

        private string ObterTextoDoPdfStream(Stream pdfStream)
        {
            _logger.LogInformation("Função obter texto do pdf iniciada.");
            var resultado = new StringBuilder();
            using var pdfReader = new PdfReader(pdfStream);
            using var pdfDoc = new PdfDocument(pdfReader);
            for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
            {
                var estrategia = new SimpleTextExtractionStrategy();
                string content = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), estrategia);
                resultado.Append(content);
            }
            return resultado.ToString();
        }

        public string RenomearEMoverNFSePdf(string caminhoArquivo, NFSe nfse, string dirDestino)
        {
            _logger.LogInformation("Função renomear arquivo iniciada.");
            string novoNomeArquivo = $"error_" + Path.GetFileName(caminhoArquivo);

            if (!string.IsNullOrEmpty(nfse?.Numero) && !string.IsNullOrEmpty(nfse?.Prestador?.RazaoSocial))
            {
                string numeroFormatado = nfse.Numero.PadLeft(4, '0');

                string razaoSocialFormatada = Regex.Replace(nfse.Prestador.RazaoSocial, @"[^a-zA-Z\s]", "");
                razaoSocialFormatada = Regex.Replace(razaoSocialFormatada.Trim().ToUpper(), @"\s+", "_");

                novoNomeArquivo = $"{numeroFormatado}_{razaoSocialFormatada}.pdf";
            }
            else
            {
                _logger.LogError("'Numero' ou 'RazaoSocial' é nulo ou vazio.");
            }

            _logger.LogInformation($"Arquivo renomeado.");

            string caminhoArquivoCopia = Path.Combine(Path.GetDirectoryName(caminhoArquivo), novoNomeArquivo);
            File.Copy(caminhoArquivo, caminhoArquivoCopia, overwrite: true);

            string novoCaminhoArquivo = Path.Combine(dirDestino, Path.GetFileName(caminhoArquivoCopia));
            File.Move(caminhoArquivoCopia, novoCaminhoArquivo);

            _logger.LogInformation($"Arquivo movido.");
            return novoCaminhoArquivo;
        }

        private readonly Dictionary<string, Dictionary<string, string>> _mapeamentoComposNFSe = new()
        {
            { "ChaveAcesso", new Dictionary<string, string>
                {
                    { "label", "Chave de Acesso da NFS-e" },
                    { "pattern", @"([\d]+)" }
                }
            },
            { "Numero", new Dictionary<string, string>
                {
                    { "label", "Número da NFS-e" },
                    { "pattern", @"(\d+)" }
                }
            },
            { "DataCompetencia", new Dictionary<string, string>
                {
                    { "label", "Competência da NFS-e" },
                    { "pattern", @"([\d/]+)" }
                }
            },
            { "DataEmissao", new Dictionary<string, string>
                {
                    { "label", "Data e Hora da emissão" },
                    { "pattern", @"([\d/]+ \d{2}:\d{2}:\d{2})" }
                }
            },
            { "CodigoServico", new Dictionary<string, string>
                {
                    { "label", "Código de Tributação Nacional" },
                    { "pattern", @"([\d.]+)" }
                }
            },
            { "DescricaoServico", new Dictionary<string, string>
                {
                    { "label", "Descrição do Serviço" },
                    { "pattern", @"([^\n]+)" }
                }
            },
            { "StatusImpostoMunicipal", new Dictionary<string, string>
                {
                    { "label", "Tributação do ISSQN" },
                    { "pattern", @"([^\n]+)" }
                }
            },
            { "IncidenciaMunicipal", new Dictionary<string, string>
                {
                    { "label", "Município de Incidência do ISSQN" },
                    { "pattern", @"([^\n]+)" }
                }
            },
            { "ValorServico", new Dictionary<string, string>
                {
                    { "label", "Valor do Serviço" },
                    { "pattern", @"R\$\s*([\d,\.]+)" }
                }
            },
            { "ValorLiquidoNotaFiscal", new Dictionary<string, string>
                {
                    { "label", "Valor Líquido da NFS-e" },
                    { "pattern", @"R\$\s*([\d.,]+)" }
                }
            },
            { "Cnpj", new Dictionary<string, string>
                {
                    { "label", @"CNPJ" },
                    { "pattern", @"(\d{2}\.?(\d{3}\.?){2}([/])?\d{4}-?\d{2})" }
                }
            },
            { "RazaoSocial", new Dictionary<string, string>
                {
                    { "label", @"(?:Nome\s*)(?:Empresarial|Raz[aã]o\s*Social)" },
                    { "pattern", @"(.+?)\n" }
                }
            },
            { "Email", new Dictionary<string, string>
                {
                    { "label", "E[-]?mail" },
                    { "pattern", @"\s*([^\n]*)" }
                }
            },
            { "Endereco", new Dictionary<string, string>
                {
                    { "label", "Endereço" },
                    { "pattern", @"([^\n]+)" }
                }
            },
            { "Municipio", new Dictionary<string, string>
                {
                    { "label", "Município" },
                    { "pattern", @"([^\n]+)" }
                }
            },
            { "Cep", new Dictionary<string, string>
                {
                    { "label", "CEP" },
                    { "pattern", @"([\d-]+)" }
                }
            }
        };

    }
}
