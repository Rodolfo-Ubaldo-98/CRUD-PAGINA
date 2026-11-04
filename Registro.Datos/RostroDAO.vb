Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports Registro.Entidades

''' <summary>Acceso ADO.NET a las muestras de rostro.</summary>
Public Class RostroDAO
    Private ReadOnly _connectionString As String

    Public Sub New()
        Dim configuracion = ConfigurationManager.ConnectionStrings("RegistroDB")
        If configuracion Is Nothing OrElse String.IsNullOrWhiteSpace(configuracion.ConnectionString) Then
            Throw New ConfigurationErrorsException("No existe la cadena de conexión RegistroDB.")
        End If

        _connectionString = configuracion.ConnectionString
    End Sub

    Public Function InsertarMuestra(idPersona As Integer, descriptor As Single()) As Integer
        Const sql As String = "INSERT INTO PersonaRostro (IdPersona, Descriptor, FechaRegistro) " &
            "VALUES (@IdPersona, @Descriptor, @FechaRegistro); " &
            "SELECT CAST(SCOPE_IDENTITY() AS int);"

        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@IdPersona", SqlDbType.Int).Value = idPersona
            comando.Parameters.Add("@Descriptor", SqlDbType.VarBinary, -1).Value = ConvertirABytes(descriptor)
            comando.Parameters.Add("@FechaRegistro", SqlDbType.DateTime).Value = DateTime.Now
            conexion.Open()
            Return Convert.ToInt32(comando.ExecuteScalar())
        End Using
    End Function

    Public Function ContarMuestras(idPersona As Integer) As Integer
        Const sql As String = "SELECT COUNT(1) FROM PersonaRostro WHERE IdPersona = @IdPersona;"

        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@IdPersona", SqlDbType.Int).Value = idPersona
            conexion.Open()
            Return Convert.ToInt32(comando.ExecuteScalar())
        End Using
    End Function

    Public Function ListarDescriptoresActivos() As List(Of DescriptorPersona)
        Const sql As String = "SELECT p.IdPersona, p.Nombre, r.Descriptor " &
            "FROM PersonaRostro AS r " &
            "INNER JOIN Persona AS p ON p.IdPersona = r.IdPersona " &
            "WHERE p.Activo = @Activo AND p.Estatus = @Estatus;"
        Dim resultado As New List(Of DescriptorPersona)()

        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@Activo", SqlDbType.Bit).Value = True
            comando.Parameters.Add("@Estatus", SqlDbType.VarChar, 20).Value = "Activo"
            conexion.Open()
            Using lector = comando.ExecuteReader()
                While lector.Read()
                    If lector("Descriptor") IsNot DBNull.Value Then
                        resultado.Add(New DescriptorPersona With {
                            .IdPersona = Convert.ToInt32(lector("IdPersona")),
                            .Nombre = Convert.ToString(lector("Nombre")),
                            .Descriptor = ConvertirASingles(DirectCast(lector("Descriptor"), Byte()))
                        })
                    End If
                End While
            End Using
        End Using

        Return resultado
    End Function

    Private Shared Function ConvertirABytes(descriptor As Single()) As Byte()
        If descriptor Is Nothing Then
            Return Nothing
        End If

        Dim bytes(descriptor.Length * 4 - 1) As Byte
        If descriptor.Length > 0 Then
            Buffer.BlockCopy(descriptor, 0, bytes, 0, bytes.Length)
        End If
        Return bytes
    End Function

    Private Shared Function ConvertirASingles(bytes As Byte()) As Single()
        If bytes Is Nothing Then
            Return Nothing
        End If
        If bytes.Length Mod 4 <> 0 Then
            Throw New InvalidOperationException("El descriptor almacenado no tiene un tamaño válido.")
        End If

        Dim descriptor(bytes.Length \ 4 - 1) As Single
        If bytes.Length > 0 Then
            Buffer.BlockCopy(bytes, 0, descriptor, 0, bytes.Length)
        End If
        Return descriptor
    End Function
End Class
