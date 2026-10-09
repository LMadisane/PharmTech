# Task speed evaluation protocol

## Purpose

The task-speed comparison examined whether PharmTech completed representative pharmacy workflows faster than the spreadsheet baseline. The planned performance target was at least 20 percent less completion time for PharmTech.

## Evaluator and session

One evaluator, who was also the system developer, performed every run on the same laptop and browser. The timing session took place on 19 September 2026, approximately 14:00 to 17:00 CAT. All spreadsheet baseline runs were completed first, followed by all PharmTech runs. This order may have introduced a learning effect.

The original plan included one practice run per task in each system. In practice, a separate practice run was not completed for every PharmTech task. Practice timings were not included in the reported results.

## Starting data

Both systems used comparable starting scope: two facilities, five medicines, twenty patients, fifteen medicine batches, three pending prescriptions, and two pending returns. The reproducible PharmTech records are defined in `PharmTech_Evaluation_Dataset.sql`. The historical spreadsheet baseline is retained in `PharmTech_Spreadsheet_Baseline_Master.xlsx`, with its structure described in `Spreadsheet_Baseline_Layout.md`.

## Timed tasks

| Task | Workflow | Start point | Stop point |
| --- | --- | --- | --- |
| T1 | Locate a medicine | First click or keystroke used to navigate or search for the specified medicine | The specified medicine row is visible |
| T2 | Add a stock batch | First click or keystroke used to begin the stock entry | The new batch is saved and visible in the register or application |
| T3 | Create a prescription | First click or keystroke used to begin the prescription entry | The prescription save confirmation or saved record is visible |
| T4 | Dispense a prescription | First click or keystroke used to enter or search for the reference code | The dispensing confirmation or completed dispensing record is visible |
| T5 | Process a patient drug return | First click or keystroke used to create the return | The administrator approval is complete and the outcome is visible |

## Timing procedure

The evaluator used a phone stopwatch. Timing started at the stated start point and stopped at the stated stop point. There were no pauses. An interrupted run would be repeated rather than recorded.

Each task was performed three times in each system, giving 5 tasks x 2 systems x 3 runs = 30 timed runs. The raw readings are retained in `Timing_Results.xlsx` in this folder. The task medians and total-time comparison use worksheet formulas so that a reader can verify the calculations.

## Calculation method

For each task and system, the reported time is the median of the three recorded runs:

```text
=MEDIAN(Run 1, Run 2, Run 3)
```

The per-task improvement is calculated as:

```text
=(Spreadsheet median - PharmTech median) / Spreadsheet median
```

Per-task results are the primary comparison because the tasks have different durations. If one overall summary is required, use total time across the five per-task medians rather than pooling unlike tasks.

## Limitations

The comparison was performed by one person who also developed the system. It was not a usability study, participant satisfaction study, page-load benchmark, API-response benchmark, or database-query benchmark. It measures only end-to-end completion time for the five selected tasks.
