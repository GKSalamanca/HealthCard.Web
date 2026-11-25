using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using HealthCard.Services;
using HealthCard.Web.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Models.ParameterBase;
using Models.UserSettings;
using Npgsql;
using Services;

if (args.Length > 0 && args[0] is "--serve" or "--drop")
{
    var database = args.ElementAtOrDefault(1) ?? throw new ArgumentException("Укажите имя тестовой базы");
    if (!Regex.IsMatch(database, "^HealthCardCodexQA_[a-zA-Z0-9_]+$")) throw new ArgumentException("Только отдельные тестовые базы HealthCardCodexQA_...");
    var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    if (string.IsNullOrEmpty(connection))
    {
        var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", "83599704-ef9f-400d-bd2a-2e0181536a4b", "secrets.json");
        if (!File.Exists(file)) file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Roaming", "Microsoft", "UserSecrets", "83599704-ef9f-400d-bd2a-2e0181536a4b", "secrets.json");
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(file));
        connection = json.RootElement.TryGetProperty("ConnectionStrings:DefaultConnection", out var flat) ? flat.GetString() : json.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
    }
    var builder = new NpgsqlConnectionStringBuilder(connection);
    if (builder.Database == database) throw new InvalidOperationException("Тестовая база должна отличаться от базы приложения");
    builder.Database = "postgres";
    await using (var admin = new NpgsqlConnection(builder.ConnectionString))
    {
        await admin.OpenAsync();
        if (args[0] == "--drop")
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync(); Console.WriteLine("Separate QA database removed"); return;
        }
        await using var exists = new NpgsqlCommand("SELECT COUNT(*) FROM pg_database WHERE datname=@name", admin); exists.Parameters.AddWithValue("name", database);
        if ((long)(await exists.ExecuteScalarAsync())! == 0)
        { await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin); await create.ExecuteNonQueryAsync(); }
    }
    builder.Database = database;
    var repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    var web = Path.Combine(repo, "HealthCard.Web");
    var start = new ProcessStartInfo("dotnet") { WorkingDirectory = web, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    start.ArgumentList.Add(Path.Combine(web, "bin", "Debug", "net10.0", "HealthCard.Web.dll"));
    start.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
    start.Environment["DOTNET_ENVIRONMENT"] = "Testing";
    start.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:5168";
    start.Environment["ConnectionStrings__DefaultConnection"] = builder.ConnectionString;
    start.Environment["Logging__LogLevel__Default"] = "Warning";
    start.Environment["Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command"] = "Warning";
    var storage = Environment.GetEnvironmentVariable("HEALTHCARD_QA_STORAGE") ?? Path.Combine(Path.GetTempPath(), database);
    start.Environment["Storage__Root"] = storage;
    using var process = Process.Start(start)!;
    process.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
    process.ErrorDataReceived += (_, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
    process.BeginOutputReadLine(); process.BeginErrorReadLine();
    Console.WriteLine($"QA server PID {process.Id}, database {database}, URL http://127.0.0.1:5168");
    var stop = Environment.GetEnvironmentVariable("HEALTHCARD_QA_STOP");
    while (!process.HasExited && (stop == null || !File.Exists(stop))) await Task.Delay(500);
    if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
    else { Console.WriteLine($"QA host exit code {process.ExitCode}"); Environment.ExitCode = process.ExitCode; }
    return;
}

var passed = 0;
void Check(string name, Action action)
{
    try { action(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception ex) { Console.Error.WriteLine("FAIL " + name + ": " + ex.Message); Environment.ExitCode = 1; }
}
void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
var evaluation = new ResultEvaluationService();
Check("Result rejects negative value", () => Throws<ArgumentException>(() => TestResult.Create(1, 1, -1)));
Check("Result preserves four decimals", () => Assert(TestResult.Create(1, 1, 1.025m).Value == 1.025m));
Check("Result rounds to four decimals", () => Assert(TestResult.Create(1, 1, 1.23456m).Value == 1.2346m));
Check("Record rejects future date", () => Throws<ArgumentException>(() => MedicalRecord.Create("user", 1, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), "Test", null)));
Check("Record rejects missing date", () => Throws<ArgumentException>(() => MedicalRecord.Create("user", 1, default, "Test", null)));
Check("Record update validates date", () => Throws<ArgumentException>(() => MedicalRecord.Create("user", 1, DateOnly.FromDateTime(DateTime.Today), "Test", null).Update(default, "Test", null)));
Check("System category protected", () => Throws<InvalidOperationException>(() => Category.CreateSystem("Base", true, 1).Update("Changed", null, null)));
Check("Custom category has owner", () => Assert(Category.CreateUser("user", "Own", true, 1).UserId == "user"));
Check("Category trims name", () => Assert(Category.CreateUser("user", "  Own  ", true, 1).Name == "Own"));
Check("Norm order validated", () => Throws<ArgumentException>(() => TestParameter.Create(1, "Test", "unit", 5, 2, 1)));
Check("Norm update rejects negative", () => Throws<ArgumentException>(() => TestParameter.Create(1, "Test", "unit", 1, 2, 1, false).UpdateNorms(-1, 2)));
Check("Personal norm preserves precision", () => Assert(UserParameterNorm.Create("u", 1, 1.010m, 1.025m).CustomNormMax == 1.025m));
Check("Personal norm order validated", () => Throws<ArgumentException>(() => UserParameterNorm.Create("u", 1, 5, 2)));
Check("Lower result outside interval", () => Assert(evaluation.Evaluate(119, 120, 160) == ResultStatus.OutOfRange));
Check("Higher result outside interval", () => Assert(evaluation.Evaluate(161, 120, 160) == ResultStatus.OutOfRange));
Check("Boundary result retained", () => Assert(evaluation.Evaluate(120, 120, 160) == ResultStatus.NearBoundary));
Check("Middle result normal", () => Assert(evaluation.Evaluate(140, 120, 160) == ResultStatus.Normal));
Check("Invalid evaluation interval rejected", () => Throws<ArgumentException>(() => evaluation.Evaluate(1, 5, 2)));
Check("Lab category requires parameters", () => Assert(new CategoryEditViewModel { Name = "Lab", IsLabTest = true }.Validate(new ValidationContext(new object())).Any()));
Check("Document category accepts no parameters", () => Assert(!new CategoryEditViewModel { Name = "Docs", IsLabTest = false }.Validate(new ValidationContext(new object())).Any()));
Check("Repeated parameter names rejected", () => Assert(new CategoryEditViewModel { IsLabTest = true, Parameters = new() { new() { Name = "A" }, new() { Name = "a" } } }.Validate(new ValidationContext(new object())).Any()));
Check("Future date of birth rejected", () => Assert(new ProfileViewModel { DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) }.Validate(new ValidationContext(new object())).Any()));
var temp = Path.Combine(Path.GetTempPath(), "HealthCardChecks_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temp);
try
{
    var env = new CheckEnvironment { ContentRootPath = temp, WebRootPath = Path.Combine(temp, "wwwroot") }; Directory.CreateDirectory(env.WebRootPath);
    var storage = new FileStorageService(env, new ConfigurationBuilder().Build());
    string path = "";
    Check("Private file save", () => { path = storage.SaveFileAsync(new MemoryStream(new byte[] { 1, 2, 3 }), "test.pdf", "user").GetAwaiter().GetResult(); Assert(!File.Exists(Path.Combine(env.WebRootPath, path))); });
    Check("Private file read", () => Assert(storage.GetFileBytesAsync(path).GetAwaiter().GetResult().SequenceEqual(new byte[] { 1, 2, 3 })));
    Check("Traversal path rejected", () => Throws<ArgumentException>(() => storage.GetFileBytesAsync("../secret").GetAwaiter().GetResult()));
    Check("Traversal owner rejected", () => Throws<ArgumentException>(() => storage.SaveFileAsync(new MemoryStream(), "test.pdf", "../user").GetAwaiter().GetResult()));
    Check("Attachment delete", () => { storage.DeleteFileAsync(path).GetAwaiter().GetResult(); Throws<IOException>(() => storage.GetFileBytesAsync(path).GetAwaiter().GetResult()); });
}
finally { if (Path.GetFullPath(temp).StartsWith(Path.Combine(Path.GetTempPath(), "HealthCardChecks_"), StringComparison.OrdinalIgnoreCase)) Directory.Delete(temp, true); }
Console.WriteLine($"Checks passed: {passed}. Exit code: {Environment.ExitCode}");

sealed class CheckEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "Checks";
    public string EnvironmentName { get; set; } = "Testing";
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
