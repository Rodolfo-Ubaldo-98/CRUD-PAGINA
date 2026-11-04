(function () {
    "use strict";

    var api = "/api";
    var modelos = "/models";
    var muestras = [];
    var stream = null;
    var modelosCargados = false;
    var capturaEnCurso = false;
    var fotoBase64 = null;

    function porId(id) { return document.getElementById(id); }
    function mostrar(mensaje, correcto) {
        var salida = porId("paginaMensaje");
        if (!salida) return;
        salida.textContent = mensaje || "";
        salida.className = "registro-message " + (correcto ? "is-success" : "is-error");
    }
    function seleccionar(id, texto) {
        var control = porId(id);
        control.innerHTML = "";
        var opcion = document.createElement("option");
        opcion.value = "";
        opcion.textContent = texto;
        control.appendChild(opcion);
        return control;
    }
    function valorPropiedad(objeto, nombre) {
        var clave = Object.keys(objeto).find(function (item) {
            return item.toLowerCase() === nombre.toLowerCase();
        });
        return clave ? objeto[clave] : "";
    }
    function rellenar(id, items, idPropiedad, textoPropiedad, textoInicial) {
        var control = seleccionar(id, textoInicial);
        items.forEach(function (item) {
            var opcion = document.createElement("option");
            opcion.value = valorPropiedad(item, idPropiedad);
            opcion.textContent = valorPropiedad(item, textoPropiedad);
            control.appendChild(opcion);
        });
        control.disabled = false;
    }
    async function jsonFetch(url, options) {
        var respuesta = await fetch(url, options || {});
        if (!respuesta.ok) {
            var mensaje = "HTTP " + respuesta.status + " en " + url;
            try {
                var error = await respuesta.json();
                var errores = error.errores || error.Errores;
                if (errores) mensaje = errores instanceof Array ? errores.join(" ") : String(errores);
                else if (error.message || error.Message) mensaje += ": " + (error.message || error.Message);
            } catch (_) { }
            throw new Error(mensaje);
        }
        return respuesta.status === 204 ? null : respuesta.json();
    }
    async function cargarCatalogos() {
        try {
            var datos = await Promise.all([jsonFetch(api + "/roles"), jsonFetch(api + "/estados")]);
            rellenar("rol", datos[0], "idRol", "nombreRol", "Selecciona un rol");
            rellenar("estado", datos[1], "idEstado", "nombre", "Selecciona un estado");
        } catch (error) { mostrar("No fue posible cargar los catálogos: " + error.message, false); }
    }
    async function cargarMunicipios() {
        var id = porId("estado").value;
        seleccionar("municipio", id ? "Cargando municipios..." : "Selecciona un estado");
        seleccionar("localidad", "Selecciona un municipio");
        porId("localidad").disabled = true;
        if (!id) { porId("municipio").disabled = true; return; }
        try { rellenar("municipio", await jsonFetch(api + "/estados/" + id + "/municipios"), "idMunicipio", "nombre", "Selecciona un municipio"); }
        catch (error) { mostrar(error.message, false); }
    }
    async function cargarLocalidades() {
        var id = porId("municipio").value;
        seleccionar("localidad", id ? "Cargando localidades..." : "Selecciona un municipio");
        if (!id) { porId("localidad").disabled = true; return; }
        try { rellenar("localidad", await jsonFetch(api + "/municipios/" + id + "/localidades"), "idLocalidad", "nombre", "Selecciona una localidad"); }
        catch (error) { mostrar(error.message, false); }
    }
    function cargarBiblioteca() {
        return new Promise(function (resolve, reject) {
            if (window.faceapi) { resolve(); return; }
            var script = document.createElement("script");
            script.src = "/Scripts/face-api.js";
            script.onload = function () { window.faceapi ? resolve() : reject(new Error("face-api.js no expuso faceapi.")); };
            script.onerror = function () {
                var respaldo = document.createElement("script");
                respaldo.src = "https://cdn.jsdelivr.net/npm/@vladmandic/face-api/dist/face-api.js";
                respaldo.onload = function () { window.faceapi ? resolve() : reject(new Error("No se pudo cargar face-api.js.")); };
                respaldo.onerror = function () { reject(new Error("No se pudo cargar face-api.js localmente ni desde CDN.")); };
                document.head.appendChild(respaldo);
            };
            document.head.appendChild(script);
        });
    }
    async function cargarModelos() {
        await cargarBiblioteca();
        var cargarDesde = async function (ruta) {
            await Promise.all([
                faceapi.nets.tinyFaceDetector.loadFromUri(ruta),
                faceapi.nets.faceLandmark68Net.loadFromUri(ruta),
                faceapi.nets.faceRecognitionNet.loadFromUri(ruta)
            ]);
        };
        try { await cargarDesde(modelos); }
        catch (_) { await cargarDesde("https://cdn.jsdelivr.net/npm/@vladmandic/face-api/model"); }
        modelosCargados = true;
    }
    async function activarCamara() {
        try {
            if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) throw new Error("Usa HTTPS o localhost para acceder a la cámara.");
            porId("cameraStatus").textContent = "Solicitando permiso...";
            stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "user", width: { ideal: 640 }, height: { ideal: 480 } }, audio: false });
            porId("videoRostro").srcObject = stream;
            porId("cameraStatus").textContent = "Cámara activa";
            await cargarModelos();
            porId("capturarBtn").disabled = false;
            mostrar("Cámara lista. Captura tres muestras.", true);
        } catch (error) {
            porId("cameraStatus").textContent = stream ? "Cámara activa; faltan recursos" : "Cámara no disponible";
            mostrar(error.name === "NotAllowedError" ? "Permiso de cámara denegado." : error.message, false);
        }
    }
    async function capturarMuestra() {
        if (!modelosCargados || capturaEnCurso || muestras.length >= 3) return;
        capturaEnCurso = true;
        try {
            var deteccion = await faceapi.detectSingleFace(porId("videoRostro"), new faceapi.TinyFaceDetectorOptions()).withFaceLandmarks().withFaceDescriptor();
            if (!deteccion) throw new Error("No se detectó un rostro. Centra la cara e inténtalo de nuevo.");
            if (!fotoBase64) {
                var canvas = document.createElement("canvas");
                var video = porId("videoRostro");
                canvas.width = video.videoWidth; canvas.height = video.videoHeight;
                canvas.getContext("2d").drawImage(video, 0, 0, canvas.width, canvas.height);
                fotoBase64 = canvas.toDataURL("image/jpeg", .85).split(",")[1];
            }
            muestras.push(Array.from(deteccion.descriptor));
            porId("contadorMuestras").textContent = muestras.length + "/3";
            var item = document.createElement("span"); item.className = "sample-pill"; item.textContent = "Muestra " + muestras.length + " capturada";
            porId("muestrasLista").appendChild(item);
            porId("guardarRegistroBtn").disabled = muestras.length !== 3;
            if (muestras.length === 3) { porId("capturarBtn").disabled = true; porId("cameraStatus").textContent = "Tres muestras listas"; }
        } catch (error) { mostrar(error.message, false); }
        finally { capturaEnCurso = false; }
    }
    async function guardar() {
        var ids = ["nombre", "correo", "telefono", "cp", "rol", "estado", "municipio", "localidad"];
        if (!ids.every(function (id) { return porId(id).value.trim() !== ""; }) || muestras.length !== 3) { mostrar("Completa los datos y captura tres muestras.", false); return; }
        var boton = porId("guardarRegistroBtn"); boton.disabled = true;
        try {
            var persona = { nombre: porId("nombre").value.trim(), idEstado: Number(porId("estado").value), idMunicipio: Number(porId("municipio").value), idLocalidad: Number(porId("localidad").value), cp: porId("cp").value.trim(), telefono: porId("telefono").value.trim(), correo: porId("correo").value.trim(), foto: fotoBase64, huella: null, huellaFormato: null, idRol: Number(porId("rol").value), activo: true };
            var alta = await jsonFetch(api + "/personas", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(persona) });
            for (var i = 0; i < muestras.length; i++) await jsonFetch(api + "/personas/" + alta.idGenerado + "/rostro", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ descriptor: muestras[i] }) });
            mostrar("Registro completado correctamente.", true);
        } catch (error) { mostrar(error.message, false); boton.disabled = false; }
    }
    porId("estado").addEventListener("change", cargarMunicipios);
    porId("municipio").addEventListener("change", cargarLocalidades);
    porId("activarCamaraBtn").addEventListener("click", activarCamara);
    porId("capturarBtn").addEventListener("click", capturarMuestra);
    porId("guardarRegistroBtn").addEventListener("click", guardar);
    cargarCatalogos();
}());
