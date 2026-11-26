''' <summary>Proyección segura para la administración de usuarios.</summary>
Public Class UsuarioAdminDto
    Public Property IdPersona As Integer
    Public Property Nombre As String
    Public Property Correo As String
    Public Property IdRol As Integer
    Public Property Rol As String
    Public Property Estatus As String
    Public Property Activo As Boolean
    Public Property FechaRegistro As DateTime
    Public Property TieneRostro As Integer
End Class

Public Class ResumenAdminDto
    Public Property TotalUsuarios As Integer
    Public Property Clientes As Integer
    Public Property Empleados As Integer
    Public Property Administradores As Integer
    Public Property Pendientes As Integer
    Public Property PorEstatus As Dictionary(Of String, Integer)
    Public Property RegistrosUltimos7Dias As Integer

    Public Sub New()
        PorEstatus = New Dictionary(Of String, Integer)()
    End Sub
End Class
