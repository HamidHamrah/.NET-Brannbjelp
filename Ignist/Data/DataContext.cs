using System;
using Microsoft.EntityFrameworkCore;
using Ignist.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Ignist.Data
{
    public class DataContext : IdentityDbContext
	{
		public DataContext(DbContextOptions<DataContext> options) : base(options)
		{
			Database.EnsureCreated();
		}
		public DbSet<publications> publications { get; set; }
	}
}

