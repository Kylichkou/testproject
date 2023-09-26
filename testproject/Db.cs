using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace testproject
{
    public class ApplicationContext : DbContext
    {
        public DbSet<RegisteredUser> RegisteredUsers { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Role> Roles { get; set; } // Added this DbSet
        public DbSet<Team> Teams { get; set; } // Added this DbSet

        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>()
                .HasOne(r => r.RegisteredUser)
                .WithMany(u => u.Roles)
                .HasForeignKey(r => r.RegisteredUserId);

            modelBuilder.Entity<Team>()
                .HasOne(t => t.RegisteredUser)
                .WithMany(u => u.Teams)
                .HasForeignKey(t => t.RegisteredUserId);

            modelBuilder.Entity<Person>()
              .HasOne(p => p.RegisteredUser)
              .WithMany(u => u.Persons)
              .HasForeignKey(p => p.RegisteredUserId);
            modelBuilder.Entity<RegisteredUser>().HasAlternateKey(u => u.Email);

            modelBuilder.Entity<Person>()
                .HasOne(p => p.Role)
                .WithMany(r => r.Persons)
                .HasForeignKey(p => p.RoleId);

            modelBuilder.Entity<Person>()
                .HasOne(p => p.Team)
                .WithMany(t => t.Persons)
                .HasForeignKey(p => p.TeamId);

            modelBuilder.Entity<RegisteredUser>().HasData(
                new RegisteredUser { Id = 1, Username = "Tom", Password = "password1", Email = "tom@example.com" },
                new RegisteredUser { Id = 2, Username = "Bob", Password = "password2", Email = "bob@example.com" },
                new RegisteredUser { Id = 3, Username = "Sam", Password = "password3", Email = "sam@example.com" }
            );

            modelBuilder.Entity<Role>().HasData(
         new Role { Id = 1, Name = "Role1", RegisteredUserId = 1 },
         new Role { Id = 2, Name = "Role2", RegisteredUserId = 2 },
         new Role { Id = 3, Name = "Role3", RegisteredUserId = 3 }
     );
            modelBuilder.Entity<Team>().HasData(
    new Team { Id = 1, Name = "Team1", RegisteredUserId = 1 },
    new Team { Id = 2, Name = "Team2", RegisteredUserId = 2 },
    new Team { Id = 3, Name = "Team3", RegisteredUserId = 3 }
);
            modelBuilder.Entity<Person>().HasData(
     new Person { Id = 1, Name = "Person1", Age = 30, RegisteredUserId = 1, RoleId = 1, TeamId = 1 },
     new Person { Id = 6, Name = "Person6", Age = 28, RegisteredUserId = 1, RoleId = 1, TeamId = 1 }, // Изменил значение Name и Age
     new Person { Id = 2, Name = "Person2", Age = 25, RegisteredUserId = 2, RoleId = 2, TeamId = 2 },
     new Person { Id = 3, Name = "Person3", Age = 35, RegisteredUserId = 3, RoleId = 3, TeamId = 3 }
 );
        }
    }
}


public class RegisteredUser
    {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; } // Add this property

        public ICollection<Person> Persons { get; set; }
        public ICollection<Role> Roles { get; set; } // Add this property
        public ICollection<Team> Teams { get; set; } // Add this property
    }



public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public int TeamId { get; set; }
    public int RoleId { get; set; }
    [JsonIgnore] // Добавьте этот атрибут перед свойствами
    public Role Role { get; set; }
    public int RegisteredUserId { get; set; }
    public RegisteredUser RegisteredUser { get; set; }
    [JsonIgnore] // Добавьте этот атрибут перед свойствами
    public Team Team { get; set; }
}
public class AddPersonRequest
{
    public string Name { get; set; }
    public int Age { get; set; }
    public Role Role { get; set; }
    public Team Team { get; set; }
}
public class UpdatePersonRequest
{
    public string Name { get; set; }
    public int Age { get; set; }
    public Role Role { get; set; }
    public Team Team { get; set; }
}

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int RegisteredUserId { get; set; }
    public ICollection<Person> Persons { get; set; } // Добавьте Persons
    public RegisteredUser RegisteredUser { get; set; }
}

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int RegisteredUserId { get; set; }
    public ICollection<Person> Persons { get; set; } // Добавьте Persons
    public RegisteredUser RegisteredUser { get; set; }
}
