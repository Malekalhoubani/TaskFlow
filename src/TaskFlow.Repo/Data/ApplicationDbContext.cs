using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Entities.Users;

namespace TaskFlow.Repo.Data;

    public class ApplicationDbContext: IdentityDbContext<ApplicationUser>
         {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options)
    {
    }
          }