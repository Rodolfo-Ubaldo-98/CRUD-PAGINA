Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Globalization
Imports Registro.Datos
Imports Registro.Entidades

''' <summary>Aplica las reglas de negocio del registro e identificación facial.</summary>
Public Class RostroBO
    Private Const LongitudDescriptor As Integer = 128
    Private Const MaximoMuestras As Integer = 5
    Private Const UmbralPorDefecto As Double = 0.5R

    Private ReadOnly _personaDAO As PersonaDAO
    Private ReadOnly _rostroDAO As RostroDAO

    Public Sub New()
        Me.New(New PersonaDAO(), New RostroDAO())
    End Sub

    Public Sub New(personaDAO As PersonaDAO, rostroDAO As RostroDAO)
        If personaDAO Is Nothing Then Throw New ArgumentNullException(NameOf(personaDAO))
        If rostroDAO Is Nothing Then Throw New ArgumentNullException(NameOf(rostroDAO))

        _personaDAO = personaDAO
        _rostroDAO = rostroDAO
    End Sub

    Public Function RegistrarMuestra(idPersona As Integer, descriptor As Single()) As ResultadoOperacion
        If idPersona <= 0 Then
            Return ResultadoOperacion.Fallo("El identificador de la persona no es válido.")
        End If

        Dim errorDescriptor = ValidarDescriptor(descriptor)
        If errorDescriptor IsNot Nothing Then
            Return ResultadoOperacion.Fallo(errorDescriptor)
        End If

        Try
            Dim persona = _personaDAO.ObtenerPorId(idPersona)
            If persona Is Nothing Then
                Return ResultadoOperacion.Fallo("La persona indicada no existe.")
            End If
            If Not persona.Activo Then
                Return ResultadoOperacion.Fallo("La persona indicada no está activa.")
            End If
            If _rostroDAO.ContarMuestras(idPersona) >= MaximoMuestras Then
                Return ResultadoOperacion.Fallo("La persona ya tiene el máximo de cinco muestras faciales.")
            End If

            Dim idRostro = _rostroDAO.InsertarMuestra(idPersona, descriptor)
            Return ResultadoOperacion.Exito(idRostro)
        Catch
            Return ResultadoOperacion.Fallo("No fue posible registrar la muestra facial.")
        End Try
    End Function

    Public Function Identificar(descriptor As Single()) As ResultadoOperacion
        Dim errorDescriptor = ValidarDescriptor(descriptor)
        If errorDescriptor IsNot Nothing Then
            Return ResultadoOperacion.Fallo(errorDescriptor)
        End If

        Try
            Dim mejor As DescriptorPersona = Nothing
            Dim menorDistancia = Double.MaxValue

            For Each muestra In _rostroDAO.ListarDescriptoresActivos()
                If muestra.Descriptor Is Nothing OrElse muestra.Descriptor.Length <> LongitudDescriptor Then
                    Continue For
                End If

                Dim distancia = CalcularDistancia(descriptor, muestra.Descriptor)
                If distancia < menorDistancia Then
                    menorDistancia = distancia
                    mejor = muestra
                End If
            Next

            Dim resultado = ResultadoOperacion.Exito()
            If mejor IsNot Nothing AndAlso menorDistancia <= ObtenerUmbral() Then
                resultado.Reconocido = True
                resultado.IdPersona = mejor.IdPersona
                resultado.Nombre = mejor.Nombre
                resultado.Distancia = menorDistancia
            End If
            Return resultado
        Catch
            Return ResultadoOperacion.Fallo("No fue posible identificar el rostro.")
        End Try
    End Function

    Private Shared Function ValidarDescriptor(descriptor As Single()) As String
        If descriptor Is Nothing Then
            Return "El descriptor es obligatorio."
        End If
        If descriptor.Length <> LongitudDescriptor Then
            Return "El descriptor debe contener exactamente 128 valores."
        End If

        For Each valor In descriptor
            If Single.IsNaN(valor) OrElse Single.IsInfinity(valor) Then
                Return "El descriptor contiene valores no válidos."
            End If
        Next

        Return Nothing
    End Function

    Private Shared Function CalcularDistancia(origen As Single(), destino As Single()) As Double
        Dim suma As Double = 0R
        For indice As Integer = 0 To LongitudDescriptor - 1
            Dim diferencia As Double = CDbl(origen(indice)) - CDbl(destino(indice))
            suma += diferencia * diferencia
        Next
        Return Math.Sqrt(suma)
    End Function

    Private Shared Function ObtenerUmbral() As Double
        Dim valor As String = ConfigurationManager.AppSettings("UmbralRostro")
        Dim umbral As Double
        If Double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, umbral) AndAlso umbral >= 0R Then
            Return umbral
        End If
        Return UmbralPorDefecto
    End Function
End Class
