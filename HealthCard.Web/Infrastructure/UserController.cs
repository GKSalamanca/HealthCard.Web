using System.Security.Claims;
using Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCard.Web.Infrastructure;

[Authorize]
public abstract class UserController(ApplicationDbContext db) : Controller
{
    protected ApplicationDbContext Db => db;
    protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
}
