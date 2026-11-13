namespace GustaMudBlazor.Models.Entities;

public enum UserRole
{
    Visualizador = 0, // Apenas visualiza Home e Receitas (somente leitura)
    Operador = 1,     // Pode criar, editar e excluir Receitas e Itens
    Admin = 2         // Acesso total + Gerenciamento/Cadastro de Usuários
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Operador;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
