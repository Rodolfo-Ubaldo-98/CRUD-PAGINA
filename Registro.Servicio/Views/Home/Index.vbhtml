@Code
    ViewBag.Title = "Alta de persona"
End Code

<main class="registro-page">
    <section class="registro-hero">
        <div>
            <span class="eyebrow">REGISTRO DIGITAL</span>
            <h1>Alta de persona y rostro</h1>
            <p>Completa los datos, captura tres muestras y guarda el perfil facial.</p>
        </div>
        <div class="hero-badge">RF</div>
    </section>

    <div id="paginaMensaje" class="registro-message" role="status"></div>

    <section class="registro-layout">
        <article class="registro-card">
            <div class="card-heading">
                <div class="card-icon">1</div>
                <div><h2>Datos de la persona</h2><p>Los campos marcados son obligatorios.</p></div>
            </div>
            <div class="registro-form-grid">
                <div class="field field-wide">
                    <label for="nombre">Nombre completo</label>
                    <input id="nombre" class="registro-input" maxlength="100" required />
                </div>
                <div class="field">
                    <label for="correo">Correo</label>
                    <input id="correo" class="registro-input" type="email" maxlength="254" required />
                </div>
                <div class="field">
                    <label for="telefono">Teléfono</label>
                    <input id="telefono" class="registro-input" maxlength="10" inputmode="numeric" required />
                </div>
                <div class="field">
                    <label for="cp">Código postal</label>
                    <input id="cp" class="registro-input" maxlength="5" inputmode="numeric" required />
                </div>
                <div class="field">
                    <label for="rol">Rol</label>
                    <select id="rol" class="registro-input" required><option value="">Cargando roles...</option></select>
                </div>
                <div class="field">
                    <label for="estado">Estado</label>
                    <select id="estado" class="registro-input" required><option value="">Cargando estados...</option></select>
                </div>
                <div class="field">
                    <label for="municipio">Municipio</label>
                    <select id="municipio" class="registro-input" disabled required><option value="">Selecciona un estado</option></select>
                </div>
                <div class="field">
                    <label for="localidad">Localidad</label>
                    <select id="localidad" class="registro-input" disabled required><option value="">Selecciona un municipio</option></select>
                </div>
            </div>
        </article>

        <article class="registro-card camera-card">
            <div class="card-heading">
                <div class="card-icon">2</div>
                <div><h2>Muestras faciales</h2><p>Coloca el rostro dentro del encuadre y captura tres muestras.</p></div>
            </div>
            <div class="camera-frame">
                <video id="videoRostro" autoplay muted playsinline></video>
                <div class="camera-guide" aria-hidden="true"></div>
                <span id="cameraStatus" class="camera-status">Cámara detenida</span>
            </div>
            <div class="camera-actions">
                <button id="activarCamaraBtn" class="registro-button secondary" type="button">Activar cámara</button>
                <button id="capturarBtn" class="registro-button" type="button" disabled>Capturar muestra <span id="contadorMuestras">0/3</span></button>
            </div>
            <div id="muestrasLista" class="sample-list" aria-live="polite"></div>
        </article>
    </section>

    <section class="registro-submit-card">
        <div><h2>3. Guardar registro</h2><p>Primero se crea la persona y después se guardan sus tres descriptores.</p></div>
        <button id="guardarRegistroBtn" class="registro-button" type="button" disabled>Guardar persona y muestras</button>
    </section>
</main>

<script src="@Url.Content("~/Scripts/RegistroRostro.js")"></script>