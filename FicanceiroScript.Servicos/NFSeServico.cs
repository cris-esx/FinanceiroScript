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
            _logger.LogInformation("Função listar todas as NFSes em PDF");

            if (!Directory.Exists(nfsePDFsPath))
            {
                _logger.LogError($"O diretório '{nfsePDFsPath}' não foi encontrado.");
                return null;
            }

            string[] pdfFiles = Directory.GetFiles(nfsePDFsPath, "*.pdf");

            if (pdfFiles.Length == 0)
            {
                _logger.LogWarning($"Nenhum arquivo PDF encontrado no diretório '{nfsePDFsPath}'.");
                return null;
            }

            return pdfFiles;
        }

        public NFSe ExtrairDadosNFSeDoPdf(Stream pdfStream)
        {
            var nfseData = new NFSe();
            string pdfText = ObterTextoDoPdfStream(pdfStream);

            ExtractNFSeFields(pdfText, nfseData);
            ExtractPessoaJuridicaData(pdfText, nfseData.Prestador, "Prestador");
            ExtractPessoaJuridicaData(pdfText, nfseData.Tomador, "Tomador");

            return nfseData;
        }

        private void ExtractNFSeFields(string pdfText, NFSe nfseData)
        {
            var fieldKeys = new List<string>
            {
                "ChaveAcesso", "Numero", "DataCompetencia", "DataEmissao",
                "CodigoServico", "DescricaoServico", "StatusImpostoMunicipal",
                "IncidenciaMunicipal", "ValorServico", "ValorLiquidoNotaFiscal"
            };

            foreach (var fieldKey in fieldKeys)
            {
                if (_nfseFieldMappings.TryGetValue(fieldKey, out var fieldConfig))
                {
                    string pattern = fieldConfig["pattern"];
                    string label = fieldConfig["label"];

                    var fieldValue = ExtractFieldFromText(pdfText, "", new List<string> { label }, pattern);
                    nfseData.GetType().GetProperty(fieldKey)?.SetValue(nfseData, fieldValue);
                }
            }
        }

        private void ExtractPessoaJuridicaData(string pdfText, PessoaJuridica pessoa, string entityPrefix)
        {
            var fieldKeys = new List<string> { "Cnpj", "RazaoSocial", "Email", "Endereco", "Municipio", "Cep" };

            foreach (var fieldKey in fieldKeys)
            {
                if (_nfseFieldMappings.TryGetValue(fieldKey, out var fieldConfig))
                {
                    string pattern = fieldConfig["pattern"];
                    string label = fieldConfig["label"];
                    var fieldValue = ExtractFieldFromText(pdfText, entityPrefix, new List<string> { label }, pattern);
                    pessoa.GetType().GetProperty(fieldKey)?.SetValue(pessoa, fieldValue);
                }
            }
        }

        private string? ExtractFieldFromText(string text, string entityPrefix, List<string> labels, string pattern)
        {
            if (!string.IsNullOrEmpty(entityPrefix))
            {
                entityPrefix = $@"(?:{entityPrefix}[\s\S]*?)";
            }

            foreach (var label in labels)
            {
                var fullPattern = $@"(?i){entityPrefix}(?:{label}\s*[:\-]?\s*)[\s\S]*?{pattern}";

                Regex regex = new Regex(fullPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                var match = regex.Match(text);

                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            return null;
        }

        private string ObterTextoDoPdfStream(Stream pdfStream)
        {
            var result = new StringBuilder();
            using var pdfReader = new PdfReader(pdfStream);
            using var pdfDoc = new PdfDocument(pdfReader);
            for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
            {
                var strategy = new SimpleTextExtractionStrategy();
                string content = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page), strategy);
                result.Append(content);
            }
            return result.ToString();
        }

        public string RenomearEMoverNFSePdf(string filePath, NFSe nfse, string destinationDir)
        {
            string newFileName = $"error_" + Path.GetFileName(filePath);

            if (!string.IsNullOrEmpty(nfse?.Numero) && !string.IsNullOrEmpty(nfse?.Prestador?.RazaoSocial))
            {
                string formattedNumero = nfse.Numero.PadLeft(4, '0');

                string formattedRazaoSocial = Regex.Replace(nfse.Prestador.RazaoSocial, @"[^a-zA-Z\s]", "");
                formattedRazaoSocial = Regex.Replace(formattedRazaoSocial.Trim().ToUpper(), @"\s+", "_");

                newFileName = $"{formattedNumero}_{formattedRazaoSocial}.pdf";
            }
            else
            {
                Console.WriteLine("Warning: 'Numero' ou 'RazaoSocial' é nulo ou vazio. Usando o nome original do arquivo.");
            }

            string copyFilePath = Path.Combine(Path.GetDirectoryName(filePath), newFileName);
            File.Copy(filePath, copyFilePath, overwrite: true);

            string newFilePath = Path.Combine(destinationDir, Path.GetFileName(copyFilePath));
            File.Move(copyFilePath, newFilePath);
            return newFilePath;
        }

        private readonly Dictionary<string, Dictionary<string, string>> _nfseFieldMappings = new()
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
