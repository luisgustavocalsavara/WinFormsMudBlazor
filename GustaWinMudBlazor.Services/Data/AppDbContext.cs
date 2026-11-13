using System.Security.Cryptography;
using System.Text;
using GustaMudBlazor.Models.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GustaMudBlazor.Services.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeMixer> RecipeMixers => Set<RecipeMixer>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
    public DbSet<RecipeBlock> RecipeBlocks => Set<RecipeBlock>();
    public DbSet<RecipeItemValue> RecipeItemValues => Set<RecipeItemValue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Tabela de Usuários
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Role).HasConversion<string>();
        });

        // 2. Tabela de Receitas (1 Receita -> 4 Mixers: MIXER 100, 200, 300, 400)
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("Receitas");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(120);
            entity.Property(r => r.Sku).IsRequired().HasMaxLength(60);

            entity.HasMany(r => r.Mixers)
                  .WithOne(m => m.Recipe)
                  .HasForeignKey(m => m.RecipeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 3. Tabela MIXER (Intermediária entre Receita e os Blocos da Receita)
        modelBuilder.Entity<RecipeMixer>(entity =>
        {
            entity.ToTable("MixersReceita");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.MixerType).HasConversion<string>().IsRequired();
            entity.HasIndex(m => new { m.RecipeId, m.MixerType }).IsUnique();

            entity.HasMany(m => m.RecipeBlocks)
                  .WithOne(rb => rb.RecipeMixer)
                  .HasForeignKey(rb => rb.RecipeMixerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 4. Tabela de Blocos (Cada Bloco pertence exclusivamente a um MixerType)
        modelBuilder.Entity<Block>(entity =>
        {
            entity.ToTable("Blocos");
            entity.HasKey(b => b.Id);
            entity.HasIndex(b => b.Code).IsUnique();
            entity.Property(b => b.MixerType).HasConversion<string>().IsRequired();
            entity.Property(b => b.Code).IsRequired().HasMaxLength(50);
            entity.Property(b => b.Name).IsRequired().HasMaxLength(120);
            entity.Property(b => b.Description).HasMaxLength(250);
            entity.Property(b => b.IconDataUrl);
            entity.Property(b => b.IconFileName).HasMaxLength(120);

            entity.HasMany(b => b.Items)
                  .WithOne(i => i.Block)
                  .HasForeignKey(i => i.BlockId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(b => b.RecipeBlocks)
                  .WithOne(rb => rb.Block)
                  .HasForeignKey(rb => rb.BlockId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 5. Tabela de Itens Estruturais do Bloco (Sem o campo Valor)
        modelBuilder.Entity<RecipeItem>(entity =>
        {
            entity.ToTable("ItensBloco");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Tag).IsRequired().HasMaxLength(80);
            entity.Property(i => i.Description).HasMaxLength(200);
            entity.Property(i => i.DataType).HasConversion<string>();

            entity.HasMany(i => i.ItemValues)
                  .WithOne(iv => iv.RecipeItem)
                  .HasForeignKey(iv => iv.RecipeItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 6. Tabela de Instâncias de Blocos dentro de cada MIXER da Receita
        modelBuilder.Entity<RecipeBlock>(entity =>
        {
            entity.ToTable("ReceitaBlocos");
            entity.HasKey(rb => rb.Id);
            entity.Property(rb => rb.SequenceOrder).IsRequired();

            entity.HasMany(rb => rb.ItemValues)
                  .WithOne(iv => iv.RecipeBlock)
                  .HasForeignKey(iv => iv.RecipeBlockId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 7. Tabela de Valores dos Itens (Associada ao Item e à Instância do Bloco no Mixer)
        modelBuilder.Entity<RecipeItemValue>(entity =>
        {
            entity.ToTable("ValoresItensReceita");
            entity.HasKey(iv => iv.Id);
            entity.Property(iv => iv.Value).IsRequired().HasMaxLength(255);
        });
    }

    public static void InitializeDatabase(IServiceProvider serviceProvider, string defaultIconDataUrl = "")
    {
        using var scope = serviceProvider.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = dbFactory.CreateDbContext();

        // EnsureCreated does not migrate an existing schema; keep the database intact if it is outdated.
        try
        {
            db.Database.EnsureCreated();
            _ = db.RecipeMixers.Any();
            _ = db.Blocks.Select(b => b.MixerType).FirstOrDefault();
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException(
                "O banco SQLite não corresponde ao esquema esperado. Os dados foram preservados; faça backup e aplique uma migração antes de iniciar novamente.",
                ex);
        }

        // Cria usuários iniciais padrão caso não existam
        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new User
                {
                    Username = "admin",
                    FullName = "Administrador do Sistema",
                    PasswordHash = HashPassword("admin123"),
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.Now
                },
                new User
                {
                    Username = "operador",
                    FullName = "Operador de Produção",
                    PasswordHash = HashPassword("op123"),
                    Role = UserRole.Operador,
                    CreatedAt = DateTime.Now
                },
                new User
                {
                    Username = "visualizador",
                    FullName = "Consulta / Qualidade",
                    PasswordHash = HashPassword("vis123"),
                    Role = UserRole.Visualizador,
                    CreatedAt = DateTime.Now
                }
            );
            db.SaveChanges();
        }

        // Cria Blocos exclusivos para cada MIXER (100, 200, 300, 400) e Receitas de exemplo
        if (!db.Blocks.Any())
        {
            var iconFile = string.IsNullOrEmpty(defaultIconDataUrl) ? "" : "favicon.ico";

            // --- BLOCOS EXCLUSIVOS DO MIXER 100 ---
            var m100Vel = new RecipeItem { Tag = "MX100.VELOCIDADE_RPM", Description = "Velocidade Agitador Mixer 100", DataType = ItemDataType.INT };
            var m100Tempo = new RecipeItem { Tag = "MX100.TEMPO_MISTURA_MS", Description = "Tempo de Mistura Mixer 100 (ms)", DataType = ItemDataType.DINT };
            var blk100Mistura = new Block
            {
                MixerType = MixerType.MIXER100,
                Code = "BLK-M100-AGIT",
                Name = "Agitação Primária (Mixer 100)",
                Description = "Controle de rotação e tempo exclusivo do Mixer 100",
                IconDataUrl = defaultIconDataUrl,
                IconFileName = iconFile,
                Items = [m100Vel, m100Tempo]
            };

            var m100Valv = new RecipeItem { Tag = "MX100.ABRE_VALV_AGUA", Description = "Habilita Dosagem de Água", DataType = ItemDataType.BOOL };
            var m100Litros = new RecipeItem { Tag = "MX100.VOLUME_LITROS", Description = "Volume de Água (L)", DataType = ItemDataType.INT };
            var blk100Dosagem = new Block
            {
                MixerType = MixerType.MIXER100,
                Code = "BLK-M100-DOSA",
                Name = "Dosagem Líquida (Mixer 100)",
                Description = "Entrada de líquidos de base do Mixer 100",
                IconDataUrl = defaultIconDataUrl,
                IconFileName = iconFile,
                Items = [m100Valv, m100Litros]
            };

            // --- BLOCOS EXCLUSIVOS DO MIXER 200 ---
            var m200Aquec = new RecipeItem { Tag = "MX200.AQUECIMENTO_ON", Description = "Liga Camisa Térmica Mixer 200", DataType = ItemDataType.BOOL };
            var m200Temp = new RecipeItem { Tag = "MX200.TEMP_SETPOINT_C", Description = "Temperatura Alvo Mixer 200 (°C)", DataType = ItemDataType.INT };
            var blk200Termico = new Block
            {
                MixerType = MixerType.MIXER200,
                Code = "BLK-M200-TERM",
                Name = "Aquecimento Térmico (Mixer 200)",
                Description = "Controle de temperatura exclusivo do Mixer 200",
                IconDataUrl = defaultIconDataUrl,
                IconFileName = iconFile,
                Items = [m200Aquec, m200Temp]
            };

            // --- BLOCOS EXCLUSIVOS DO MIXER 300 ---
            var m300Vacuo = new RecipeItem { Tag = "MX300.PRESSAO_VACUO_MBAR", Description = "Pressão de Vácuo (mbar)", DataType = ItemDataType.INT };
            var m300Homog = new RecipeItem { Tag = "MX300.RPM_HOMOGENEIZADOR", Description = "Rotação do Homogeneizador", DataType = ItemDataType.DINT };
            var blk300Homog = new Block
            {
                MixerType = MixerType.MIXER300,
                Code = "BLK-M300-HOMOG",
                Name = "Homogeneização a Vácuo (Mixer 300)",
                Description = "Bloco de dispersão sob vácuo exclusivo do Mixer 300",
                IconDataUrl = defaultIconDataUrl,
                IconFileName = iconFile,
                Items = [m300Vacuo, m300Homog]
            };

            // --- BLOCOS EXCLUSIVOS DO MIXER 400 ---
            var m400Pressao = new RecipeItem { Tag = "MX400.BOMBA_DESCARGA_BAR", Description = "Pressão de Descarga (bar)", DataType = ItemDataType.INT };
            var m400Lote = new RecipeItem { Tag = "MX400.LOTE_DESTINO", Description = "Identificador do Tanque Final", DataType = ItemDataType.STRING };
            var blk400Descarga = new Block
            {
                MixerType = MixerType.MIXER400,
                Code = "BLK-M400-DESC",
                Name = "Descarga e Transferência (Mixer 400)",
                Description = "Transferência final exclusiva do Mixer 400",
                IconDataUrl = defaultIconDataUrl,
                IconFileName = iconFile,
                Items = [m400Pressao, m400Lote]
            };

            db.Blocks.AddRange(blk100Mistura, blk100Dosagem, blk200Termico, blk300Homog, blk400Descarga);
            db.SaveChanges();

            // Cria Receita de Exemplo já com os 4 MIXERS (MIXER 100, MIXER 200, MIXER 300, MIXER 400)
            var r1 = new Recipe
            {
                Name = "Receita Industrial Completa A1",
                Sku = "SKU-IND-100400",
                CreatedAt = DateTime.Now.AddDays(-1),
                Mixers =
                [
                    new RecipeMixer
                    {
                        MixerType = MixerType.MIXER100,
                        RecipeBlocks =
                        [
                            new RecipeBlock
                            {
                                BlockId = blk100Dosagem.Id,
                                SequenceOrder = 1,
                                ItemValues =
                                [
                                    new RecipeItemValue { RecipeItemId = m100Valv.Id, Value = "True" },
                                    new RecipeItemValue { RecipeItemId = m100Litros.Id, Value = "250" }
                                ]
                            },
                            new RecipeBlock
                            {
                                BlockId = blk100Mistura.Id,
                                SequenceOrder = 2,
                                ItemValues =
                                [
                                    new RecipeItemValue { RecipeItemId = m100Vel.Id, Value = "900" },
                                    new RecipeItemValue { RecipeItemId = m100Tempo.Id, Value = "60000" }
                                ]
                            },
                            new RecipeBlock
                            {
                                BlockId = blk100Mistura.Id,
                                SequenceOrder = 3,
                                ItemValues =
                                [
                                    new RecipeItemValue { RecipeItemId = m100Vel.Id, Value = "1750" },
                                    new RecipeItemValue { RecipeItemId = m100Tempo.Id, Value = "120000" }
                                ]
                            }
                        ]
                    },
                    new RecipeMixer
                    {
                        MixerType = MixerType.MIXER200,
                        RecipeBlocks =
                        [
                            new RecipeBlock
                            {
                                BlockId = blk200Termico.Id,
                                SequenceOrder = 1,
                                ItemValues =
                                [
                                    new RecipeItemValue { RecipeItemId = m200Aquec.Id, Value = "True" },
                                    new RecipeItemValue { RecipeItemId = m200Temp.Id, Value = "85" }
                                ]
                            }
                        ]
                    },
                    new RecipeMixer
                    {
                        MixerType = MixerType.MIXER300,
                        RecipeBlocks =
                        [
                            new RecipeBlock
                            {
                                BlockId = blk300Homog.Id,
                                SequenceOrder = 1,
                                ItemValues =
                                [
                                    new RecipeItemValue { RecipeItemId = m300Vacuo.Id, Value = "150" },
                                    new RecipeItemValue { RecipeItemId = m300Homog.Id, Value = "3600" }
                                ]
                            }
                        ]
                    },
                    new RecipeMixer
                    {
                        MixerType = MixerType.MIXER400,
                        RecipeBlocks =
                        [
                            new RecipeBlock
                            {
                                BlockId = blk400Descarga.Id,
                                SequenceOrder = 1,
                                ItemValues =
                                [
                                    new RecipeItemValue { RecipeItemId = m400Pressao.Id, Value = "6" },
                                    new RecipeItemValue { RecipeItemId = m400Lote.Id, Value = "TQ_ENVASE_01" }
                                ]
                            }
                        ]
                    }
                ]
            };

            db.Recipes.Add(r1);
            db.SaveChanges();
        }
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}
