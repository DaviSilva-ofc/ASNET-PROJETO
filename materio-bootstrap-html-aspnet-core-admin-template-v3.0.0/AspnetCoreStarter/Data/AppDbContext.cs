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
    }
}
