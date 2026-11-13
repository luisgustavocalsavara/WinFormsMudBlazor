namespace GustaMudBlazor.Models.Entities
{
    public class Recipe
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Cada receita possui os 4 MIXERS (MIXER 100, MIXER 200, MIXER 300, MIXER 400)
        public List<RecipeMixer> Mixers { get; set; } = [];
    }
}
