using Microsoft.EntityFrameworkCore;
using PharmTech.Models;

namespace PharmTech.Data
{
    public class PharmTechContext : DbContext
    {
        public PharmTechContext(DbContextOptions<PharmTechContext> options)
            : base(options)
        {
        }

        public DbSet<DispenseRecord> DispenseRecords { get; set; } = null!;
        public DbSet<DrugReturn> DrugReturns { get; set; } = null!;
        public DbSet<Facility> Facilities { get; set; } = null!;
        public DbSet<InventoryItem> InventoryItems { get; set; } = null!;
        public DbSet<Medicine> Medicines { get; set; } = null!;
        public DbSet<OrderRequest> OrderRequests { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DrugReturn>()
                .HasOne(dr => dr.Medicine)
                .WithMany(m => m.DrugReturns)
                .HasForeignKey(dr => dr.MedId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DrugReturn>()
                .HasOne(dr => dr.Patient)
                .WithMany(u => u.DrugReturnsAsPatient)
                .HasForeignKey(dr => dr.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DrugReturn>()
                .HasOne(dr => dr.ProcessedBy)
                .WithMany(u => u.DrugReturnsAsProcessor)
                .HasForeignKey(dr => dr.ProcessedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<InventoryItem>()
                .HasOne(ii => ii.Facility)
                .WithMany(f => f.InventoryItems)
                .HasForeignKey(ii => ii.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InventoryItem>()
                .HasOne(ii => ii.Medicine)
                .WithMany()
                .HasForeignKey(ii => ii.MedId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderRequest>()
                .HasOne(or => or.Medicine)
                .WithMany(m => m.OrderRequests)
                .HasForeignKey(or => or.MedId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderRequest>()
                .HasOne(or => or.Facility)
                .WithMany(f => f.OrderRequests)
                .HasForeignKey(or => or.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderRequest>()
                .HasOne(or => or.RequestedBy)
                .WithMany(u => u.OrderRequestsAsRequester)
                .HasForeignKey(or => or.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderRequest>()
                .HasOne(or => or.ApprovedBy)
                .WithMany(u => u.OrderRequestsAsApprover)
                .HasForeignKey(or => or.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Facility)
                .WithMany(f => f.Users)
                .HasForeignKey(u => u.FacilityId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}