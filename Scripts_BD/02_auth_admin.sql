USE [RegistroDB];
GO

SET XACT_ABORT ON;
GO

/* Roles nuevos: no se eliminan ni renombran los roles existentes. */
IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE NombreRol = 'Administrador')
	INSERT INTO dbo.Rol (NombreRol, Descripcion) VALUES ('Administrador', 'Administrador del sistema');
IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE NombreRol = 'Cliente')
	INSERT INTO dbo.Rol (NombreRol, Descripcion) VALUES ('Cliente', 'Cliente registrado');
IF NOT EXISTS (SELECT 1 FROM dbo.Rol WHERE NombreRol = 'Empleado')
	INSERT INTO dbo.Rol (NombreRol, Descripcion) VALUES ('Empleado', 'Empleado registrado');
GO

/* Columnas de autenticación y aprobación. */
IF COL_LENGTH('dbo.Persona', 'PasswordHash') IS NULL
	ALTER TABLE dbo.Persona ADD PasswordHash VARBINARY(64) NULL;
IF COL_LENGTH('dbo.Persona', 'PasswordSalt') IS NULL
	ALTER TABLE dbo.Persona ADD PasswordSalt VARBINARY(32) NULL;
IF COL_LENGTH('dbo.Persona', 'Estatus') IS NULL
	ALTER TABLE dbo.Persona ADD Estatus VARCHAR(20) NOT NULL CONSTRAINT DF_Persona_Estatus DEFAULT ('Pendiente') WITH VALUES;
IF COL_LENGTH('dbo.Persona', 'IntentosFallidos') IS NULL
	ALTER TABLE dbo.Persona ADD IntentosFallidos INT NOT NULL CONSTRAINT DF_Persona_IntentosFallidos DEFAULT (0) WITH VALUES;
IF COL_LENGTH('dbo.Persona', 'BloqueadoHasta') IS NULL
	ALTER TABLE dbo.Persona ADD BloqueadoHasta DATETIME NULL;
IF COL_LENGTH('dbo.Persona', 'UltimoAcceso') IS NULL
	ALTER TABLE dbo.Persona ADD UltimoAcceso DATETIME NULL;
IF COL_LENGTH('dbo.Persona', 'AceptoPrivacidad') IS NULL
	ALTER TABLE dbo.Persona ADD AceptoPrivacidad BIT NOT NULL CONSTRAINT DF_Persona_AceptoPrivacidad DEFAULT (0) WITH VALUES;
IF COL_LENGTH('dbo.Persona', 'FechaAceptacion') IS NULL
	ALTER TABLE dbo.Persona ADD FechaAceptacion DATETIME NULL;
IF COL_LENGTH('dbo.Persona', 'IdAprobador') IS NULL
	ALTER TABLE dbo.Persona ADD IdAprobador INT NULL;
IF COL_LENGTH('dbo.Persona', 'FechaAprobacion') IS NULL
	ALTER TABLE dbo.Persona ADD FechaAprobacion DATETIME NULL;
GO

/* Los registros existentes se conservan como usuarios activos. */
UPDATE dbo.Persona
SET Estatus = 'Activo',
	Activo = 1
WHERE PasswordHash IS NULL
  AND (Estatus IS NULL OR Estatus = 'Pendiente' OR Estatus NOT IN ('Pendiente', 'Activo', 'Rechazado', 'Inactivo'));
GO

IF NOT EXISTS (
	SELECT 1
	FROM sys.check_constraints
	WHERE name = 'CK_Persona_Estatus'
	  AND parent_object_id = OBJECT_ID('dbo.Persona')
)
BEGIN
	ALTER TABLE dbo.Persona WITH CHECK
		ADD CONSTRAINT CK_Persona_Estatus
		CHECK (Estatus IN ('Pendiente', 'Activo', 'Rechazado', 'Inactivo'));
END;
GO

IF NOT EXISTS (
	SELECT 1
	FROM sys.foreign_keys
	WHERE name = 'FK_Persona_IdAprobador'
	  AND parent_object_id = OBJECT_ID('dbo.Persona')
)
BEGIN
	ALTER TABLE dbo.Persona WITH CHECK
		ADD CONSTRAINT FK_Persona_IdAprobador
		FOREIGN KEY (IdAprobador) REFERENCES dbo.Persona (IdPersona);
END;
GO

/* Normalización y unicidad del correo para registros nuevos y existentes. */
UPDATE dbo.Persona
SET Correo = LOWER(LTRIM(RTRIM(Correo)))
WHERE Correo <> LOWER(LTRIM(RTRIM(Correo)));
GO

IF NOT EXISTS (
	SELECT 1
	FROM sys.indexes
	WHERE name = 'UX_Persona_Correo_Normalizado'
	  AND object_id = OBJECT_ID('dbo.Persona')
)
BEGIN
	CREATE UNIQUE INDEX UX_Persona_Correo_Normalizado
		ON dbo.Persona (Correo);
END;
GO

IF NOT EXISTS (
	SELECT 1
	FROM sys.indexes
	WHERE name = 'IX_Persona_Estatus_Rol'
	  AND object_id = OBJECT_ID('dbo.Persona')
)
BEGIN
	CREATE INDEX IX_Persona_Estatus_Rol
		ON dbo.Persona (Estatus, IdRol);
END;
GO

/* Garantiza coherencia entre el estatus nuevo y el campo Activo existente. */
UPDATE dbo.Persona
SET Activo = CASE WHEN Estatus = 'Activo' THEN 1 ELSE 0 END;
GO
