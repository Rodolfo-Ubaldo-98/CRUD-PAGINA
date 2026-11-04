Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text.RegularExpressions
Imports Registro.Datos
Imports Registro.Entidades

''' <summary>Aplica las validaciones y reglas de negocio de Persona.</summary>
Public Class PersonaBO
    Private ReadOnly _personaDAO As PersonaDAO
    Private ReadOnly _catalogoDAO As CatalogoDAO

    Public Sub New()
        Me.New(New PersonaDAO(), New CatalogoDAO())
    End Sub

    Public Sub New(personaDAO As PersonaDAO, catalogoDAO As CatalogoDAO)
        If personaDAO Is Nothing Then Throw New ArgumentNullException(NameOf(personaDAO))
        If catalogoDAO Is Nothing Then Throw New ArgumentNullException(NameOf(catalogoDAO))

        _personaDAO = personaDAO
        _catalogoDAO = catalogoDAO
    End Sub

    Public Function Registrar(persona As Persona) As ResultadoOperacion
        If persona Is Nothing Then
            Return ResultadoOperacion.Fallo("La persona es obligatoria.")
        End If

        Try
            Dim errores = ValidarPersona(persona)
            If errores.Count > 0 Then
                Return ResultadoOperacion.Fallo(errores)
            End If

            persona.Activo = True
            If persona.FechaRegistro = DateTime.MinValue Then
                persona.FechaRegistro = DateTime.Now
            End If

            Dim idGenerado = _personaDAO.Insertar(persona)
            Return ResultadoOperacion.Exito(idGenerado)
        Catch
            Return ResultadoOperacion.Fallo("No fue posible registrar la persona.")
        End Try
    End Function

    ''' <summary>Valida y registra un Cliente o Empleado con sus tres muestras.</summary>
    Public Function RegistrarPublico(registro As RegistroAutenticacion) As ResultadoOperacion
        If registro Is Nothing OrElse registro.Persona Is Nothing Then
            Return ResultadoOperacion.Fallo("Los datos de registro son obligatorios.")
        End If

        Dim persona = registro.Persona
        persona.Correo = If(persona.Correo, String.Empty).Trim().ToLowerInvariant()
        Dim errores = ValidarPersona(persona)

        If Not PasswordHasher.EsValida(registro.Password) Then
            errores.Add("La contraseña debe tener al menos 8 caracteres, letras y números.")
        End If
        If Not persona.AceptoPrivacidad Then
            errores.Add("Debe aceptar el consentimiento para el tratamiento de datos biométricos.")
        End If
        If persona.Foto Is Nothing OrElse persona.Foto.Length = 0 Then
            errores.Add("La fotografía es obligatoria.")
        End If
        If registro.Descriptores Is Nothing OrElse registro.Descriptores.Count <> 3 Then
            errores.Add("Debe proporcionar exactamente 3 muestras faciales.")
        ElseIf registro.Descriptores.Any(Function(descriptor) descriptor Is Nothing OrElse descriptor.Length <> 128) Then
            errores.Add("Cada muestra facial debe contener 128 valores.")
        End If

        Dim roles = _catalogoDAO.ObtenerRoles()
        Dim rol = roles.FirstOrDefault(Function(item) item.IdRol = persona.IdRol)
        If rol Is Nothing OrElse (Not String.Equals(rol.NombreRol, "Cliente", StringComparison.OrdinalIgnoreCase) AndAlso
                                  Not String.Equals(rol.NombreRol, "Empleado", StringComparison.OrdinalIgnoreCase)) Then
            errores.Add("El tipo de cuenta debe ser Cliente o Empleado.")
        End If

        If errores.Count > 0 Then Return ResultadoOperacion.Fallo(errores)

        Try
            PasswordHasher.CrearHash(registro.Password, persona.PasswordHash, persona.PasswordSalt)
            persona.Estatus = "Pendiente"
            persona.Activo = False
            persona.FechaRegistro = If(persona.FechaRegistro = DateTime.MinValue, DateTime.Now, persona.FechaRegistro)
            persona.FechaAceptacion = DateTime.Now
            Return ResultadoOperacion.Exito(_personaDAO.RegistrarPublicoAtomico(registro))
        Catch
            Return ResultadoOperacion.Fallo("No fue posible registrar la persona.")
        End Try
    End Function

    Public Function Actualizar(id As Integer, persona As Persona) As ResultadoOperacion
        If id <= 0 Then
            Return ResultadoOperacion.Fallo("El identificador de la persona no es válido.")
        End If

        If persona Is Nothing Then
            Return ResultadoOperacion.Fallo("La persona es obligatoria.")
        End If

        Try
            If _personaDAO.ObtenerPorId(id) Is Nothing Then
                Return ResultadoOperacion.Fallo("La persona indicada no existe.")
            End If

            Dim errores = ValidarPersona(persona, id)
            If errores.Count > 0 Then
                Return ResultadoOperacion.Fallo(errores)
            End If

            persona.IdPersona = id
            _personaDAO.Actualizar(persona)
            Return ResultadoOperacion.Exito(id)
        Catch
            Return ResultadoOperacion.Fallo("No fue posible actualizar la persona.")
        End Try
    End Function

    Public Function Eliminar(id As Integer) As ResultadoOperacion
        If id <= 0 Then
            Return ResultadoOperacion.Fallo("El identificador de la persona no es válido.")
        End If

        Try
            If _personaDAO.ObtenerPorId(id) Is Nothing Then
                Return ResultadoOperacion.Fallo("La persona indicada no existe.")
            End If

            _personaDAO.BajaLogica(id)
            Return ResultadoOperacion.Exito(id)
        Catch
            Return ResultadoOperacion.Fallo("No fue posible eliminar la persona.")
        End Try
    End Function

    Public Function ObtenerPorId(id As Integer) As Persona
        If id <= 0 Then
            Return Nothing
        End If

        Return _personaDAO.ObtenerPorId(id)
    End Function

    Public Function Listar(Optional rol As Nullable(Of Integer) = Nothing,
                           Optional buscar As String = Nothing) As List(Of Persona)
        Return _personaDAO.Listar(rol, buscar)
    End Function

    Private Function ValidarPersona(persona As Persona,
                                    Optional excluirIdPersona As Nullable(Of Integer) = Nothing) As List(Of String)
        Dim errores As New List(Of String)()
        Dim nombre = If(persona.Nombre, String.Empty).Trim()
        Dim cp = If(persona.CP, String.Empty).Trim()
        Dim telefono = If(persona.Telefono, String.Empty).Trim()
        Dim correo = If(persona.Correo, String.Empty).Trim()

        If String.IsNullOrWhiteSpace(nombre) Then
            errores.Add("El nombre es obligatorio.")
        ElseIf nombre.Length > 100 Then
            errores.Add("El nombre no puede tener más de 100 caracteres.")
        End If

        If Not Regex.IsMatch(cp, "^\d{5}$") Then
            errores.Add("El CP debe contener exactamente 5 dígitos numéricos.")
        End If

        If Not Regex.IsMatch(telefono, "^\d{10}$") Then
            errores.Add("El teléfono debe contener exactamente 10 dígitos numéricos.")
        End If

        If String.IsNullOrWhiteSpace(correo) OrElse
           Not Regex.IsMatch(correo, "^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant) Then
            errores.Add("El correo no tiene un formato válido.")
        ElseIf _personaDAO.ExisteCorreo(correo, excluirIdPersona) Then
            errores.Add("El correo ya está registrado.")
        End If

        If persona.Huella IsNot Nothing AndAlso persona.Huella.Length > 0 AndAlso
           String.IsNullOrWhiteSpace(persona.HuellaFormato) Then
            errores.Add("HuellaFormato es obligatorio cuando se proporciona una huella.")
        End If

        ValidarCatalogos(persona, errores)
        Return errores
    End Function

    Private Sub ValidarCatalogos(persona As Persona, errores As List(Of String))
        Dim estados = _catalogoDAO.ObtenerEstados()
        Dim estadoExiste = estados.Any(Function(estado) estado.IdEstado = persona.IdEstado)
        If Not estadoExiste Then
            errores.Add("El estado indicado no existe.")
        End If

        Dim municipios = _catalogoDAO.ObtenerMunicipiosPorEstado(persona.IdEstado)
        Dim municipioExiste = municipios.Any(Function(municipio) municipio.IdMunicipio = persona.IdMunicipio)
        If Not municipioExiste Then
            errores.Add("El municipio no existe o no pertenece al estado indicado.")
        End If

        Dim localidades = _catalogoDAO.ObtenerLocalidadesPorMunicipio(persona.IdMunicipio)
        If Not localidades.Any(Function(localidad) localidad.IdLocalidad = persona.IdLocalidad) Then
            errores.Add("La localidad no existe o no pertenece al municipio indicado.")
        End If

        Dim roles = _catalogoDAO.ObtenerRoles()
        If Not roles.Any(Function(rol) rol.IdRol = persona.IdRol) Then
            errores.Add("El rol indicado no existe.")
        End If
    End Sub
End Class
