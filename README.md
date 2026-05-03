# PharmTech a <Pharmacy Inventory and Operations Management System>!

<Overview>!
This project is a web-based Pharmacy and Health Facility Management System designed to improve the efficiency, accuracy, and accountability of pharmacy operations. It replaces manual and semi-digital processes with a structured, data-driven system that supports inventory management, drug dispensing, returns handling, and decision-making.
The system is built using ASP.NET Core and follows a layered MVC architecture, integrating real-world pharmacy workflows into a scalable digital platform.

<Objectives>!
(i).	Improve inventory tracking and reduce stock shortages
(ii).	Implement structured workflows for drug returns and recalls
(iii).	Enforce role-based access control for system security
(iv).	Provide data-driven insights through analytics
(v).	Support patient care through refill tracking
(vi).	Ensure accountability through audit logging

<Core Features>!
1. Predictive Stock Management
Tracks medicine usage patterns and estimates future stock depletion based on consumption rates. Alerts are generated before stock reaches critical levels.

2. Workflow-Driven Returns and Recalls
Implements a structured process for handling returned or recalled drugs:
Pending → Approved/Rejected
Restockable items re-enter inventory
Unsafe drugs are logged for disposal

3. Role-Based Access Control (RBAC)
Restricts system actions based on user roles:
Admin – full system control
Pharmacist – dispensing, returns, inventory
Doctor – prescriptions and refill approvals

4. Patient Refill Reminder System
Estimates when a patient’s medication will run out using:
Dosage per day
Duration
Triggers reminders for follow-up prescriptions.

5. Analytics Dashboard
i)Provides insights such as:
ii)Stock levels
iii)Dispensing trends
iv)Frequently used medicines
v)Supports data-driven decision-making.

6. Audit Logging System
Tracks all system activities:
i)Who performed an action
ii)What action was performed
iii)When it occurred
iv)Ensures transparency and accountability.

<System Architecture>!
The system follows a layered MVC architecture:
i).		Presentation Layer – Razor Views (UI)
ii).	Application Layer – Controllers (API logic)
iii).	Business Logic Layer – Services (background jobs, workflows)
iv).	Data Access Layer – EF Core (database interaction)
v).		Database Layer – MySQL

<Key System Components>!
i).		Medicine – Master record of all medicines
ii).	InventoryItem – Summary stock per facility
iii).	MedicineBatch – Batch-level stock for FIFO dispensing
iii).	Prescription – Doctor-issued medication orders
iv).	DispenseRecord – Records of dispensed medicines
v).		OrderRequest – Stock ordering workflow
vi).	DrugReturn – Handles returned medicines
vii).	DisposalRecord – Tracks unsafe/expired drugs
viii).	SystemAlert – Notifications (low stock, expiry)
ix).	AuditLog – Tracks user actions

<Tech Stack>!
	1. Frontend: Razor Views + Bootstrap
	2. Backend: ASP.NET Core 8 (C#)
Database: MySQL
ORM: Entity Framework Core
Background Processing: IHostedService (for alerts & checks)

<Key Functional Logic>!
	1. FIFO Dispensing
Medicines are dispensed based on:
i).		Earliest expiry date
ii).	Oldest stock first
iii).	Ensures minimal wastage and safe dispensing.
iv).	Background Services
2.		A scheduled service runs daily to:
).		Check low stock levels
).		Detect expiring medicines
).		Generate system alerts

<How to Run the Project>!
Clone the repository
Bash
git clone https://github.com/LMadisane/PharmTech
Open in Visual Studio
Configure database connection in:
appsettings.json
Run migrations:
Bash
Update-Database
Run the application using https://localhost:....

<Expected Outcomes>!
Reduced stock shortages
Improved workflow efficiency
Better tracking of medicine lifecycle
Enhanced patient support
Increased system accountability

<Future Improvements>!
SMS/Email notification integration
Mobile application support
Advanced predictive analytics (AI-based)
Integration with national drug regulatory systems

<Author>!
Louis Madisane
Software Engineering Student
University of Zambia