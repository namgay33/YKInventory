using Microsoft.EntityFrameworkCore;
using YKInventory.Data;
using YKInventory.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"), 
    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddScoped(sp => new HttpClient());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<YKInventory.Components.App>().AddInteractiveServerRenderMode();

// Asset Endpoints
app.MapGet("/api/assets", async (ApplicationDbContext db) => await db.Assets.ToListAsync());
app.MapGet("/api/assets/available", async (ApplicationDbContext db) => await db.Assets.Where(a => a.Status == "Available").ToListAsync());
app.MapPost("/api/assets", async (Asset asset, ApplicationDbContext db) => 
{
    asset.CreatedAt = DateTime.Now;
    db.Assets.Add(asset);
    await db.SaveChangesAsync();
    return Results.Ok(asset);
});
app.MapDelete("/api/assets/{id}", async (int id, ApplicationDbContext db) => 
{
    var asset = await db.Assets.FindAsync(id);
    if (asset != null) db.Assets.Remove(asset);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// Employee Endpoints
app.MapGet("/api/employees", async (ApplicationDbContext db) => await db.Employees.ToListAsync());
app.MapPost("/api/employees", async (Employee employee, ApplicationDbContext db) => 
{
    employee.JoiningDate = employee.JoiningDate == default ? DateTime.Now : employee.JoiningDate;
    db.Employees.Add(employee);
    await db.SaveChangesAsync();
    return Results.Ok(employee);
});
app.MapDelete("/api/employees/{id}", async (int id, ApplicationDbContext db) => 
{
    var employee = await db.Employees.FindAsync(id);
    if (employee != null) db.Employees.Remove(employee);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// Transaction Endpoints
app.MapGet("/api/transactions", async (ApplicationDbContext db) =>
{
    var transactions = await db.Transactions
        .Include(t => t.Asset)
        .Include(t => t.Employee)
        .OrderByDescending(t => t.TransactionDate)
        .Select(t => new
        {
            t.Id,
            Date = t.TransactionDate.ToString("yyyy-MM-dd"),
            AssetTag = t.Asset.AssetTag,
            AssetName = t.Asset.Name,
            EmployeeName = t.Employee.FullName,
            Department = t.Employee.Department,
            t.Quantity,
            Status = t.ActualReturnDate == null ? "Issued" : "Returned",
            ReturnedDate = t.ActualReturnDate.HasValue ? t.ActualReturnDate.Value.ToString("yyyy-MM-dd") : null,
            Condition = t.ConditionOnReturn ?? "N/A",
            t.Remarks
        })
        .ToListAsync();
    return Results.Ok(transactions);
});

app.MapPost("/api/checkout", async (CheckoutRequest request, ApplicationDbContext db) =>
{
    var asset = await db.Assets.FindAsync(request.AssetId);
    if (asset == null) return Results.BadRequest("Asset not found");
    if (asset.Status != "Available") return Results.BadRequest("Asset not available");
    
    var transaction = new Transaction
    {
        AssetId = request.AssetId,
        EmployeeId = request.EmployeeId,
        TransactionDate = DateTime.Now,
        TransactionType = "CheckOut",
        Quantity = request.Quantity,
        ExpectedReturnDate = request.ExpectedReturnDate,
        Remarks = request.Remarks ?? "",
        ApprovalStatus = "Approved"
    };
    
    asset.Status = "Issued";
    db.Transactions.Add(transaction);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Checked out successfully" });
});

app.MapPost("/api/return", async (ReturnRequest request, ApplicationDbContext db) =>
{
    var transaction = await db.Transactions
        .Include(t => t.Asset)
        .FirstOrDefaultAsync(t => t.Id == request.TransactionId);
    
    if (transaction == null) return Results.BadRequest("Transaction not found");
    if (transaction.ActualReturnDate != null) return Results.BadRequest("Asset already returned");
    
    transaction.ActualReturnDate = DateTime.Now;
    transaction.ConditionOnReturn = request.Condition;
    transaction.Remarks = request.Remarks;
    transaction.Asset.Status = "Available";
    
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Returned successfully" });
});

// Update Asset 
app.MapPut("/api/assets/{id}", async (int id, Asset updatedAsset, ApplicationDbContext db) =>
{
    var asset = await db.Assets.FindAsync(id);
    if (asset == null) return Results.NotFound();
    
    asset.AssetTag = updatedAsset.AssetTag;
    asset.Name = updatedAsset.Name;
    asset.Category = updatedAsset.Category;
    asset.Model = updatedAsset.Model;
    asset.SerialNumber = updatedAsset.SerialNumber;
    asset.Vendor = updatedAsset.Vendor;
    asset.PurchaseCost = updatedAsset.PurchaseCost;
    asset.PurchaseDate = updatedAsset.PurchaseDate;
    asset.WarrantyExpiry = updatedAsset.WarrantyExpiry;
    asset.Specifications = updatedAsset.Specifications;
    asset.Status = updatedAsset.Status;  
    
    await db.SaveChangesAsync();
    return Results.Ok(asset);
});

// Update Employee 
app.MapPut("/api/employees/{id}", async (int id, Employee updatedEmployee, ApplicationDbContext db) =>
{
    var employee = await db.Employees.FindAsync(id);
    if (employee == null) return Results.NotFound();
    
    employee.EmployeeCode = updatedEmployee.EmployeeCode;
    employee.FullName = updatedEmployee.FullName;
    employee.Email = updatedEmployee.Email;
    employee.Department = updatedEmployee.Department;
    employee.Designation = updatedEmployee.Designation;
    employee.JoiningDate = updatedEmployee.JoiningDate;
    employee.IsActive = updatedEmployee.IsActive;
    
    await db.SaveChangesAsync();
    return Results.Ok(employee);
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

public class CheckoutRequest
{
    public int AssetId { get; set; }
    public int EmployeeId { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime? ExpectedReturnDate { get; set; }
    public string Remarks { get; set; } = "";
}

public class ReturnRequest
{
    public int TransactionId { get; set; }
    public string Condition { get; set; } = "Good";
    public string Remarks { get; set; } = "";
}
