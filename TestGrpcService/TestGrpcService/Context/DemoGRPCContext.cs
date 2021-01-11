using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TestGrpcService.Entires;

#nullable disable

namespace TestGrpcService.Context
{
    public partial class DemoGRPCContext : DbContext
    {
        public DemoGRPCContext()
        {
        }

        public DemoGRPCContext(DbContextOptions<DemoGRPCContext> options)
            : base(options)
        {
        }

        public virtual DbSet<UserLogin> UserLogins { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=mydbinstancelc.cy2aarzpjal1.ap-southeast-1.rds.amazonaws.com;Database=DemoTemplateWebsite;User Id=admin;Password=123123Cong.");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<UserLogin>(entity =>
            {
                entity.ToTable("UserLogin");

                entity.Property(e => e.ID).HasMaxLength(50);

                entity.Property(e => e.ByCreate).HasMaxLength(25);

                entity.Property(e => e.ByModify).HasMaxLength(25);

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.Fullname).HasMaxLength(255);

                entity.Property(e => e.ModifyDate).HasColumnType("datetime");

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.UserName)
                    .IsRequired()
                    .HasMaxLength(25);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
