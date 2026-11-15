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
    }
}