# Synthetic RAG validation corpus

These three fictional, deliberately detailed documents are intended for local ingestion and retrieval tests. They contain made-up company and policy information; do not treat them as real operational guidance.

## Files

- `01-travel-expense-policy.txt` — exact amounts, approval thresholds, exceptions, and examples.
- `02-knowledge-platform-operations.txt` — ingestion stages, troubleshooting, and retrieval limitations.
- `03-customer-support-playbook.html` — HTML extraction, support priorities, response targets, and escalation.

Upload each file separately in the app. Keep the original filenames so citations are easy to identify. These samples are long enough to exercise recursive chunking and include facts spread across sections. The HTML also contains script/style content that should not appear in extracted text.

## Suggested questions and expected evidence

1. **Does a text-based PDF require Azure AI Document Intelligence in the local app?** The operations handbook says text PDFs are extracted locally; OCR is needed for scanned/image-only PDFs.
2. **How many approvals are required for an international trip estimated at USD 3,400?** The travel policy says both the direct manager and department director must approve.
3. **What is the per-mile personal vehicle reimbursement rate, and when did it take effect?** The travel policy says USD 0.67 per mile, effective 1 October 2026.
4. **How soon should an operator update a customer during a Priority 1 incident?** The support playbook says at least once every four staffed hours.
5. **Is the four-hour Priority 2 target a resolution guarantee?** No. It is a target for a human first response during staffed hours.
6. **A customer can sign in but has slow search and a cached export workaround. What severity is typical, and what does the response target mean?** Generally Priority 2, based on impact; four staffed hours is the first-response target, not a guaranteed fix.
7. **How long does Finance normally take to process a compliant expense report?** The travel policy says within ten business days after manager and Finance reviews are complete.
8. **A PDF opens in a viewer but has no selectable text. What should the operator conclude?** It is likely scanned/image-only and requires OCR or an approved text export.
9. **What release date is promised for the export feature?** No release date is given. The support playbook says backlog prioritization is not a delivery commitment.
10. **What is Aurora's approved business-class flight allowance?** No standard allowance is specified; the policy says business class is not reimbursable under the standard policy, with limited documented exceptions.

For manual evaluation, check whether each answer preserves conditions and distinctions (for example, *response* versus *resolution*, or local text extraction versus scanned-document OCR) and whether its citations support each factual claim. Questions 9 and 10 intentionally test abstention and avoidance of invented facts.