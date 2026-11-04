''' <summary>Representa el resultado de una operación de escritura.</summary>
Public Class ResultadoOperacion
    Public Sub New()
        Errores = New List(Of String)()
    End Sub

    Public Property Exitoso As Boolean
    Public ReadOnly Property Errores As List(Of String)
    Public Property IdGenerado As Nullable(Of Integer)
    Public Property Reconocido As Boolean
    Public Property IdPersona As Nullable(Of Integer)
    Public Property Nombre As String
    Public Property Distancia As Nullable(Of Double)

    Public Shared Function Exito(Optional idGenerado As Nullable(Of Integer) = Nothing) As ResultadoOperacion
        Return New ResultadoOperacion With {
            .Exitoso = True,
            .IdGenerado = idGenerado
        }
    End Function

    Public Shared Function Fallo(errores As IEnumerable(Of String)) As ResultadoOperacion
        Dim resultado As New ResultadoOperacion With {.Exitoso = False}
        resultado.Errores.AddRange(errores)
        Return resultado
    End Function

    Public Shared Function Fallo(errorValidacion As String) As ResultadoOperacion
        Return Fallo(New List(Of String) From {errorValidacion})
    End Function
End Class
