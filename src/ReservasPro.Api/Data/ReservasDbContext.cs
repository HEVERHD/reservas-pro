using Microsoft.EntityFrameworkCore;
using ReservasPro.Domain;

namespace ReservasPro.Api.Data;

public sealed class ReservasDbContext(DbContextOptions<ReservasDbContext> options) : DbContext(options)
{
    public DbSet<Negocio> Negocios => Set<Negocio>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Horario> Horarios => Set<Horario>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Negocio>(n =>
        {
            n.Property(x => x.Nombre).HasMaxLength(120);
            n.Property(x => x.Slug).HasMaxLength(60);
            n.HasIndex(x => x.Slug).IsUnique();
            n.Property(x => x.WhatsApp).HasMaxLength(15);
            n.Property(x => x.ZonaHoraria).HasMaxLength(60);
            n.HasMany(x => x.Servicios).WithOne().HasForeignKey(x => x.NegocioId);
            n.HasMany(x => x.Horarios).WithOne().HasForeignKey(x => x.NegocioId);
        });

        model.Entity<Servicio>(s =>
        {
            s.Property(x => x.Nombre).HasMaxLength(120);
            s.Property(x => x.Precio).HasPrecision(12, 2);
        });

        model.Entity<Horario>().Ignore(x => x.Franja);

        model.Entity<Cita>(c =>
        {
            c.Ignore(x => x.Franja);
            c.Property(x => x.ClienteNombre).HasMaxLength(120);
            c.Property(x => x.ClienteTelefono).HasMaxLength(15);
            c.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            // Un servicio con citas no se puede borrar: se desactiva, y el historial de citas se conserva
            c.HasOne(x => x.Servicio).WithMany().HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
            c.HasOne<Negocio>().WithMany().HasForeignKey(x => x.NegocioId);
            c.HasIndex(x => new { x.NegocioId, x.Fecha }); // la disponibilidad siempre consulta un día de un negocio
        });

        model.Entity<Usuario>(u =>
        {
            u.Property(x => x.Email).HasMaxLength(254);
            u.HasIndex(x => x.Email).IsUnique();
            u.HasOne<Negocio>().WithMany().HasForeignKey(x => x.NegocioId);
        });
    }
}
