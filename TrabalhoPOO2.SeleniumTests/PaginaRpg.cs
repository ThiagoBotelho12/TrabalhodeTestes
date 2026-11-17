using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace TrabalhoPOO2.SeleniumTests
{
    // Page Object: concentra os seletores e as ações na tela,
    // para os testes ficarem legíveis e não repetirem código do Selenium.
    public class PaginaRpg
    {
        private readonly IWebDriver driver;
        private readonly string urlBase;
        private readonly int pausaMs;

        public PaginaRpg(IWebDriver driver, string urlBase)
        {
            this.driver = driver;
            this.urlBase = urlBase;
            int.TryParse(Environment.GetEnvironmentVariable("SELENIUM_PAUSA_MS"), out pausaMs);
        }

        // ---------- Ações ----------

        public void AbrirInicio()
        {
            driver.Navigate().GoToUrl(urlBase + "/");
            Pausa();
        }

        public void CriarPersonagem(string nome, string raca, string alinhamento, string nivel, string classe)
        {
            AbrirInicio();
            Preencher("nome", nome);
            Preencher("raca", raca);
            Preencher("alinhamento", alinhamento);
            Preencher("nivel", nivel);
            new SelectElement(driver.FindElement(By.Id("classe"))).SelectByText(classe);
            EnviarEEsperar(driver.FindElement(By.Id("btnCriar")));
        }

        public void AdicionarXp(int xp)
        {
            Preencher("qtdXp", xp.ToString());
            EnviarEEsperar(driver.FindElement(By.Id("btnAdicionarXp")));
        }

        public void AdicionarItem(string tipo, string nome, string peso, string valor,
                                  string slot = "", string bonus = "0", string quantidade = "1", string efeito = "")
        {
            new SelectElement(driver.FindElement(By.Id("tipoItem"))).SelectByText(tipo);
            Preencher("nomeItem", nome);
            Preencher("pesoItem", peso);
            Preencher("valorItem", valor);
            Preencher("slotItem", slot);
            Preencher("bonusItem", bonus);
            Preencher("quantidadeItem", quantidade);
            Preencher("efeitoItem", efeito);
            EnviarEEsperar(driver.FindElement(By.Id("btnAdicionarItem")));
        }

        public void CriarItensExemplo() =>
            EnviarEEsperar(driver.FindElement(By.Id("btnItensExemplo")));

        public void Equipar(string item) =>
            EnviarEEsperar(LinhaDoItem(item).FindElement(By.CssSelector("button.equipar")));

        public void Remover(string item) =>
            EnviarEEsperar(LinhaDoItem(item).FindElement(By.CssSelector("button.remover")));

        public void Desequipar(string slot) =>
            EnviarEEsperar(driver.FindElement(By.CssSelector($"#equipamentos li[data-slot='{slot}'] button.desequipar")));

        // ---------- Leituras ----------

        public string PaginaAtual => driver.FindElement(By.TagName("body")).GetDomAttribute("data-pagina") ?? "";

        public string Mensagem => driver.FindElements(By.Id("mensagem")).FirstOrDefault()?.Text ?? "";

        public bool MensagemDeErro => driver.FindElements(By.CssSelector("#mensagem.erro")).Count > 0;

        public string Texto(string id) => driver.FindElement(By.Id(id)).Text.Trim();

        public bool ItemNoInventario(string item) =>
            driver.FindElements(By.CssSelector($"#tabelaInventario tr[data-item='{item}']")).Count > 0;

        public string ItemEquipadoNoSlot(string slot) =>
            driver.FindElement(By.CssSelector($"#equipamentos li[data-slot='{slot}'] .equipado")).Text.Trim();

        public List<string> PersonagensListados =>
            driver.FindElements(By.CssSelector("#listaPersonagens li[data-nome]"))
                  .Select(li => li.GetDomAttribute("data-nome") ?? "")
                  .ToList();

        // ---------- Apoio ----------

        private IWebElement LinhaDoItem(string item) =>
            driver.FindElement(By.CssSelector($"#tabelaInventario tr[data-item='{item}']"));

        private void Preencher(string id, string valor)
        {
            var campo = driver.FindElement(By.Id(id));
            campo.Clear();
            if (!string.IsNullOrEmpty(valor))
                campo.SendKeys(valor);
        }

        // Clica num botão de formulário e espera a página nova (pós-redirect) terminar de carregar
        private void EnviarEEsperar(IWebElement botao)
        {
            Pausa();
            var corpoAntigo = driver.FindElement(By.TagName("body"));
            botao.Click();

            new WebDriverWait(driver, TimeSpan.FromSeconds(10)).Until(d =>
                PaginaFoiTrocada(corpoAntigo) &&
                ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString() == "complete");
            Pausa();
        }

        private static bool PaginaFoiTrocada(IWebElement corpoAntigo)
        {
            try
            {
                _ = corpoAntigo.Enabled; // ainda é a página antiga
                return false;
            }
            catch (StaleElementReferenceException)
            {
                return true;
            }
        }

        private void Pausa()
        {
            if (pausaMs > 0) Thread.Sleep(pausaMs);
        }
    }
}
