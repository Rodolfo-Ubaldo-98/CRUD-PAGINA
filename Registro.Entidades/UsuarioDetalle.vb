''' <summary>Detalle administrativo sin hash, salt ni huella facial.</summary>
Public Class UsuarioDetalle
    Public Property IdPersona As Integer
    Public Property Nombre As String
    Public Property Correo As String
    Public Property IdRol As Integer
    Public Property Estatus As String
    Public Property Activo As Boolean
    Public Property Foto As Byte()
    Public Property TieneRostro As Integer
End Class
