(function () {
    function contenedor() {
        return document.getElementById('contenedorAsistencia');
    }

    function setTexto(id, valor) {
        var el = document.getElementById(id);
        if (el) { el.textContent = valor; }
    }

    // Contadores de la lista
    function actualizarContadores() {
        var lista = document.querySelector('.asist-lista');
        if (!lista) { return; }

        var c = { pre: 0, tar: 0, aus: 0, sin: 0 };

        lista.querySelectorAll('tbody tr').forEach(function (fila) {
            var marcado = fila.querySelector('input[type=radio]:checked');
            if (!marcado) { c.sin++; return; }
            if (marcado.value === 'Presente') { c.pre++; }
            else if (marcado.value === 'Tardía') { c.tar++; }
            else { c.aus++; }
        });

        setTexto('contPresentes', c.pre);
        setTexto('contTardias', c.tar);
        setTexto('contAusentes', c.aus);
        setTexto('contSinMarcar', c.sin);
    }

    function mostrar(html) {
        if (html === null) { return; }
        var destino = document.getElementById('listaAsistencia');
        if (destino) { destino.innerHTML = html; }
        actualizarContadores();
    }

    function pedir(url, opciones) {
        opciones = opciones || {};
        opciones.credentials = 'same-origin';
        opciones.headers = { 'X-Requested-With': 'XMLHttpRequest' };

        fetch(url, opciones)
            .then(function (respuesta) {
                // Si la sesión venció, el servidor redirige al inicio de sesión
                if (respuesta.redirected) {
                    window.location.href = respuesta.url;
                    return null;
                }
                return respuesta.text();
            })
            .then(mostrar)
            .catch(function () {
                mostrar('<div class="alert alert-danger">No se pudo cargar la lista de asistencia. Intente de nuevo.</div>');
            });
    }

    // Carga la lista de la clase para la fecha elegida (pestaña del Aula Virtual)
    function cargarLista() {
        var cont = contenedor();
        if (!cont) { return; }

        var fecha = document.getElementById('asistFecha').value;
        var url = cont.getAttribute('data-url-lista')
            + '?clase=' + encodeURIComponent(cont.getAttribute('data-clase'))
            + '&fecha=' + encodeURIComponent(fecha);

        pedir(url);
    }

    document.addEventListener('change', function (e) {
        if (e.target.matches && e.target.matches('.asist-lista input[type=radio]')) {
            var fila = e.target.closest('tr');
            if (fila) { fila.classList.remove('table-danger'); }
            actualizarContadores();
        }
    });

    document.addEventListener('click', function (e) {
        // Marcar todos presentes
        if (e.target.closest('#btnTodosPresentes')) {
            document.querySelectorAll('.asist-lista input[type=radio][value="Presente"]').forEach(function (r) {
                r.checked = true;
            });
            document.querySelectorAll('.asist-lista tbody tr').forEach(function (f) {
                f.classList.remove('table-danger');
            });
            actualizarContadores();
            return;
        }

        // Cargar lista de la fecha elegida
        if (e.target.closest('#btnCargarAsistencia')) {
            cargarLista();
            return;
        }

        // Enlaces "Editar lista de este día" y "Cancelar" dentro de la pestaña
        var enlace = e.target.closest('a.asist-ajax');
        if (enlace && contenedor()) {
            e.preventDefault();
            pedir(enlace.href);
        }
    });

    // Guardar sin recargar la página (pestaña del Aula Virtual)
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (form.classList && form.classList.contains('asist-ajax') && contenedor()) {
            e.preventDefault();
            pedir(form.action, { method: 'POST', body: new FormData(form) });
        }
    });

    document.addEventListener('DOMContentLoaded', function () {
        actualizarContadores();

        if (contenedor()) {
            var fecha = document.getElementById('asistFecha');
            if (fecha) { fecha.addEventListener('change', cargarLista); }
            cargarLista();
        }
    });
})();