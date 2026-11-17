using Xunit;

namespace TrabalhoPOO2.SeleniumTests
{
    // Suíte de testes funcionais automatizados (Selenium WebDriver + Chrome).
    // Cada teste começa com os dados zerados (o construtor roda antes de cada teste).
    public class TestesFuncionaisSelenium : IClassFixture<SiteRpgFixture>
    {
        private readonly PaginaRpg pagina;

        public TestesFuncionaisSelenium(SiteRpgFixture site)
        {
            site.Reiniciar();
            pagina = new PaginaRpg(site.Driver, site.UrlBase);
        }

        // CT11 - Criar personagem com dados válidos
        [Fact]
        [Trait("Categoria", "Selenium")]
        public void CT11_CriarPersonagem_DadosValidos_DeveAbrirFichaEListar()
        {
            pagina.CriarPersonagem("Thorin", "Anão", "Leal Bom", "1", "Guerreiro");

            Assert.Equal("personagem", pagina.PaginaAtual);
            Assert.Contains("Personagem Thorin criado com sucesso", pagina.Mensagem);
            Assert.Equal("Thorin", pagina.Texto("nomePersonagem"));
            Assert.Equal("Guerreiro", pagina.Texto("classePersonagem"));
            Assert.Equal("1", pagina.Texto("nivelPersonagem"));

            pagina.AbrirInicio();
            Assert.Contains("Thorin", pagina.PersonagensListados);
        }

        // CT12 - Criar personagem com nome de 1 caractere
        [Fact]
        [Trait("Categoria", "Selenium")]
        public void CT12_CriarPersonagem_NomeComUmCaractere_DeveExibirErro()
        {
            pagina.CriarPersonagem("T", "Anão", "Leal Bom", "1", "Guerreiro");

            Assert.Equal("inicio", pagina.PaginaAtual);
            Assert.True(pagina.MensagemDeErro);
            Assert.Contains("Nome deve ter pelo menos 2 caracteres", pagina.Mensagem);
            Assert.Equal("0", pagina.Texto("totalPersonagens"));
        }

        // CT13 - Criar personagem com nível acima do limite (101)
        [Fact]
        [Trait("Categoria", "Selenium")]
        public void CT13_CriarPersonagem_NivelAcimaDoLimite_DeveExibirErro()
        {
            pagina.CriarPersonagem("Gandalf", "Humano", "Neutro Bom", "101", "Mago");

            Assert.Equal("inicio", pagina.PaginaAtual);
            Assert.True(pagina.MensagemDeErro);
            Assert.Contains("Nível deve estar entre 1 e 100", pagina.Mensagem);
            Assert.Empty(pagina.PersonagensListados);
        }

        // CT14 - Adicionar XP suficiente para subir de nível
        [Fact]
        [Trait("Categoria", "Selenium")]
        public void CT14_AdicionarXp_Suficiente_DeveSubirDeNivel()
        {
            pagina.CriarPersonagem("Thorin", "Anão", "Leal Bom", "1", "Guerreiro");

            pagina.AdicionarXp(100);

            Assert.Contains("subiu para o nível 2", pagina.Mensagem);
            Assert.Equal("2", pagina.Texto("nivelPersonagem"));
            Assert.Equal("0 / 150", pagina.Texto("xpPersonagem"));
        }

        // CT15 - Adicionar item consumível válido ao inventário
        [Fact]
        [Trait("Categoria", "Selenium")]
        public void CT15_AdicionarItem_ConsumivelValido_DeveAparecerNoInventario()
        {
            pagina.CriarPersonagem("Thorin", "Anão", "Leal Bom", "1", "Guerreiro");

            pagina.AdicionarItem("Consumível", "Elixir de Vida", "0,5", "40",
                                 quantidade: "2", efeito: "Restaura 40 HP");

            Assert.False(pagina.MensagemDeErro);
            Assert.Contains("Elixir de Vida adicionado ao inventário", pagina.Mensagem);
            Assert.True(pagina.ItemNoInventario("Elixir de Vida"));
            Assert.Equal("4", pagina.Texto("qtdItens"));
        }

