''' <summary>Representa un registro de persona.</summary>
Public Class Persona
    Public Property IdPersona As Integer
    Public Property Nombre As String
    Public Property IdEstado As Integer
    Public Property IdMunicipio As Integer
    Public Property IdLocalidad As Integer
    Public Property CP As String
    Public Property Telefono As String
    Public Property Correo As String
    Public Property Foto As Byte()
    Public Property Huella As Byte()
    Public Property HuellaFormato As String
    Public Property IdRol As Integer
    Public Property Activo As Boolean
    Public Property FechaRegistro As DateTime
    Public Property PasswordHash As Byte()
    Public Property PasswordSalt As Byte()
    Public Property Estatus As String
    Public Property IntentosFallidos As Integer
    Public Property BloqueadoHasta As Nullable(Of DateTime)
    Public Property UltimoAcceso As Nullable(Of DateTime)
    Public Property AceptoPrivacidad As Boolean
    Public Property FechaAceptacion As Nullable(Of DateTime)
    Public Property IdAprobador As Nullable(Of Integer)
    Public Property FechaAprobacion As Nullable(Of DateTime)
End Class
