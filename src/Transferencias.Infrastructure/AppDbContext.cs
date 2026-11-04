using Microsoft.EntityFrameworkCore;
using Transferencias.Domain;

namespace Transferencias.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conta>(conta =>
        {
            conta.ToTable("contas");
            conta.Property(c => c.Id).HasColumnName("id");
            conta.Property(c => c.SaldoCentavos).HasColumnName("saldo_centavos");
            conta.HasData(new Conta("A", 100000), new Conta("B", 50000));
        });

        modelBuilder.Entity<Transferencia>(transferencia =>
        {
            transferencia.ToTable("transferencias");
            transferencia.Property(t => t.Id).HasColumnName("id");
            transferencia.Property(t => t.Origem).HasColumnName("origem");
            transferencia.Property(t => t.Destino).HasColumnName("destino");
            transferencia.Property(t => t.ValorCentavos).HasColumnName("valor_centavos");
            transferencia.HasOne<Conta>().WithMany().HasForeignKey(t => t.Origem);
            transferencia.HasOne<Conta>().WithMany().HasForeignKey(t => t.Destino);
        });
    }
}
