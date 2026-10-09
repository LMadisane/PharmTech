# Spreadsheet baseline layout

The spreadsheet baseline mirrored a simple pharmacy register. It was maintained separately from PharmTech and contained the following sheets.

| Sheet | Columns |
| --- | --- |
| Home | Navigation only; no transaction data. |
| Medicines | Medicine ID, Medicine Name, Strength, Dosage Form, Buffer Quantity |
| Batches | Batch ID, Medicine ID, Medicine Name, Lot Number, Quantity, Expiry Date, Date Received, Facility |
| Patients | Patient ID, Patient Name, Facility |
| Prescriptions | Reference, Patient ID, Medicine, Dosage Per Day, Duration, Quantity, Status, Date |
| Dispensing Log | Reference, Patient ID, Medicine, Quantity, Lot Number, Dispensing Date |
| Returns Log | Return ID, Patient ID, Medicine, Quantity, Reason, Restockable, Status, Date |

For each spreadsheet timing run, the evaluator opened a fresh working copy made from the baseline master. This prevented an earlier run from changing the starting records for a later run.
