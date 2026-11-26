Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports Registro.Entidades

''' <summary>Acceso ADO.NET a los registros de Persona.</summary>
Public Class PersonaDAO
    Private ReadOnly _connectionString As String

    Public Sub New()
        Dim configuracion = ConfigurationManager.ConnectionStrings("RegistroDB")
        If configuracion Is Nothing OrElse String.IsNullOrWhiteSpace(configuracion.ConnectionString) Then
            Throw New ConfigurationErrorsException("No existe la cadena de conexión RegistroDB.")
        End If

        _connectionString = configuracion.ConnectionString
    End Sub

    Public Function Insertar(persona As Persona) As Integer
        Const sql As String = "INSERT INTO Persona " &
            "(Nombre, IdEstado, IdMunicipio, IdLocalidad, CP, Telefono, Correo, Foto, Huella, HuellaFormato, IdRol, Activo, FechaRegistro) " &
            "VALUES (@Nombre, @IdEstado, @IdMunicipio, @IdLocalidad, @CP, @Telefono, @Correo, @Foto, @Huella, @HuellaFormato, @IdRol, @Activo, @FechaRegistro); " &
            "SELECT CAST(SCOPE_IDENTITY() AS int);"

        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            AgregarParametrosPersona(comando, persona)
            conexion.Open()
            Return Convert.ToInt32(comando.ExecuteScalar())
        End Using
    End Function

    Public Function ObtenerUsuariosAdmin(rol As String, estatus As String, buscar As String, pagina As Integer, tamano As Integer, ByRef total As Integer) As List(Of UsuarioAdminDto)
        Dim resultado As New List(Of UsuarioAdminDto)()
        Dim inicio = (pagina - 1) * tamano
        Const sql As String = "SELECT p.IdPersona, p.Nombre, p.Correo, p.IdRol, r.NombreRol, p.Estatus, p.Activo, p.FechaRegistro, " &
            "(SELECT COUNT(1) FROM PersonaRostro pr WHERE pr.IdPersona = p.IdPersona) AS TieneRostro, COUNT(1) OVER() AS TotalFilas " &
            "FROM Persona p INNER JOIN Rol r ON r.IdRol = p.IdRol " &
            "WHERE (@Rol IS NULL OR r.NombreRol = @Rol) AND (@Estatus IS NULL OR p.Estatus = @Estatus) " &
            "AND (@Buscar IS NULL OR p.Nombre LIKE '%' + @Buscar + '%' OR p.Correo LIKE '%' + @Buscar + '%') " &
            "ORDER BY p.FechaRegistro DESC OFFSET @Inicio ROWS FETCH NEXT @Tamano ROWS ONLY;"
        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@Rol", SqlDbType.NVarChar, 100).Value = If(String.IsNullOrWhiteSpace(rol), CType(DBNull.Value, Object), rol.Trim())
            comando.Parameters.Add("@Estatus", SqlDbType.VarChar, 20).Value = If(String.IsNullOrWhiteSpace(estatus), CType(DBNull.Value, Object), estatus.Trim())
            comando.Parameters.Add("@Buscar", SqlDbType.NVarChar, 200).Value = If(String.IsNullOrWhiteSpace(buscar), CType(DBNull.Value, Object), buscar.Trim())
            comando.Parameters.Add("@Inicio", SqlDbType.Int).Value = inicio
            comando.Parameters.Add("@Tamano", SqlDbType.Int).Value = tamano
            conexion.Open()
            Using lector = comando.ExecuteReader()
                While lector.Read()
                    total = Convert.ToInt32(lector("TotalFilas"))
                    resultado.Add(New UsuarioAdminDto With {.IdPersona = Convert.ToInt32(lector("IdPersona")), .Nombre = Convert.ToString(lector("Nombre")), .Correo = Convert.ToString(lector("Correo")), .IdRol = Convert.ToInt32(lector("IdRol")), .Rol = Convert.ToString(lector("NombreRol")), .Estatus = Convert.ToString(lector("Estatus")), .Activo = Convert.ToBoolean(lector("Activo")), .FechaRegistro = Convert.ToDateTime(lector("FechaRegistro")), .TieneRostro = Convert.ToInt32(lector("TieneRostro"))})
                End While
            End Using
        End Using
        Return resultado
    End Function

    Public Function ObtenerResumenAdmin() As ResumenAdminDto
        Dim resumen As New ResumenAdminDto()
        Const sql As String = "SELECT COUNT(1) AS TotalUsuarios, SUM(CASE WHEN r.NombreRol='Cliente' THEN 1 ELSE 0 END) AS Clientes, SUM(CASE WHEN r.NombreRol='Empleado' THEN 1 ELSE 0 END) AS Empleados, SUM(CASE WHEN r.NombreRol='Administrador' THEN 1 ELSE 0 END) AS Administradores, SUM(CASE WHEN p.Estatus='Pendiente' THEN 1 ELSE 0 END) AS Pendientes, SUM(CASE WHEN p.FechaRegistro >= DATEADD(DAY,-7,GETDATE()) THEN 1 ELSE 0 END) AS Ultimos7 FROM Persona p INNER JOIN Rol r ON r.IdRol=p.IdRol; SELECT Estatus, COUNT(1) AS Total FROM Persona GROUP BY Estatus;"
        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            conexion.Open()
            Using lector = comando.ExecuteReader()
                If lector.Read() Then
                    resumen.TotalUsuarios = Convert.ToInt32(lector("TotalUsuarios"))
                    resumen.Clientes = Convert.ToInt32(lector("Clientes"))
                    resumen.Empleados = Convert.ToInt32(lector("Empleados"))
                    resumen.Administradores = Convert.ToInt32(lector("Administradores"))
                    resumen.Pendientes = Convert.ToInt32(lector("Pendientes"))
                    resumen.RegistrosUltimos7Dias = Convert.ToInt32(lector("Ultimos7"))
                End If
                lector.NextResult()
                While lector.Read()
                    resumen.PorEstatus(Convert.ToString(lector("Estatus"))) = Convert.ToInt32(lector("Total"))
                End While
            End Using
        End Using
        Return resumen
    End Function

    Public Function ObtenerDetalleAdmin(idPersona As Integer) As UsuarioDetalle
        Const sql As String = "SELECT p.IdPersona, p.Nombre, p.Correo, p.IdRol, p.Estatus, p.Activo, p.Foto, " &
            "(SELECT COUNT(1) FROM PersonaRostro pr WHERE pr.IdPersona = p.IdPersona) AS TieneRostro FROM Persona p WHERE p.IdPersona = @IdPersona;"
        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@IdPersona", SqlDbType.Int).Value = idPersona
            conexion.Open()
            Using lector = comando.ExecuteReader(CommandBehavior.SingleRow)
                If lector.Read() Then
                    Return New UsuarioDetalle With {
                        .IdPersona = Convert.ToInt32(lector("IdPersona")),
                        .Nombre = Convert.ToString(lector("Nombre")),
                        .Correo = Convert.ToString(lector("Correo")),
                        .IdRol = Convert.ToInt32(lector("IdRol")),
                        .Estatus = Convert.ToString(lector("Estatus")),
                        .Activo = Convert.ToBoolean(lector("Activo")),
                        .Foto = If(lector("Foto") Is DBNull.Value, Nothing, DirectCast(lector("Foto"), Byte())),
                        .TieneRostro = Convert.ToInt32(lector("TieneRostro"))
                    }
                End If
            End Using
        End Using
        Return Nothing
    End Function

    ''' <summary>Obtiene el nombre del rol sin confiar en datos enviados por el cliente.</summary>
    Public Function ObtenerNombreRol(idRol As Integer) As String
        Const sql As String = "SELECT NombreRol FROM Rol WHERE IdRol = @IdRol;"
        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@IdRol", SqlDbType.Int).Value = idRol
            conexion.Open()
            Dim valor = comando.ExecuteScalar()
            Return If(valor Is Nothing OrElse valor Is DBNull.Value, Nothing, Convert.ToString(valor))
        End Using
    End Function

    Public Function ObtenerIdRol(nombreRol As String) As Nullable(Of Integer)
        Const sql As String = "SELECT IdRol FROM Rol WHERE NombreRol = @NombreRol;"
        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@NombreRol", SqlDbType.NVarChar, 100).Value = nombreRol
            conexion.Open()
            Dim valor = comando.ExecuteScalar()
            If valor Is Nothing OrElse valor Is DBNull.Value Then Return Nothing
            Return Convert.ToInt32(valor)
        End Using
    End Function

    Public Function ContarAdministradoresActivos() As Integer
        Const sql As String = "SELECT COUNT(1) FROM Persona p INNER JOIN Rol r ON r.IdRol = p.IdRol " &
                              "WHERE r.NombreRol = 'Administrador' AND p.Estatus = 'Activo' AND p.Activo = 1;"
        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            conexion.Open()
            Return Convert.ToInt32(comando.ExecuteScalar())
        End Using
    End Function

    ''' <summary>Actualiza estado y aprobación con el contexto de auditoría de la aplicación.</summary>
    Public Sub ActualizarEstatus(idPersona As Integer, estatus As String, idAprobador As Integer)
        Const sql As String = "EXEC sys.sp_set_session_context @key = N'IdUsuarioApp', @value = @IdAprobador; " &
                              "UPDATE Persona SET Estatus = @Estatus, Activo = CASE WHEN @Estatus = 'Activo' THEN 1 ELSE 0 END, " &
                              "IdAprobador = @IdAprobador, FechaAprobacion = GETDATE() WHERE IdPersona = @IdPersona;"
        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@IdAprobador", SqlDbType.Int).Value = idAprobador
            comando.Parameters.Add("@Estatus", SqlDbType.VarChar, 20).Value = estatus
            comando.Parameters.Add("@IdPersona", SqlDbType.Int).Value = idPersona
            conexion.Open()
            comando.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>Registra un intento y aplica el bloqueo temporal cuando corresponde.</summary>
    Public Sub ActualizarIntentoLogin(correo As String, intentos As Integer, bloqueadoHasta As Nullable(Of DateTime))
        Const sql As String = "UPDATE Persona SET IntentosFallidos = @IntentosFallidos, BloqueadoHasta = @BloqueadoHasta " &
                              "WHERE Correo = @Correo;"
        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@IntentosFallidos", SqlDbType.Int).Value = intentos
            comando.Parameters.Add("@BloqueadoHasta", SqlDbType.DateTime).Value = If(bloqueadoHasta.HasValue, CType(bloqueadoHasta.Value, Object), DBNull.Value)
            comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 254).Value = correo
            conexion.Open()
            comando.ExecuteNonQuery()
        End Using
    End Sub

    Public Sub RegistrarAcceso(idPersona As Integer)
        Const sql As String = "UPDATE Persona SET UltimoAcceso = GETDATE(), IntentosFallidos = 0, BloqueadoHasta = NULL WHERE IdPersona = @IdPersona;"
        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@IdPersona", SqlDbType.Int).Value = idPersona
            conexion.Open()
            comando.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>Crea el primer administrador; el actor es nulo porque aún no existe.</summary>
    Public Function InsertarAdministrador(persona As Persona) As Integer
        Const sql As String = "INSERT INTO Persona (Nombre, Correo, Foto, IdRol, Activo, FechaRegistro, PasswordHash, PasswordSalt, Estatus, " &
                              "IntentosFallidos, AceptoPrivacidad, FechaAceptacion) VALUES (@Nombre, @Correo, @Foto, @IdRol, 1, @FechaRegistro, " &
                              "@PasswordHash, @PasswordSalt, 'Activo', 0, @AceptoPrivacidad, @FechaAceptacion); SELECT CAST(SCOPE_IDENTITY() AS int);"
        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@Nombre", SqlDbType.NVarChar, 200).Value = persona.Nombre
            comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 254).Value = persona.Correo
            comando.Parameters.Add("@Foto", SqlDbType.VarBinary, -1).Value = If(persona.Foto Is Nothing, CType(DBNull.Value, Object), persona.Foto)
            comando.Parameters.Add("@IdRol", SqlDbType.Int).Value = persona.IdRol
            comando.Parameters.Add("@FechaRegistro", SqlDbType.DateTime).Value = DateTime.Now
            comando.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 64).Value = persona.PasswordHash
            comando.Parameters.Add("@PasswordSalt", SqlDbType.VarBinary, 32).Value = persona.PasswordSalt
            comando.Parameters.Add("@AceptoPrivacidad", SqlDbType.Bit).Value = persona.AceptoPrivacidad
            comando.Parameters.Add("@FechaAceptacion", SqlDbType.DateTime).Value = If(persona.FechaAceptacion.HasValue, CType(persona.FechaAceptacion.Value, Object), DBNull.Value)
            conexion.Open()
            Return Convert.ToInt32(comando.ExecuteScalar())
        End Using
    End Function

    ''' <summary>Guarda persona, credenciales y muestras faciales en una sola transacción.</summary>
    Public Function RegistrarPublicoAtomico(registro As RegistroAutenticacion) As Integer
        If registro Is Nothing OrElse registro.Persona Is Nothing Then Throw New ArgumentNullException(NameOf(registro))

        Const sqlPersona As String = "INSERT INTO Persona " &
            "(Nombre, IdEstado, IdMunicipio, IdLocalidad, CP, Telefono, Correo, Foto, Huella, HuellaFormato, IdRol, Activo, FechaRegistro, " &
            "PasswordHash, PasswordSalt, Estatus, IntentosFallidos, BloqueadoHasta, UltimoAcceso, AceptoPrivacidad, FechaAceptacion) " &
            "VALUES (@Nombre, @IdEstado, @IdMunicipio, @IdLocalidad, @CP, @Telefono, @Correo, @Foto, @Huella, @HuellaFormato, @IdRol, @Activo, @FechaRegistro, " &
            "@PasswordHash, @PasswordSalt, @Estatus, 0, NULL, NULL, @AceptoPrivacidad, @FechaAceptacion); " &
            "SELECT CAST(SCOPE_IDENTITY() AS int);"
        Const sqlRostro As String = "INSERT INTO PersonaRostro (IdPersona, Descriptor, FechaRegistro) VALUES (@IdPersona, @Descriptor, @FechaRegistro);"

        Using conexion As New SqlConnection(_connectionString)
            conexion.Open()
            Using transaccion As SqlTransaction = conexion.BeginTransaction()
                Try
                    Using contexto As New SqlCommand("EXEC sys.sp_set_session_context @key = N'IdUsuarioApp', @value = NULL;", conexion, transaccion)
                        contexto.ExecuteNonQuery()
                    End Using
                    Dim idPersona As Integer
                    Using comando As New SqlCommand(sqlPersona, conexion, transaccion)
                        AgregarParametrosPersona(comando, registro.Persona)
                        comando.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 64).Value = registro.Persona.PasswordHash
                        comando.Parameters.Add("@PasswordSalt", SqlDbType.VarBinary, 32).Value = registro.Persona.PasswordSalt
                        comando.Parameters.Add("@Estatus", SqlDbType.VarChar, 20).Value = registro.Persona.Estatus
                        comando.Parameters.Add("@AceptoPrivacidad", SqlDbType.Bit).Value = registro.Persona.AceptoPrivacidad
                        comando.Parameters.Add("@FechaAceptacion", SqlDbType.DateTime).Value = If(registro.Persona.FechaAceptacion.HasValue, CType(registro.Persona.FechaAceptacion.Value, Object), DBNull.Value)
                        idPersona = Convert.ToInt32(comando.ExecuteScalar())
                    End Using

                    For Each descriptor As Single() In registro.Descriptores
                        Using comando As New SqlCommand(sqlRostro, conexion, transaccion)
                            comando.Parameters.Add("@IdPersona", SqlDbType.Int).Value = idPersona
                            comando.Parameters.Add("@Descriptor", SqlDbType.VarBinary, -1).Value = ConvertirABytes(descriptor)
                            comando.Parameters.Add("@FechaRegistro", SqlDbType.DateTime).Value = DateTime.Now
                            comando.ExecuteNonQuery()
                        End Using
                    Next

                    transaccion.Commit()
                    Return idPersona
                Catch
                    transaccion.Rollback()
                    Throw
                End Try
            End Using
        End Using
    End Function

    ''' <summary>Obtiene credenciales y estado para el futuro inicio de sesión.</summary>
    Public Function ObtenerParaAutenticacion(correo As String) As Persona
        Const sql As String = "SELECT IdPersona, Nombre, Correo, IdRol, Activo, FechaRegistro, PasswordHash, PasswordSalt, Estatus, " &
            "IntentosFallidos, BloqueadoHasta, UltimoAcceso, AceptoPrivacidad, FechaAceptacion, IdAprobador, FechaAprobacion " &
            "FROM Persona WHERE Correo = @Correo;"

        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 254).Value = correo
            conexion.Open()
            Using lector = comando.ExecuteReader(CommandBehavior.SingleRow)
                If lector.Read() Then
                    Return New Persona With {
                        .IdPersona = Convert.ToInt32(lector("IdPersona")),
                        .Nombre = Convert.ToString(lector("Nombre")),
                        .Correo = Convert.ToString(lector("Correo")),
                        .IdRol = Convert.ToInt32(lector("IdRol")),
                        .Activo = Convert.ToBoolean(lector("Activo")),
                        .FechaRegistro = Convert.ToDateTime(lector("FechaRegistro")),
                        .PasswordHash = If(lector("PasswordHash") Is DBNull.Value, Nothing, DirectCast(lector("PasswordHash"), Byte())),
                        .PasswordSalt = If(lector("PasswordSalt") Is DBNull.Value, Nothing, DirectCast(lector("PasswordSalt"), Byte())),
                        .Estatus = Convert.ToString(lector("Estatus")),
                        .IntentosFallidos = Convert.ToInt32(lector("IntentosFallidos")),
                        .BloqueadoHasta = If(lector("BloqueadoHasta") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(lector("BloqueadoHasta"))),
                        .UltimoAcceso = If(lector("UltimoAcceso") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(lector("UltimoAcceso"))),
                        .AceptoPrivacidad = Convert.ToBoolean(lector("AceptoPrivacidad")),
                        .FechaAceptacion = If(lector("FechaAceptacion") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(lector("FechaAceptacion"))),
                        .IdAprobador = If(lector("IdAprobador") Is DBNull.Value, CType(Nothing, Nullable(Of Integer)), Convert.ToInt32(lector("IdAprobador"))),
                        .FechaAprobacion = If(lector("FechaAprobacion") Is DBNull.Value, CType(Nothing, Nullable(Of DateTime)), Convert.ToDateTime(lector("FechaAprobacion")))
                    }
                End If
            End Using
        End Using
        Return Nothing
    End Function

    Public Sub Actualizar(persona As Persona)
        Const sql As String = "UPDATE Persona SET Nombre = @Nombre, IdEstado = @IdEstado, IdMunicipio = @IdMunicipio, " &
            "IdLocalidad = @IdLocalidad, CP = @CP, Telefono = @Telefono, Correo = @Correo, Foto = @Foto, " &
            "Huella = @Huella, HuellaFormato = @HuellaFormato, IdRol = @IdRol, Activo = @Activo " &
            "WHERE IdPersona = @IdPersona;"

        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            AgregarParametrosPersona(comando, persona)
            comando.Parameters.Add("@IdPersona", SqlDbType.Int).Value = persona.IdPersona
            conexion.Open()
            comando.ExecuteNonQuery()
        End Using
    End Sub

    Public Sub BajaLogica(idPersona As Integer)
        Const sql As String = "UPDATE Persona SET Activo = @Activo WHERE IdPersona = @IdPersona;"

        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@Activo", SqlDbType.Bit).Value = False
            comando.Parameters.Add("@IdPersona", SqlDbType.Int).Value = idPersona
            conexion.Open()
            comando.ExecuteNonQuery()
        End Using
    End Sub

    Public Function ObtenerPorId(idPersona As Integer) As Persona
        Const sql As String = "SELECT IdPersona, Nombre, IdEstado, IdMunicipio, IdLocalidad, CP, Telefono, Correo, " &
            "Foto, Huella, HuellaFormato, IdRol, Activo, FechaRegistro FROM Persona WHERE IdPersona = @IdPersona;"

        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@IdPersona", SqlDbType.Int).Value = idPersona
            conexion.Open()
            Using lector = comando.ExecuteReader(CommandBehavior.SingleRow)
                If lector.Read() Then
                    Return MapearPersona(lector, True)
                End If
            End Using
        End Using

        Return Nothing
    End Function

    Public Function Listar(Optional idRol As Nullable(Of Integer) = Nothing,
                           Optional busquedaNombre As String = Nothing) As List(Of Persona)
        Const sql As String = "SELECT IdPersona, Nombre, IdEstado, IdMunicipio, IdLocalidad, CP, Telefono, Correo, " &
            "HuellaFormato, IdRol, Activo, FechaRegistro FROM Persona " &
            "WHERE Activo = @Activo AND (@IdRol IS NULL OR IdRol = @IdRol) " &
            "AND (@BusquedaNombre IS NULL OR Nombre LIKE '%' + @BusquedaNombre + '%') " &
            "ORDER BY Nombre;"

        Dim personas As New List(Of Persona)()
        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@Activo", SqlDbType.Bit).Value = True
            comando.Parameters.Add("@IdRol", SqlDbType.Int).Value = If(idRol.HasValue, CType(idRol.Value, Object), DBNull.Value)
            comando.Parameters.Add("@BusquedaNombre", SqlDbType.NVarChar, 200).Value = If(String.IsNullOrWhiteSpace(busquedaNombre), CType(DBNull.Value, Object), busquedaNombre.Trim())
            conexion.Open()
            Using lector = comando.ExecuteReader()
                While lector.Read()
                    personas.Add(MapearPersona(lector, False))
                End While
            End Using
        End Using

        Return personas
    End Function

    Public Function ExisteCorreo(correo As String, Optional excluirIdPersona As Nullable(Of Integer) = Nothing) As Boolean
        Const sql As String = "SELECT COUNT(1) FROM Persona " &
            "WHERE Correo = @Correo AND (@IdPersona IS NULL OR IdPersona <> @IdPersona);"

        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 254).Value = correo
            comando.Parameters.Add("@IdPersona", SqlDbType.Int).Value = If(excluirIdPersona.HasValue, CType(excluirIdPersona.Value, Object), DBNull.Value)
            conexion.Open()
            Return Convert.ToInt32(comando.ExecuteScalar()) > 0
        End Using
    End Function

    Private Shared Sub AgregarParametrosPersona(comando As SqlCommand, persona As Persona)
        comando.Parameters.Add("@Nombre", SqlDbType.NVarChar, 200).Value = persona.Nombre
        comando.Parameters.Add("@IdEstado", SqlDbType.Int).Value = persona.IdEstado
        comando.Parameters.Add("@IdMunicipio", SqlDbType.Int).Value = persona.IdMunicipio
        comando.Parameters.Add("@IdLocalidad", SqlDbType.Int).Value = persona.IdLocalidad
        comando.Parameters.Add("@CP", SqlDbType.VarChar, 5).Value = persona.CP
        comando.Parameters.Add("@Telefono", SqlDbType.VarChar, 10).Value = persona.Telefono
        comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 254).Value = persona.Correo
        comando.Parameters.Add("@Foto", SqlDbType.VarBinary, -1).Value = If(persona.Foto Is Nothing, CType(DBNull.Value, Object), persona.Foto)
        comando.Parameters.Add("@Huella", SqlDbType.VarBinary, -1).Value = If(persona.Huella Is Nothing, CType(DBNull.Value, Object), persona.Huella)
        comando.Parameters.Add("@HuellaFormato", SqlDbType.NVarChar, 50).Value = If(String.IsNullOrWhiteSpace(persona.HuellaFormato), CType(DBNull.Value, Object), persona.HuellaFormato)
        comando.Parameters.Add("@IdRol", SqlDbType.Int).Value = persona.IdRol
        comando.Parameters.Add("@Activo", SqlDbType.Bit).Value = persona.Activo
        comando.Parameters.Add("@FechaRegistro", SqlDbType.DateTime).Value = persona.FechaRegistro
    End Sub

    Private Shared Function ConvertirABytes(descriptor As Single()) As Byte()
        If descriptor Is Nothing Then Return Nothing
        Dim bytes(descriptor.Length * 4 - 1) As Byte
        If descriptor.Length > 0 Then Buffer.BlockCopy(descriptor, 0, bytes, 0, bytes.Length)
        Return bytes
    End Function

    Private Shared Function MapearPersona(lector As SqlDataReader, incluirBiometricos As Boolean) As Persona
        Dim persona As New Persona With {
            .IdPersona = Convert.ToInt32(lector("IdPersona")),
            .Nombre = Convert.ToString(lector("Nombre")),
            .IdEstado = Convert.ToInt32(lector("IdEstado")),
            .IdMunicipio = Convert.ToInt32(lector("IdMunicipio")),
            .IdLocalidad = Convert.ToInt32(lector("IdLocalidad")),
            .CP = Convert.ToString(lector("CP")),
            .Telefono = Convert.ToString(lector("Telefono")),
            .Correo = Convert.ToString(lector("Correo")),
            .HuellaFormato = If(lector("HuellaFormato") Is DBNull.Value, Nothing, Convert.ToString(lector("HuellaFormato"))),
            .IdRol = Convert.ToInt32(lector("IdRol")),
            .Activo = Convert.ToBoolean(lector("Activo")),
            .FechaRegistro = Convert.ToDateTime(lector("FechaRegistro"))
        }

        If incluirBiometricos Then
            persona.Foto = If(lector("Foto") Is DBNull.Value, Nothing, DirectCast(lector("Foto"), Byte()))
            persona.Huella = If(lector("Huella") Is DBNull.Value, Nothing, DirectCast(lector("Huella"), Byte()))
        End If

        Return persona
    End Function
End Class
