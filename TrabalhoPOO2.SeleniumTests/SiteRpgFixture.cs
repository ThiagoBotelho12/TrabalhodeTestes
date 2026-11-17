using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using TrabalhoPOO2.Web;

namespace TrabalhoPOO2.SeleniumTests
{
    // Sobe o site do RPG numa porta livre e abre o Chrome UMA vez para toda a suíte.
    // Variáveis de ambiente opcionais:
    //   SELENIUM_HEADLESS=1   -> roda sem abrir a janela do Chrome
    //   SELENIUM_PAUSA_MS=800 -> pausa entre as ações (bom para gravar o vídeo)
    public class SiteRpgFixture : IAsyncLifetime
    {
        private WebApplication? site;

        public IWebDriver Driver { get; private set; } = null!;
        public string UrlBase { get; private set; } = "";

        public async Task InitializeAsync()
        {
            UrlBase = $"http://127.0.0.1:{PortaLivre()}";
            site = SiteRpg.Criar(Array.Empty<string>(), UrlBase);
            await site.StartAsync();

            var opcoes = new ChromeOptions();
            if (Environment.GetEnvironmentVariable("SELENIUM_HEADLESS") == "1")
                opcoes.AddArgument("--headless=new");
            opcoes.AddArgument("--window-size=1280,1000");
            opcoes.AddArgument("--lang=pt-BR");

            // Opcional: usar um Chrome específico
            string? chrome = Environment.GetEnvironmentVariable("CHROME_BIN");
            if (!string.IsNullOrEmpty(chrome))
                opcoes.BinaryLocation = chrome;

            // Por padrão o Selenium Manager baixa sozinho o ChromeDriver compatível com o Chrome instalado.
            // Se preferir um ChromeDriver já instalado, informe a pasta dele em CHROMEDRIVER_DIR.
            string? pastaDriver = Environment.GetEnvironmentVariable("CHROMEDRIVER_DIR");
            Driver = string.IsNullOrEmpty(pastaDriver)
                ? new ChromeDriver(opcoes)
                : new ChromeDriver(ChromeDriverService.CreateDefaultService(pastaDriver), opcoes);
            // Sem espera implícita: a PaginaRpg espera explicitamente cada página carregar
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
        }

        public async Task DisposeAsync()
        {
            Driver?.Quit();
            if (site != null)
            {
                await site.StopAsync();
                await site.DisposeAsync();
            }
        }

        // Limpa os personagens em memória para cada teste começar do zero
        public void Reiniciar() => Driver.Navigate().GoToUrl(UrlBase + "/reset");

        private static int PortaLivre()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int porta = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return porta;
        }
    }
}
