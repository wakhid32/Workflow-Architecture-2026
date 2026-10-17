# TASK SPECIFICATION: KEYCLOAK USER MANAGEMENT & STAFF LINKING MODULE (.NET CORE)

## 1. PROJECT OBJECTIVE & TECH STACK
Tolong buatkan arsitektur dan implementasi kode untuk **Modul User Management & Autentikasi** menggunakan **.NET Core (.NET 8)** yang terintegrasi dengan Keycloak dan tabel data karyawan internal (`M_STAF`).
Modul ini harus dirancang secara **Modular & Decoupled** (dalam folder/project library terpisah di dalam solution) agar mudah di-plug-and-play ke proyek .NET Core lainnya.

### Tech Stack yang Digunakan:
- **Framework**: .NET 8 (ASP.NET Core Web API)
- **ORM**: Entity Framework Core
- **Database**: PostgreSQL / SQL Server
- **Autentikasi**: Keycloak (OIDC / OAuth2 / Keycloak Admin REST API)
- **Pattern**: Clean Architecture / Modular Monolith (Separation of concerns: Domain, Application/Services, Infrastructure, API/Controllers)

---

## 2. CORE FEATURES & WORKFLOW REQUIREMENT

### A. Authentication & User Self-Service (via Keycloak)
1. **Register**: Mendaftarkan user baru ke Keycloak Realm via Keycloak Admin API / Registration flow.
2. **Login**: Autentikasi user (mendapatkan JWT Token dari Keycloak, baik via Direct Access Grants / Password Grant untuk backend API atau Authorization Code Flow).
3. **Lupa Password**: Memicu email *reset password* melalui Keycloak Admin API (`Execute Actions Email` / `UPDATE_PASSWORD`).

### B. Role Management & Access Control
1. **Superadmin Role**: 
   - Role khusus (`ROLE_SUPERADMIN`) di Keycloak untuk mengelola user, menghubungkan staf, dan aktivasi akun.
   - Dilindungi oleh ASP.NET Core Authorization Policies (`[Authorize(Roles = "ROLE_SUPERADMIN")]`).
2. **User Role**: Role dasar (`ROLE_USER`).

### C. Relasi User dengan Tabel Karyawan (`M_STAF`)
1. **Strict 1-to-1 Active User Rule**: 
   - **1 data karyawan pada tabel `M_STAF` HANYA BOLEH terhubung dengan maksimal 1 akun User Keycloak AKTIF pada satu waktu.**
   - Jika staf sudah terhubung dengan user aktif, sistem harus menolak penautan baru atau menonaktifkan user lama sesuai aturan bisnis.
2. **Entity Mapping (`M_STAF`)**:
   - Kolom wajib: `Id` (Guid/Long, PK), `NipKaryawan`, `NamaKaryawan`, `Email`, `IsActive` (bool), dan `KeycloakUserId` (Guid/String, Nullable, Indexed, Unique constraint for active records).

---

## 3. MODULAR FOLDER STRUCTURE (.NET SOLUTION)

Sediakan struktur project/folder di dalam Solution:
```text
Modules/
└── UserManagement/
    ├── UserManagement.Domain/          # Entities (Staf, Value Objects), Domain Exceptions
    ├── UserManagement.Application/     # DTOs, Interfaces (IKeycloakService, IUserRepository), Services/Handlers
    ├── UserManagement.Infrastructure/  # EF Core DbContext, Migrations, KeycloakHttpClient, Repositories
    └── UserManagement.Api/             # Controllers (AuthController, SuperadminUserController), Extensions