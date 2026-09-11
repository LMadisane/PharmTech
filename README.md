# PharmTech – Pharmacy Inventory and Operations Management System

<Overview>

PharmTech is a web-based Pharmacy and Health Facility Management System designed to improve the efficiency, accuracy, and accountability of pharmacy operations. It replaces manual and semi-digital processes with a structured, data-driven system that supports inventory management, drug dispensing, returns handling, and decision-making.

The system is built using ASP.NET Core and follows a layered MVC architecture, integrating real-world pharmacy workflows into a scalable digital platform.

<Objectives>

(i).   Improve inventory tracking and reduce stock shortages
(ii).  Implement structured workflows for drug returns and recalls
(iii). Enforce role-based access control for system security
(iv).  Provide data-driven insights through analytics
(v).   Support patient care through prescription and dispensing workflows
(vi).  Ensure accountability through audit logging

<Core Features>

1. Stock Management with Automated Alerts
Tracks medicine quantities, batches, and expiry dates in real-time. Configurable thresholds trigger low-stock alerts before stock reaches critical levels. A background service runs daily to check stock levels and expiry dates, generating alerts automatically.

2. Workflow-Driven Returns and Recalls
Implements a structured process for handling returned or recalled drugs:
Pending → Approved/Rejected
Restockable items re-enter inventory with new batch tracking
Unsafe drugs are logged for disposal with full traceability

3. Role-Based Access Control (RBAC)
Restricts system actions based on user roles:
Admin – full system control and user management
Pharmacist – dispensing, returns, inventory management
Doctor – prescription creation and management

4. Prescription and Dispensing Workflow
Doctors can issue prescriptions with calculated total quantities (dosage × duration). Pharmacists dispense prescriptions using batch-level FEFO (First-Expiry-First-Out) logic. Dispensing generates receipts and automatically updates inventory.

5. Analytics Dashboard
Provides insights such as:
i).  Stock levels across facilities
ii). Prescription status (dispensed vs pending)
iii). Drug returns by medicine
iv). Supports data-driven decision-making

6. Audit Logging System
Tracks all system activities:
i).  Who performed an action
ii). What action was performed
iii). When it occurred
iv). IP address and user agent for accountability
Ensures transparency and accountability.

7. Reporting
Generates daily and weekly summary reports as downloadable PDFs, covering dispenses, returns, and orders.

<System Architecture>

The system follows a layered MVC architecture:
i).    Presentation Layer – Razor Views (UI)
ii).   Application Layer – Controllers (API logic)
iii).  Business Logic Layer – Services (background jobs, workflows)
iv).   Data Access Layer – EF Core (database interaction)
v).    Database Layer – MySQL

<Key System Components>

i).    Medicine – Master record of all medicines
ii).   InventoryItem – Summary stock per facility
iii).  MedicineBatch – Batch-level stock for FEFO dispensing
iv).   Prescription – Doctor-issued medication orders
v).    DispenseRecord – Records of dispensed medicines
vi).   OrderRequest – Stock ordering workflow
vii).  DrugReturn – Handles returned medicines
viii). DisposalRecord – Tracks unsafe/expired drugs
ix).   SystemAlert – Notifications (low stock, expiry)
x).    AuditLog – Tracks user actions
xi).   Receipt – Generates printable transaction receipts

<Tech Stack>

1. Frontend: Razor Views + Tailwind CSS + Alpine.js
2. Backend: ASP.NET Core 8 (C#)
3. Database: MySQL
4. ORM: Entity Framework Core
5. Background Processing: IHostedService (for alerts & checks)
6. PDF Generation: QuestPDF
7. Charts: Chart.js

<Key Functional Logic>

1. Batch-Level Dispensing (FEFO)
Medicines are dispensed using First-Expiry-First-Out (FEFO) logic:
i).    Earliest expiry date is dispensed first
ii).   Batches with the same expiry date use oldest received date as tie-breaker
iii).  Ensures minimal wastage and safe dispensing of near-expiry stock first

2. Automated Background Checks
A scheduled service runs daily to:
i).    Check low stock levels against configurable thresholds
ii).   Detect expiring medicines (30, 60, 90 days before expiry)
iii).  Identify already expired stock for immediate disposal
iv).   Generate system alerts for all detected issues

3. On-the-Fly Password Migration
Existing user passwords (SHA256) are automatically migrated to the secure PasswordHasher format on next login, preserving all user data without requiring manual intervention.

<How to Run the Project>

1. Clone the repository
git clone https://github.com/LMadisane/PharmTech

2. Open the solution in Visual Studio

3. Configure the database connection:
- For development, use User Secrets (recommended):
- dotnet user-secrets set "ConnectionStrings:DefaultConnection" "server=localhost;database=pharmtech_db;user=root;password=YourPassword"
- Or set environment variable: `ConnectionStrings__DefaultConnection`
- Do not commit passwords to the repository

4. Run migrations:
- dotnet ef database update

5. Run the application:
- dotnet run
- Access at: https://localhost:7001

<Default Admin Account>

After seeding, the system includes a default administrator:
- Email: admin@pharmtech.com
- Password: [REDACTED]

Demo accounts for Doctors and Pharmacists are also seeded during initial setup.

<Expected Outcomes>

- Reduced stock shortages
- Improved workflow efficiency
- Better tracking of medicine lifecycle
- Enhanced patient support
- Increased system accountability

<Future Improvements>

- SMS/Email notification integration
- Mobile application support
- Advanced analytics and forecasting
- Integration with national drug regulatory systems

<License>

This project is licensed under the MIT License – see the LICENSE file for details.

<Author>

Louis Madisane
Software Engineering Student
University of Zambia