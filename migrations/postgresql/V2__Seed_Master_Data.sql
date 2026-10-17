-- ==============================================================================
-- Enterprise Workflow Engine Database Seed Script (PostgreSQL)
-- Module: Master Workflow Configuration, Actions, Transitions, and Seed Data
-- ==============================================================================

-- 1. Insert Master Tipe Disposisi
INSERT INTO "WM_TIPE_DISPOSISI" (
    "ID_TIPE_DISPOSISI", "NAMA_DISPOSISI", "DESKRIPSI", "KODE_MODUL", 
    "IS_ACTIVE", "IS_REOPEN_CLOSED", "CREATED_DATE", "CREATED_BY"
) VALUES 
(101, 'Persetujuan Purchase Requisition (PR)', 'Alur persetujuan pengadaan barang operasional dan inventaris kantor', 'PROCUREMENT_PR', TRUE, FALSE, CURRENT_TIMESTAMP, 'System'),
(201, 'Pengajuan Capex Asset & Investasi', 'Persetujuan pengadaan barang modal dan aset bernilai tinggi', 'ASSET_MGMT', TRUE, FALSE, CURRENT_TIMESTAMP, 'System'),
(301, 'Klaim Reimbursement Operasional', 'Penggantian biaya operasional dan perjalanan dinas karyawan', 'FIN_REIMBURSE', TRUE, FALSE, CURRENT_TIMESTAMP, 'System')
ON CONFLICT ("ID_TIPE_DISPOSISI") DO NOTHING;

-- 2. Insert Master Urutan Disposisi (State Machine Transitions)
-- Modul 201: Capex Asset
INSERT INTO "WM_URUTAN_DISPOSISI" (
    "ID_TIPE_DISPOSISI", "ID_ROLE_ASAL", "ID_STATUS_DOK_ASAL", "ID_ROLE", "ID_STATUS_DOK",
    "ID_FORM", "NAMA", "CLASS_BUTTON", "CLASS_ICON", "ACTION_TYPE", "APPROVAL_MODE",
    "UI_FORM_ACTION", "UI_FORM_SETING", "IS_AWAL", "IS_AKHIR", "IS_KIRIM_ATASAN",
    "IS_KIRIM_PEMBUAT", "IS_KIRIM_SEBELUMNYA", "IS_REQUIRE_ATTACHMENT", "SLA_HOURS_DEFAULT",
    "AMT_LIMIT_MIN", "AMT_LIMIT_MAX", "NO_URUTAN", "IS_ACTIVE"
) VALUES 
(201, 1, 'CAPEX.DRAFT', 2, 'CAPEX.WAITING_SUPERVISOR', 501, 'Ajukan ke Atasan Langsung', 'btn-primary', 'fa fa-paper-plane', 'Submit', 'Single', 'internalsave', 'insertable', TRUE, FALSE, TRUE, FALSE, FALSE, FALSE, 24, 0.00, 0.00, 1, TRUE),
(201, 2, 'CAPEX.WAITING_SUPERVISOR', 5, 'CAPEX.WAITING_FINANCE_DIR', 501, 'Setujui & Teruskan ke Direksi Keuangan', 'btn-success', 'fa fa-check', 'Approve', 'Single', NULL, 'readonly', FALSE, FALSE, FALSE, FALSE, FALSE, FALSE, 48, 0.00, 0.00, 2, TRUE),
(201, 2, 'CAPEX.WAITING_SUPERVISOR', 1, 'CAPEX.REVISED', 501, 'Minta Perbaikan / Revisi', 'btn-warning', 'fa fa-reply', 'Revise', 'Single', NULL, 'readonly', FALSE, FALSE, FALSE, TRUE, FALSE, FALSE, 72, 0.00, 0.00, 3, TRUE),
(201, 5, 'CAPEX.WAITING_FINANCE_DIR', 5, 'CAPEX.APPROVED', 501, 'Persetujuan Final (Approve)', 'btn-success', 'fa fa-check-double', 'Approve', 'Single', NULL, 'readonly', FALSE, TRUE, FALSE, FALSE, FALSE, FALSE, NULL, 0.00, 0.00, 4, TRUE),
(201, 5, 'CAPEX.WAITING_FINANCE_DIR', 1, 'CAPEX.REJECTED', 501, 'Tolak Pengajuan (Reject)', 'btn-danger', 'fa fa-times', 'Reject', 'Single', NULL, 'readonly', FALSE, TRUE, FALSE, TRUE, FALSE, TRUE, NULL, 0.00, 0.00, 5, TRUE);

-- Modul 101: Procurement PR
INSERT INTO "WM_URUTAN_DISPOSISI" (
    "ID_TIPE_DISPOSISI", "ID_ROLE_ASAL", "ID_STATUS_DOK_ASAL", "ID_ROLE", "ID_STATUS_DOK",
    "ID_FORM", "NAMA", "CLASS_BUTTON", "CLASS_ICON", "ACTION_TYPE", "APPROVAL_MODE",
    "UI_FORM_ACTION", "UI_FORM_SETING", "IS_AWAL", "IS_AKHIR", "IS_KIRIM_ATASAN",
    "IS_KIRIM_PEMBUAT", "IS_KIRIM_SEBELUMNYA", "IS_REQUIRE_ATTACHMENT", "SLA_HOURS_DEFAULT",
    "AMT_LIMIT_MIN", "AMT_LIMIT_MAX", "NO_URUTAN", "IS_ACTIVE"
) VALUES 
(101, 1, 'PR.DRAFT', 2, 'PR.WAITING_APPROVAL', 101, 'Kirim Pengajuan PR', 'btn-primary', 'fa fa-paper-plane', 'Submit', 'Single', 'internalsave', 'insertable', TRUE, FALSE, TRUE, FALSE, FALSE, FALSE, 24, 0.00, 0.00, 1, TRUE),
(101, 2, 'PR.WAITING_APPROVAL', 2, 'PR.APPROVED', 101, 'Setujui PR (Approve)', 'btn-success', 'fa fa-check', 'Approve', 'Single', NULL, 'readonly', FALSE, TRUE, FALSE, FALSE, FALSE, FALSE, NULL, 0.00, 50000000.00, 2, TRUE),
(101, 2, 'PR.WAITING_APPROVAL', 1, 'PR.REJECTED', 101, 'Tolak PR', 'btn-danger', 'fa fa-times', 'Reject', 'Single', NULL, 'readonly', FALSE, TRUE, FALSE, TRUE, FALSE, FALSE, NULL, 0.00, 0.00, 3, TRUE);

-- 3. Insert Master Delegasi Wewenang Contoh (Plt)
INSERT INTO "WM_WORKFLOW_DELEGATION" (
    "ID_USER_PEMBERI", "ID_USER_PENERIMA", "ID_TIPE_DISPOSISI",
    "TANGGAL_MULAI", "TANGGAL_SELESAI", "ALASAN_DELEGASI",
    "IS_ACTIVE", "CREATED_DATE", "CREATED_BY"
) VALUES (
    2, 6, NULL,
    CURRENT_TIMESTAMP - INTERVAL '1 day',
    CURRENT_TIMESTAMP + INTERVAL '14 days',
    'Cuti Tahunan & Perjalanan Dinas Luar Kota',
    TRUE, CURRENT_TIMESTAMP, 2
);
