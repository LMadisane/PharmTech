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

        // DbSets for all models
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<DispenseRecord> DispenseRecords { get; set; } = null!;
        public DbSet<DisposalRecord> DisposalRecords { get; set; } = null!;
        public DbSet<DrugReturn> DrugReturns { get; set; } = null!;
        public DbSet<Facility> Facilities { get; set; } = null!;
        public DbSet<InventoryItem> InventoryItems { get; set; } = null!;
        public DbSet<Medicine> Medicines { get; set; } = null!;
        public DbSet<MedicineBatch> MedicineBatches { get; set; } = null!;
        public DbSet<OrderRequest> OrderRequests { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;
        public DbSet<Receipt> Receipts { get; set; } = null!;
        public DbSet<RefillRequest> RefillRequests { get; set; } = null!;
        public DbSet<StockThreshold> StockThresholds { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<SystemAlert> SystemAlerts { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================== PRESCRIPTION RELATIONSHIPS ====================
            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Patient)
                .WithMany(u => u.Prescriptions)
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Medicine)
                .WithMany(m => m.Prescriptions)
                .HasForeignKey(p => p.MedId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Facility)
                .WithMany()
                .HasForeignKey(p => p.FacilityId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==================== DRUG RETURN RELATIONSHIPS ====================
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

            // ==================== INVENTORY ITEM RELATIONSHIPS ====================
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

            // ==================== ORDER REQUEST RELATIONSHIPS ====================
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

            // ==================== USER RELATIONSHIPS ====================
            modelBuilder.Entity<User>()
                .HasOne(u => u.Facility)
                .WithMany(f => f.Users)
                .HasForeignKey(u => u.FacilityId)
                .OnDelete(DeleteBehavior.SetNull);

            // ==================== RECEIPT RELATIONSHIPS ====================
            modelBuilder.Entity<Receipt>()
                .HasOne(r => r.GeneratedBy)
                .WithMany()
                .HasForeignKey(r => r.GeneratedById)
                .OnDelete(DeleteBehavior.Restrict);

            // ==================== REFILL REQUEST RELATIONSHIPS ====================
            modelBuilder.Entity<RefillRequest>()
                .HasOne(rr => rr.OriginalPrescription)
                .WithMany()
                .HasForeignKey(rr => rr.OriginalPrescriptionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RefillRequest>()
                .HasOne(rr => rr.RequestedBy)
                .WithMany()
                .HasForeignKey(rr => rr.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RefillRequest>()
                .HasOne(rr => rr.Doctor)
                .WithMany()
                .HasForeignKey(rr => rr.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==================== STOCK THRESHOLD RELATIONSHIPS ====================
            modelBuilder.Entity<StockThreshold>()
                .HasOne(st => st.Medicine)
                .WithMany()
                .HasForeignKey(st => st.MedId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockThreshold>()
                .HasOne(st => st.Facility)
                .WithMany()
                .HasForeignKey(st => st.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockThreshold>()
                .HasOne(st => st.SetBy)
                .WithMany()
                .HasForeignKey(st => st.SetById)
                .OnDelete(DeleteBehavior.Restrict);

            // ==================== SYSTEM ALERT RELATIONSHIPS ====================
            modelBuilder.Entity<SystemAlert>()
                .HasOne(sa => sa.Medicine)
                .WithMany()
                .HasForeignKey(sa => sa.MedId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SystemAlert>()
                .HasOne(sa => sa.Facility)
                .WithMany()
                .HasForeignKey(sa => sa.FacilityId)
                .OnDelete(DeleteBehavior.SetNull);

            // ==================== MEDICINE BATCH RELATIONSHIPS ====================
            modelBuilder.Entity<MedicineBatch>()
                .HasOne(mb => mb.Medicine)
                .WithMany(m => m.Batches)
                .HasForeignKey(mb => mb.MedId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MedicineBatch>()
                .HasOne(mb => mb.Facility)
                .WithMany(f => f.Batches)
                .HasForeignKey(mb => mb.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for performance
            modelBuilder.Entity<MedicineBatch>()
                .HasIndex(mb => new { mb.MedId, mb.FacilityId });

            // ==================== DISPOSAL RECORD RELATIONSHIPS ====================
            modelBuilder.Entity<DisposalRecord>()
                .HasOne(d => d.Medicine)
                .WithMany(m => m.DisposalRecords)
                .HasForeignKey(d => d.MedId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DisposalRecord>()
                .HasOne(d => d.Facility)
                .WithMany(f => f.DisposalRecords)
                .HasForeignKey(d => d.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DisposalRecord>()
                .HasOne(d => d.Batch)
                .WithMany()
                .HasForeignKey(d => d.BatchId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}