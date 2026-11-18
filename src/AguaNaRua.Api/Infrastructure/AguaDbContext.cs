using AguaNaRua.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AguaNaRua.Api.Infrastructure;

public sealed class AguaDbContext(DbContextOptions<AguaDbContext> options) : DbContext(options)
{
    public DbSet<PontoMonitorado> Pontos => Set<PontoMonitorado>();
    public DbSet<Observacao> Observacoes => Set<Observacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var ponto = modelBuilder.Entity<PontoMonitorado>();
        ponto.ToTable("PontosMonitorados", t =>
        {
            t.HasCheckConstraint("CK_Ponto_Latitude", "Latitude BETWEEN -90 AND 90");
            t.HasCheckConstraint("CK_Ponto_Longitude", "Longitude BETWEEN -180 AND 180");
        });
        ponto.HasKey(x => x.Id);
        ponto.Property(x => x.Nome).HasMaxLength(160).IsRequired();
        ponto.Property(x => x.Bairro).HasMaxLength(100).IsRequired();
        ponto.HasMany(x => x.Observacoes).WithOne(x => x.PontoMonitorado)
            .HasForeignKey(x => x.PontoMonitoradoId).OnDelete(DeleteBehavior.Restrict).IsRequired();
        ponto.Navigation(x => x.Observacoes).UsePropertyAccessMode(PropertyAccessMode.Field);

        var observacao = modelBuilder.Entity<Observacao>();
        observacao.ToTable("Observacoes", t =>
        {
            t.HasCheckConstraint("CK_Observacao_Condicao", "Condicao IN ('SEM_AGUA_ACUMULADA_OBSERVADA', 'AGUA_ACUMULADA', 'ALAGAMENTO_RELATADO')");
            t.HasCheckConstraint("CK_Observacao_Origem", "Origem IN ('RELATO_CIDADAO', 'DEMONSTRACAO')");
            t.HasCheckConstraint("CK_Observacao_Horarios", "ObservadoEm <= RegistradoEm");
        });
        observacao.HasKey(x => x.Id);
        observacao.Property(x => x.Condicao).HasConversion<string>().HasMaxLength(40).IsRequired();
        observacao.Property(x => x.Origem).HasConversion<string>().HasMaxLength(24).IsRequired();
        // INTEGER UTC ticks evita a limitação de ORDER BY de DateTimeOffset no SQLite.
        var utcTicks = new ValueConverter<DateTimeOffset, long>(
            value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));
        observacao.Property(x => x.ObservadoEm).HasConversion(utcTicks).IsRequired();
        observacao.Property(x => x.RegistradoEm).HasConversion(utcTicks).IsRequired();
        observacao.HasIndex(x => new { x.PontoMonitoradoId, x.ObservadoEm, x.RegistradoEm, x.Id });
    }
}
