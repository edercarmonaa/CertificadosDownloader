PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS SevNivelesEducativos
(
    NivelEducativoId INTEGER NOT NULL PRIMARY KEY,
    Descripcion TEXT NOT NULL CHECK (length(Descripcion) <= 120),
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1)),
    LastSyncedAt TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_SevNivelesEducativos_Descripcion
    ON SevNivelesEducativos (Descripcion);

CREATE TABLE IF NOT EXISTS SevCiclosEscolares
(
    NivelEducativoId INTEGER NOT NULL,
    CicloEscolarId INTEGER NOT NULL,
    Descripcion TEXT NOT NULL CHECK (length(Descripcion) <= 80),
    AnioInicio INTEGER NULL,
    AnioFin INTEGER NULL,
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1)),
    LastSyncedAt TEXT NOT NULL,
    PRIMARY KEY (NivelEducativoId, CicloEscolarId),
    FOREIGN KEY (NivelEducativoId)
        REFERENCES SevNivelesEducativos (NivelEducativoId)
);

CREATE INDEX IF NOT EXISTS IX_SevCiclosEscolares_NivelDescripcion
    ON SevCiclosEscolares (NivelEducativoId, Descripcion);

CREATE TABLE IF NOT EXISTS SevCatalogAudit
(
    AuditId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Actor TEXT NOT NULL,
    OccurredAtUtc TEXT NOT NULL,
    Action TEXT NOT NULL,
    Catalog TEXT NOT NULL,
    NivelEducativoId INTEGER NULL,
    CicloEscolarId INTEGER NULL,
    ProcessedCount INTEGER NOT NULL,
    Result TEXT NOT NULL,
    Warnings TEXT NULL
);
