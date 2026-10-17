using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WorkflowEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WM_TIPE_DISPOSISI",
                columns: table => new
                {
                    ID_TIPE_DISPOSISI = table.Column<int>(type: "integer", nullable: false),
                    NAMA_DISPOSISI = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DESKRIPSI = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    KODE_MODUL = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IS_ACTIVE = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IS_REOPEN_CLOSED = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CREATED_DATE = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CREATED_BY = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WM_TIPE_DISPOSISI", x => x.ID_TIPE_DISPOSISI);
                });

            migrationBuilder.CreateTable(
                name: "WM_URUTAN_DISPOSISI",
                columns: table => new
                {
                    ID_URUTAN_DISP = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_TIPE_DISPOSISI = table.Column<int>(type: "integer", nullable: false),
                    ID_ROLE_ASAL = table.Column<int>(type: "integer", nullable: false),
                    ID_STATUS_DOK_ASAL = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ID_ROLE = table.Column<int>(type: "integer", nullable: false),
                    ID_STATUS_DOK = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ID_FORM = table.Column<int>(type: "integer", nullable: false),
                    KODE_DEPARTEMEN = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    KODE_JABATAN = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NAMA = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CLASS_BUTTON = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "btn-primary"),
                    CLASS_ICON = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "fa fa-check"),
                    ACTION_TYPE = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    APPROVAL_MODE = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Single"),
                    UI_FORM_ACTION = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UI_FORM_SETING = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "readonly"),
                    IS_AWAL = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IS_AKHIR = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IS_KIRIM_ATASAN = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IS_KIRIM_PEMBUAT = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IS_KIRIM_SEBELUMNYA = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IS_REQUIRE_ATTACHMENT = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SLA_HOURS_DEFAULT = table.Column<int>(type: "integer", nullable: true),
                    AMT_LIMIT_MIN = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    AMT_LIMIT_MAX = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    CONDITION_EXPRESSION_JSON = table.Column<string>(type: "text", nullable: true),
                    NO_URUTAN = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    IS_ACTIVE = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WM_URUTAN_DISPOSISI", x => x.ID_URUTAN_DISP);
                    table.ForeignKey(
                        name: "FK_WM_URUTAN_DISPOSISI_WM_TIPE_DISPOSISI_ID_TIPE_DISPOSISI",
                        column: x => x.ID_TIPE_DISPOSISI,
                        principalTable: "WM_TIPE_DISPOSISI",
                        principalColumn: "ID_TIPE_DISPOSISI",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WM_WORKFLOW_DELEGATION",
                columns: table => new
                {
                    ID_DELEGATION = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_USER_PEMBERI = table.Column<long>(type: "bigint", nullable: false),
                    ID_USER_PENERIMA = table.Column<long>(type: "bigint", nullable: false),
                    ID_TIPE_DISPOSISI = table.Column<int>(type: "integer", nullable: true),
                    TANGGAL_MULAI = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TANGGAL_SELESAI = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ALASAN_DELEGASI = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IS_ACTIVE = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CREATED_DATE = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CREATED_BY = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WM_WORKFLOW_DELEGATION", x => x.ID_DELEGATION);
                    table.ForeignKey(
                        name: "FK_WM_WORKFLOW_DELEGATION_WM_TIPE_DISPOSISI_ID_TIPE_DISPOSISI",
                        column: x => x.ID_TIPE_DISPOSISI,
                        principalTable: "WM_TIPE_DISPOSISI",
                        principalColumn: "ID_TIPE_DISPOSISI",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WT_TASKLIST",
                columns: table => new
                {
                    ID_TASK = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HASH_ID = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ID_TIPE_DISPOSISI = table.Column<int>(type: "integer", nullable: false),
                    ID_URUTAN_DISP = table.Column<int>(type: "integer", nullable: false),
                    ID_DOKUMEN = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NO_DOKUMEN = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ID_FORM = table.Column<int>(type: "integer", nullable: false),
                    ID_USER_ASAL = table.Column<long>(type: "bigint", nullable: false),
                    ID_USER_TUJUAN = table.Column<long>(type: "bigint", nullable: false),
                    PREV_ID_USER = table.Column<long>(type: "bigint", nullable: true),
                    ID_STATUS_DOK = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    APPROVAL_STATUS = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "InProgress"),
                    PRIORITY = table.Column<byte>(type: "smallint", nullable: false),
                    TGL_DISP_MULAI = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TGL_DISP_AKHIR = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DUE_DATE = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SLA_HOURS = table.Column<int>(type: "integer", nullable: true),
                    IS_OVERDUE = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TASK_INSTANCE_JSON = table.Column<string>(type: "text", nullable: true),
                    ROW_VERSION = table.Column<byte[]>(type: "bytea", nullable: false),
                    CREATED_DATE = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CREATED_BY = table.Column<long>(type: "bigint", nullable: false),
                    UPDATED_DATE = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UPDATED_BY = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WT_TASKLIST", x => x.ID_TASK);
                    table.ForeignKey(
                        name: "FK_WT_TASKLIST_WM_TIPE_DISPOSISI_ID_TIPE_DISPOSISI",
                        column: x => x.ID_TIPE_DISPOSISI,
                        principalTable: "WM_TIPE_DISPOSISI",
                        principalColumn: "ID_TIPE_DISPOSISI",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WT_TASKLIST_WM_URUTAN_DISPOSISI_ID_URUTAN_DISP",
                        column: x => x.ID_URUTAN_DISP,
                        principalTable: "WM_URUTAN_DISPOSISI",
                        principalColumn: "ID_URUTAN_DISP",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WT_TASKLIST_ASSIGNEE",
                columns: table => new
                {
                    ID_ASSIGNEE = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_TASK = table.Column<long>(type: "bigint", nullable: false),
                    ID_URUTAN_DISP = table.Column<int>(type: "integer", nullable: false),
                    ID_USER = table.Column<long>(type: "bigint", nullable: false),
                    STATUS_APPROVAL = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    TGL_RESPON = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CATATAN = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IS_DELEGATED = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ID_DELEGATED_FROM = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WT_TASKLIST_ASSIGNEE", x => x.ID_ASSIGNEE);
                    table.ForeignKey(
                        name: "FK_WT_TASKLIST_ASSIGNEE_WT_TASKLIST_ID_TASK",
                        column: x => x.ID_TASK,
                        principalTable: "WT_TASKLIST",
                        principalColumn: "ID_TASK",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WT_TASKLIST_HIST",
                columns: table => new
                {
                    ID_TASK_HIST = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_TASK = table.Column<long>(type: "bigint", nullable: false),
                    ID_URUTAN_DISP = table.Column<int>(type: "integer", nullable: false),
                    ID_USER_ASAL = table.Column<long>(type: "bigint", nullable: false),
                    ID_USER_TUJUAN = table.Column<long>(type: "bigint", nullable: false),
                    ID_USER_EKSEKUTOR = table.Column<long>(type: "bigint", nullable: false),
                    ID_STATUS_DOK = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ACTION_TAKEN = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TGL_TASK_MULAI = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TGL_TASK_AKHIR = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DURATION_MINUTES = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TASK_INSTANCE_JSON = table.Column<string>(type: "text", nullable: true),
                    TOTAL_ATTACHMENTS = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CLIENT_IP = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    USER_AGENT = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WT_TASKLIST_HIST", x => x.ID_TASK_HIST);
                    table.ForeignKey(
                        name: "FK_WT_TASKLIST_HIST_WT_TASKLIST_ID_TASK",
                        column: x => x.ID_TASK,
                        principalTable: "WT_TASKLIST",
                        principalColumn: "ID_TASK",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WT_WORKFLOW_OUTBOX",
                columns: table => new
                {
                    ID_OUTBOX = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_TASK = table.Column<long>(type: "bigint", nullable: false),
                    EVENT_TYPE = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RECIPIENT_PAYLOAD_JSON = table.Column<string>(type: "text", nullable: false),
                    MESSAGE_PAYLOAD_JSON = table.Column<string>(type: "text", nullable: false),
                    STATUS = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RETRY_COUNT = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CREATED_DATE = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PROCESSED_DATE = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LAST_ERROR = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WT_WORKFLOW_OUTBOX", x => x.ID_OUTBOX);
                    table.ForeignKey(
                        name: "FK_WT_WORKFLOW_OUTBOX_WT_TASKLIST_ID_TASK",
                        column: x => x.ID_TASK,
                        principalTable: "WT_TASKLIST",
                        principalColumn: "ID_TASK",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WT_TASKLIST_ATTACHMENT",
                columns: table => new
                {
                    ID_ATTACHMENT = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_TASK = table.Column<long>(type: "bigint", nullable: false),
                    ID_TASK_HIST = table.Column<long>(type: "bigint", nullable: true),
                    FILE_NAME = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FILE_EXTENSION = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CONTENT_TYPE = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FILE_SIZE_BYTES = table.Column<long>(type: "bigint", nullable: false),
                    STORAGE_PATH = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CAPTION = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    UPLOADED_DATE = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UPLOADED_BY = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WT_TASKLIST_ATTACHMENT", x => x.ID_ATTACHMENT);
                    table.ForeignKey(
                        name: "FK_WT_TASKLIST_ATTACHMENT_WT_TASKLIST_HIST_ID_TASK_HIST",
                        column: x => x.ID_TASK_HIST,
                        principalTable: "WT_TASKLIST_HIST",
                        principalColumn: "ID_TASK_HIST",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WT_TASKLIST_ATTACHMENT_WT_TASKLIST_ID_TASK",
                        column: x => x.ID_TASK,
                        principalTable: "WT_TASKLIST",
                        principalColumn: "ID_TASK",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WM_TIPE_DISPOSISI_KODEMODUL",
                table: "WM_TIPE_DISPOSISI",
                column: "KODE_MODUL");

            migrationBuilder.CreateIndex(
                name: "IX_URUTAN_STATE_LOOKUP",
                table: "WM_URUTAN_DISPOSISI",
                columns: new[] { "ID_TIPE_DISPOSISI", "ID_STATUS_DOK_ASAL", "IS_ACTIVE" });

            migrationBuilder.CreateIndex(
                name: "IX_DELEGATION_ACTIVE",
                table: "WM_WORKFLOW_DELEGATION",
                columns: new[] { "ID_USER_PEMBERI", "IS_ACTIVE", "TANGGAL_MULAI", "TANGGAL_SELESAI" });

            migrationBuilder.CreateIndex(
                name: "IX_WM_WORKFLOW_DELEGATION_ID_TIPE_DISPOSISI",
                table: "WM_WORKFLOW_DELEGATION",
                column: "ID_TIPE_DISPOSISI");

            migrationBuilder.CreateIndex(
                name: "IX_TASKLIST_DOC_LOOKUP",
                table: "WT_TASKLIST",
                columns: new[] { "ID_DOKUMEN", "ID_TIPE_DISPOSISI" });

            migrationBuilder.CreateIndex(
                name: "IX_TASKLIST_PIC_STATUS",
                table: "WT_TASKLIST",
                columns: new[] { "ID_USER_TUJUAN", "ID_STATUS_DOK" });

            migrationBuilder.CreateIndex(
                name: "IX_TASKLIST_SLA_DUE",
                table: "WT_TASKLIST",
                columns: new[] { "DUE_DATE", "IS_OVERDUE" });

            migrationBuilder.CreateIndex(
                name: "IX_WT_TASKLIST_ID_TIPE_DISPOSISI",
                table: "WT_TASKLIST",
                column: "ID_TIPE_DISPOSISI");

            migrationBuilder.CreateIndex(
                name: "IX_WT_TASKLIST_ID_URUTAN_DISP",
                table: "WT_TASKLIST",
                column: "ID_URUTAN_DISP");

            migrationBuilder.CreateIndex(
                name: "UQ_WT_TASKLIST_HASH_ID",
                table: "WT_TASKLIST",
                column: "HASH_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ASSIGNEE_TASK_USER",
                table: "WT_TASKLIST_ASSIGNEE",
                columns: new[] { "ID_TASK", "ID_USER", "STATUS_APPROVAL" });

            migrationBuilder.CreateIndex(
                name: "IX_ATT_HIST",
                table: "WT_TASKLIST_ATTACHMENT",
                column: "ID_TASK_HIST");

            migrationBuilder.CreateIndex(
                name: "IX_ATT_TASK",
                table: "WT_TASKLIST_ATTACHMENT",
                column: "ID_TASK");

            migrationBuilder.CreateIndex(
                name: "IX_HIST_TASK_AUDIT",
                table: "WT_TASKLIST_HIST",
                columns: new[] { "ID_TASK", "TGL_TASK_AKHIR" });

            migrationBuilder.CreateIndex(
                name: "IX_OUTBOX_PENDING",
                table: "WT_WORKFLOW_OUTBOX",
                columns: new[] { "STATUS", "CREATED_DATE" });

            migrationBuilder.CreateIndex(
                name: "IX_WT_WORKFLOW_OUTBOX_ID_TASK",
                table: "WT_WORKFLOW_OUTBOX",
                column: "ID_TASK");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WM_WORKFLOW_DELEGATION");

            migrationBuilder.DropTable(
                name: "WT_TASKLIST_ASSIGNEE");

            migrationBuilder.DropTable(
                name: "WT_TASKLIST_ATTACHMENT");

            migrationBuilder.DropTable(
                name: "WT_WORKFLOW_OUTBOX");

            migrationBuilder.DropTable(
                name: "WT_TASKLIST_HIST");

            migrationBuilder.DropTable(
                name: "WT_TASKLIST");

            migrationBuilder.DropTable(
                name: "WM_URUTAN_DISPOSISI");

            migrationBuilder.DropTable(
                name: "WM_TIPE_DISPOSISI");
        }
    }
}
