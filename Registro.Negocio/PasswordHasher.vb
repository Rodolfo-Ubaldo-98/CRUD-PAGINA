Imports System
Imports System.Security.Cryptography

''' <summary>Protege contraseñas sin almacenar nunca el texto original.</summary>
Public NotInheritable Class PasswordHasher
    Private Const Iteraciones As Integer = 100000
    Private Const TamanoSalt As Integer = 16
    Private Const TamanoHash As Integer = 64

    Private Sub New()
    End Sub

    Public Shared Function EsValida(password As String) As Boolean
        If String.IsNullOrEmpty(password) OrElse password.Length < 8 Then Return False
        Dim tieneLetra As Boolean = False
        Dim tieneNumero As Boolean = False
        For Each caracter As Char In password
            If Char.IsLetter(caracter) Then tieneLetra = True
            If Char.IsDigit(caracter) Then tieneNumero = True
        Next
        Return tieneLetra AndAlso tieneNumero
    End Function

    Public Shared Sub CrearHash(password As String, ByRef hash As Byte(), ByRef salt As Byte())
        If Not EsValida(password) Then Throw New ArgumentException("La contraseña debe tener al menos 8 caracteres, letras y números.", NameOf(password))
        salt = New Byte(TamanoSalt - 1) {}
        Using rng As RandomNumberGenerator = RandomNumberGenerator.Create()
            rng.GetBytes(salt)
        End Using
        Using derivador As New Rfc2898DeriveBytes(password, salt, Iteraciones, HashAlgorithmName.SHA256)
            hash = derivador.GetBytes(TamanoHash)
        End Using
    End Sub

    Public Shared Function Verificar(password As String, hash As Byte(), salt As Byte()) As Boolean
        If String.IsNullOrEmpty(password) OrElse hash Is Nothing OrElse salt Is Nothing Then Return False
        Dim calculado As Byte()
        Using derivador As New Rfc2898DeriveBytes(password, salt, Iteraciones, HashAlgorithmName.SHA256)
            calculado = derivador.GetBytes(hash.Length)
        End Using
        Return ComparacionConstante(calculado, hash)
    End Function

    Private Shared Function ComparacionConstante(a As Byte(), b As Byte()) As Boolean
        If a Is Nothing OrElse b Is Nothing OrElse a.Length <> b.Length Then Return False
        Dim diferencia As Integer = 0
        For indice As Integer = 0 To a.Length - 1
            diferencia = diferencia Or (a(indice) Xor b(indice))
        Next
        Return diferencia = 0
    End Function
End Class
