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
