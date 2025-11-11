using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace ManagementEmployee.Models;

public partial class ManagementEmployeeContext : DbContext
{
    public ManagementEmployeeContext()
    {
    }

    public ManagementEmployeeContext(DbContextOptions<ManagementEmployeeContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<LeaveEntitlement> LeaveEntitlements { get; set; }

    public virtual DbSet<LeaveRequest> LeaveRequests { get; set; }

    public virtual DbSet<LeaveType> LeaveTypes { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payroll> Payrolls { get; set; }

    public virtual DbSet<PayrollAdjustment> PayrollAdjustments { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        optionsBuilder.UseSqlServer(config.GetConnectionString("DefaultConnection"));

    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.ActivityId);

            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EntityName).HasMaxLength(60);

            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_ActivityLogs_Users");
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.ToTable("Attendance");

            entity.HasIndex(e => new { e.EmployeeId, e.WorkDate }, "IX_Attendance_EmpDate");

            entity.HasIndex(e => new { e.EmployeeId, e.WorkDate }, "UQ_Attendance_Employee_Date").IsUnique();

            entity.Property(e => e.CheckIn).HasPrecision(0);
            entity.Property(e => e.CheckOut).HasPrecision(0);
            entity.Property(e => e.Notes).HasMaxLength(250);
            entity.Property(e => e.OvertimeHours).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Present");
            entity.Property(e => e.WorkHours).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Employee).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Employees");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(e => e.DepartmentName, "UQ_Departments_Name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DepartmentName).HasMaxLength(100);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.DepartmentId, "IX_Employees_Department");

            entity.HasIndex(e => new { e.FullName, e.Position }, "IX_Employees_NamePosition");

            entity.Property(e => e.Address).HasMaxLength(250);
            entity.Property(e => e.AvatarUrl).HasMaxLength(260);
            entity.Property(e => e.BaseSalary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.Gender)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Position).HasMaxLength(100);

            entity.HasOne(d => d.Department).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employees_Departments");
        });

        modelBuilder.Entity<LeaveEntitlement>(entity =>
        {
            entity.HasKey(e => e.EntitlementId);

            entity.HasIndex(e => new { e.EmployeeId, e.LeaveTypeId, e.Year }, "UQ_LeaveEntitlements_Key").IsUnique();

            entity.Property(e => e.CarriedOver).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EntitledDays).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Employee).WithMany(p => p.LeaveEntitlements)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveEntitlements_Employees");

            entity.HasOne(d => d.LeaveType).WithMany(p => p.LeaveEntitlements)
                .HasForeignKey(d => d.LeaveTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveEntitlements_LeaveTypes");
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasIndex(e => new { e.Status, e.StartDate, e.EndDate }, "IX_LeaveRequests_Status");

            entity.Property(e => e.ApprovedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Reason).HasMaxLength(300);
            entity.Property(e => e.TotalDays).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.LeaveRequests)
                .HasForeignKey(d => d.ApprovedByUserId)
                .HasConstraintName("FK_LeaveRequests_ApprovedBy");

            entity.HasOne(d => d.Employee).WithMany(p => p.LeaveRequests)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveRequests_Employees");

            entity.HasOne(d => d.LeaveType).WithMany(p => p.LeaveRequests)
                .HasForeignKey(d => d.LeaveTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeaveRequests_LeaveTypes");
        });

        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.HasIndex(e => e.TypeName, "UQ_LeaveTypes_TypeName").IsUnique();

            entity.Property(e => e.DefaultDays).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.IsPaid).HasDefaultValue(true);
            entity.Property(e => e.TypeName).HasMaxLength(80);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Title).HasMaxLength(300);

            entity.HasOne(d => d.Department).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK_Notifications_Departments");

            entity.HasOne(d => d.ReceiverUser).WithMany(p => p.NotificationReceiverUsers)
                .HasForeignKey(d => d.ReceiverUserId)
                .HasConstraintName("FK_Notifications_Receiver");

            entity.HasOne(d => d.SenderUser).WithMany(p => p.NotificationSenderUsers)
                .HasForeignKey(d => d.SenderUserId)
                .HasConstraintName("FK_Notifications_Sender");
        });

        modelBuilder.Entity<Payroll>(entity =>
        {
            entity.HasIndex(e => new { e.PeriodYear, e.PeriodMonth }, "IX_Payrolls_Period");

            entity.HasIndex(e => new { e.EmployeeId, e.PeriodYear, e.PeriodMonth }, "UQ_Payrolls_Period").IsUnique();

            entity.Property(e => e.BasicSalary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Gross)
                .HasComputedColumnSql("(((([BasicSalary]+[OvertimePay])+[TotalAllowance])+[TotalBonus])-[TotalPenalty])", true)
                .HasColumnType("decimal(22, 2)");
            entity.Property(e => e.Net)
                .HasComputedColumnSql("((((([BasicSalary]+[OvertimePay])+[TotalAllowance])+[TotalBonus])-[TotalPenalty])-[TotalDeduction])", true)
                .HasColumnType("decimal(23, 2)");
            entity.Property(e => e.OvertimePay).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalAllowance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalBonus).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalDeduction).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalPenalty).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Employee).WithMany(p => p.Payrolls)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payrolls_Employees");
        });

        modelBuilder.Entity<PayrollAdjustment>(entity =>
        {
            entity.HasKey(e => e.AdjustmentId);

            entity.Property(e => e.AdjType).HasMaxLength(20);
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(200);

            entity.HasOne(d => d.Payroll).WithMany(p => p.PayrollAdjustments)
                .HasForeignKey(d => d.PayrollId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PayrollAdjustments_Payrolls");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.RoleName, "UQ_Roles_RoleName").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PasswordSalt).HasMaxLength(255);

            entity.HasOne(d => d.Employee).WithMany(p => p.Users)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK_Users_Employees");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
