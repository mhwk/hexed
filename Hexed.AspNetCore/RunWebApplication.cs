using Microsoft.AspNetCore.Builder;
using System.Threading.Tasks;

namespace Hexed.AspNetCore;

public delegate Task<int> RunWebApplication(WebApplication app, string[] args);