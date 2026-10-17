# 🏢 Enterprise Workflow Engine API (.NET Core 10 LTS)

Solusi backend *enterprise-grade* untuk orkestrasi alur kerja approval dokumen bisnis multi-tahap, multi-approver, dan multi-cabang. Dibangun di atas fondasi **Clean Architecture**, **Domain-Driven Design (DDD)**, **C# 13**, dan **Entity Framework Core 10** dengan dukungan penuh untuk **PostgreSQL**, **Microsoft SQL Server**, dan **In-Memory Database**.

---

## 📋 Daftar Isi
1. [Fitur Utama](#-fitur-utama)
2. [Arsitektur Solusi & Struktur Direktori](#-arsitektur-solusi--struktur-direktori)
3. [Prasyarat Sistem](#-prasyarat-sistem)
4. [Panduan Setup & Migrasi Database PostgreSQL](#-panduan-setup--migrasi-database-postgresql)
   - [Metode 1: Menggunakan Docker Compose (Sangat Direkomendasikan)](#metode-1-menggunakan-docker-compose-sangat-direkomendasikan)
   - [Metode 2: Menggunakan .NET EF Core CLI](#metode-2-menggunakan-net-ef-core-cli)
   - [Metode 3: Eksekusi File SQL Script Manual](#metode-3-eksekusi-file-sql-script-manual)
   - [Opsi Alternatif: Mode In-Memory Database](#opsi-alternatif-mode-in-memory-database)
5. [Cara Menjalankan Aplikasi](#-cara-menjalankan-aplikasi)
6. [Panduan Pengujian dengan Swagger UI](#-panduan-pengujian-dengan-swagger-ui)
7. [Dokumentasi & Referensi API Lengkap](#-dokumentasi--referensi-api-lengkap)
8. [Pengujian Otomatis (Testing)](#-pengujian-otomatis-testing)
9. [Fitur Keamanan Enterprise](#-fitur-keamanan-enterprise)
10. [Troubleshooting & FAQ](#-troubleshooting--faq)

---

## 🚀 Fitur Utama

- **Dynamic State Machine Engine**: Transisi status dokumen fleksibel berbasis master data konfigurasi (`WM_TIPE_DISPOSISI` & `WM_URUTAN_DISPOSISI`) tanpa perlu *hardcoded* alur di logika kode.
- **Dukungan PostgreSQL Asli**: Skema relasional yang dioptimalkan, indeks komposit performa tinggi, dan migrasi Entity Framework Core siap pakai.
- **Optimistic Concurrency Control**: Menjamin integritas data dari *race condition* dan *double-approval* saat banyak approver membuka dokumen bersamaan menggunakan token konkurensi (`RowVersion`).
- **Dynamic Approver Matrix & Delegasi (Plt)**: Resolusi otomatis rantai atasan langsung, matriks departemen/jabatan, serta pengalihan wewenang otomatis saat pejabat sedang cuti atau dinas luar.
- **Transactional Outbox Pattern**: Menjamin konsistensi pengiriman event/notifikasi (At-Least-Once Delivery) tanpa risiko *dual-write anomaly* antara transaksi database dan message broker/notifikasi.
- **Background SLA Monitoring Engine**: Background service terjadwal yang memantau batas waktu penyelesaian dokumen secara berkala dan menandai status dokumen yang terlambat (`IS_OVERDUE`).
- **Enterprise File Upload Security**: Validasi ketat berkas lampiran dengan inspeksi *magic byte header*, sanitasi *path traversal*, dan pembatasan ukuran berkas.
- **Real-Time SignalR WebSockets**: Integrasi live notification hub (`/hubs/workflow-notification`) untuk *push update* instan ke antarmuka pengguna frontend (Angular / React).

---

## 🏛 Arsitektur Solusi & Struktur Direktori

Solusi ini menerapkan prinsip **Clean Architecture (Onion Architecture)** di mana dependensi hanya mengarah ke dalam (Domain Core).

```
workflow/
├── docker-compose.yml                      # Stack PostgreSQL 16 & pgAdmin 4
├── test_api.ps1                            # Skrip pengujian otomatis PowerShell
├── migrations/
│   └── postgresql/
│       ├── V1__Initial_Workflow_PostgreSql.sql # Skrip DDL lengkap PostgreSQL
│       └── V2__Seed_Master_Data.sql            # Master data alur & seeder contoh
├── Dokumentation/
│   ├── README.md                           # Dokumentasi panduan operasional (file ini)
│   ├── workflow.md                         # Spesifikasi arsitektur & schema detail
│   └── SECURITY.md                         # Standar keamanan enterprise
├── src/
│   ├── WorkflowEngine.Domain/              # Core: Entitas, Enums, Interfaces, Domain Exceptions
│   │   ├── Entities/                       # WtTasklist, WmUrutanDisposisi, WtWorkflowOutbox, dll.
│   │   ├── Enums/                          # ApprovalStatus, WorkflowActionType, ApprovalMode, dll.
│   │   └── Interfaces/                     # IWorkflowDbContext, ICurrentUserService
│   ├── WorkflowEngine.Application/         # Use Cases, DTOs, Business Logic, FluentValidation
│   │   ├── Common/                         # BaseResponse, Pagination, Exceptions
│   │   ├── DTOs/                           # StartWorkflowDto, NextActionDto, WorkflowStateDto
│   │   └── Services/                       # WorkflowEngineService, FileValidationService
│   ├── WorkflowEngine.Infrastructure/      # Persistence EF Core, Migrations, Background Workers
│   │   ├── Hubs/                           # WorkflowNotificationHub (SignalR)
│   │   ├── Persistence/
│   │   │   ├── Configurations/             # EF Core Entity Type Configurations
│   │   │   ├── Migrations/                 # EF Core Code-First Migrations PostgreSQL
│   │   │   ├── Seed/                       # Seeder Master Data otomatis saat startup
│   │   │   └── WorkflowDbContextFactory.cs # Design-time factory untuk EF CLI
│   │   └── Services/                       # LocalFileStorageService, WorkflowSlaMonitoringService
│   └── WorkflowEngine.WebApi/              # ASP.NET Core Web API, Controllers, Middleware, OpenAPI
│       ├── Controllers/                    # WorkflowController, DevToolsController
│       ├── Filters/                        # ApiExceptionFilterAttribute
│       ├── Properties/launchSettings.json  # Konfigurasi binding port (5000 & 5241)
│       ├── appsettings.json                # Pengaturan koneksi database & logging
│       └── Program.cs                      # Entry point, dependency injection, pipeline
└── tests/
    └── WorkflowEngine.Tests/               # Unit & Integration Tests (xUnit + FluentAssertions)
        └── WorkflowEngineServiceTests.cs   # Pengujian validasi transisi, outbox, dan konkurensi
```

---

## 💻 Prasyarat Sistem

Sebelum memulai, pastikan perangkat Anda telah terpasang:

1. **.NET SDK**: Versi **10.0** (atau .NET 8 / 9 LTS)  
   Periksa instalasi:
   ```powershell
   dotnet --version
   ```
2. **EF Core Global CLI Tool**:
   ```powershell
   dotnet tool install --global dotnet-ef
   # Atau jika sudah ada, update ke versi terbaru:
   dotnet tool update --global dotnet-ef
   ```
3. **Database PostgreSQL**: Versi **15** atau **16** (dapat diinstal lokal atau via Docker).
4. **Docker Desktop** *(opsional, sangat direkomendasikan)* untuk instalasi PostgreSQL sekali jalan.

---

## 🐘 Panduan Setup & Migrasi Database PostgreSQL

Tersedia **3 metode** untuk menyiapkan database PostgreSQL. Pilih metode yang paling sesuai dengan lingkungan kerja Anda.

### Metode 1: Menggunakan Docker Compose (Sangat Direkomendasikan)

Metode ini adalah cara paling instan. File `docker-compose.yml` telah dikonfigurasi untuk menjalankan PostgreSQL 16 beserta **pgAdmin 4** dan otomatis mengeksekusi inisialisasi tabel serta master data awal.

1. **Jalankan Docker Compose**:
   ```powershell
   docker compose up -d
   ```
2. **Periksa status container**:
   ```powershell
   docker compose ps
   ```
   Container `workflow-postgres` akan berjalan pada port **5432**, dan `workflow-pgadmin` pada port **5050**.
3. **Akses pgAdmin 4 (Opsional via Web Browser)**:
   - URL: `http://localhost:5050`
   - Email: `admin@workflow.local`
   - Password: `admin`
   - Untuk koneksi ke server database di pgAdmin:
     - **Host name/address**: `postgres` (jika dalam jaringan Docker yang sama) atau `host.docker.internal`
     - **Port**: `5432`
     - **Maintenance database**: `workflow_engine_db`
     - **Username**: `postgres`
     - **Password**: `postgres`

---

### Metode 2: Menggunakan .NET EF Core CLI

Gunakan metode ini jika Anda menjalankan database PostgreSQL lokal atau di server terpisah.

1. **Buka file `src/WorkflowEngine.WebApi/appsettings.json`** dan pastikan konfigurasi mengarah ke PostgreSQL:
   ```json
   {
     "DatabaseProvider": "PostgreSQL",
     "UseInMemoryDatabase": false,
     "ConnectionStrings": {
       "PostgreSqlConnection": "Host=localhost;Port=5432;Database=workflow_engine_db;Username=postgres;Password=postgres",
       "DefaultConnection": "Server=localhost;Database=WorkflowEngineEnterpriseDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
     }
   }
   ```
2. **Jalankan pembaruan skema database langsung via terminal**:
   ```powershell
   dotnet ef database update --project src/WorkflowEngine.Infrastructure --startup-project src/WorkflowEngine.WebApi
   ```
3. **(Opsional) Membuat file migrasi baru di masa mendatang**:
   Jika Anda memodifikasi entitas pada domain, buat migrasi baru dengan perintah:
   ```powershell
   dotnet ef migrations add <NamaMigrasiBaru> --project src/WorkflowEngine.Infrastructure --startup-project src/WorkflowEngine.WebApi --output-dir Persistence/Migrations
   ```

---

### Metode 3: Eksekusi File SQL Script Manual

Jika Anda menggunakan GUI database seperti **DBeaver**, **Navicat**, **pgAdmin**, atau command line `psql`:

1. Buat database baru bernama `workflow_engine_db` di PostgreSQL Anda:
   ```sql
   CREATE DATABASE workflow_engine_db;
   ```
2. Eksekusi skrip struktur tabel (DDL):
   - Buka dan jalankan file: [migrations/postgresql/V1__Initial_Workflow_PostgreSql.sql](file:///d:/wakhid/.net%20project/workflow/migrations/postgresql/V1__Initial_Workflow_PostgreSql.sql)
3. Eksekusi skrip data awal (Master Seeder):
   - Buka dan jalankan file: [migrations/postgresql/V2__Seed_Master_Data.sql](file:///d:/wakhid/.net%20project/workflow/migrations/postgresql/V2__Seed_Master_Data.sql)

Atau via command-line `psql`:
```powershell
psql -h localhost -p 5432 -U postgres -d workflow_engine_db -f migrations/postgresql/V1__Initial_Workflow_PostgreSql.sql
psql -h localhost -p 5432 -U postgres -d workflow_engine_db -f migrations/postgresql/V2__Seed_Master_Data.sql
```

---

### Opsi Alternatif: Mode In-Memory Database

Untuk pengujian cepat atau lingkungan demo tanpa perlu menginstal database PostgreSQL sama sekali:
Buka `src/WorkflowEngine.WebApi/appsettings.json` dan ubah:
```json
{
  "DatabaseProvider": "InMemory",
  "UseInMemoryDatabase": true
}
```
Aplikasi akan secara otomatis membuat dan menyemai database di dalam memori RAM saat dijalankan.

---

## 🏃 Cara Menjalankan Aplikasi

Pastikan container database PostgreSQL telah berjalan terlebih dahulu:
```powershell
docker compose up -d
```

Pilih salah satu dari 2 mode berikut untuk menjalankan Web API:

### Mode 1: Development dengan Auto Hot Reload (Sangat Direkomendasikan 🔥)
Gunakan `dotnet watch` agar setiap kali Anda mengedit kode C# atau file konfigurasi JSON lalu menyimpannya (`Ctrl + S`), aplikasi akan secara otomatis mengompilasi ulang dan menerapkan perubahan secara instan **tanpa perlu mematikan dan me-restart server secara manual**!

Jalankan perintah ini dari root folder proyek:
```powershell
dotnet watch --project src/WorkflowEngine.WebApi/WorkflowEngine.WebApi.csproj
```
> 💡 **Penting**: Flag `--project src/WorkflowEngine.WebApi/WorkflowEngine.WebApi.csproj` **wajib disertakan** saat berada di root folder agar .NET mengetahui secara spesifik proyek utama mana yang ingin dijalankan, atau Anda dapat masuk ke foldernya terlebih dahulu (`cd src/WorkflowEngine.WebApi` lalu `dotnet watch`).

---

### Mode 2: Standar Run (Single Execution)
Jika Anda hanya ingin menjalankan aplikasi satu kali jalan tanpa file watcher:
```powershell
dotnet run --project src/WorkflowEngine.WebApi/WorkflowEngine.WebApi.csproj
```

---

### Output Terminal & Konfirmasi Port Aktif
Saat aplikasi berhasil dijalankan, terminal akan mengonfirmasi port yang terbuka:
```text
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5241
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

Verifikasi server berjalan dengan mengakses endpoint root:
```powershell
curl http://localhost:5000/
```
Response JSON:
```json
{
  "application": "Enterprise Workflow Engine API (.NET Core 10 LTS)",
  "status": "Healthy",
  "version": "2.1.0",
  "swaggerUI": "/swagger",
  "openApiSchema": "/openapi/v1.json"
}
```

---

## 📖 Panduan Pengujian dengan Swagger UI

Aplikasi dilengkapi dengan antarmuka **Swagger UI** interaktif untuk menguji seluruh endpoint secara langsung melalui peramban web (*web browser*).

1. Buka peramban web dan arahkan ke alamat:
   - 👉 **[http://localhost:5000/swagger](http://localhost:5000/swagger)**
   - Atau port alternatif: **[http://localhost:5241/swagger](http://localhost:5241/swagger)**
2. Anda akan melihat halaman dokumentasi OpenAPI dengan daftar controller:
   - `Workflow`: Manajemen lifecycle dokumen approval.
   - `DevTools`: Dashboard visual metrik workflow untuk pengembang.
3. Anda dapat mengklik tombol **"Try it out"** pada setiap endpoint untuk mengirimkan *request payload* dan melihat respon HTTP secara langsung.
4. Spesifikasi raw OpenAPI JSON dapat diakses di:
   - `http://localhost:5000/openapi/v1.json`

---

## 📡 Dokumentasi & Referensi API Lengkap

### Ringkasan Endpoint

| HTTP Method | Endpoint Path | Keterangan & Tujuan |
|---|---|---|
| `POST` | `/api/v1/workflow/start` | Menginisiasi workflow baru dari draft dokumen bisnis |
| `GET` | `/api/v1/workflow/get` | Mengambil status, riwayat, dan tombol aksi workflow aktif |
| `POST` | `/api/v1/workflow/next` | Menjalankan aksi transisi (Approve, Reject, Revise, Forward) |
| `GET` | `/api/v1/workflow/approver-candidates` | Mendapatkan daftar approver valid untuk tahap berikutnya |
| `POST` | `/api/v1/workflow/attachment` | Mengunggah dokumen lampiran pendukung berkas |
| `GET` | `/api/v1/workflow/delegations` | Melihat status pendelegasian wewenang aktif (Plt) |
| `GET` | `/api/v1/dev/dashboard` | Dashboard analitik: jumlah task, SLA overdue, status outbox |
| `GET` | `/api/v1/master/workflow/tipe` | [Master] Daftar seluruh tipe disposisi / modul bisnis |
| `POST` | `/api/v1/master/workflow/tipe` | [Master] Daftarkan tipe disposisi / modul bisnis baru |
| `PUT` | `/api/v1/master/workflow/tipe/{id}` | [Master] Perbarui konfigurasi tipe disposisi |
| `DELETE` | `/api/v1/master/workflow/tipe/{id}` | [Master] Hapus / nonaktifkan tipe disposisi |
| `GET` | `/api/v1/master/workflow/urutan` | [Master] Daftar urutan langkah state machine & tombol aksi |
| `POST` | `/api/v1/master/workflow/urutan` | [Master] Tambah langkah transisi / tombol aksi workflow |
| `PUT` | `/api/v1/master/workflow/urutan/{id}` | [Master] Perbarui konfigurasi langkah transisi |
| `DELETE` | `/api/v1/master/workflow/urutan/{id}` | [Master] Hapus / nonaktifkan langkah urutan |
| `GET` | `/api/v1/master/workflow/delegation` | [Master] Daftar pendelegasian wewenang (Plt) |
| `POST` | `/api/v1/master/workflow/delegation` | [Master] Buat aturan pendelegasian wewenang baru |
| `PUT` | `/api/v1/master/workflow/delegation/{id}`| [Master] Perbarui data pendelegasian wewenang |
| `DELETE`| `/api/v1/master/workflow/delegation/{id}`| [Master] Cabut / hapus pendelegasian wewenang |

---

### Contoh Alur Skenario Lengkap (End-to-End)

Berikut adalah contoh urutan pengujian alur bisnis persetujuan **Capex Asset** (Modul 201):

#### Skenario 1: Inisiasi Workflow Baru (Submit Draft)
Staff inisiator mengajukan dokumen pembelian laptop server senilai Rp 85.000.000 ke atasan langsungnya.

- **Request**:
  `POST http://localhost:5000/api/v1/workflow/start`
  ```json
  {
    "idTipeDisposisi": 201,
    "idDokumen": "CAPEX-2026-X100",
    "noDokumen": "DOC/CPX/2026/001",
    "idForm": 501,
    "idUserAsal": 1,
    "idUserTujuan": 2,
    "priority": 1,
    "slaHours": 48,
    "taskInstanceJson": "{\"item\":\"Server Rack AI\",\"nominal\":85000000,\"dept\":\"IT Infrastructure\"}"
  }
  ```

- **Response (`201 Created`)**:
  ```json
  {
    "success": true,
    "message": "Workflow started successfully",
    "data": {
      "idTask": 1,
      "hashId": "4f9c5d18d45e45a0b59b19e4871da801",
      "idDokumen": "CAPEX-2026-X100",
      "idStatusDok": "CAPEX.WAITING_SUPERVISOR",
      "approvalStatus": "InProgress",
      "assignedToUserId": 2,
      "rowVersion": "AQIDBAUGBwgJCgsMDQ4PEA=="
    }
  }
  ```
  *(Simpan nilai `hashId` dan `rowVersion` untuk langkah berikutnya).*

---

#### Skenario 2: Mengambil Status & Daftar Tombol Aksi yang Tersedia
Saat PIC Atasan (User ID: 2) membuka aplikasi, sistem memanggil endpoint ini untuk mengetahui tombol apa saja yang boleh dia klik.

- **Request**:
  `GET http://localhost:5000/api/v1/workflow/get?hashId=4f9c5d18d45e45a0b59b19e4871da801&currentUserId=2`

- **Response (`200 OK`)**:
  ```json
  {
    "success": true,
    "data": {
      "hashId": "4f9c5d18d45e45a0b59b19e4871da801",
      "noDokumen": "DOC/CPX/2026/001",
      "idStatusDok": "CAPEX.WAITING_SUPERVISOR",
      "currentUserTujuan": 2,
      "approvalStatus": "InProgress",
      "rowVersion": "AQIDBAUGBwgJCgsMDQ4PEA==",
      "actions": [
        {
          "idUrutanDisp": 2,
          "nama": "Setujui & Teruskan ke Direksi Keuangan",
          "actionType": "Approve",
          "classButton": "btn-success",
          "classIcon": "fa fa-check",
          "isRequireAttachment": false
        },
        {
          "idUrutanDisp": 3,
          "nama": "Minta Perbaikan / Revisi",
          "actionType": "Revise",
          "classButton": "btn-warning",
          "classIcon": "fa fa-reply",
          "isRequireAttachment": false
        }
      ]
    }
  }
  ```

---

#### Skenario 3: Atasan Menyetujui Dokumen (Next Action)
Atasan memilih tombol aksi `idUrutanDisp: 2` untuk meneruskan dokumen ke Direksi Keuangan (User ID: 5).

- **Request**:
  `POST http://localhost:5000/api/v1/workflow/next`
  ```json
  {
    "hashId": "4f9c5d18d45e45a0b59b19e4871da801",
    "idUrutanDisp": 2,
    "idUserTujuan": 5,
    "catatan": "Disetujui. Sesuai dengan RKAP Divisi IT Q4.",
    "rowVersion": "AQIDBAUGBwgJCgsMDQ4PEA=="
  }
  ```

- **Response (`200 OK`)**:
  ```json
  {
    "success": true,
    "message": "Workflow action completed successfully",
    "data": {
      "idTask": 1,
      "hashId": "4f9c5d18d45e45a0b59b19e4871da801",
      "idDokumen": "CAPEX-2026-X100",
      "idStatusDok": "CAPEX.WAITING_FINANCE_DIR",
      "approvalStatus": "InProgress",
      "assignedToUserId": 5,
      "rowVersion": "KjS87f+9Dk2Mv/8AAQIDBA=="
    }
  }
  ```

---

#### Skenario 4: Upload Dokumen Lampiran (Attachment)
Pengguna mengunggah dokumen bukti telaah atau faktur penawaran harga.

- **Request**:
  `POST http://localhost:5000/api/v1/workflow/attachment`  
  *Content-Type*: `multipart/form-data`
  - `HashId`: `4f9c5d18d45e45a0b59b19e4871da801`
  - `File`: *(Pilih berkas .pdf / .png / .docx)*

- **Response (`200 OK`)**:
  ```json
  {
    "success": true,
    "message": "File uploaded successfully",
    "data": {
      "idAttachment": 1,
      "fileName": "4f9c5d18d45e45a0b59b19e4871da801_faktur_penawaran.pdf",
      "originalName": "faktur_penawaran.pdf",
      "fileSize": 1048576,
      "contentType": "application/pdf"
    }
  }
  ```

---

#### Skenario 5: Developer Monitoring Dashboard
Melihat ringkasan metrik kesehatan sistem workflow.

- **Request**:
  `GET http://localhost:5000/api/v1/dev/dashboard`

- **Response (`200 OK`)**:
  ```json
  {
    "success": true,
    "data": {
      "totalTasks": 12,
      "inProgressTasks": 8,
      "approvedTasks": 3,
      "rejectedTasks": 1,
      "overdueTasks": 0,
      "pendingOutboxCount": 0,
      "serverTimeUtc": "2026-10-17T09:30:00Z"
    }
  }
  ```

---

## 🛠 Panduan CRUD Manajemen Master Workflow

Endpoint administrasi master workflow berada di prefix `/api/v1/master/workflow` dan dapat dicoba langsung melalui **Swagger UI** (`/swagger`).

### 1. CRUD Tipe Disposisi (`WM_TIPE_DISPOSISI`)
Tipe Disposisi merepresentasikan modul alur kerja (misal: PR, Capex, Reimbursement, Perjalanan Dinas / SPPD).

#### a. Ambil Semua Tipe Disposisi
- **Method**: `GET /api/v1/master/workflow/tipe`
- **Query Params**: `?onlyActive=true` (opsional)

#### b. Tambah Tipe Disposisi Baru
- **Method**: `POST /api/v1/master/workflow/tipe`
- **Request Body**:
  ```json
  {
    "idTipeDisposisi": 401,
    "namaDisposisi": "Surat Perintah Perjalanan Dinas (SPPD)",
    "deskripsi": "Alur pengajuan tugas dinas luar kota dan akomodasi",
    "kodeModul": "HR_SPPD",
    "isActive": true,
    "isReopenClosed": false
  }
  ```
  *(Catatan: Jika `idTipeDisposisi` dikosongkan atau bernilai null, sistem akan meng-generate ID numerik baru secara otomatis).*

#### c. Update Tipe Disposisi
- **Method**: `PUT /api/v1/master/workflow/tipe/401`
- **Request Body**:
  ```json
  {
    "namaDisposisi": "Surat Perintah Perjalanan Dinas (SPPD) Revisi",
    "deskripsi": "Alur pengajuan dinas luar kota terintegrasi",
    "kodeModul": "HR_SPPD_V2",
    "isActive": true,
    "isReopenClosed": true
  }
  ```

#### d. Hapus / Nonaktifkan Tipe Disposisi
- **Method**: `DELETE /api/v1/master/workflow/tipe/401?hardDelete=false`
  - `hardDelete=false` (Default): Soft delete, mengubah `IS_ACTIVE = false`.
  - `hardDelete=true`: Menghapus baris permanen dari database (hanya diizinkan jika belum memiliki data task aktif).

---

### 2. CRUD Urutan Disposisi (State Machine Transitions)
Urutan Disposisi mendefinisikan tombol aksi, transisi status asal ke tujuan, role target, SLA default, dan batasan wewenang nominal (Authority Limit).

#### a. Ambil Daftar Urutan Disposisi
- **Method**: `GET /api/v1/master/workflow/urutan`
- **Query Params**:
  - `?idTipeDisposisi=401` (Filter modul tertentu)
  - `?idStatusDokAsal=SPPD.DRAFT` (Filter status tertentu)
  - `?onlyActive=true`

#### b. Tambah Langkah / Tombol Aksi Baru
- **Method**: `POST /api/v1/master/workflow/urutan`
- **Request Body**:
  ```json
  {
    "idTipeDisposisi": 401,
    "idRoleAsal": 1,
    "idStatusDokAsal": "SPPD.DRAFT",
    "idRole": 2,
    "idStatusDok": "SPPD.WAITING_APPROVAL",
    "idForm": 401,
    "kodeDepartemen": "HRD",
    "kodeJabatan": "MGR",
    "nama": "Ajukan Permohonan SPPD",
    "classButton": "btn-primary",
    "classIcon": "fa fa-paper-plane",
    "actionType": "Submit",
    "approvalMode": "Single",
    "uiFormAction": "internalsave",
    "uiFormSeting": "insertable",
    "isAwal": true,
    "isAkhir": false,
    "isKirimAtasan": true,
    "isKirimPembuat": false,
    "isKirimSebelumnya": false,
    "isRequireAttachment": true,
    "slaHoursDefault": 24,
    "amtLimitMin": 0.00,
    "amtLimitMax": 20000000.00,
    "noUrutan": 1,
    "isActive": true
  }
  ```

#### c. Update Konfigurasi Langkah
- **Method**: `PUT /api/v1/master/workflow/urutan/{id}`
- **Request Body**:
  ```json
  {
    "idRoleAsal": 1,
    "idStatusDokAsal": "SPPD.DRAFT",
    "idRole": 2,
    "idStatusDok": "SPPD.WAITING_APPROVAL",
    "idForm": 401,
    "kodeDepartemen": "HRD",
    "kodeJabatan": "MGR",
    "nama": "Ajukan Permohonan SPPD (SLA Diperpanjang)",
    "classButton": "btn-primary",
    "classIcon": "fa fa-paper-plane",
    "actionType": "Submit",
    "approvalMode": "Single",
    "uiFormAction": "internalsave",
    "uiFormSeting": "insertable",
    "isAwal": true,
    "isAkhir": false,
    "isKirimAtasan": true,
    "isKirimPembuat": false,
    "isKirimSebelumnya": false,
    "isRequireAttachment": true,
    "slaHoursDefault": 48,
    "amtLimitMin": 0.00,
    "amtLimitMax": 30000000.00,
    "noUrutan": 1,
    "isActive": true
  }
  ```

#### d. Hapus / Nonaktifkan Urutan Disposisi
- **Method**: `DELETE /api/v1/master/workflow/urutan/{id}?hardDelete=false`

---

### 3. CRUD Workflow Delegation (Pendelegasian Wewenang / Plt)
Mengatur pelimpahan hak persetujuan dari pejabat asli ke pengganti sementara (Plt) saat cuti atau perjalanan dinas.

#### a. Ambil Daftar Delegasi Wewenang
- **Method**: `GET /api/v1/master/workflow/delegation`
- **Query Params**:
  - `?userId=2` (Filter pemberi atau penerima tertentu)
  - `?onlyActive=true`

#### b. Buat Pendelegasian Wewenang Baru
- **Method**: `POST /api/v1/master/workflow/delegation`
- **Request Body**:
  ```json
  {
    "idUserPenerima": 6,
    "idTipeDisposisi": 201,
    "tanggalMulai": "2026-10-20T00:00:00Z",
    "tanggalSelesai": "2026-10-30T23:59:59Z",
    "alasanDelegasi": "Cuti Tahunan & Perjalanan Dinas Luar Kota"
  }
  ```
  *(Catatan: Jika `idTipeDisposisi` diisi `null`, delegasi wewenang berlaku untuk seluruh tipe alur kerja).*

#### c. Update Pendelegasian Wewenang
- **Method**: `PUT /api/v1/master/workflow/delegation/{id}`
- **Request Body**:
  ```json
  {
    "idUserPenerima": 6,
    "idTipeDisposisi": 201,
    "tanggalMulai": "2026-10-20T00:00:00Z",
    "tanggalSelesai": "2026-11-05T23:59:59Z",
    "alasanDelegasi": "Perpanjangan Cuti Tahunan",
    "isActive": true
  }
  ```

#### d. Cabut / Hapus Delegasi Wewenang
- **Method**: `DELETE /api/v1/master/workflow/delegation/{id}`

---

## 🧪 Pengujian Otomatis (Testing)

### 1. Menjalankan Unit & Integration Tests (xUnit)
Solusi dilengkapi dengan rangkaian pengujian otomatis untuk memvalidasi:
- Transisi status state machine
- Perlindungan race-condition & optimistic concurrency (`DbUpdateConcurrencyException`)
- Validasi pencegahan status final yang sudah *closed*
- Mekanisme transactional outbox dispatcher

Jalankan perintah berikut di terminal:
```powershell
dotnet test
```

Hasil eksekusi:
```text
Passed!  - Failed: 0, Passed: 8, Skipped: 0, Total: 8, Duration: 1 s
```

### 2. Menjalankan Skrip Pengujian End-to-End PowerShell
Tersedia skrip [test_api.ps1](file:///d:/wakhid/.net%20project/workflow/test_api.ps1) yang secara otomatis melakukan tes fungsional API:
```powershell
powershell -ExecutionPolicy Bypass -File ./test_api.ps1
```

---

## 🛡 Fitur Keamanan Enterprise

1. **Anti-Malware & Magic Byte Header Inspection**: Berkas lampiran diverifikasi berdasarkan byte biner sesungguhnya, bukan sekadar ekstensi nama file, untuk mencegah pengunggahan berkas biner berbahaya bertopeng `.pdf` atau `.jpg`.
2. **Path Traversal Shield**: Nama file acak UUID digunakan pada sistem penyimpanan lokal untuk mencegah serangan direktori (`../../etc/passwd`).
3. **Optimistic Concurrency Protection**: Setiap modifikasi dokumen mewajibkan penyertaan token `rowVersion`. Jika ada dua approver mengeklik tombol dalam milidetik yang sama, salah satu request akan ditolak dengan status `409 Conflict`, mencegah terjadinya data korup.
4. **Immutable Audit Trail (`WT_TASKLIST_HIST`)**: Setiap perpindahan status dan catatan revisi dicatat secara permanen tanpa opsi perubahan riwayat lampau.
5. **CORS Policy Terkelola**: Akses lintas domain dibatasi dan dikonfigurasi melalui middleware ASP.NET Core untuk komunikasi aman dengan Single Page Application (SPA).

---

## ❓ Troubleshooting & FAQ

### 1. Muncul error `Connection refused` atau database tidak dapat dihubungi saat startup
**Solusi**:
- Pastikan container PostgreSQL telah berjalan:
  ```powershell
  docker compose up -d
  ```
- Periksa kesesuaian user, password, dan port pada `appsettings.json`.
- Jika Anda tidak memiliki Docker atau PostgreSQL lokal saat pengembangan cepat, aktifkan mode In-Memory di `appsettings.json`:
  ```json
  "UseInMemoryDatabase": true
  ```

### 2. Halaman Swagger tidak bisa dibuka di `http://localhost:5000/swagger`
**Solusi**:
- Periksa konsol terminal saat menjalankan `dotnet run`. Pastikan aplikasi mendengarkan pada port yang sesuai:
  - Coba buka alamat alternatif: `http://localhost:5241/swagger`
- Pastikan Anda mengetikkan `/swagger` dengan huruf kecil lengkap.

### 3. Terjadi error `Conflict: Document has been modified by another process (Concurrency violation)`
**Penyebab**:
Token `rowVersion` yang dikirim dalam request `POST /api/v1/workflow/next` tidak sesuai dengan versi terkini di database (ada user lain yang baru saja melakukan aksi pada dokumen tersebut).  
**Solusi**:
Lakukan `GET /api/v1/workflow/get?hashId=...` terlebih dahulu untuk mengambil `rowVersion` terbaru, lalu ulangi aksi.

---

## 📄 Lisensi & Tim Pengembang
Dikembangkan untuk kebutuhan arsitektur alur kerja enterprise modern berbasis **.NET 10 LTS** & **PostgreSQL**.
Didokumentasikan dan diverifikasi dengan standar industri oleh **Antigravity Prime Architecture Engine**.
