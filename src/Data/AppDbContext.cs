using ChamadosTi.Models;
using ChamadosTi.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ChamadosTi.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public DbSet<Setor> Setores { get; set; }
        public DbSet<Especialidade> Especialidades { get; set; }
        public DbSet<DadoTecnico> DadosTecnicos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Prioridade> Prioridades { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Tipo> Tipos { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Item> Itens { get; set; }
        public DbSet<Status> Status { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<ItemTicket> ItensTickets { get; set; }
        public DbSet<AuditoriaTicket> AuditoriasTickets { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ProcessarSoftDelete();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ProcessarSoftDelete();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Setor>(p=>
            {
                p.ToTable("Setores");
                p.HasKey(s => s.Id);

                p.Property(s => s.Nome).HasMaxLength(100).IsRequired();
                p.Property(s => s.Descricao).HasMaxLength(500);
            });
            
            builder.Entity<Especialidade>(p=>
            {
                p.ToTable("Especialidades");
                p.HasKey(e => e.Id);

                p.Property(s => s.Nome).HasMaxLength(100).IsRequired();
                p.Property(s => s.Descricao).HasMaxLength(500);
            });
            
            builder.Entity<DadoTecnico>(p=>
            {
                p.ToTable("DadosTecnicos");
                p.HasKey(d => d.Id);

                p.HasOne(d => d.Especialidades)
                 .WithMany()
                 .HasForeignKey(d => d.EspecialidadeId)
                 .IsRequired();

                p.Property(d => d.Nivel).HasMaxLength(100).IsRequired();
            });
            
            builder.Entity<Usuario>(p=>
            {
                p.ToTable("Usuarios");
                p.HasKey(u => u.Id);

                p.HasOne(u => u.AspNetUser)          
                 .WithOne()                           
                 .HasForeignKey<Usuario>(u => u.AspNetUserId)
                 .IsRequired();

                p.HasOne(u => u.Setores)
                 .WithMany()
                 .HasForeignKey(u => u.SetorId)
                 .IsRequired();

                p.HasOne(u => u.DadosTecnicos)
                 .WithMany()
                 .HasForeignKey(u => u.DadoTecnicoId);

                p.Property(u => u.Nome).HasMaxLength(100).IsRequired(); 
            });
            
            builder.Entity<Prioridade>(p=>
            {
                p.ToTable("Prioridades");
                p.HasKey(p => p.Id);

                p.Property(p => p.Nome).HasMaxLength(100).IsRequired();
            });
            
            builder.Entity<Categoria>(p=>
            {
                p.ToTable("Categorias");
                p.HasKey(c => c.Id);

                p.Property(c => c.Nome).HasMaxLength(100).IsRequired();
                p.Property(c => c.Descricao).HasMaxLength(500);
            });
            
            builder.Entity<Tipo>(p=>
            {
                p.ToTable("Tipos");
                p.HasKey(t => t.Id);

                p.Property(t => t.Nome).HasMaxLength(100).IsRequired();
                p.Property(t => t.Comentario).HasMaxLength(1000); 
            });
            
            builder.Entity<Marca>(p=>
            {
                p.ToTable("Marcas");
                p.HasKey(m => m.Id);

                p.Property(m => m.Nome).HasMaxLength(100).IsRequired();
            });
            
            builder.Entity<Item>(p=>
            {
                p.ToTable("Itens");
                p.HasKey(i => i.Id);

                p.HasOne(i => i.Tipos)
                 .WithMany()
                 .HasForeignKey(i => i.TipoId).IsRequired();
                
                p.HasOne(i => i.Marcas)
                 .WithMany()
                 .HasForeignKey(i => i.MarcaId).IsRequired();
                
                p.HasOne(i => i.Setores)
                 .WithMany()
                 .HasForeignKey(i => i.SetorId);

                p.Property(i => i.Nome).HasMaxLength(100).IsRequired();
                p.Property(i => i.Comentario).HasMaxLength(1000);
            });
            
            builder.Entity<Status>(p=>
            {
                p.ToTable("Status");
                p.HasKey(p => p.Id);

                p.Property(s => s.Nome).HasMaxLength(100).IsRequired();
            });
            
            builder.Entity<Ticket>(p=>
            {
                p.ToTable("Tickets");
                p.HasKey(t => t.Id);
                
                p.HasOne(t => t.Usuarios)
                 .WithMany()
                 .HasForeignKey(t => t.UsuarioId);
                
                p.HasOne(t => t.UsuariosTecnicos)
                 .WithMany()
                 .HasForeignKey(t => t.UsuarioTecnicoId);
                
                p.HasOne(t => t.Setores)
                 .WithMany()
                 .HasForeignKey(t => t.SetorId);
                
                p.HasOne(t => t.Categorias)
                 .WithMany()
                 .HasForeignKey(t => t.CategoriaId);
                
                p.HasOne(t => t.Prioridades)
                 .WithMany()
                 .HasForeignKey(t => t.PrioridadeId);
                
                p.HasOne(t => t.Status)
                 .WithMany()
                 .HasForeignKey(t => t.StatusId);

                p.Property(t => t.Titulo).HasMaxLength(100).IsRequired();
                p.Property(t => t.Comentario).HasMaxLength(1000).IsRequired();  
            });
            
            builder.Entity<ItemTicket>(p=>
            {
                p.ToTable("ItensTickets");
                p.HasKey(x => new { x.ItemId, x.TicketId });
                
                p.HasOne(x => x.Item)
                 .WithMany(x => x.ItensTickets)
                 .HasForeignKey(x => x.ItemId);
                
                p.HasOne(x => x.Ticket)
                 .WithMany(x => x.ItensTickets)
                 .HasForeignKey(x => x.TicketId);

                p.Property(x => x.Comentario).HasMaxLength(1000);
            });
            
            builder.Entity<AuditoriaTicket>(p=>
            {
                p.ToTable("AuditoriasTickets");
                p.HasKey(at=>at.Id);

                p.HasOne(at => at.Ticket)
                 .WithMany()
                 .HasForeignKey(at => at.TicketId);
                
                p.Property(at => at.Log).HasMaxLength(-1);
            });

            AplicaSoftDelete(builder);
        }
    
        private void AplicaSoftDelete(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                                .Property(nameof(ISoftDelete.Ativo))
                                .HasDefaultValue(true);

                    // Cria dinamicamente o filtro: x => x.Ativo == true
                    var parameter = Expression.Parameter(entityType.ClrType, "x");
                    var property = Expression.Property(parameter, nameof(ISoftDelete.Ativo));
                    var condition = Expression.Equal(property, Expression.Constant(true));
                    var lambda = Expression.Lambda(condition, parameter);

                    // Aplica o Filtro Global no EF Core
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                
                    //Desativa o Delete Cascade
                    foreach (var foreignKey in entityType.GetForeignKeys())
                    {
                        foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
                    }
                }
            }    
        }

        private void ProcessarSoftDelete()
        {
            var entradasDeExclusao = ChangeTracker.Entries()
                .Where(x => x.State == EntityState.Deleted && x.Entity is ISoftDelete);

            foreach (var entrada in entradasDeExclusao)
            {
                var entidade = (ISoftDelete)entrada.Entity;
                entrada.State = EntityState.Modified;
                entidade.Ativo = false;
            }
        }
    }
}