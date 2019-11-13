using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static CashDesk.Model.Model;

namespace CashDesk.Data
{
    class CashDeskDbContext : DbContext
    {
        public DbSet<Member> Members { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<Deposit> Deposits { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Member>()
                .HasIndex(m => m.LastName)
                .IsUnique();

            modelBuilder.Entity<Member>()
               .HasMany(m => m.Memberships)
               .WithOne(m => m.Member)
               .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Membership>()
               .HasMany(m => m.Deposits)
               .WithOne(m => m.Membership)
               .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
