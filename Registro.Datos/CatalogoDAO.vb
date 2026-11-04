Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports Registro.Entidades

''' <summary>Acceso ADO.NET a los catálogos geográficos y de roles.</summary>
Public Class CatalogoDAO
    Private ReadOnly _connectionString As String

    Public Sub New()
        Dim configuracion = ConfigurationManager.ConnectionStrings("RegistroDB")
        If configuracion Is Nothing OrElse String.IsNullOrWhiteSpace(configuracion.ConnectionString) Then
            Throw New ConfigurationErrorsException("No existe la cadena de conexión RegistroDB.")
        End If

        _connectionString = configuracion.ConnectionString
    End Sub

    Public Function ObtenerRoles() As List(Of Rol)
        Const sql As String = "SELECT IdRol, NombreRol, Descripcion FROM Rol ORDER BY NombreRol;"
        Dim resultado As New List(Of Rol)()

        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            conexion.Open()
            Using lector = comando.ExecuteReader()
                While lector.Read()
                    resultado.Add(New Rol With {
                        .IdRol = Convert.ToInt32(lector("IdRol")),
                        .NombreRol = Convert.ToString(lector("NombreRol")),
                        .Descripcion = If(lector("Descripcion") Is DBNull.Value, Nothing, Convert.ToString(lector("Descripcion")))
                    })
                End While
            End Using
        End Using

        Return resultado
    End Function

    Public Function ObtenerEstados() As List(Of Estado)
        Const sql As String = "SELECT IdEstado, Nombre FROM Estado ORDER BY Nombre;"
        Dim resultado As New List(Of Estado)()

        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            conexion.Open()
            Using lector = comando.ExecuteReader()
                While lector.Read()
                    resultado.Add(New Estado With {
                        .IdEstado = Convert.ToInt32(lector("IdEstado")),
                        .Nombre = Convert.ToString(lector("Nombre"))
                    })
                End While
            End Using
        End Using

        Return resultado
    End Function

    Public Function ObtenerMunicipiosPorEstado(idEstado As Integer) As List(Of Municipio)
        Const sql As String = "SELECT IdMunicipio, IdEstado, Nombre FROM Municipio WHERE IdEstado = @IdEstado ORDER BY Nombre;"
        Dim resultado As New List(Of Municipio)()

        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@IdEstado", SqlDbType.Int).Value = idEstado
            conexion.Open()
            Using lector = comando.ExecuteReader()
                While lector.Read()
                    resultado.Add(New Municipio With {
                        .IdMunicipio = Convert.ToInt32(lector("IdMunicipio")),
                        .IdEstado = Convert.ToInt32(lector("IdEstado")),
                        .Nombre = Convert.ToString(lector("Nombre"))
                    })
                End While
            End Using
        End Using

        Return resultado
    End Function

    Public Function ObtenerLocalidadesPorMunicipio(idMunicipio As Integer) As List(Of Localidad)
        Const sql As String = "SELECT IdLocalidad, IdMunicipio, Nombre FROM Localidad WHERE IdMunicipio = @IdMunicipio ORDER BY Nombre;"
        Dim resultado As New List(Of Localidad)()

        Using conexion As New SqlConnection(_connectionString), comando As New SqlCommand(sql, conexion)
            comando.Parameters.Add("@IdMunicipio", SqlDbType.Int).Value = idMunicipio
            conexion.Open()
            Using lector = comando.ExecuteReader()
                While lector.Read()
                    resultado.Add(New Localidad With {
                        .IdLocalidad = Convert.ToInt32(lector("IdLocalidad")),
                        .IdMunicipio = Convert.ToInt32(lector("IdMunicipio")),
                        .Nombre = Convert.ToString(lector("Nombre"))
                    })
                End While
            End Using
        End Using

        Return resultado
    End Function
End Class
