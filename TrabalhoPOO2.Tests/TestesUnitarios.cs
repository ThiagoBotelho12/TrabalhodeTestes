using System;
using Xunit;

namespace TrabalhoPOO2.Tests
{
    public class TestesUnitarios
    {
        // CT01 - Rejeitar item com peso zero
        [Fact]
        [Trait("Categoria", "Unitario")]
        public void CT01_Item_PesoZero_DeveLancarArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new ItemConsumivel("Poção de Cura", 0, 20, "Restaura 25 HP", 3));

            Assert.Contains("Peso", ex.Message);
        }

        // CT02 - Inventário recusa item acima da capacidade
        [Fact]
        [Trait("Categoria", "Unitario")]
        public void CT02_Inventario_ItemMaisPesadoQueCapacidade_NaoDeveSerAdicionado()
        {
            var inventario = new Inventario(5.0);
            var espada = new ItemArma("Espada Longa", 6.0, 50, "Corte", 5);

            bool adicionou = inventario.AdicionarItem(espada);

            Assert.False(adicionou);
            Assert.Equal(0, inventario.QuantidadeItens);
        }

        // CT03 - XP suficiente faz o personagem subir de nível
        [Fact]
        [Trait("Categoria", "Unitario")]
        public void CT03_Personagem_XpSuficiente_DeveSubirDeNivel()
        {
            var guerreiro = new Guerreiro("Thorin", "Anão", "Leal Bom", 1);

            guerreiro.AdicionarExperiencia(100);

            Assert.Equal(2, guerreiro.Nivel);
            Assert.Equal(0, guerreiro.Experiencia);
        }

        // CT04 - Subir de nível deve manter os itens do inventário (esperado: FALHAR)
        [Fact]
        [Trait("Categoria", "Unitario")]
        public void CT04_Personagem_SubirDeNivel_DeveManterItensDoInventario()
        {
            var guerreiro = new Guerreiro("Thorin", "Anão", "Leal Bom", 1);
            guerreiro.Inventario.AdicionarItem(
                new ItemConsumivel("Poção de Cura", 0.5, 20, "Restaura 25 HP", 3));

            guerreiro.AdicionarExperiencia(100);

            Assert.Equal(1, guerreiro.Inventario.QuantidadeItens);
            Assert.NotNull(guerreiro.Inventario.BuscarItem("Poção de Cura"));
        }

        // CT05 - Equipar item com requisito acentuado "força" (esperado: FALHAR)
        [Fact]
        [Trait("Categoria", "Unitario")]
        public void CT05_Personagem_EquiparItemComRequisitoAcentuado_DeveEquipar()
        {
            var guerreiro = new Guerreiro("Thorin", "Anão", "Leal Bom", 1);
            var lamina = new ItemArma("Lâmina do Dragão", 4.0, 1000, "Fogo", 25, 1, "Lendário");
            lamina.AdicionarRequisito("força");
            guerreiro.Inventario.AdicionarItem(lamina);

            bool equipou = guerreiro.EquiparItem(lamina);

            Assert.True(equipou);
            Assert.Same(lamina, guerreiro.Equipamentos["Arma"]);
        }

        // CT06 - Valor-limite: item com peso exatamente igual à capacidade deve ser aceito
        [Fact]
        [Trait("Categoria", "Unitario")]
        public void CT06_Inventario_ItemComPesoIgualACapacidade_DeveSerAdicionado()
        {
            var inventario = new Inventario(5.0);
            var armadura = new ItemEquipamento("Armadura de Placas", 5.0, 300, "Torso", 6);

            bool adicionou = inventario.AdicionarItem(armadura);

            Assert.True(adicionou);
            Assert.Equal(1, inventario.QuantidadeItens);
            Assert.Equal(5.0, inventario.PesoAtual, 3);
        }

        // CT07 - Usar munição reduz quantidade, peso e valor na mesma proporção
        [Fact]
        [Trait("Categoria", "Unitario")]
        public void CT07_Municao_Usar_DeveReduzirQuantidadePesoEValor()
        {
            // 20 flechas de 0,1 kg e 2 moedas cada: 2,0 kg e 40 no total
            var flechas = new ItemMunicao("Flechas", 0.1, 2, 20, "Flecha");

            bool usou = flechas.Usar(5);

            Assert.True(usou);
            Assert.Equal(15, flechas.Quantidade);
            Assert.Equal(1.5, flechas.Peso, 3);
            Assert.Equal(30m, flechas.Valor);
        }

        // CT08 - Nível fora do intervalo 1..100 deve ser rejeitado (valores-limite 0 e 101)
        [Theory]
        [Trait("Categoria", "Unitario")]
        [InlineData(0)]
        [InlineData(101)]
        public void CT08_Personagem_NivelForaDoIntervalo_DeveLancarPersonagemException(int nivel)
        {
            var ex = Assert.Throws<PersonagemException>(() =>
                new Mago("Gandalf", "Humano", "Neutro Bom", nivel));

            Assert.Contains("Nível deve estar entre 1 e 100", ex.Message);
        }

        // CT09 - Equipar item com slot inexistente deve recusar sem perder o item (esperado: FALHAR)
        [Fact]
        [Trait("Categoria", "Unitario")]
        public void CT09_Personagem_EquiparItemComSlotInexistente_NaoDevePerderItem()
        {
            var guerreiro = new Guerreiro("Thorin", "Anão", "Leal Bom", 1);
            var anel = new ItemEquipamento("Anel de Ouro", 0.1, 100, "Anel", 1);
            guerreiro.Inventario.AdicionarItem(anel);

            bool equipou = true;
            var ex = Record.Exception(() => equipou = guerreiro.EquiparItem(anel));

            Assert.Null(ex);
            Assert.False(equipou);
            Assert.NotNull(guerreiro.Inventario.BuscarItem("Anel de Ouro"));
        }

        // CT10 - Idade deve considerar se o aniversário do ano já passou (esperado: FALHAR)
        [Fact]
        [Trait("Categoria", "Unitario")]
        public void CT10_Personagem_Idade_DeveConsiderarAniversarioDoAno()
        {
            var bardo = new Bardo("Jaskier", "Humano", "Caótico Bom", 1);
            var nascimento = new DateTime(DateTime.Today.Year - 20, 12, 31);
            bardo.DataNascimento = nascimento;

            // Só completa 20 anos em 31/12; antes disso ainda tem 19
            int idadeEsperada = DateTime.Today >= nascimento.AddYears(20) ? 20 : 19;

            Assert.Equal(idadeEsperada, bardo.Idade);
        }
    }
}