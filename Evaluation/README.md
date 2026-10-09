# Evaluation evidence

This folder contains the non-sensitive materials needed to reproduce the final evaluation of PharmTech.

## Contents

- `PharmTech_Evaluation_Dataset.sql` creates the fictitious PharmTech records used by the manual workflow and corrective tests.
- `Task_Speed_Protocol.md` records the five timed tasks, timing boundaries, run order, and calculation method.
- `PharmTech_Spreadsheet_Baseline_Master.xlsx` is the spreadsheet register used as the task-speed baseline.
- `Timing_Results.xlsx` contains the 30 raw stopwatch readings. Its task medians, total-time values, and overall reduction are calculated with formulas.
- `Spreadsheet_Baseline_Layout.md` gives a text description of the baseline workbook structure.

The spreadsheet baseline and the PharmTech SQL dataset document comparable task scope: two facilities, five medicines, twenty patients, fifteen batches, three pending prescriptions, and two pending returns. They are retained as separate system-specific records rather than presented as a row-for-row export of one another.

## Safe setup

Use these materials only with a separate evaluation database. Do not run the SQL script against `pharmtech_db` or any database containing real records.

1. Create an empty evaluation database and configure it locally through User Secrets.
2. Apply the PharmTech migrations and start the application once so that the normal application seeder creates its local admin, doctor, and pharmacist accounts.
3. Sign in as the locally configured administrator and set local passwords for the seeded test roles if they are needed for a run. No password values are stored in this repository.
4. Select the evaluation database in MySQL Workbench and run `PharmTech_Evaluation_Dataset.sql` once.
5. Confirm the verification query returns 5 medicines, 20 patients, 15 batches, 3 pending prescriptions, and 2 pending returns.

The SQL script is designed for a fresh evaluation database. Manual tests change stock and workflow states. To repeat the full evaluation from the same baseline, recreate the dedicated evaluation database, apply migrations, seed local accounts, and run the script again.

All names, email addresses, lot numbers, and facilities created by the script are fictitious test data.
