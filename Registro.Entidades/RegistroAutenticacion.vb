''' <summary>Datos necesarios para el registro público atómico.</summary>
Public Class RegistroAutenticacion
    Public Property Persona As Persona
    Public Property Password As String
    Public Property Descriptores As List(Of Single())

    Public Sub New()
        Descriptores = New List(Of Single())()
    End Sub
End Class