using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QRCodeGenerator.Models
{
    public partial class QRDbContextName : DbContext
    {
        public QRDbContextName(DbContextOptions<QRDbContextName> options)
            : base(options)
        {
        }


        //DBSet
        public DbSet<User> Users { get; set; }

 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