        // CT16 - Adicionar item mais pesado que a capacidade restante
        [Fact]
        [Trait("Categoria", "Selenium")]
        public void CT16_AdicionarItem_AcimaDaCapacidade_DeveSerRecusado()
        {
            pagina.CriarPersonagem("Thorin", "Anão", "Leal Bom", "1", "Guerreiro"); // 11,5 / 60,0 kg

            pagina.AdicionarItem("Equipamento", "Armadura de Mithril", "60", "5000",
                                 slot: "Torso", bonus: "10");

            Assert.True(pagina.MensagemDeErro);
            Assert.Contains("Não há espaço suficiente", pagina.Mensagem);
            Assert.False(pagina.ItemNoInventario("Armadura de Mithril"));
            Assert.Equal("3", pagina.Texto("qtdItens"));
        }

        // CT17 - Remover item do inventário
        [Fact]
        [Trait("Categoria", "Selenium")]
        public void CT17_RemoverItem_DeveSairDoInventario()
        {
            pagina.CriarPersonagem("Thorin", "Anão", "Leal Bom", "1", "Guerreiro");

            pagina.Remover("Poção de Cura");

            Assert.Contains("Poção de Cura removido do inventário", pagina.Mensagem);
            Assert.False(pagina.ItemNoInventario("Poção de Cura"));
            Assert.Equal("2", pagina.Texto("qtdItens"));
        }

        // CT18 - Equipar e desequipar a arma inicial
        [Fact]
        [Trait("Categoria", "Selenium")]
        public void CT18_EquiparEDesequiparArma_DeveMoverEntreInventarioESlot()
        {
            pagina.CriarPersonagem("Thorin", "Anão", "Leal Bom", "1", "Guerreiro");

            pagina.Equipar("Espada Longa");
            Assert.Contains("Espada Longa equipado no slot Arma", pagina.Mensagem);
            Assert.Equal("Espada Longa", pagina.ItemEquipadoNoSlot("Arma"));
            Assert.False(pagina.ItemNoInventario("Espada Longa"));

            pagina.Desequipar("Arma");
            Assert.Equal("—", pagina.ItemEquipadoNoSlot("Arma"));
            Assert.True(pagina.ItemNoInventario("Espada Longa"));
        }

        // CT19 - Equipar a "Lâmina do Dragão" (requer força e destreza 10) com Guerreiro forte (esperado: FALHAR)
        [Fact]
        [Trait("Categoria", "Selenium")]
        public void CT19_EquiparLaminaDoDragao_GuerreiroComRequisitos_DeveEquipar()
        {
            pagina.CriarPersonagem("Thorin", "Anão", "Leal Bom", "1", "Guerreiro"); // Força 17, Destreza 13
            pagina.CriarItensExemplo();

            pagina.Equipar("Lâmina do Dragão");

            Assert.False(pagina.MensagemDeErro, $"O sistema recusou: {pagina.Mensagem}");
            Assert.Equal("Lâmina do Dragão", pagina.ItemEquipadoNoSlot("Arma"));
        }

        // CT20 - Subir de nível não pode apagar os itens do inventário (esperado: FALHAR)
        [Fact]
        [Trait("Categoria", "Selenium")]
        public void CT20_SubirDeNivel_DeveManterItensDoInventario()
        {
            pagina.CriarPersonagem("Thorin", "Anão", "Leal Bom", "1", "Guerreiro"); // 3 itens iniciais

            pagina.AdicionarXp(100);

            Assert.Equal("2", pagina.Texto("nivelPersonagem"));
            Assert.Equal("3", pagina.Texto("qtdItens"));
            Assert.True(pagina.ItemNoInventario("Espada Longa"));
        }
    }
}
