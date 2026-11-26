(function () {
    // Después de publicar, la página se recarga y vuelve a abrir esta pestaña
    document.addEventListener('DOMContentLoaded', function () {
        if (window.location.hash === '#tab-material') {
            var boton = document.getElementById('tab-material-btn');
            if (boton && window.bootstrap) {
                bootstrap.Tab.getOrCreateInstance(boton).show();
            }
        }
    });

    var form = document.getElementById('formMaterial');
    if (!form) { return; }

    var tipo = document.getElementById('tipoMaterial');
    var grupoArchivo = document.getElementById('grupoArchivo');
    var grupoUrl = document.getElementById('grupoUrl');
    var alerta = document.getElementById('alertaMaterial');

    function alternarTipo() {
        var esArchivo = tipo.value === 'Archivo';
        grupoArchivo.classList.toggle('d-none', !esArchivo);
        grupoUrl.classList.toggle('d-none', esArchivo);
    }

    function limpiarErrores() {
        alerta.classList.add('d-none');
        alerta.textContent = '';
        form.querySelectorAll('.is-invalid').forEach(function (el) { el.classList.remove('is-invalid'); });
        form.querySelectorAll('[data-error]').forEach(function (el) { el.textContent = ''; });
    }

    function mostrarErrores(errores) {
        Object.keys(errores).forEach(function (campo) {
            var entrada = form.querySelector('[name="' + campo + '"]');
            var mensaje = form.querySelector('[data-error="' + campo + '"]');
            if (entrada) { entrada.classList.add('is-invalid'); }
            if (mensaje) { mensaje.textContent = errores[campo]; }
        });
    }

    function mostrarAlerta(texto) {
        alerta.textContent = texto;
        alerta.classList.remove('d-none');
    }

    tipo.addEventListener('change', function () {
        limpiarErrores();
        alternarTipo();
    });

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        limpiarErrores();

        // Tamaño máximo: se avisa antes de subir el archivo
        var archivo = form.querySelector('[name="archivo"]');
        var maximo = parseInt(form.getAttribute('data-max-bytes'), 10);
        if (tipo.value === 'Archivo' && archivo.files.length > 0 && archivo.files[0].size > maximo) {
            mostrarErrores({ archivo: form.getAttribute('data-msg-tamano') });
            return;
        }

        var boton = form.querySelector('button[type=submit]');
        boton.disabled = true;

        fetch(form.getAttribute('data-url'), {
            method: 'POST',
            body: new FormData(form),
            credentials: 'same-origin',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(function (respuesta) {
                // Si la sesión venció, el servidor redirige al inicio de sesión
                if (respuesta.redirected) {
                    window.location.href = respuesta.url;
                    return null;
                }
                return respuesta.json();
            })
            .then(function (datos) {
                if (!datos) { return; }

                if (datos.ok) {
                    window.location.hash = 'tab-material';
                    window.location.reload();
                    return;
                }

                if (datos.errores) { mostrarErrores(datos.errores); }
                if (datos.mensaje) { mostrarAlerta(datos.mensaje); }
            })
            .catch(function () {
                mostrarAlerta('No se pudo publicar el material. Intente de nuevo.');
            })
            .then(function () {
                boton.disabled = false;
            });
    });

    alternarTipo();
})();