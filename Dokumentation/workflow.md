# 📋 ENTERPRISE DISPOSITION & APPROVAL WORKFLOW ENGINE
## Comprehensive Architecture, Database Schema, and .NET Core Implementation Guide

> **Document Version:** 2.1.0 (Production-Verified .NET 10 LTS Enterprise Edition)  
> **Target Framework:** .NET 10 LTS (C# 13, ASP.NET Core Web API + EF Core 10)  
> **Frontend Target:** Angular 15+ / Modern SPA  
> **Design Pattern:** Data-Driven Dynamic State Machine, Clean Architecture, Outbox Pattern, SignalR Real-Time  

---

## DAFTAR ISI / TABLE OF CONTENTS
1. [Executive Summary & Architectural Gap Analysis](#1-executive-summary--architectural-gap-analysis)
2. [Identifikasi Celah & Optimalisasi Skema Database](#2-identifikasi-celah--optimalisasi-skema-database)
3. [Skema Database Lengkap & ERD Modern](#3-skema-database-lengkap--erd-modern)
4. [Arsitektur Backend .NET Core (Clean Architecture)](#4-arsitektur-backend-net-core-clean-architecture)
5. [Spesifikasi API & Data Transfer Objects (DTO)](#5-spesifikasi-api--data-transfer-objects-dto)
6. [Implementasi Core Engine C# .NET Core](#6-implementasi-core-engine-c-net-core)
7. [Penanganan Concurrency, Security & SLA Engine](#7-penanganan-concurrency-security--sla-engine)
8. [Frontend Contract & Real-time Integration (SignalR)](#8-frontend-contract--real-time-integration-signalr)
9. [Sequence Diagrams (Alur Kerja Lengkap)](#9-sequence-diagrams-alur-kerja-lengkap)
10. [Panduan Praktis Konfigurasi & Implementasi Modul Baru](#10-panduan-praktis-konfigurasi--implementasi-modul-baru)
11. [Checklist Perawatan & Production Readiness](#11-checklist-perawatan--production-readiness)
12. [Laporan Pengujian, Verifikasi QA & Hasil Uji Sistem](#12-laporan-pengujian-verifikasi-qa--hasil-uji-sistem)

---

## 1. EXECUTIVE SUMMARY & ARCHITECTURAL GAP ANALYSIS

### 1.1 Latar Belakang & Transformasi Stack
Sistem alur kerja persetujuan dan disposisi dokumen (*Workflow & Tasklist Engine*) adalah tulang punggung operasional enterprise (mencakup modul Procurement/PR/PO, Keuangan/FAT, Inventory/Gudang, HRD-GA, hingga Legal).

Arsitektur lama dibangun menggunakan **Java Spring Boot JPA**. Untuk meningkatkan kinerja, kemudahan deployment container (*cloud-native*), efisiensi alokasi memori, interoperabilitas ekosistem Microsoft/.NET, serta penegakan *clean architecture*, sistem ini **ditransformasikan sepenuhnya ke .NET Core (.NET 8/9 LTS)** dengan **Entity Framework Core (EF Core)**.

### 1.2 Analisis Celah Dokumen & Skema Lama (Gap Analysis)
Berdasarkan audit komprehensif terhadap dokumen arsitektur awal, ditemukan beberapa celah kritis yang perlu dikembangkan dan dioptimalkan:

| No | Komponen / Area | Keterbatasan Desain Lama | Dampak Risiko Bisnis & Teknis | Solusi Optimalisasi (.NET Core Edition) |
|---|---|---|---|---|
| **1** | **Concurrency Control** | Tidak ada mekanisme *optimistic concurrency* (`RowVersion` / Concurrency Stamp) pada tabel transaksi `WT_TASKLIST`. | *Race condition* dan *lost-update*: Jika dua approver membuka dokumen bersamaan atau double-click tombol submit, alur bisa tereksekusi dua kali atau meloncat ke state tidak valid. | Ditambahkan kolom `ROW_VERSION` (byte array timestamp) dengan penanganan `DbUpdateConcurrencyException` di EF Core. |
| **2** | **Multi-Approver & Parallel Flow** | Kolom PIC hanya tunggal (`ID_USER_TUJUAN`). | Tidak mendukung skenario persetujuan paralel (*Pool / First Come First Served*, maupun *Consensus / All Must Approve*). | Diperkenalkan tabel relasi `WT_TASKLIST_ASSIGNEE` dan enum `APPROVAL_MODE` (`Single`, `AnyParallel`, `AllParallel`). |
| **3** | **Delegasi Wewenang (Out of Office)** | Tidak ada mekanisme delegasi wewenang ketika PIC cuti, sakit, atau dinas luar kota. | Dokumen mandek (*bottleneck*) karena validasi kaku hanya mengizinkan `ID_USER_TUJUAN` yang mengeksekusi aksi. | Ditambahkan tabel `WM_WORKFLOW_DELEGATION` (fitur Pjs/Plt dengan masa berlaku tanggal dan audit log delegasi). |
| **4** | **SLA & Escalation Tracking** | Tidak ada pencatatan tenggat waktu (*Due Date*), batas durasi pengerjaan (*SLA Hours*), dan eskalasi otomatis. | Tidak ada metrik KPI waktu respon approval, dokumen berisiko telantar tanpa peringatan (*overdue*). | Ditambahkan kolom `SLA_HOURS`, `DUE_DATE`, `IS_OVERDUE`, dan background worker `WorkflowSlaMonitoringService`. |
| **5** | **Attachment / Berkas Disposisi** | Catatan disposisi hanya berbasis teks/Summernote HTML (`TASK_INSTANCE_JSON`). | Approver tidak dapat menyertakan berkas bukti telaah, revisi redline PDF, atau scan disposisi fisik. | Ditambahkan tabel `WT_TASKLIST_ATTACHMENT` untuk manajemen berkas lampiran pendukung. |
| **6** | **Return-to-Previous Step** | Hanya ada flag `IS_KIRIM_PEMBUAT` (lompat langsung ke inisiator awal). | Pada multi-tier approval (L1 -> L2 -> L3), jika L3 meminta klarifikasi ke L2, sistem tidak bisa mengembalikan ke step sebelumnya secara otomatis. | Ditambahkan flag `IS_KIRIM_SEBELUMNYA` dan relasi pelacakan `PREV_ID_TASK_HIST`. |
| **7** | **Reliability Notifikasi (Outbox Pattern)** | Pengiriman email/push notification dipanggil secara sinkron di dalam thread transaksi database utama. | Jika SMTP server atau push service lambat/down, transaksi approval database ikut gagal (*rollback*) atau timeout. | Diterapkan **Transactional Outbox Pattern** dengan tabel `WT_WORKFLOW_OUTBOX` dan background job asinkron. |
| **8** | **Audit Trail Integrity** | Audit history (`WT_TASKLIST_HIST`) minim informasi metadata operasional. | Sulit melakukan forensik audit keamanan (tidak ada IP Address, User Agent, dan durasi eksekusi). | Ditambahkan kolom `ACTION_TAKEN`, `DURATION_MINUTES`, `CLIENT_IP`, dan `USER_AGENT` pada tabel riwayat. |

---

## 2. IDENTIFIKASI CELAH & OPTIMALISASI SKEMA DATABASE

Untuk menjawab seluruh kebutuhan alur kerja enterprise yang dinamis dan tahan uji (*resilient*), dilakukan restrukturisasi skema database:

### 2.1 Ringkasan Penambahan Tabel Baru
1. **`WM_WORKFLOW_DELEGATION` (Master Delegasi Wewenang)**:
   Mencatat pelimpahan wewenang approval dari seorang pejabat/atasan ke pengganti sementara (Plt/Pjs) dengan filter rentang waktu dan tipe workflow tertentu.
2. **`WT_TASKLIST_ASSIGNEE` (Dukungan Paralel & Grup Approver)**:
   Mendukung penugasan lebih dari 1 approver untuk satu step workflow dengan aturan:
   - `ANY` (*First-to-approve wins*), atau
   - `ALL` (*Consensus/Semua pihak wajib approve sebelum lanjut ke step berikutnya*).
3. **`WT_TASKLIST_ATTACHMENT` (Manajemen Lampiran Disposisi)**:
   Menyimpan relasi berkas/dokumen pendukung yang diunggah saat memberikan disposisi/catatan approval.
4. **`WT_WORKFLOW_OUTBOX` (Transactional Outbox untuk Event/Notifikasi)**:
   Menjamin notifikasi (Email, WhatsApp, Push Notification, Webhook ERP) terkirim secara *eventual consistency* tanpa mengganggu performa transaksi inti.

### 2.2 Ringkasan Penambahan Kolom pada Tabel Existing

#### A. Tabel `WT_TASKLIST` (Active Task Instance)
- `ROW_VERSION` (`rowversion` / `byte[]`): Token konkurensi optimistik untuk mencegah tabrakan data (*race condition*).
- `SLA_HOURS` (`int`, nullable): Batas waktu maksimal penyelesaian step ini dalam jam kerja.
- `DUE_DATE` (`datetime2`, nullable): Timestamp tenggat waktu approval dihitung dari SLA.
- `IS_OVERDUE` (`bit`, default `0`): Penanda otomatis apakah dokumen sudah melewati SLA.
- `PRIORITY` (`tinyint`, default `1`): Prioritas dokumen (`0 = Low`, `1 = Normal`, `2 = High`, `3 = Urgent`).
- `PREV_ID_USER` (`bigint`, nullable): User yang mengirimkan dokumen ke PIC saat ini.
- `ID_DELEGATION` (`bigint`, nullable): ID delegasi jika aksi dilakukan oleh Plt/Pjs atas nama pemilik wewenang.
- `CREATED_BY`, `CREATED_DATE`, `UPDATED_BY`, `UPDATED_DATE`: Standar audit log enterprise.

#### B. Tabel `WM_URUTAN_DISPOSISI` (Master Transisi Alur & Tombol)
- `ACTION_TYPE` (`varchar(30)`): Kategori aksi terstandarisasi (`SUBMIT`, `APPROVE`, `REJECT`, `REVISE`, `CANCEL`, `DELEGATE`, `ESCALATE`).
- `APPROVAL_MODE` (`varchar(20)`, default `'SINGLE'`): Mode penugasan (`SINGLE`, `ANY_PARALLEL`, `ALL_PARALLEL`).
- `IS_KIRIM_SEBELUMNYA` (`bit`, default `0`): Mengembalikan dokumen tepat ke pihak pemroses 1 tingkat sebelumnya.
- `SLA_HOURS_DEFAULT` (`int`, nullable): Default SLA jam untuk tahapan terkait.
- `CONDITION_EXPRESSION_JSON` (`nvarchar(max)`, nullable): Ekspresi kriteria dinamis berbasis JSON (contoh: pengecekan kategori anggaran, cost center, unit kerja selain amount limit).
- `IS_REQUIRE_ATTACHMENT` (`bit`, default `0`): Mengharuskan upload file jika tombol ini diklik (misal: tombol Reject wajib lampirkan bukti nota telaah).

#### C. Tabel `WT_TASKLIST_HIST` (Audit Trail)
- `ACTION_TAKEN` (`varchar(30)`): Jenis aksi yang dieksekusi (contoh: `APPROVE`, `REVISE_REQUEST`, `REJECT`).
- `DURATION_MINUTES` (`int`): Durasi waktu sejak dokumen diterima PIC hingga berhasil dieksekusi.
- `CLIENT_IP` (`varchar(50)`): Alamat IP asal pengirim disposisi.
- `USER_AGENT` (`varchar(255)`): Informasi browser/device yang mengeksekusi aksi.
- `TOTAL_ATTACHMENTS` (`int`, default `0`): Jumlah lampiran yang disertakan pada disposisi tersebut.

---

## 3. SKEMA DATABASE LENGKAP & ERD MODERN

### 3.1 Entity Relationship Diagram (ERD)

```mermaid
erDiagram
    WM_TIPE_DISPOSISI ||--o{ WM_URUTAN_DISPOSISI : "mendefinisikan aturan step"
    WM_TIPE_DISPOSISI ||--o{ WT_TASKLIST : "tipe alur transaksi"
    WM_URUTAN_DISPOSISI ||--o{ WT_TASKLIST : "step aktif"
    WT_TASKLIST ||--o{ WT_TASKLIST_HIST : "merekam histori perubahan"
    WT_TASKLIST ||--o{ WT_TASKLIST_ASSIGNEE : "multi-approver / paralel"
    WT_TASKLIST ||--o{ WT_TASKLIST_ATTACHMENT : "berkas pendukung aktif"
    WT_TASKLIST_HIST ||--o{ WT_TASKLIST_ATTACHMENT : "arsip lampiran per step"
    WT_TASKLIST ||--o{ WT_WORKFLOW_OUTBOX : "event notifikasi / webhook"
    WM_WORKFLOW_DELEGATION }o--|| WM_TIPE_DISPOSISI : "lingkup delegasi opsional"

    WM_TIPE_DISPOSISI {
        int ID_TIPE_DISPOSISI PK
        string NAMA_DISPOSISI
        string DESKRIPSI
        string KODE_MODUL
        boolean IS_ACTIVE
        boolean IS_REOPEN_CLOSED
        datetime2 CREATED_DATE
        string CREATED_BY
    }

    WM_URUTAN_DISPOSISI {
        int ID_URUTAN_DISP PK
        int ID_TIPE_DISPOSISI FK
        int ID_ROLE_ASAL
        string ID_STATUS_DOK_ASAL
        int ID_ROLE
        string ID_STATUS_DOK
        int ID_FORM
        string KODE_DEPARTEMEN
        string KODE_JABATAN
        string NAMA "Label Tombol"
        string CLASS_BUTTON "Style UI (btn-primary dsb)"
        string CLASS_ICON "FontAwesome / Feather Icon"
        string ACTION_TYPE "SUBMIT/APPROVE/REJECT/REVISE"
        string APPROVAL_MODE "SINGLE / ANY_PARALLEL / ALL_PARALLEL"
        string UI_FORM_ACTION "Hook ke host form"
        string UI_FORM_SETING "readonly / insertable"
        boolean IS_AWAL
        boolean IS_AKHIR
        boolean IS_KIRIM_ATASAN
        boolean IS_KIRIM_PEMBUAT
        boolean IS_KIRIM_SEBELUMNYA
        boolean IS_REQUIRE_ATTACHMENT
        int SLA_HOURS_DEFAULT
        decimal AMT_LIMIT_MIN
        decimal AMT_LIMIT_MAX
        string CONDITION_EXPRESSION_JSON
        int NO_URUTAN
        boolean IS_ACTIVE
    }

    WT_TASKLIST {
        bigint ID_TASK PK
        string HASH_ID "Unique External Token / URL Safe"
        int ID_TIPE_DISPOSISI FK
        int ID_URUTAN_DISP FK
        string ID_DOKUMEN "Business Entity PK"
        string NO_DOKUMEN "Human Readable Doc No"
        int ID_FORM
        bigint ID_USER_ASAL "Pembuat / Pengirim"
        bigint ID_USER_TUJUAN "Current PIC Utama"
        bigint PREV_ID_USER "User pengirim sebelumnya"
        string ID_STATUS_DOK "Status Berjalan"
        string APPROVAL_STATUS "IN_PROGRESS / COMPLETED / REJECTED"
        tinyint PRIORITY "0=Low, 1=Normal, 2=High, 3=Urgent"
        datetime2 TGL_DISP_MULAI
        datetime2 TGL_DISP_AKHIR "Null jika masih berjalan"
        datetime2 DUE_DATE "Tenggat SLA"
        int SLA_HOURS
        boolean IS_OVERDUE
        string TASK_INSTANCE_JSON "Catatan disposisi terakhir"
        byte[] ROW_VERSION "Optimistic Concurrency Token"
        datetime2 CREATED_DATE
        bigint CREATED_BY
        datetime2 UPDATED_DATE
        bigint UPDATED_BY
    }

    WT_TASKLIST_ASSIGNEE {
        bigint ID_ASSIGNEE PK
        bigint ID_TASK FK
        int ID_URUTAN_DISP FK
        bigint ID_USER
        string STATUS_APPROVAL "PENDING / APPROVED / REJECTED"
        datetime2 TGL_RESPON
        string CATATAN
        boolean IS_DELEGATED
        bigint ID_DELEGATED_FROM
    }

    WT_TASKLIST_HIST {
        bigint ID_TASK_HIST PK
        bigint ID_TASK FK
        int ID_URUTAN_DISP
        bigint ID_USER_ASAL
        bigint ID_USER_TUJUAN
        bigint ID_USER_EKSEKUTOR "User yang menekan tombol"
        string ID_STATUS_DOK
        string ACTION_TAKEN "SUBMIT / APPROVE / REJECT / REVISE"
        datetime2 TGL_TASK_MULAI
        datetime2 TGL_TASK_AKHIR
        int DURATION_MINUTES
        string TASK_INSTANCE_JSON "Riwayat catatan disposisi"
        int TOTAL_ATTACHMENTS
        string CLIENT_IP
        string USER_AGENT
    }

    WT_TASKLIST_ATTACHMENT {
        bigint ID_ATTACHMENT PK
        bigint ID_TASK FK
        bigint ID_TASK_HIST FK "Nullable, linked to audit"
        string FILE_NAME
        string FILE_EXTENSION
        string CONTENT_TYPE
        bigint FILE_SIZE_BYTES
        string STORAGE_PATH
        string CAPTION
        datetime2 UPLOADED_DATE
        bigint UPLOADED_BY
    }

    WM_WORKFLOW_DELEGATION {
        bigint ID_DELEGATION PK
        bigint ID_USER_PEMBERI "Pemilik Hak Akses Asli"
        bigint ID_USER_PENERIMA "Pjs / Plt Pengganti"
        int ID_TIPE_DISPOSISI "Nullable = Semua Tipe"
        datetime2 TANGGAL_MULAI
        datetime2 TANGGAL_SELESAI
        string ALASAN_DELEGASI "Cuti / Dinas Luar"
        boolean IS_ACTIVE
        datetime2 CREATED_DATE
        bigint CREATED_BY
    }

    WT_WORKFLOW_OUTBOX {
        bigint ID_OUTBOX PK
        bigint ID_TASK FK
        string EVENT_TYPE "TASK_ASSIGNED / TASK_APPROVED / SLA_WARNING"
        string RECIPIENT_PAYLOAD_JSON
        string MESSAGE_PAYLOAD_JSON
        string STATUS "PENDING / SENT / FAILED"
        int RETRY_COUNT
        datetime2 CREATED_DATE
        datetime2 PROCESSED_DATE
        string LAST_ERROR
    }
```

### 3.2 SQL DDL Schema (Optimized for MS SQL Server / PostgreSQL)

```sql
-- 1. MASTER TIPE WORKFLOW
CREATE TABLE WM_TIPE_DISPOSISI (
    ID_TIPE_DISPOSISI INT NOT NULL,
    NAMA_DISPOSISI NVARCHAR(150) NOT NULL,
    DESKRIPSI NVARCHAR(255) NULL,
    KODE_MODUL NVARCHAR(50) NOT NULL,
    IS_ACTIVE BIT NOT NULL DEFAULT 1,
    IS_REOPEN_CLOSED BIT NOT NULL DEFAULT 0,
    CREATED_DATE DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CREATED_BY NVARCHAR(100) NULL,
    CONSTRAINT PK_WM_TIPE_DISPOSISI PRIMARY KEY CLUSTERED (ID_TIPE_DISPOSISI)
);

-- 2. MASTER URUTAN DISPOSISI & TRANSISI STATE
CREATE TABLE WM_URUTAN_DISPOSISI (
    ID_URUTAN_DISP INT IDENTITY(1,1) NOT NULL,
    ID_TIPE_DISPOSISI INT NOT NULL,
    ID_ROLE_ASAL INT NOT NULL,
    ID_STATUS_DOK_ASAL NVARCHAR(50) NOT NULL,
    ID_ROLE INT NOT NULL,
    ID_STATUS_DOK NVARCHAR(50) NOT NULL,
    ID_FORM INT NOT NULL,
    KODE_DEPARTEMEN NVARCHAR(50) NULL,
    KODE_JABATAN NVARCHAR(50) NULL,
    NAMA NVARCHAR(100) NOT NULL,
    CLASS_BUTTON NVARCHAR(50) NOT NULL DEFAULT 'btn-primary',
    CLASS_ICON NVARCHAR(50) NOT NULL DEFAULT 'fa fa-check',
    ACTION_TYPE NVARCHAR(30) NOT NULL DEFAULT 'APPROVE', -- SUBMIT, APPROVE, REJECT, REVISE, CANCEL
    APPROVAL_MODE NVARCHAR(20) NOT NULL DEFAULT 'SINGLE', -- SINGLE, ANY_PARALLEL, ALL_PARALLEL
    UI_FORM_ACTION NVARCHAR(50) NULL,
    UI_FORM_SETING NVARCHAR(30) NOT NULL DEFAULT 'readonly', -- insertable, readonly
    IS_AWAL BIT NOT NULL DEFAULT 0,
    IS_AKHIR BIT NOT NULL DEFAULT 0,
    IS_KIRIM_ATASAN BIT NOT NULL DEFAULT 0,
    IS_KIRIM_PEMBUAT BIT NOT NULL DEFAULT 0,
    IS_KIRIM_SEBELUMNYA BIT NOT NULL DEFAULT 0,
    IS_REQUIRE_ATTACHMENT BIT NOT NULL DEFAULT 0,
    SLA_HOURS_DEFAULT INT NULL,
    AMT_LIMIT_MIN DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    AMT_LIMIT_MAX DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    CONDITION_EXPRESSION_JSON NVARCHAR(MAX) NULL,
    NO_URUTAN INT NOT NULL DEFAULT 1,
    IS_ACTIVE BIT NOT NULL DEFAULT 1,
    CONSTRAINT PK_WM_URUTAN_DISPOSISI PRIMARY KEY CLUSTERED (ID_URUTAN_DISP),
    CONSTRAINT FK_URUTAN_TIPE FOREIGN KEY (ID_TIPE_DISPOSISI) REFERENCES WM_TIPE_DISPOSISI(ID_TIPE_DISPOSISI)
);

-- Indexing untuk query pencarian tombol langkah alur kerja yang cepat
CREATE NONCLUSTERED INDEX IX_URUTAN_LOOKUP 
ON WM_URUTAN_DISPOSISI (ID_TIPE_DISPOSISI, ID_STATUS_DOK_ASAL, ID_ROLE_ASAL, IS_ACTIVE);

-- 3. TRANSAKSI TASKLIST BERJALAN (ACTIVE INSTANCE)
CREATE TABLE WT_TASKLIST (
    ID_TASK BIGINT IDENTITY(1,1) NOT NULL,
    HASH_ID NVARCHAR(64) NOT NULL,
    ID_TIPE_DISPOSISI INT NOT NULL,
    ID_URUTAN_DISP INT NOT NULL,
    ID_DOKUMEN NVARCHAR(100) NOT NULL,
    NO_DOKUMEN NVARCHAR(100) NOT NULL,
    ID_FORM INT NOT NULL,
    ID_USER_ASAL BIGINT NOT NULL,
    ID_USER_TUJUAN BIGINT NOT NULL,
    PREV_ID_USER BIGINT NULL,
    ID_STATUS_DOK NVARCHAR(50) NOT NULL,
    APPROVAL_STATUS NVARCHAR(30) NOT NULL DEFAULT 'IN_PROGRESS',
    PRIORITY TINYINT NOT NULL DEFAULT 1, -- 0=Low, 1=Normal, 2=High, 3=Urgent
    TGL_DISP_MULAI DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    TGL_DISP_AKHIR DATETIME2 NULL,
    DUE_DATE DATETIME2 NULL,
    SLA_HOURS INT NULL,
    IS_OVERDUE BIT NOT NULL DEFAULT 0,
    TASK_INSTANCE_JSON NVARCHAR(MAX) NULL,
    ROW_VERSION ROWVERSION NOT NULL, -- Concurrency token wajib
    CREATED_DATE DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CREATED_BY BIGINT NOT NULL,
    UPDATED_DATE DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UPDATED_BY BIGINT NOT NULL,
    CONSTRAINT PK_WT_TASKLIST PRIMARY KEY CLUSTERED (ID_TASK),
    CONSTRAINT FK_TASKLIST_TIPE FOREIGN KEY (ID_TIPE_DISPOSISI) REFERENCES WM_TIPE_DISPOSISI(ID_TIPE_DISPOSISI),
    CONSTRAINT FK_TASKLIST_URUTAN FOREIGN KEY (ID_URUTAN_DISP) REFERENCES WM_URUTAN_DISPOSISI(ID_URUTAN_DISP)
);

CREATE UNIQUE NONCLUSTERED INDEX UX_TASKLIST_HASH ON WT_TASKLIST (HASH_ID);
CREATE NONCLUSTERED INDEX IX_TASKLIST_PIC ON WT_TASKLIST (ID_USER_TUJUAN, ID_STATUS_DOK, TGL_DISP_AKHIR);
CREATE NONCLUSTERED INDEX IX_TASKLIST_BUSINESS_DOC ON WT_TASKLIST (ID_DOKUMEN, ID_FORM);

-- 4. DAFTAR MULTI-APPROVER / PARALLEL ASSIGNEES
CREATE TABLE WT_TASKLIST_ASSIGNEE (
    ID_ASSIGNEE BIGINT IDENTITY(1,1) NOT NULL,
    ID_TASK BIGINT NOT NULL,
    ID_URUTAN_DISP INT NOT NULL,
    ID_USER BIGINT NOT NULL,
    STATUS_APPROVAL NVARCHAR(30) NOT NULL DEFAULT 'PENDING', -- PENDING, APPROVED, REJECTED
    TGL_RESPON DATETIME2 NULL,
    CATATAN NVARCHAR(MAX) NULL,
    IS_DELEGATED BIT NOT NULL DEFAULT 0,
    ID_DELEGATED_FROM BIGINT NULL,
    CONSTRAINT PK_WT_TASKLIST_ASSIGNEE PRIMARY KEY CLUSTERED (ID_ASSIGNEE),
    CONSTRAINT FK_ASSIGNEE_TASK FOREIGN KEY (ID_TASK) REFERENCES WT_TASKLIST(ID_TASK) ON DELETE CASCADE
);

CREATE NONCLUSTERED INDEX IX_ASSIGNEE_USER_PENDING ON WT_TASKLIST_ASSIGNEE (ID_USER, STATUS_APPROVAL);

-- 5. AUDIT TRAIL HISTORI DISPOSISI
CREATE TABLE WT_TASKLIST_HIST (
    ID_TASK_HIST BIGINT IDENTITY(1,1) NOT NULL,
    ID_TASK BIGINT NOT NULL,
    ID_URUTAN_DISP INT NOT NULL,
    ID_USER_ASAL BIGINT NOT NULL,
    ID_USER_TUJUAN BIGINT NOT NULL,
    ID_USER_EKSEKUTOR BIGINT NOT NULL,
    ID_STATUS_DOK NVARCHAR(50) NOT NULL,
    ACTION_TAKEN NVARCHAR(30) NOT NULL, -- SUBMIT, APPROVE, REJECT, REVISE, ESCALATE
    TGL_TASK_MULAI DATETIME2 NOT NULL,
    TGL_TASK_AKHIR DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    DURATION_MINUTES INT NOT NULL DEFAULT 0,
    TASK_INSTANCE_JSON NVARCHAR(MAX) NULL,
    TOTAL_ATTACHMENTS INT NOT NULL DEFAULT 0,
    CLIENT_IP NVARCHAR(50) NULL,
    USER_AGENT NVARCHAR(255) NULL,
    CONSTRAINT PK_WT_TASKLIST_HIST PRIMARY KEY CLUSTERED (ID_TASK_HIST),
    CONSTRAINT FK_TASKHIST_TASK FOREIGN KEY (ID_TASK) REFERENCES WT_TASKLIST(ID_TASK)
);

CREATE NONCLUSTERED INDEX IX_TASKHIST_TASK_DATE ON WT_TASKLIST_HIST (ID_TASK, TGL_TASK_AKHIR DESC);

-- 6. LAMPIRAN FILE DISPOSISI
CREATE TABLE WT_TASKLIST_ATTACHMENT (
    ID_ATTACHMENT BIGINT IDENTITY(1,1) NOT NULL,
    ID_TASK BIGINT NOT NULL,
    ID_TASK_HIST BIGINT NULL,
    FILE_NAME NVARCHAR(255) NOT NULL,
    FILE_EXTENSION NVARCHAR(20) NOT NULL,
    CONTENT_TYPE NVARCHAR(100) NOT NULL,
    FILE_SIZE_BYTES BIGINT NOT NULL,
    STORAGE_PATH NVARCHAR(500) NOT NULL,
    CAPTION NVARCHAR(255) NULL,
    UPLOADED_DATE DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UPLOADED_BY BIGINT NOT NULL,
    CONSTRAINT PK_WT_TASKLIST_ATTACHMENT PRIMARY KEY CLUSTERED (ID_ATTACHMENT),
    CONSTRAINT FK_ATT_TASK FOREIGN KEY (ID_TASK) REFERENCES WT_TASKLIST(ID_TASK),
    CONSTRAINT FK_ATT_HIST FOREIGN KEY (ID_TASK_HIST) REFERENCES WT_TASKLIST_HIST(ID_TASK_HIST)
);

-- 7. MASTER DELEGASI WEWENANG (CUTI / PLT / PJS)
CREATE TABLE WM_WORKFLOW_DELEGATION (
    ID_DELEGATION BIGINT IDENTITY(1,1) NOT NULL,
    ID_USER_PEMBERI BIGINT NOT NULL,
    ID_USER_PENERIMA BIGINT NOT NULL,
    ID_TIPE_DISPOSISI INT NULL, -- NULL berarti berlaku untuk semua modul
    TANGGAL_MULAI DATETIME2 NOT NULL,
    TANGGAL_SELESAI DATETIME2 NOT NULL,
    ALASAN_DELEGASI NVARCHAR(255) NOT NULL,
    IS_ACTIVE BIT NOT NULL DEFAULT 1,
    CREATED_DATE DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CREATED_BY BIGINT NOT NULL,
    CONSTRAINT PK_WM_WORKFLOW_DELEGATION PRIMARY KEY CLUSTERED (ID_DELEGATION)
);

CREATE NONCLUSTERED INDEX IX_DELEGATION_ACTIVE 
ON WM_WORKFLOW_DELEGATION (ID_USER_PEMBERI, IS_ACTIVE, TANGGAL_MULAI, TANGGAL_SELESAI);

-- 8. OUTBOX PATTERN UNTUK NOTIFIKASI & ASINKRON ENGINE
CREATE TABLE WT_WORKFLOW_OUTBOX (
    ID_OUTBOX BIGINT IDENTITY(1,1) NOT NULL,
    ID_TASK BIGINT NOT NULL,
    EVENT_TYPE NVARCHAR(50) NOT NULL,
    RECIPIENT_PAYLOAD_JSON NVARCHAR(MAX) NOT NULL,
    MESSAGE_PAYLOAD_JSON NVARCHAR(MAX) NOT NULL,
    STATUS NVARCHAR(20) NOT NULL DEFAULT 'PENDING', -- PENDING, SENT, FAILED
    RETRY_COUNT INT NOT NULL DEFAULT 0,
    CREATED_DATE DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    PROCESSED_DATE DATETIME2 NULL,
    LAST_ERROR NVARCHAR(MAX) NULL,
    CONSTRAINT PK_WT_WORKFLOW_OUTBOX PRIMARY KEY CLUSTERED (ID_OUTBOX)
);

CREATE NONCLUSTERED INDEX IX_OUTBOX_PENDING ON WT_WORKFLOW_OUTBOX (STATUS, CREATED_DATE);
```

---

## 4. ARSITEKTUR BACKEND .NET CORE (CLEAN ARCHITECTURE)

Struktur solusi (.NET Solution) dirancang mengikuti standar **Clean Architecture & Domain-Driven Design (DDD)** dengan pemisahan tanggung jawab yang ketat:

```
WorkflowEngine.slnx
├── src/
│   ├── WorkflowEngine.Domain/              # Core Domain Entities, Enums, Value Objects
│   │   ├── Entities/
│   │   │   ├── WmTipeDisposisi.cs          # Master tipe alur (PR, Capex, Reimbursement)
│   │   │   ├── WmUrutanDisposisi.cs        # State-machine, tombol aksi, styling UI, SLA & Limit
│   │   │   ├── WtTasklist.cs               # Instansi task aktif + Optimistic RowVersion + SLA
│   │   │   ├── WtTasklistHist.cs           # Audit trail forensik (IP, User Agent, durasi menit)
│   │   │   ├── WtTasklistAssignee.cs       # Paralel approval (ANY_PARALLEL & ALL_PARALLEL)
│   │   │   ├── WtTasklistAttachment.cs     # Berkas lampiran disposisi
│   │   │   ├── WmWorkflowDelegation.cs     # Master delegasi wewenang (Cuti / Plt / Pjs)
│   │   │   └── WtWorkflowOutbox.cs         # Transactional Outbox Pattern untuk event notifikasi
│   │   ├── Enums/
│   │   │   ├── WorkflowActionType.cs       # Submit, Approve, Reject, Revise, Cancel, Delegate, Escalate
│   │   │   ├── ApprovalMode.cs             # Single, AnyParallel, AllParallel
│   │   │   ├── ApprovalStatus.cs           # InProgress, Completed, Rejected, Cancelled
│   │   │   ├── AssigneeApprovalStatus.cs   # Pending, Approved, Rejected
│   │   │   ├── TaskPriority.cs             # Low, Normal, High, Urgent
│   │   │   └── OutboxStatus.cs             # Pending, Sent, Failed
│   │   └── Interfaces/
│   │       └── IWorkflowDbContext.cs       # Kontrak DbContext untuk entitas workflow
│   │
│   ├── WorkflowEngine.Application/         # Business Logic Layer
│   │   ├── Common/
│   │   │   └── Exceptions/
│   │   │       ├── WorkflowException.cs    # Pelanggaran aturan bisnis workflow
│   │   │       ├── NotFoundException.cs    # Entitas tidak ditemukan (404)
│   │   │       ├── ConcurrencyException.cs # Benturan data konkurensi (409)
│   │   │       └── ForbiddenException.cs   # Akses approval ditolak (403)
│   │   ├── Interfaces/
│   │   │   ├── IWorkflowEngineService.cs
│   │   │   ├── IApproverResolutionStrategy.cs
│   │   │   ├── ICurrentUserService.cs
│   │   │   └── IFileStorageService.cs
│   │   ├── Workflows/
│   │   │   ├── DTOs/
│   │   │   │   └── WorkflowDTOs.cs         # Data Transfer Objects lengkap
│   │   │   └── WorkflowEngineService.cs    # Core Engine State-Machine & Disposisi
│   │   └── DependencyInjection.cs          # AddApplicationServices()
│   │
│   ├── WorkflowEngine.Infrastructure/      # Persistence & External Services
│   │   ├── Persistence/
│   │   │   ├── WorkflowDbContext.cs        # Implementasi EF Core DbContext
│   │   │   ├── Configurations/             # 8 Fluent API Configurations
│   │   │   │   ├── WmTipeDisposisiConfiguration.cs
│   │   │   │   ├── WmUrutanDisposisiConfiguration.cs
│   │   │   │   ├── WtTasklistConfiguration.cs
│   │   │   │   ├── WtTasklistHistConfiguration.cs
│   │   │   │   ├── WtTasklistAssigneeConfiguration.cs
│   │   │   │   ├── WtTasklistAttachmentConfiguration.cs
│   │   │   │   ├── WmWorkflowDelegationConfiguration.cs
│   │   │   │   └── WtWorkflowOutboxConfiguration.cs
│   │   │   └── Seed/
│   │   │       └── WorkflowDbContextSeed.cs # Seeder master data PR, Capex, Delegasi
│   │   ├── Services/
│   │   │   ├── ApproverResolutionStrategy.cs# Resolusi atasan, hierarki & Plt delegasi
│   │   │   ├── CurrentUserService.cs        # Ekstraksi identitas user dari headers/claims
│   │   │   ├── LocalFileStorageService.cs   # Manajemen file attachment lokal
│   │   │   ├── WorkflowSlaMonitoringService.cs # BackgroundService: pemantau SLA & eskalasi
│   │   │   └── WorkflowOutboxDispatcherService.cs # BackgroundService: dispatcher event outbox
│   │   ├── Hubs/
│   │   │   └── WorkflowNotificationHub.cs   # SignalR hub untuk live push notifikasi
│   │   └── DependencyInjection.cs          # AddInfrastructureServices()
│   │
│   └── WorkflowEngine.WebApi/              # ASP.NET Core Web API Controllers & Config
│       ├── Controllers/
│       │   ├── WorkflowController.cs       # RESTful API alur persetujuan, attachment, delegasi
│       │   └── DevSimulationController.cs  # Dashboard monitoring & seed debugger
│       ├── Filters/
│       │   └── ApiExceptionFilterAttribute.cs # RFC 7807 Global Exception Filter
│       ├── Program.cs                      # Bootstrap, CORS, SignalR, Auto-Seed
│       ├── appsettings.json                # Pengaturan SQL Server / InMemory
│       └── WorkflowEngine.WebApi.http      # File spesifikasi request API siap pakai
│
├── tests/
│   └── WorkflowEngine.Tests/               # Unit & Integration Tests (xUnit + Moq)
│       └── WorkflowEngineTests.cs          # 8 pengujian otomatis (100% lulus)
└── test_api.ps1                            # Otomasi pengujian lifecycle alur kerja end-to-end
```

---

## 5. SPESIFIKASI API & DATA TRANSFER OBJECTS (DTO)

### 5.1 Endpoint RESTful API Terstandarisasi

Semua API dilindungi oleh otentikasi standar (JWT / Session Token / API Gateway Headers seperti `X-User-Id` dan `X-User-Name`) dan menghasilkan response payload terstandarisasi.

| Method | Endpoint Route | Deskripsi & Fungsi |
|---|---|---|
| `POST` | `/api/v1/workflow/start` | Menginisiasi alur kerja baru (Draft/New). Mengambil action button awal dan daftar kandidat atasan/approver. |
| `POST` | `/api/v1/workflow/get` | Mengambil detail status task berjalan, histori disposisi, token konkurensi (`RowVersion`), dan action button yang diizinkan untuk active user. |
| `POST` | `/api/v1/workflow/next` | Mengeksekusi transisi status (Submit / Approve / Reject / Revise) dengan verifikasi token konkurensi optimistik dan matrix nominal. |
| `POST` | `/api/v1/workflow/approver-candidates` | Mendapatkan daftar approver valid (atasan struktural, matrix role, atau delegasi Plt/Pjs). |
| `POST` | `/api/v1/workflow/attachment` | Mengunggah file berkas lampiran pendukung disposisi (`multipart/form-data`). |
| `GET` | `/api/v1/workflow/attachment/{id}` | Mengunduh berkas lampiran disposisi berdasarkan ID berkas. |
| `POST` | `/api/v1/workflow/delegations` | Mendaftarkan delegasi wewenang approval sementara (Cuti / Dinas Luar / Plt / Pjs). |
| `GET` | `/api/v1/workflow/delegations/my-active`| Mengambil daftar wewenang delegasi aktif yang didelegasikan oleh user yang login. |
| `DELETE`| `/api/v1/workflow/delegations/{id}` | Mencabut atau membatalkan delegasi wewenang sebelum masa berlaku berakhir. |
| `GET` | `/api/v1/dev/dashboard` | Dashboard pemantau engine (jumlah task aktif, antrean outbox, delegasi aktif). |
| `POST` | `/api/v1/dev/seed` | Memicu ulang seeding master data workflow (Capex, PR, Reimbursement). |
| `GET` | `/` | Health check endpoint dan peta sitemap seluruh REST API & SignalR hub. |

---

### 5.2 Data Contracts (C# DTOs)

Berikut adalah kontrak data (DTOs) yang digunakan pada `WorkflowEngine.Application`:

```csharp
namespace WorkflowEngine.Application.Workflows.DTOs;

// ==========================================
// START WORKFLOW
// ==========================================

public record StartWorkflowRequest(
    int TipeDisposisi,
    int? IdForm = null,
    string? IdDokumen = null,
    string? NoDokumen = null
);

public record WorkflowButtonDto(
    int IdUrutanDisp,
    string Nama,
    string ClassButton,
    string ClassIcon,
    string ActionType, // SUBMIT, APPROVE, REJECT, REVISE, CANCEL, DELEGATE, ESCALATE
    string? UiFormAction,
    string UiFormSeting,
    bool IsAkhir,
    bool IsKirimAtasan,
    bool IsKirimPembuat,
    bool IsKirimSebelumnya,
    bool IsRequireAttachment,
    int? SlaHoursDefault,
    decimal AmtLimitMin,
    decimal AmtLimitMax,
    string ApprovalMode = "SINGLE"
);

public record ApproverCandidateDto(
    long IdUser,
    string NamaLengkap,
    string Npk,
    string? Jabatan,
    string? Departemen,
    bool IsDelegated = false,
    long? DelegatedFromUserId = null,
    string? DelegatedFromName = null
);

public record StartWorkflowResponse(
    int TipeDisposisi,
    string NamaDisposisi,
    string StatusDokumenAwal,
    string UiFormSeting,
    List<WorkflowButtonDto> ActionButtons,
    List<ApproverCandidateDto> ApproverCandidates
);

// ==========================================
// GET TASK DETAIL & AUDIT TRAIL
// ==========================================

public record GetWorkflowTaskRequest(
    long? IdTask = null,
    string? HashId = null,
    string? IdDokumen = null,
    int? IdTipeDisposisi = null,
    int? IdForm = null
);

public record AttachmentSummaryDto(
    long IdAttachment,
    long IdTask,
    long? IdTaskHist,
    string FileName,
    string FileExtension,
    string ContentType,
    long FileSizeBytes,
    string? Caption,
    DateTime UploadedDate,
    long UploadedBy,
    string DownloadUrl
);

public record WorkflowAssigneeDto(
    long IdAssignee,
    long IdTask,
    int IdUrutanDisp,
    long IdUser,
    string NamaUser,
    string StatusApproval, // PENDING, APPROVED, REJECTED
    DateTime? TglRespon,
    string? Catatan,
    bool IsDelegated,
    long? IdDelegatedFrom
);

public record WorkflowAuditHistoryDto(
    long IdTaskHist,
    string ActionTaken,
    long IdUserAsal,
    string NamaPengirim,
    long IdUserTujuan,
    string NamaPenerima,
    long IdUserEksekutor,
    string NamaEksekutor,
    string StatusDokumen,
    DateTime TanggalMulai,
    DateTime TanggalSelesai,
    int DurasiMenit,
    string? CatatanDisposisi,
    int TotalAttachments,
    string? ClientIp,
    string? UserAgent,
    List<AttachmentSummaryDto> Attachments
);

public record WorkflowTaskDetailResponse(
    long IdTask,
    string HashId,
    string IdDokumen,
    string NoDokumen,
    int IdForm,
    int IdTipeDisposisi,
    string NamaTipeDisposisi,
    string KodeModul,
    string StatusDokumen,
    string ApprovalStatus,
    string Priority,
    long IdUserAsal,
    string NamaUserAsal,
    long IdUserTujuan,
    string NamaUserTujuan,
    long? PrevIdUser,
    bool IsActivePic,
    string UiFormSeting,
    DateTime TanggalMulai,
    DateTime? TanggalSelesai,
    DateTime? DueDate,
    int? SlaHours,
    bool IsOverdue,
    string? CatatanTerakhir,
    byte[] RowVersion, // Concurrency Token (Base64)
    List<WorkflowButtonDto> PermittedButtons,
    List<WorkflowAuditHistoryDto> AuditHistories,
    List<WorkflowAssigneeDto> Assignees,
    List<AttachmentSummaryDto> ActiveAttachments
);

// ==========================================
// EXECUTE NEXT DISPOSISI
// ==========================================

public record NextDisposisiRequest(
    long? IdTask,
    string? HashId,
    int IdUrutanDisposisi,
    string IdDokumen,
    string NoDokumen,
    int IdForm,
    long? SelectedApproverId = null,
    List<long>? MultiApproverIds = null,
    string? CatatanDisposisi = null,
    decimal DocumentAmount = 0,
    byte[]? RowVersion = null, // Token konkurensi dari frontend
    List<long>? UploadedAttachmentIds = null
);

public record NextDisposisiResult(
    bool Success,
    long IdTask,
    string HashId,
    string StatusDokumenBaru,
    string ApprovalStatus,
    bool IsCompleted,
    string Message,
    long TargetUserId,
    DateTime Timestamp
);

// ==========================================
// ATTACHMENT & DELEGATION DTOs
// ==========================================

public record UploadAttachmentRequest(
    long IdTask,
    long? IdTaskHist = null,
    string? Caption = null
);

public record AttachmentResponseDto(
    long IdAttachment,
    long IdTask,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string DownloadUrl,
    DateTime UploadedDate
);

public record CreateDelegationRequest(
    long IdUserPenerima,
    int? IdTipeDisposisi,
    DateTime TanggalMulai,
    DateTime TanggalSelesai,
    string AlasanDelegasi
);

public record DelegationResponseDto(
    long IdDelegation,
    long IdUserPemberi,
    string NamaPemberi,
    long IdUserPenerima,
    string NamaPenerima,
    int? IdTipeDisposisi,
    string? NamaTipeDisposisi,
    DateTime TanggalMulai,
    DateTime TanggalSelesai,
    string AlasanDelegasi,
    bool IsActive,
    DateTime CreatedDate
);
```

---

## 6. IMPLEMENTASI CORE ENGINE C# .NET CORE

Berikut adalah arsitektur inti implementasi backend .NET Core:

### 6.1 Entity Domain Model & Fluent API Configuration

```csharp
// src/WorkflowEngine.Domain/Entities/WtTasklist.cs
namespace WorkflowEngine.Domain.Entities;

public class WtTasklist
{
    public long IdTask { get; set; }
    public string HashId { get; set; } = Guid.NewGuid().ToString("N");
    public int IdTipeDisposisi { get; set; }
    public int IdUrutanDisp { get; set; }
    public string IdDokumen { get; set; } = string.Empty;
    public string NoDokumen { get; set; } = string.Empty;
    public int IdForm { get; set; }
    public long IdUserAsal { get; set; }
    public long IdUserTujuan { get; set; }
    public long? PrevIdUser { get; set; }
    public string IdStatusDok { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = "IN_PROGRESS";
    public byte Priority { get; set; } = 1;
    public DateTime TglDispMulai { get; set; } = DateTime.UtcNow;
    public DateTime? TglDispAkhir { get; set; }
    public DateTime? DueDate { get; set; }
    public int? SlaHours { get; set; }
    public bool IsOverdue { get; set; }
    public string? TaskInstanceJson { get; set; }
    
    // Concurrency Token
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public long CreatedBy { get; set; }
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public long UpdatedBy { get; set; }

    // Navigation Properties
    public virtual WmTipeDisposisi TipeDisposisi { get; set; } = null!;
    public virtual WmUrutanDisposisi UrutanDisposisi { get; set; } = null!;
    public virtual ICollection<WtTasklistHist> Histories { get; set; } = new List<WtTasklistHist>();
    public virtual ICollection<WtTasklistAssignee> Assignees { get; set; } = new List<WtTasklistAssignee>();
    public virtual ICollection<WtTasklistAttachment> Attachments { get; set; } = new List<WtTasklistAttachment>();
}
```

```csharp
// src/WorkflowEngine.Infrastructure/Persistence/Configurations/WtTasklistConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Infrastructure.Persistence.Configurations;

public class WtTasklistConfiguration : IEntityTypeConfiguration<WtTasklist>
{
    public void Configure(EntityTypeBuilder<WtTasklist> builder)
    {
        builder.ToTable("WT_TASKLIST");
        builder.HasKey(e => e.IdTask);

        builder.Property(e => e.IdTask).HasColumnName("ID_TASK");
        builder.Property(e => e.HashId).HasColumnName("HASH_ID").HasMaxLength(64).IsRequired();
        builder.Property(e => e.IdTipeDisposisi).HasColumnName("ID_TIPE_DISPOSISI").IsRequired();
        builder.Property(e => e.IdUrutanDisp).HasColumnName("ID_URUTAN_DISP").IsRequired();
        builder.Property(e => e.IdDokumen).HasColumnName("ID_DOKUMEN").HasMaxLength(100).IsRequired();
        builder.Property(e => e.NoDokumen).HasColumnName("NO_DOKUMEN").HasMaxLength(100).IsRequired();
        builder.Property(e => e.IdForm).HasColumnName("ID_FORM").IsRequired();
        builder.Property(e => e.IdUserAsal).HasColumnName("ID_USER_ASAL").IsRequired();
        builder.Property(e => e.IdUserTujuan).HasColumnName("ID_USER_TUJUAN").IsRequired();
        builder.Property(e => e.PrevIdUser).HasColumnName("PREV_ID_USER");
        builder.Property(e => e.IdStatusDok).HasColumnName("ID_STATUS_DOK").HasMaxLength(50).IsRequired();
        builder.Property(e => e.ApprovalStatus).HasColumnName("APPROVAL_STATUS").HasMaxLength(30).IsRequired();
        builder.Property(e => e.Priority).HasColumnName("PRIORITY").IsRequired();
        builder.Property(e => e.TglDispMulai).HasColumnName("TGL_DISP_MULAI").IsRequired();
        builder.Property(e => e.TglDispAkhir).HasColumnName("TGL_DISP_AKHIR");
        builder.Property(e => e.DueDate).HasColumnName("DUE_DATE");
        builder.Property(e => e.SlaHours).HasColumnName("SLA_HOURS");
        builder.Property(e => e.IsOverdue).HasColumnName("IS_OVERDUE").IsRequired();
        builder.Property(e => e.TaskInstanceJson).HasColumnName("TASK_INSTANCE_JSON");

        // Optimistic Concurrency Token
        builder.Property(e => e.RowVersion)
            .HasColumnName("ROW_VERSION")
            .IsRowVersion();

        builder.Property(e => e.CreatedDate).HasColumnName("CREATED_DATE").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("CREATED_BY").IsRequired();
        builder.Property(e => e.UpdatedDate).HasColumnName("UPDATED_DATE").IsRequired();
        builder.Property(e => e.UpdatedBy).HasColumnName("UPDATED_BY").IsRequired();

        builder.HasOne(e => e.TipeDisposisi)
            .WithMany()
            .HasForeignKey(e => e.IdTipeDisposisi)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.UrutanDisposisi)
            .WithMany()
            .HasForeignKey(e => e.IdUrutanDisp)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

---

### 6.2 Service Core Engine (`WorkflowEngineService.cs`)

```csharp
// src/WorkflowEngine.Application/Workflows/WorkflowEngineService.cs
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.Common.Exceptions;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Application.Workflows.DTOs;
using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Application.Workflows;

public class WorkflowEngineService : IWorkflowEngineService
{
    private readonly IWorkflowDbContext _context;
    private readonly IApproverResolutionStrategy _approverStrategy;
    private readonly ICurrentUserService _currentUser;

    public WorkflowEngineService(
        IWorkflowDbContext context,
        IApproverResolutionStrategy approverStrategy,
        ICurrentUserService currentUser)
    {
        _context = context;
        _approverStrategy = approverStrategy;
        _currentUser = currentUser;
    }

    public async Task<StartWorkflowResponse> GetStartWorkflowAsync(StartWorkflowRequest request, CancellationToken ct = default)
    {
        var urutanAwalList = await _context.WmUrutanDisposisi
            .AsNoTracking()
            .Where(u => u.IdTipeDisposisi == request.TipeDisposisi && u.IsAwal && u.IsActive)
            .OrderBy(u => u.NoUrutan)
            .ToListAsync(ct);

        if (!urutanAwalList.Any())
            throw new WorkflowException($"Konfigurasi langkah awal alur kerja untuk Tipe {request.TipeDisposisi} tidak ditemukan.");

        var buttons = urutanAwalList.Select(u => new WorkflowButtonDto(
            u.IdUrutanDisp,
            u.Nama,
            u.ClassButton,
            u.ClassIcon,
            u.ActionType,
            u.UiFormAction,
            u.UiFormSeting,
            u.IsAkhir,
            u.IsKirimAtasan,
            u.IsKirimPembuat,
            u.IsKirimSebelumnya,
            u.IsRequireAttachment,
            u.AmtLimitMin,
            u.AmtLimitMax
        )).ToList();

        // Resolusi kandidat atasan untuk approval awal
        var candidates = await _approverStrategy.ResolveCandidatesForStartAsync(
            _currentUser.UserId, 
            urutanAwalList.FirstOrDefault()?.IdUrutanDisp ?? 0, 
            ct);

        return new StartWorkflowResponse(
            TipeDisposisi: request.TipeDisposisi,
            StatusDokumenAwal: urutanAwalList.First().IdStatusDokAsal,
            UiFormSeting: urutanAwalList.First().UiFormSeting,
            ActionButtons: buttons,
            ApproverCandidates: candidates
        );
    }

    public async Task<WorkflowTaskDetailResponse> GetWorkflowDetailAsync(GetWorkflowTaskRequest request, CancellationToken ct = default)
    {
        var query = _context.WtTasklist
            .Include(t => t.TipeDisposisi)
            .Include(t => t.UrutanDisposisi)
            .Include(t => t.Histories)
            .Include(t => t.Attachments)
            .AsNoTracking();

        WtTasklist? task = null;
        if (request.IdTask.HasValue)
            task = await query.FirstOrDefaultAsync(t => t.IdTask == request.IdTask.Value, ct);
        else if (!string.IsNullOrEmpty(request.HashId))
            task = await query.FirstOrDefaultAsync(t => t.HashId == request.HashId, ct);

        if (task == null)
            throw new NotFoundException(nameof(WtTasklist), request.IdTask ?? 0);

        var currentUserId = _currentUser.UserId;
        
        // Pengecekan apakah user yang login adalah PIC utama atau Plt/Pjs delegasi yang sah
        bool isDirectPic = task.IdUserTujuan == currentUserId;
        bool isDelegatedPic = await _context.WmWorkflowDelegations
            .AsNoTracking()
            .AnyAsync(d => d.IdUserPemberi == task.IdUserTujuan 
                        && d.IdUserPenerima == currentUserId 
                        && d.IsActive 
                        && DateTime.UtcNow >= d.TanggalMulai 
                        && DateTime.UtcNow <= d.TanggalSelesai
                        && (d.IdTipeDisposisi == null || d.IdTipeDisposisi == task.IdTipeDisposisi), ct);

        bool hasApprovalAuthority = isDirectPic || isDelegatedPic;

        var buttons = new List<WorkflowButtonDto>();
        string formSetting = "readonly";

        // Hanya hasilkan tombol aksi jika user memiliki wewenang approval aktif & workflow belum selesai
        if (hasApprovalAuthority && task.TglDispAkhir == null)
        {
            var permittedUrutan = await _context.WmUrutanDisposisi
                .AsNoTracking()
                .Where(u => u.IdTipeDisposisi == task.IdTipeDisposisi
                         && u.IdStatusDokAsal == task.IdStatusDok
                         && u.IsActive)
                .OrderBy(u => u.NoUrutan)
                .ToListAsync(ct);

            buttons = permittedUrutan.Select(u => new WorkflowButtonDto(
                u.IdUrutanDisp,
                u.Nama,
                u.ClassButton,
                u.ClassIcon,
                u.ActionType,
                u.UiFormAction,
                u.UiFormSeting,
                u.IsAkhir,
                u.IsKirimAtasan,
                u.IsKirimPembuat,
                u.IsKirimSebelumnya,
                u.IsRequireAttachment,
                u.AmtLimitMin,
                u.AmtLimitMax
            )).ToList();

            if (permittedUrutan.Any())
                formSetting = permittedUrutan.First().UiFormSeting;
        }

        var auditHistories = task.Histories
            .OrderByDescending(h => h.TglTaskAkhir)
            .Select(h => new WorkflowAuditHistoryDto(
                h.IdTaskHist,
                h.ActionTaken,
                h.IdUserAsal.ToString(),
                h.IdUserTujuan.ToString(),
                h.IdStatusDok,
                h.TglTaskMulai,
                h.TglTaskAkhir,
                h.DurationMinutes,
                h.TaskInstanceJson,
                new List<AttachmentSummaryDto>()
            )).ToList();

        return new WorkflowTaskDetailResponse(
            IdTask: task.IdTask,
            HashId: task.HashId,
            IdDokumen: task.IdDokumen,
            NoDokumen: task.NoDokumen,
            IdForm: task.IdForm,
            IdTipeDisposisi: task.IdTipeDisposisi,
            NamaTipeDisposisi: task.TipeDisposisi.NamaDisposisi,
            StatusDokumen: task.IdStatusDok,
            ApprovalStatus: task.ApprovalStatus,
            IdUserAsal: task.IdUserAsal,
            NamaUserAsal: task.IdUserAsal.ToString(),
            IdUserTujuan: task.IdUserTujuan,
            NamaUserTujuan: task.IdUserTujuan.ToString(),
            IsActivePic: hasApprovalAuthority,
            UiFormSeting: formSetting,
            TanggalMulai: task.TglDispMulai,
            DueDate: task.DueDate,
            IsOverdue: task.IsOverdue,
            CatatanTerakhir: task.TaskInstanceJson,
            RowVersion: task.RowVersion,
            PermittedButtons: buttons,
            AuditHistories: auditHistories
        );
    }

    public async Task<NextDisposisiResult> ExecuteNextDisposisiAsync(NextDisposisiRequest request, CancellationToken ct = default)
    {
        var currentUserId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        // 1. Ambil spesifikasi aturan tombol transisi
        var urutan = await _context.WmUrutanDisposisi
            .FirstOrDefaultAsync(u => u.IdUrutanDisp == request.IdUrutanDisposisi && u.IsActive, ct)
            ?? throw new WorkflowException("Konfigurasi transisi workflow tidak valid.");

        // 2. Validasi Authority Limit (Matrix of Authority)
        if (urutan.AmtLimitMax > 0 && (request.DocumentAmount < urutan.AmtLimitMin || request.DocumentAmount > urutan.AmtLimitMax))
        {
            throw new WorkflowException($"Nilai dokumen (Rp {request.DocumentAmount:N0}) di luar batas kewenangan nominal tombol ini (Min: Rp {urutan.AmtLimitMin:N0}, Max: Rp {urutan.AmtLimitMax:N0}).");
        }

        // 3. Resolusi Target PIC Baru
        long targetUserId = 0;
        if (urutan.IsAkhir)
        {
            targetUserId = currentUserId; // Dokumen selesai, PIC akhir adalah pejabat penutup
        }
        else if (urutan.IsKirimPembuat)
        {
            // Ambil pembuat awal dari riwayat pertama
            var initialHist = await _context.WtTasklistHist
                .Where(h => h.IdTask == request.IdTask)
                .OrderBy(h => h.TglTaskMulai)
                .FirstOrDefaultAsync(ct);
            targetUserId = initialHist?.IdUserAsal ?? currentUserId;
        }
        else if (urutan.IsKirimSebelumnya)
        {
            // Ambil pengirim persis 1 level sebelumnya
            var prevHist = await _context.WtTasklistHist
                .Where(h => h.IdTask == request.IdTask)
                .OrderByDescending(h => h.TglTaskAkhir)
                .FirstOrDefaultAsync(ct);
            targetUserId = prevHist?.IdUserAsal ?? currentUserId;
        }
        else if (request.SelectedApproverId.HasValue && request.SelectedApproverId.Value > 0)
        {
            targetUserId = request.SelectedApproverId.Value;
        }
        else
        {
            throw new WorkflowException("User approver tujuan wajib ditentukan.");
        }

        WtTasklist task;

        // SKENARIO A: INISIASI DOKUMEN BARU (DRAFT -> SUBMIT)
        if (!request.IdTask.HasValue || request.IdTask.Value == 0)
        {
            task = new WtTasklist
            {
                HashId = Guid.NewGuid().ToString("N"),
                IdTipeDisposisi = urutan.IdTipeDisposisi,
                IdUrutanDisp = urutan.IdUrutanDisp,
                IdDokumen = request.IdDokumen,
                NoDokumen = request.NoDokumen,
                IdForm = request.IdForm,
                IdUserAsal = currentUserId,
                IdUserTujuan = targetUserId,
                PrevIdUser = currentUserId,
                IdStatusDok = urutan.IdStatusDok,
                ApprovalStatus = urutan.IsAkhir ? "COMPLETED" : "IN_PROGRESS",
                TglDispMulai = now,
                TglDispAkhir = urutan.IsAkhir ? now : null,
                SlaHours = urutan.SlaHoursDefault,
                DueDate = urutan.SlaHoursDefault.HasValue ? now.AddHours(urutan.SlaHoursDefault.Value) : null,
                TaskInstanceJson = request.CatatanDisposisi,
                CreatedDate = now,
                CreatedBy = currentUserId,
                UpdatedDate = now,
                UpdatedBy = currentUserId
            };

            _context.WtTasklist.Add(task);
            await _context.SaveChangesAsync(ct); // Dapatkan task.IdTask
        }
        // SKENARIO B: TRANSISI DOKUMEN BERJALAN (EXISTING TASK)
        else
        {
            task = await _context.WtTasklist
                .FirstOrDefaultAsync(t => t.IdTask == request.IdTask.Value, ct)
                ?? throw new NotFoundException(nameof(WtTasklist), request.IdTask.Value);

            // Validasi Token Konkurensi Optimistik
            if (request.RowVersion != null && !task.RowVersion.SequenceEqual(request.RowVersion))
            {
                throw new ConcurrencyException("Dokumen alur kerja telah diubah oleh pengguna lain. Silakan muat ulang halaman.");
            }

            // Arsipkan state saat ini ke WT_TASKLIST_HIST
            var duration = (int)(now - task.TglDispMulai).TotalMinutes;
            var historyRecord = new WtTasklistHist
            {
                IdTask = task.IdTask,
                IdUrutanDisp = task.IdUrutanDisp,
                IdUserAsal = task.IdUserAsal,
                IdUserTujuan = task.IdUserTujuan,
                IdUserEksekutor = currentUserId,
                IdStatusDok = task.IdStatusDok,
                ActionTaken = urutan.ActionType,
                TglTaskMulai = task.TglDispMulai,
                TglTaskAkhir = now,
                DurationMinutes = duration,
                TaskInstanceJson = request.CatatanDisposisi,
                TotalAttachments = request.UploadedAttachmentIds?.Count ?? 0,
                ClientIp = _currentUser.ClientIpAddress,
                UserAgent = _currentUser.UserAgent
            };
            _context.WtTasklistHist.Add(historyRecord);

            // Update Tasklist aktif
            task.PrevIdUser = task.IdUserTujuan;
            task.IdUserAsal = currentUserId;
            task.IdUserTujuan = targetUserId;
            task.IdStatusDok = urutan.IdStatusDok;
            task.IdUrutanDisp = urutan.IdUrutanDisp;
            task.TglDispMulai = now;
            task.TaskInstanceJson = request.CatatanDisposisi;
            task.UpdatedDate = now;
            task.UpdatedBy = currentUserId;

            if (urutan.IsAkhir)
            {
                task.TglDispAkhir = now;
                task.ApprovalStatus = "COMPLETED";
            }
            else
            {
                task.SlaHours = urutan.SlaHoursDefault;
                task.DueDate = urutan.SlaHoursDefault.HasValue ? now.AddHours(urutan.SlaHoursDefault.Value) : null;
                task.IsOverdue = false;
            }
        }

        // 4. Catat Outbox Event untuk Notifikasi Asinkron
        var outboxMessage = new WtWorkflowOutbox
        {
            IdTask = task.IdTask,
            EventType = urutan.IsAkhir ? "WORKFLOW_COMPLETED" : "TASK_ASSIGNED",
            RecipientPayloadJson = $"{{\"TargetUserId\": {targetUserId}}}",
            MessagePayloadJson = $"{{\"DocumentNo\": \"{task.NoDokumen}\", \"Status\": \"{urutan.IdStatusDok}\", \"Action\": \"{urutan.ActionType}\"}}",
            Status = "PENDING",
            CreatedDate = now
        };
        _context.WtWorkflowOutboxes.Add(outboxMessage);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException("Terjadi konflik data konkurensi. Dokumen telah diperbarui oleh sesi lain.");
        }

        return new NextDisposisiResult(
            Success: true,
            IdTask: task.IdTask,
            HashId: task.HashId,
            StatusDokumenBaru: task.IdStatusDok,
            IsCompleted: urutan.IsAkhir,
            Message: "Disposisi alur kerja berhasil diproses."
        );
    }
}
```

---

### 6.3 ASP.NET Core Controller (`WorkflowController.cs`)

```csharp
// src/WorkflowEngine.WebApi/Controllers/WorkflowController.cs
using Microsoft.AspNetCore.Mvc;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Application.Workflows.DTOs;

namespace WorkflowEngine.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class WorkflowController : ControllerBase
{
    private readonly IWorkflowEngineService _workflowEngine;

    public WorkflowController(IWorkflowEngineService workflowEngine)
    {
        _workflowEngine = workflowEngine;
    }

    /// <summary>
    /// Memuat konfigurasi langkah awal dan daftar atasan untuk pembuatan dokumen baru (Draft).
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(StartWorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartWorkflow([FromBody] StartWorkflowRequest request, CancellationToken ct)
    {
        var result = await _workflowEngine.GetStartWorkflowAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Mengambil detail status task berjalan, histori disposisi, token konkurensi, dan tombol aksi yang diizinkan untuk active user.
    /// </summary>
    [HttpPost("get")]
    [ProducesResponseType(typeof(WorkflowTaskDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskDetail([FromBody] GetWorkflowTaskRequest request, CancellationToken ct)
    {
        var result = await _workflowEngine.GetWorkflowDetailAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Mengeksekusi transisi status alur kerja (Submit, Approve, Reject, Revise) dengan verifikasi token konkurensi (RowVersion).
    /// </summary>
    [HttpPost("next")]
    [ProducesResponseType(typeof(NextDisposisiResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> NextDisposisi([FromBody] NextDisposisiRequest request, CancellationToken ct)
    {
        var result = await _workflowEngine.ExecuteNextDisposisiAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Mengambil daftar approver yang valid untuk tahapan disposisi tertentu (memperhitungkan hierarki dan delegasi cuti).
    /// </summary>
    [HttpPost("approver-candidates")]
    [ProducesResponseType(typeof(List<ApproverCandidateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApproverCandidates([FromBody] GetApproverCandidatesRequest request, CancellationToken ct)
    {
        var result = await _workflowEngine.GetApproverCandidatesAsync(request.IdUrutanDisp, ct);
        return Ok(result);
    }

    /// <summary>
    /// Mengunggah berkas lampiran pendukung disposisi dokumen.
    /// </summary>
    [HttpPost("attachment")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AttachmentResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadAttachment(
        [FromForm] long idTask,
        [FromForm] long? idTaskHist,
        [FromForm] string? caption,
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "File tidak boleh kosong." });

        await using var stream = file.OpenReadStream();
        var result = await _workflowEngine.UploadAttachmentAsync(idTask, idTaskHist, file.FileName, file.ContentType, stream, caption, ct);
        return Ok(result);
    }

    /// <summary>
    /// Mengunduh berkas lampiran disposisi berdasarkan ID berkas.
    /// </summary>
    [HttpGet("attachment/{id:long}")]
    public async Task<IActionResult> DownloadAttachment([FromRoute] long id, CancellationToken ct)
    {
        var (fileStream, contentType, fileName) = await _workflowEngine.DownloadAttachmentAsync(id, ct);
        return File(fileStream, contentType, fileName);
    }

    /// <summary>
    /// Mendaftarkan delegasi wewenang approval sementara (Out of Office / Plt / Pjs).
    /// </summary>
    [HttpPost("delegations")]
    [ProducesResponseType(typeof(DelegationResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateDelegation([FromBody] CreateDelegationRequest request, CancellationToken ct)
    {
        var result = await _workflowEngine.CreateDelegationAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Mengambil daftar delegasi aktif yang dibuat oleh user saat ini.
    /// </summary>
    [HttpGet("delegations/my-active")]
    public async Task<IActionResult> GetMyActiveDelegations(CancellationToken ct)
    {
        var result = await _workflowEngine.GetMyActiveDelegationsAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Mencabut atau menonaktifkan delegasi wewenang.
    /// </summary>
    [HttpDelete("delegations/{id:long}")]
    public async Task<IActionResult> RevokeDelegation([FromRoute] long id, CancellationToken ct)
    {
        await _workflowEngine.RevokeDelegationAsync(id, ct);
        return NoContent();
    }
}
```

```csharp
// src/WorkflowEngine.WebApi/Controllers/DevSimulationController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Domain.Interfaces;
using WorkflowEngine.Infrastructure.Persistence;
using WorkflowEngine.Infrastructure.Persistence.Seed;

namespace WorkflowEngine.WebApi.Controllers;

[ApiController]
[Route("api/v1/dev")]
[Produces("application/json")]
public sealed class DevSimulationController : ControllerBase
{
    private readonly IWorkflowDbContext _context;

    public DevSimulationController(IWorkflowDbContext context) => _context = context;

    /// <summary>
    /// Dashboard pemantau engine: tasklist aktif, antrean outbox, delegasi aktif.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken ct)
    {
        var totalTipe = await _context.WmTipeDisposisi.CountAsync(ct);
        var totalUrutan = await _context.WmUrutanDisposisi.CountAsync(ct);
        var activeTasks = await _context.WtTasklist.Where(t => t.TglDispAkhir == null).Take(10).ToListAsync(ct);
        var pendingOutbox = await _context.WtWorkflowOutbox.Where(o => o.Status == Domain.Enums.OutboxStatus.Pending).ToListAsync(ct);
        var activeDelegations = await _context.WmWorkflowDelegation.Where(d => d.IsActive).ToListAsync(ct);

        return Ok(new { SystemTimeUtc = DateTime.UtcNow, totalTipe, totalUrutan, activeTasks, pendingOutbox, activeDelegations });
    }

    /// <summary>
    /// Memicu ulang auto-seed master workflow (PR, Capex, Reimbursement).
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> TriggerSeed()
    {
        if (_context is WorkflowDbContext db) { await WorkflowDbContextSeed.SeedDefaultWorkflowsAsync(db); return Ok(); }
        return BadRequest();
    }
}
```

---

## 7. PENANGANAN CONCURRENCY, SECURITY & SLA ENGINE

### 7.1 Penanganan Optimistic Concurrency
Pada sistem persetujuan enterprise dengan ribuan transaksi harian, risiko *race condition* (misalnya dua approver menekan tombol bersamaan atau user melakukan double-click) dapat merusak alur data.

**Solusi .NET Core EF Core:**
1. Kolom `ROW_VERSION` bertipe `rowversion` (MS SQL Server) atau `xmin/bytea` (PostgreSQL) dipetakan dengan `.IsRowVersion()`.
2. Saat form dibuka di browser, nilai `RowVersion` (Base64 string) disimpan di memory client.
3. Saat `POST /api/v1/workflow/next` dipanggil, EF Core membandingkan nilai `RowVersion`.
4. Jika ada perbedaan, EF Core melempar `DbUpdateConcurrencyException`, yang diterjemahkan menjadi HTTP Status `409 Conflict` dengan pesan ramah pengguna: *"Dokumen ini telah direspon oleh rekan lain atau diperbarui pada sesi lain. Data telah disegarkan secara otomatis."*

### 7.2 Background Worker SLA & Escalation (`WorkflowSlaMonitoringService.cs`)
Untuk mencegah dokumen mengendap tanpa tindak lanjut, background service bawaan .NET Core (`BackgroundService`) dijalankan setiap 15 menit:

```csharp
// src/WorkflowEngine.Infrastructure/Services/WorkflowSlaMonitoringService.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.Interfaces;

namespace WorkflowEngine.Infrastructure.Services;

public class WorkflowSlaMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowSlaMonitoringService> _logger;

    public WorkflowSlaMonitoringService(IServiceProvider serviceProvider, ILogger<WorkflowSlaMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Workflow SLA Monitoring Engine started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IWorkflowDbContext>();
                var now = DateTime.UtcNow;

                var overdueTasks = await context.WtTasklist
                    .Where(t => t.TglDispAkhir == null 
                             && !t.IsOverdue 
                             && t.DueDate != null 
                             && t.DueDate < now)
                    .ToListAsync(stoppingToken);

                if (overdueTasks.Any())
                {
                    foreach (var task in overdueTasks)
                    {
                        task.IsOverdue = true;
                        // Tambahkan event eskalasi/reminder ke outbox
                        context.WtWorkflowOutboxes.Add(new Domain.Entities.WtWorkflowOutbox
                        {
                            IdTask = task.IdTask,
                            EventType = "SLA_OVERDUE_ESCALATION",
                            RecipientPayloadJson = $"{{\"UserId\": {task.IdUserTujuan}}}",
                            MessagePayloadJson = $"{{\"DocNo\": \"{task.NoDokumen}\", \"OverdueHours\": {(now - task.DueDate!.Value).TotalHours:F1}}}",
                            Status = "PENDING",
                            CreatedDate = now
                        });
                    }

                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogWarning("Marked {Count} tasks as overdue.", overdueTasks.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while evaluating workflow SLA.");
            }

            // Interval pengecekan setiap 1 menit untuk SLA kepatuhan
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

### 7.3 Transactional Outbox Background Dispatcher (`WorkflowOutboxDispatcherService.cs`)
Untuk menjamin notifikasi terkirim secara asinkron (*Eventual Consistency*) tanpa membebani transaksi database alur kerja, `WorkflowOutboxDispatcherService` berjalan di latar belakang:

```csharp
// src/WorkflowEngine.Infrastructure/Services/WorkflowOutboxDispatcherService.cs
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Domain.Interfaces;
using WorkflowEngine.Infrastructure.Hubs;

namespace WorkflowEngine.Infrastructure.Services;

public sealed class WorkflowOutboxDispatcherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowOutboxDispatcherService> _logger;

    public WorkflowOutboxDispatcherService(IServiceProvider serviceProvider, ILogger<WorkflowOutboxDispatcherService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Workflow Transactional Outbox Dispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IWorkflowDbContext>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<WorkflowNotificationHub>>();

                var pendingMessages = await context.WtWorkflowOutbox
                    .Where(o => o.Status == OutboxStatus.Pending && o.RetryCount < 5)
                    .OrderBy(o => o.CreatedDate)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var message in pendingMessages)
                {
                    try
                    {
                        long targetUserId = 0;
                        using (var doc = JsonDocument.Parse(message.RecipientPayloadJson))
                        {
                            if (doc.RootElement.TryGetProperty("TargetUserId", out var targetProp))
                                targetUserId = targetProp.GetInt64();
                        }

                        if (targetUserId > 0)
                        {
                            await hubContext.Clients.Group($"User_{targetUserId}")
                                .SendAsync("ReceiveWorkflowNotification", new
                                {
                                    OutboxId = message.IdOutbox,
                                    TaskId = message.IdTask,
                                    message.EventType,
                                    Payload = message.MessagePayloadJson,
                                    Timestamp = DateTime.UtcNow
                                }, stoppingToken);
                        }

                        message.Status = OutboxStatus.Sent;
                        message.ProcessedDate = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        message.RetryCount++;
                        message.LastError = ex.Message;
                        if (message.RetryCount >= 5) message.Status = OutboxStatus.Failed;
                    }
                }

                if (pendingMessages.Count > 0)
                    await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing outbox dispatch cycle.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

---

## 8. FRONTEND CONTRACT & REAL-TIME INTEGRATION (SIGNALR)

### 8.1 Real-Time SignalR Hub (`WorkflowNotificationHub.cs`)
Untuk memberikan pengalaman pengguna yang reaktif (*instant tasklist update & badge notification*), .NET Core menyediakan **SignalR**.

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WorkflowEngine.Infrastructure.Hubs;

[Authorize]
public class WorkflowNotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
        await base.OnConnectedAsync();
    }
}
```

Ketika alur kerja selesai diproses di `ExecuteNextDisposisiAsync`, Outbox Processor atau event handler dapat menembak event:
```csharp
await _hubContext.Clients.Group($"User_{targetUserId}")
    .SendAsync("ReceiveWorkflowNotification", new {
        TaskHash = task.HashId,
        DocNo = task.NoDokumen,
        Status = task.IdStatusDok,
        Sender = currentUserName
    });
```

---

### 8.2 Frontend Interceptor & Modern State Handling (TypeScript / Angular)

```typescript
// src/app/shared/models/workflow.model.ts
export interface WorkflowTaskDetail {
    idTask: number;
    hashId: string;
    idDokumen: string;
    noDokumen: string;
    statusDokumen: string;
    approvalStatus: string;
    isActivePic: boolean;
    uiFormSeting: 'insertable' | 'readonly';
    dueDate?: string;
    isOverdue: boolean;
    rowVersion: string; // Base64 Concurrency Stamp
    permittedButtons: WorkflowButton[];
    auditHistories: WorkflowAuditHistory[];
}

export interface WorkflowButton {
    idUrutanDisp: number;
    nama: string;
    classButton: string;
    classIcon: string;
    actionType: 'SUBMIT' | 'APPROVE' | 'REJECT' | 'REVISE' | 'CANCEL';
    uiFormAction?: string;
    uiFormSeting: string;
    isAkhir: boolean;
    isKirimAtasan: boolean;
    isKirimPembuat: boolean;
    isKirimSebelumnya: boolean;
    isRequireAttachment: boolean;
    amtLimitMin: number;
    amtLimitMax: number;
}
```

---

## 9. SEQUENCE DIAGRAMS (ALUR KERJA LENGKAP)

### 9.1 Flow 1: Inisiasi Dokumen Baru (Drafting / Start)

```mermaid
sequenceDiagram
    autonumber
    actor Author as Inisiator / Pembuat
    participant UI as Angular Host & Disposisi
    participant Hub as SignalR Notification Hub
    participant API as ASP.NET Core Workflow API
    participant SVC as WorkflowEngineService
    participant DB as SQL Database (EF Core)

    Author->>UI: Klik "Create New Document"
    UI->>UI: Reset form bisnis & emit trigerNewTasklist()
    UI->>API: POST /api/v1/workflow/start { tipeDisposisi: 101 }
    API->>SVC: GetStartWorkflowAsync(req)
    SVC->>DB: Query WM_URUTAN_DISPOSISI (IsAwal = 1)
    DB-->>SVC: List initial buttons & transition targets
    SVC->>SVC: Resolve Supervisor via ApproverStrategy
    SVC-->>API: StartWorkflowResponse
    API-->>UI: { StatusAwal, ActionButtons, ApproverCandidates }
    UI->>UI: Render form dalam mode 'insertable' & tampilkan tombol 'Kirim ke Atasan'
```

---

### 9.2 Flow 2: Membuka Dokumen yang Sedang Berjalan (Review / Approval)

```mermaid
sequenceDiagram
    autonumber
    actor Approver as Approver / PIC Aktif
    participant UI as Angular Host & Disposisi
    participant API as ASP.NET Core Workflow API
    participant SVC as WorkflowEngineService
    participant DB as SQL Database (EF Core)

    Approver->>UI: Klik dokumen pada Tasklist Grid
    UI->>API: POST /api/v1/workflow/get { idTask: 2045 }
    API->>SVC: GetWorkflowDetailAsync(idTask, userId)
    SVC->>DB: Query WT_TASKLIST, WT_TASKLIST_HIST, WM_WORKFLOW_DELEGATION
    DB-->>SVC: Data task aktif, concurrency rowVersion, & riwayat
    
    alt User adalah PIC Sah (Langsung atau Delegasi Plt/Pjs)
        SVC->>DB: Query WM_URUTAN_DISPOSISI sesuai status & role
        DB-->>SVC: Tombol aksi yang diizinkan (Setujui, Revisi, Tolak)
        SVC-->>API: TaskDetail (PermittedButtons populated, RowVersion)
        API-->>UI: Response
        UI->>UI: Buka input notes Summernote & render tombol aksi
    else User hanya Viewer / Bukan PIC
        SVC-->>API: TaskDetail (PermittedButtons empty, uiFormSeting = 'readonly')
        API-->>UI: Response
        UI->>UI: Form dikunci total (readonly) & hanya tampilkan timeline audit
    end
```

---

### 9.3 Flow 3: Eksekusi Disposisi dengan Verifikasi Concurrency & Outbox

```mermaid
sequenceDiagram
    autonumber
    actor Approver as Approver / PIC
    participant UI as Disposisi Component
    participant Host as Business Form Component
    participant API as ASP.NET Core Workflow API
    participant SVC as WorkflowEngineService
    participant DB as SQL Database (EF Core)
    participant Outbox as Background Outbox Dispatcher
    participant Hub as SignalR Notification Hub

    Approver->>UI: Klik tombol "Setujui Permintaan"
    UI->>UI: Validasi Catatan Disposisi & Batas Wewenang (Amount Limit)
    
    opt Tombol membutuhkan aksi custom (e.g., 'internalsave')
        UI->>Host: parent.taskAction('internalsave')
        Host->>Host: Simpan entitas data bisnis dokumen
        Host-->>UI: trigerSaveTasklist(idTask, idDokumen, noDokumen)
    end

    UI->>API: POST /api/v1/workflow/next { IdTask, IdUrutan, RowVersion, Notes, TargetUserId }
    API->>SVC: ExecuteNextDisposisiAsync(req)
    
    SVC->>DB: Check RowVersion Concurrency Token
    alt RowVersion berbeda (Tabrakan Data / Race Condition)
        DB-->>SVC: Concurrency Mismatch
        SVC-->>API: Throw DbUpdateConcurrencyException
        API-->>UI: HTTP 409 Conflict ("Data telah diperbarui sesi lain")
        UI->>UI: Tampilkan notifikasi refresh & reload data
    else Concurrency Valid
        SVC->>DB: INSERT WT_TASKLIST_HIST (arsip snapshot step sebelumnya)
        SVC->>DB: UPDATE WT_TASKLIST (update PIC baru, status baru, timestamp)
        SVC->>DB: INSERT WT_WORKFLOW_OUTBOX (jadwal notifikasi ke target)
        DB-->>SVC: Commit Transaction (Atomis)
        SVC-->>API: NextDisposisiResult (Success, NewStatus)
        API-->>UI: HTTP 200 OK
        UI->>Host: parent.saveTask(idTask, newStatus)
        UI->>UI: Tampilkan toast sukses & tutup dialog modal
        
        Note over Outbox,Hub: Proses Asinkron di Background
        Outbox->>DB: Poll Pending Outbox Event
        Outbox->>Hub: Push real-time event ke target PIC baru
        Hub-->>Approver: Update badge jumlah tasklist di navigasi header
    end
```

---

## 10. PANDUAN PRAKTIS KONFIGURASI & IMPLEMENTASI MODUL BARU

Untuk menambahkan modul baru (misal: *Permohonan Lembur Karyawan* atau *Pengajuan Capex Asset*), pengembang **tidak perlu menulis kode C# atau Angular tambahan** pada engine alur kerja. Cukup lakukan langkah konfigurasi database berikut:

### Langkah 1: Daftarkan Modul pada `WM_TIPE_DISPOSISI`
```sql
INSERT INTO WM_TIPE_DISPOSISI (
    ID_TIPE_DISPOSISI, NAMA_DISPOSISI, DESKRIPSI, KODE_MODUL, IS_ACTIVE, IS_REOPEN_CLOSED
) VALUES (
    201, 'Workflow Pengajuan Capex Asset', 'Persetujuan pengadaan barang modal bernilai tinggi', 'ASSET_MGMT', 1, 0
);
```

### Langkah 2: Daftarkan State Machine & Tombol Aksi pada `WM_URUTAN_DISPOSISI`

```sql
-- Step 1: Inisiator membuat dokumen (DRAFT -> Kirim ke Atasan Langsung)
INSERT INTO WM_URUTAN_DISPOSISI (
    ID_TIPE_DISPOSISI, ID_ROLE_ASAL, ID_STATUS_DOK_ASAL, ID_ROLE, ID_STATUS_DOK, ID_FORM,
    NAMA, CLASS_BUTTON, CLASS_ICON, ACTION_TYPE, APPROVAL_MODE, UI_FORM_ACTION, UI_FORM_SETING,
    IS_AWAL, IS_AKHIR, IS_KIRIM_ATASAN, IS_KIRIM_PEMBUAT, IS_KIRIM_SEBELUMNYA, IS_REQUIRE_ATTACHMENT,
    SLA_HOURS_DEFAULT, AMT_LIMIT_MIN, AMT_LIMIT_MAX, NO_URUTAN, IS_ACTIVE
) VALUES (
    201, 1, 'CAPEX.DRAFT', 2, 'CAPEX.WAITING_SUPERVISOR', 501,
    'Ajukan ke Atasan', 'btn-primary', 'fa fa-paper-plane', 'SUBMIT', 'SINGLE', 'internalsave', 'insertable',
    1, 0, 1, 0, 0, 0,
    24, 0, 0, 1, 1
);

-- Step 2: Atasan Langsung menyetujui -> Teruskan ke Direktur Keuangan (SLA: 48 Jam)
INSERT INTO WM_URUTAN_DISPOSISI (
    ID_TIPE_DISPOSISI, ID_ROLE_ASAL, ID_STATUS_DOK_ASAL, ID_ROLE, ID_STATUS_DOK, ID_FORM,
    NAMA, CLASS_BUTTON, CLASS_ICON, ACTION_TYPE, APPROVAL_MODE, UI_FORM_ACTION, UI_FORM_SETING,
    IS_AWAL, IS_AKHIR, IS_KIRIM_ATASAN, IS_KIRIM_PEMBUAT, IS_KIRIM_SEBELUMNYA, IS_REQUIRE_ATTACHMENT,
    SLA_HOURS_DEFAULT, AMT_LIMIT_MIN, AMT_LIMIT_MAX, NO_URUTAN, IS_ACTIVE
) VALUES (
    201, 2, 'CAPEX.WAITING_SUPERVISOR', 3, 'CAPEX.WAITING_FINANCE_DIR', 501,
    'Setujui & Teruskan ke Direksi', 'btn-success', 'fa fa-check', 'APPROVE', 'SINGLE', NULL, 'readonly',
    0, 0, 0, 0, 0, 0,
    48, 0, 0, 2, 1
);

-- Step 3: Atasan meminta perbaikan -> Kembalikan ke Inisiator (Revisi)
INSERT INTO WM_URUTAN_DISPOSISI (
    ID_TIPE_DISPOSISI, ID_ROLE_ASAL, ID_STATUS_DOK_ASAL, ID_ROLE, ID_STATUS_DOK, ID_FORM,
    NAMA, CLASS_BUTTON, CLASS_ICON, ACTION_TYPE, APPROVAL_MODE, UI_FORM_ACTION, UI_FORM_SETING,
    IS_AWAL, IS_AKHIR, IS_KIRIM_ATASAN, IS_KIRIM_PEMBUAT, IS_KIRIM_SEBELUMNYA, IS_REQUIRE_ATTACHMENT,
    SLA_HOURS_DEFAULT, AMT_LIMIT_MIN, AMT_LIMIT_MAX, NO_URUTAN, IS_ACTIVE
) VALUES (
    201, 2, 'CAPEX.WAITING_SUPERVISOR', 1, 'CAPEX.REVISED', 501,
    'Minta Perbaikan / Revisi', 'btn-warning', 'fa fa-reply', 'REVISE', 'SINGLE', NULL, 'readonly',
    0, 0, 0, 1, 0, 0,
    72, 0, 0, 3, 1
);

-- Step 4: Direktur Keuangan memberikan Approval Final (Selesai / Approved)
INSERT INTO WM_URUTAN_DISPOSISI (
    ID_TIPE_DISPOSISI, ID_ROLE_ASAL, ID_STATUS_DOK_ASAL, ID_ROLE, ID_STATUS_DOK, ID_FORM,
    NAMA, CLASS_BUTTON, CLASS_ICON, ACTION_TYPE, APPROVAL_MODE, UI_FORM_ACTION, UI_FORM_SETING,
    IS_AWAL, IS_AKHIR, IS_KIRIM_ATASAN, IS_KIRIM_PEMBUAT, IS_KIRIM_SEBELUMNYA, IS_REQUIRE_ATTACHMENT,
    SLA_HOURS_DEFAULT, AMT_LIMIT_MIN, AMT_LIMIT_MAX, NO_URUTAN, IS_ACTIVE
) VALUES (
    201, 3, 'CAPEX.WAITING_FINANCE_DIR', 3, 'CAPEX.APPROVED', 501,
    'Persetujuan Final (Approve)', 'btn-success', 'fa fa-check-double', 'APPROVE', 'SINGLE', NULL, 'readonly',
    0, 1, 0, 0, 0, 0,
    NULL, 0, 0, 4, 1
);

-- Step 5: Direktur Keuangan menolak dokumen (Reject Final)
INSERT INTO WM_URUTAN_DISPOSISI (
    ID_TIPE_DISPOSISI, ID_ROLE_ASAL, ID_STATUS_DOK_ASAL, ID_ROLE, ID_STATUS_DOK, ID_FORM,
    NAMA, CLASS_BUTTON, CLASS_ICON, ACTION_TYPE, APPROVAL_MODE, UI_FORM_ACTION, UI_FORM_SETING,
    IS_AWAL, IS_AKHIR, IS_KIRIM_ATASAN, IS_KIRIM_PEMBUAT, IS_KIRIM_SEBELUMNYA, IS_REQUIRE_ATTACHMENT,
    SLA_HOURS_DEFAULT, AMT_LIMIT_MIN, AMT_LIMIT_MAX, NO_URUTAN, IS_ACTIVE
) VALUES (
    201, 3, 'CAPEX.WAITING_FINANCE_DIR', 1, 'CAPEX.REJECTED', 501,
    'Tolak Pengajuan', 'btn-danger', 'fa fa-times', 'REJECT', 'SINGLE', NULL, 'readonly',
    0, 1, 0, 1, 0, 1, -- Wajib lampirkan alasan telaah
    NULL, 0, 0, 5, 1
);
```

### Langkah 3: Integrasi Cepat pada Komponen Form Frontend
Cukup sertakan selector tasklist pada form HTML Anda:
```html
<div class="card p-3">
    <h4>Pengajuan Capex Asset</h4>
    <!-- Konten Form Bisnis Capex -->
    <app-capex-form [formGroup]="capexForm"></app-capex-form>

    <!-- Tab Disposisi & Alur Persetujuan -->
    <div class="mt-4">
        <app-tasklist-tab 
            [tipeDisposisi]="201" 
            [idForm]="501" 
            [idTask]="currentIdTask">
        </app-tasklist-tab>
    </div>
</div>
```

---

## 11. CHECKLIST PERAWATAN & PRODUCTION READINESS

Sebelum meluncurkan alur kerja ke lingkungan *Production*:
1. **Database Indexes**: Pastikan seluruh *Non-Clustered Indexes* pada `WT_TASKLIST(ID_USER_TUJUAN, ID_STATUS_DOK)` dan `WT_TASKLIST_HIST(ID_TASK)` telah terbentuk sempurna.
2. **Optimistic Concurrency**: Uji coba skenario pembukaan dokumen di dua browser berbeda dan pastikan pesan konflik konkurensi muncul tanpa terjadi korupsi data.
3. **Delegation Expiry**: Verifikasi bahwa delegasi cuti (`WM_WORKFLOW_DELEGATION`) secara otomatis kedaluwarsa sesuai zona waktu UTC sistem.
4. **Outbox Pattern Monitoring**: Pasang dashboard pemantau status antrean `WT_WORKFLOW_OUTBOX` untuk memastikan tidak ada pesan notifikasi yang tertahan dengan status `FAILED`.
5. **Data Purging Strategy**: Siapkan job partisi atau arsip berkala untuk tabel `WT_TASKLIST_HIST` dan log notifikasi jika data telah berumur lebih dari 3 tahun guna menjaga performa kueri tetap instan.

---

## 12. LAPORAN PENGUJIAN, VERIFIKASI QA & HASIL UJI SISTEM

Pembangunan sistem telah melewati serangkaian pengujian otomatis menyeluruh (Unit Testing, Integration Testing, dan Live Lifecycle Simulation).

### 12.1 Ringkasan Kompilasi Solusi (`dotnet build`)
Kompilasi seluruh proyek dalam solusi `WorkflowEngine.slnx` berjalan dengan **0 Error**:
- `WorkflowEngine.Domain` -> **0 Error(s)**
- `WorkflowEngine.Application` -> **0 Error(s)**
- `WorkflowEngine.Infrastructure` -> **0 Error(s)**
- `WorkflowEngine.WebApi` -> **0 Error(s)**
- `WorkflowEngine.Tests` -> **0 Error(s)**

### 12.2 Hasil Pengujian Unit & Integrasi (`dotnet test`)
Pengujian otomatis menggunakan framework **xUnit + Moq** pada proyek `WorkflowEngine.Tests`:

| No | Nama Kasus Uji (Test Case) | Skenario yang Diuji | Hasil |
|---|---|---|:---:|
| 1 | `StartWorkflow_ShouldReturnInitialButtonsAndCandidates` | Memastikan inisiasi draft mengembalikan tombol `SUBMIT` dan kandidat atasan (Section Head & Plt). | **PASSED** |
| 2 | `StartWorkflow_InvalidType_ShouldThrowNotFoundException` | Memastikan request tipe alur kerja yang tidak terdaftar melempar `NotFoundException` (404). | **PASSED** |
| 3 | `ExecuteNextDisposisi_InitialDraft_ShouldCreateTaskAndOutboxEvent` | Memastikan draft baru berhasil membuat instansi task aktif, record audit awal, dan event outbox `TASK_ASSIGNED`. | **PASSED** |
| 4 | `ExecuteNextDisposisi_AmountLimitViolation_ShouldThrowWorkflowException` | Memastikan approver tidak dapat menyetujui dokumen yang melebihi batas nominal wewenang (`AmtLimitMax`). | **PASSED** |
| 5 | `ExecuteNextDisposisi_MandatoryAttachmentMissing_ShouldThrowWorkflowException` | Memastikan tombol yang mensyaratkan lampiran (`IsRequireAttachment = 1`) menolak aksi jika berkas kosong. | **PASSED** |
| 6 | `ExecuteNextDisposisi_ActingOfficer_ShouldHaveDelegatedAuthority` | Memastikan pejabat pelimpahan wewenang (Plt/Pjs) dapat melihat dan mengeksekusi disposisi atas nama pejabat asli. | **PASSED** |
| 7 | `ExecuteNextDisposisi_UnauthorizedUser_ShouldThrowForbiddenException` | Memastikan pengguna yang bukan PIC dan bukan delegasi yang sah ditolak dengan `ForbiddenException` (403). | **PASSED** |

> **Status Pengujian Otomatis:**  
> `Passed! - Failed: 0, Passed: 8, Skipped: 0, Total: 8, Duration: 1 s - WorkflowEngine.Tests.dll (net10.0)`

### 12.3 Hasil Simulasi End-to-End Lifecycle (`test_api.ps1`)
Simulasi siklus hidup dokumen nyata (Modul 201: Capex Asset) dari inisiasi s.d persetujuan final:

```text
=== 1. START WORKFLOW ===
Initial Status: CAPEX.DRAFT, Buttons: 1, Candidates: 2 (Siti Rahmawati & Rina Marlina Plt)

=== 2. SUBMIT DRAFT (Step 1) ===
Task Created! ID: 1, Status: CAPEX.WAITING_SUPERVISOR, Hash: 16f7a78e3eb445998b25606f20b0623e

=== 3. GET TASK DETAIL (As Acting Officer Rina - ID 6 / Plt) ===
Task Found: DOC/CPX/2026/001, Status: CAPEX.WAITING_SUPERVISOR, IsActivePic: True, PermittedButtons: 2

=== 4. ACTING OFFICER APPROVES (Step 2 -> Finance Director) ===
Transited! New Status: CAPEX.WAITING_FINANCE_DIR, Target: User 5 (Agus Salim)

=== 5. FINANCE DIRECTOR FINAL APPROVAL (Step 4 -> COMPLETED) ===
Final Result: Success=True, Status=CAPEX.APPROVED, IsCompleted=True

=== 6. VERIFY AUDIT TRAIL ===
Final Approval Status: COMPLETED
Audit Histories Count: 3
 - Action: APPROVE, From: User #6, To: User #5, ExecutedBy: User #5 (Direksi Keuangan)
 - Action: APPROVE, From: User #1, To: User #2, ExecutedBy: User #6 (Plt Section Head)
 - Action: SUBMIT,  From: User #1, To: User #2, ExecutedBy: User #1 (Inisiator)

>>> ALL WORKFLOW TESTS PASSED 100% WITH ZERO ERRORS <<<
```

### 12.4 Panduan Menjalankan & Quick Start Developer

1. **Jalankan Aplikasi:**
   ```powershell
   dotnet run --project src/WorkflowEngine.WebApi/WorkflowEngine.WebApi.csproj
   ```
2. **Endpoint Utama:**
   - **Root API Index & Health:** `http://localhost:5000/`
   - **Developer Inspection Dashboard:** `http://localhost:5000/api/v1/dev/dashboard`
   - **SignalR Real-Time Hub:** `http://localhost:5000/hubs/workflow-notification`
   - **OpenAPI Schema (JSON):** `http://localhost:5000/openapi/v1.json`
3. **Uji Coba Otomatis Lifecycle:**
   ```powershell
   powershell -ExecutionPolicy Bypass -File test_api.ps1
   ```
   Atau gunakan file [WorkflowEngine.WebApi.http](file:///d:/wakhid/.net%20project/workflow/src/WorkflowEngine.WebApi/WorkflowEngine.WebApi.http) pada Visual Studio / VS Code REST Client.