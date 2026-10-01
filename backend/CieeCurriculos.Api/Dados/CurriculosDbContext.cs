using CieeCurriculos.Api.Candidatos;
using Microsoft.EntityFrameworkCore;

namespace CieeCurriculos.Api.Dados;

public class CurriculosDbContext : DbContext
{
    public CurriculosDbContext(DbContextOptions<CurriculosDbContext> opcoes) : base(opcoes)
    {
    }

    public DbSet<Candidato> Candidatos => Set<Candidato>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Candidato>(candidato =>
        {
            candidato.Property(c => c.NomeCompleto).HasMaxLength(200);
            candidato.Property(c => c.Email).HasMaxLength(256);
            candidato.Property(c => c.Telefone).HasMaxLength(20);
            candidato.Property(c => c.AreaOuCargoDeInteresse).HasMaxLength(120);
            candidato.Property(c => c.ResumoProfissional).HasMaxLength(2000);

            // datetime2 não guarda fuso. A data é sempre gravada em UTC, então ao
            // ler marcamos o valor como UTC; sem isso a API o devolveria sem o "Z"
            // e o navegador o interpretaria como horário local.
            candidato.Property(c => c.DataCadastro)
                .HasColumnType("datetime2")
                .HasConversion(valor => valor, valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));

            // Rede de segurança contra cadastro duplicado, inclusive em condição de corrida.
            candidato.HasIndex(c => c.Email).IsUnique();
        });
    }
}
