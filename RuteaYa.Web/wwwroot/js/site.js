// ---------- Tema claro/oscuro ----------
// El tema inicial lo aplica un script en el <head> del layout (para evitar el parpadeo).
// Acá solo se maneja el botón que alterna y se guarda la preferencia.
(function () {
    var CLAVE = 'qz-tema';
    var boton = document.getElementById('qz-tema-toggle');
    if (!boton) { return; }

    function temaActual() {
        return document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'dark' : 'light';
    }

    // El ícono indica a qué tema se pasa al hacer clic: ☀ estando en oscuro, ☾ estando en claro.
    function pintarIcono() {
        boton.textContent = temaActual() === 'dark' ? '☀' : '☾';
    }

    boton.addEventListener('click', function () {
        var nuevo = temaActual() === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-bs-theme', nuevo);
        try { localStorage.setItem(CLAVE, nuevo); } catch (e) { /* sin almacenamiento: no se recuerda */ }
        pintarIcono();
    });

    pintarIcono();
})();

// ---------- Botón de envío con spinner ----------
// Los formularios con data-loading="texto" deshabilitan el botón al enviarse y muestran un spinner.
document.addEventListener('submit', function (evento) {
    var form = evento.target;
    var texto = form.getAttribute && form.getAttribute('data-loading');
    if (!texto) { return; }

    var boton = evento.submitter || form.querySelector('button[type="submit"], input[type="submit"]');
    if (!boton) { return; }

    // setTimeout 0: el navegador ya tomó los datos del formulario, así que deshabilitar no afecta el envío.
    setTimeout(function () {
        if (evento.defaultPrevented) { return; } // la validación del cliente frenó el envío

        boton.disabled = true;
        boton.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>';
        boton.appendChild(document.createTextNode(texto));
    }, 0);
});
