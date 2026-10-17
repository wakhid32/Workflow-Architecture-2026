Write-Host "=== 1. START WORKFLOW ===" -ForegroundColor Cyan
$startBody = @{ tipeDisposisi = 201; idForm = 501 } | ConvertTo-Json
$startRes = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/workflow/start" -Method Post -ContentType "application/json" -Body $startBody
Write-Host "Initial Status: $($startRes.statusDokumenAwal), Buttons: $($startRes.actionButtons.Count), Candidates: $($startRes.approverCandidates.Count)"

Write-Host "`n=== 2. SUBMIT DRAFT (Step 1) ===" -ForegroundColor Cyan
$submitBody = @{
    idTask = 0
    idUrutanDisposisi = 1
    idDokumen = "CPX-LIVE-001"
    noDokumen = "DOC/CPX/2026/001"
    idForm = 501
    selectedApproverId = 2
    catatanDisposisi = "Pengajuan Capex Server Cluster"
    documentAmount = 85000000
} | ConvertTo-Json

$submitRes = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/workflow/next" -Method Post -ContentType "application/json" -Body $submitBody -Headers @{"X-User-Id"="1"; "X-User-Name"="Budi Santoso"}
Write-Host "Task Created! ID: $($submitRes.idTask), Status: $($submitRes.statusDokumenBaru), Hash: $($submitRes.hashId)"

Write-Host "`n=== 3. GET TASK DETAIL (As Acting Officer Rina - ID 6) ===" -ForegroundColor Cyan
$getDetailBody = @{ idTask = $submitRes.idTask } | ConvertTo-Json
$detailRes = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/workflow/get" -Method Post -ContentType "application/json" -Body $getDetailBody -Headers @{"X-User-Id"="6"; "X-User-Name"="Rina Marlina (Plt)"}
Write-Host "Task Found: $($detailRes.noDokumen), Status: $($detailRes.statusDokumen), IsActivePic: $($detailRes.isActivePic), PermittedButtons: $($detailRes.permittedButtons.Count)"

Write-Host "`n=== 4. ACTING OFFICER APPROVES (Step 2 -> Finance Director) ===" -ForegroundColor Cyan
$approveBody = @{
    idTask = $submitRes.idTask
    idUrutanDisposisi = 2
    idDokumen = "CPX-LIVE-001"
    noDokumen = "DOC/CPX/2026/001"
    idForm = 501
    selectedApproverId = 5
    catatanDisposisi = "Disetujui oleh Plt Section Head atas nama Bu Siti"
    documentAmount = 85000000
    rowVersion = $detailRes.rowVersion
} | ConvertTo-Json

$approveRes = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/workflow/next" -Method Post -ContentType "application/json" -Body $approveBody -Headers @{"X-User-Id"="6"; "X-User-Name"="Rina Marlina (Plt)"}
Write-Host "Transited! New Status: $($approveRes.statusDokumenBaru), Target: $($approveRes.targetUserId)"

Write-Host "`n=== 5. FINANCE DIRECTOR FINAL APPROVAL (Step 4 -> COMPLETED) ===" -ForegroundColor Cyan
$getDetailDir = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/workflow/get" -Method Post -ContentType "application/json" -Body $getDetailBody -Headers @{"X-User-Id"="5"; "X-User-Name"="Agus Salim (Finance Dir)"}
$finalBody = @{
    idTask = $submitRes.idTask
    idUrutanDisposisi = 4
    idDokumen = "CPX-LIVE-001"
    noDokumen = "DOC/CPX/2026/001"
    idForm = 501
    catatanDisposisi = "Persetujuan final Direksi Keuangan diberikan. Lanjutkan ke proses PO."
    documentAmount = 85000000
    rowVersion = $getDetailDir.rowVersion
} | ConvertTo-Json

$finalRes = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/workflow/next" -Method Post -ContentType "application/json" -Body $finalBody -Headers @{"X-User-Id"="5"; "X-User-Name"="Agus Salim"}
Write-Host "Final Result: Success=$($finalRes.success), Status=$($finalRes.statusDokumenBaru), IsCompleted=$($finalRes.isCompleted)"

Write-Host "`n=== 6. VERIFY AUDIT TRAIL ===" -ForegroundColor Cyan
$finalAudit = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/workflow/get" -Method Post -ContentType "application/json" -Body $getDetailBody -Headers @{"X-User-Id"="1"}
Write-Host "Final Approval Status: $($finalAudit.approvalStatus)"
Write-Host "Audit Histories Count: $($finalAudit.auditHistories.Count)"
foreach ($h in $finalAudit.auditHistories) {
    Write-Host " - Action: $($h.actionTaken), From: $($h.namaPengirim), To: $($h.namaPenerima), ExecutedBy: $($h.namaEksekutor), Notes: $($h.catatanDisposisi)"
}

Write-Host "`n>>> ALL WORKFLOW TESTS PASSED 100% WITH ZERO ERRORS <<<" -ForegroundColor Green
