---
name: deepthink
description: Invoke IMMEDIATELY via python script when user requests structured reasoning for open-ended analytical questions. Do NOT explore first - the script orchestrates the thinking workflow.
---

# DeepThink

Structured multi-step reasoning for open-ended analytical questions where the
answer structure is itself unknown. Handles taxonomy design, conceptual
analysis, trade-off exploration, and definitional questions.

## InvocationBenefits:

Calendar becomes the single hub for all shift VIEWING and CREATION
Rotation templates remain separate (admin config, rarely used)
"Apply Rotation" bridges the gap without page bloat
Minimal code changes compared to full consolidation
Users can do everything from Calendar page
Implementation Outline:

Rename Schedule.razor → Calendar.razor
Update nav menu: "Schedule" → "Calendar"
Add "Apply Rotation" button to Calendar toolbar
Clicking it opens RotationAssignmentDialog (already exists)
Rename ShiftRotations.razor → RotationTemplates.razor
Update nav: "Shift Rotations" → "Rotation Templates"
Remove "Current Assignments" tab from RotationTemplates

<invoke working-dir=".claude/skills/scripts" cmd="python3 -m skills.deepthink.think --step 1 --total-steps 14" />

Do NOT explore or analyze first. Run the script and follow its output.
