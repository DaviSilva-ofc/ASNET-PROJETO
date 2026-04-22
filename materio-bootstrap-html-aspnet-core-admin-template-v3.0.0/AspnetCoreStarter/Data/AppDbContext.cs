using Microsoft.EntityFrameworkCore;
using AspnetCoreStarter.Models;

namespace AspnetCoreStarter.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<School> Schools { get; set; }
        public DbSet<Agrupamento> Agrupamentos { get; set; }
        public DbSet<Bloco> Blocos { get; set; }
        public DbSet<Sala> Salas { get; set; }
        public DbSet<Equipamento> Equipamentos { get; set; }
        public DbSet<Administrador> Administradores { get; set; }
        public DbSet<Diretor> Diretores { get; set; }
        public DbSet<Tecnico> Tecnicos { get; set; }
        public DbSet<Coordenador> Coordenadores { get; set; }
        public DbSet<Professor> Professores { get; set; }
        public DbSet<Contrato> Contratos { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<StockEmpresa> StockEmpresa { get; set; }
        public DbSet<StockTecnico> StockTecnico { get; set; }
        public DbSet<Mensagem> Mensagens { get; set; }

        // New tables from schema sync
        public DbSet<ComponenteEquipamento> ComponentesEquipamentos { get; set; }
        public DbSet<StatusEquipamento> StatusEquipamentos { get; set; }
        public DbSet<Historico> Historico { get; set; }
        public DbSet<Reparo> Reparos { get; set; }
        public DbSet<Emprestimo> Emprestimos { get; set; }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<PedidoStock> PedidosStock { get; set; }
        public DbSet<Departamento> Departamentos { get; set; }
        public DbSet<Setor> Setores { get; set; }
        public DbSet<TechnicianActivity> TechnicianActivities { get; set; }
        public DbSet<TicketHistorico> TicketHistorico { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── GLOBAL SOFT DELETE QUERY FILTERS ──────────────────────────────────
            // Any entity implementing ISoftDeletable will automatically be filtered.
            // Queries will only return records where IsDeleted == false.
            modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<School>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Agrupamento>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Bloco>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Sala>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Equipamento>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Ticket>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<StockEmpresa>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Empresa>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Contrato>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Emprestimo>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Reparo>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<PedidoStock>().HasQueryFilter(e => !e.IsDeleted);
        }

        // ── SOFT DELETE INTERCEPTOR ────────────────────────────────────────────────
        // Overrides SaveChangesAsync to intercept any Delete state on ISoftDeletable
        // entities and convert it to a soft delete instead of a physical row removal.
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<ISoftDeletable>()
                         .Where(e => e.State == EntityState.Deleted))
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            foreach (var entry in ChangeTracker.Entries<ISoftDeletable>()
                         .Where(e => e.State == EntityState.Deleted))
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
            }

            return base.SaveChanges();
        }
    }
}
