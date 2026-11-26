
(function () {
    function contenedor() {
        return document.getElementById('contenedorAsistencia');
    }

    function setTexto(id, valor) {
        var el = document.getElementById(id);
        if (el) {
            el.textContent = valor;
        }
    }

    // Contadores de la lista
    function actualizarContadores() {
        var lista = document.querySelector('.asist-lista');
        if (!lista) {
            return;
        }

        var c = { pre: 0, tar: 0, aus: 0, sin: 0 };

        lista.querySelectorAll('tbody tr').forEach(function (fila) {
            var seleccion = fila.querySelector('select.asist-estado');

            if (!seleccion || !seleccion.value) {
                c.sin++;
                return;
            }

            if (seleccion.value === 'Presente') {
                c.pre++;
            }
            else if (seleccion.value === 'Tardía') {
                c.tar++;
            }
            else {
                c.aus++;
            }
        });

        setTexto('contPresentes', c.pre);
        setTexto('contTardias', c.tar);
        setTexto('contAusentes', c.aus);
        setTexto('contSinMarcar', c.sin);
    }

    // Estado que requiere motivo de justificación
    var ESTADO_JUSTIFICADO = 'Ausencia justificada';

    // Mostrar u ocultar el motivo según el estado seleccionado
    function actualizarMotivo(fila) {
        var bloque = fila.querySelector('.asist-motivo');

        if (!bloque) {
            return;
        }

        var seleccion = fila.querySelector('select.asist-estado');
        var justificada = !!seleccion && seleccion.value === ESTADO_JUSTIFICADO;
        var campo = bloque.querySelector('textarea');

        bloque.classList.toggle('d-none', !justificada);

        if (campo) {
            campo.required = justificada;

            if (!justificada) {
                campo.classList.remove('is-invalid');
            }
        }
    }

    // Actualizar todos los campos de motivo
    function actualizarMotivosLista() {
        var lista = document.querySelector('.asist-lista');

        if (!lista) {
            return;
        }

        lista.querySelectorAll('tbody tr').forEach(function (fila) {
            actualizarMotivo(fila);
        });
    }

    function mostrar(html) {
        if (html === null) {
            return;
        }

        var destino = document.getElementById('listaAsistencia');

        if (destino) {
            destino.innerHTML = html;
        }

        actualizarMotivosLista();
        actualizarContadores();
    }

    function pedir(url, opciones) {
        opciones = opciones || {};
        opciones.credentials = 'same-origin';
        opciones.headers = {
            'X-Requested-With': 'XMLHttpRequest'
        };

        fetch(url, opciones)
            .then(function (respuesta) {
                // Si la sesión venció, redirigir al inicio de sesión
                if (respuesta.redirected) {
                    window.location.href = respuesta.url;
                    return null;
                }

                return respuesta.text();
            })
            .then(mostrar)
            .catch(function () {
                mostrar(
                    '<div class="alert alert-danger">No se pudo cargar la lista de asistencia. Intente de nuevo.</div>'
                );
            });
    }

    // Cargar la lista de la clase para la fecha elegida
    function cargarLista() {
        var cont = contenedor();

        if (!cont) {
            return;
        }

        var fecha = document.getElementById('asistFecha');

        if (!fecha) {
            return;
        }

        var url = cont.getAttribute('data-url-lista')
            + '?clase=' + encodeURIComponent(cont.getAttribute('data-clase'))
            + '&fecha=' + encodeURIComponent(fecha.value);

        pedir(url);
    }

    // Detectar cambios en los menús desplegables
    document.addEventListener('change', function (e) {
        if (e.target.matches && e.target.matches('.asist-lista select.asist-estado')) {
            var fila = e.target.closest('tr');

            if (fila) {
                fila.classList.remove('table-danger');
                actualizarMotivo(fila);
            }

            actualizarContadores();
        }
    });

    document.addEventListener('click', function (e) {
        // Marcar todos los estudiantes como presentes
        if (e.target.closest('#btnTodosPresentes')) {
            document.querySelectorAll('.asist-lista select.asist-estado').forEach(function (select) {
                select.value = 'Presente';
            });

            document.querySelectorAll('.asist-lista tbody tr').forEach(function (fila) {
                fila.classList.remove('table-danger');
                actualizarMotivo(fila);
            });

            actualizarContadores();
            return;
        }

        // Cargar lista de la fecha elegida
        if (e.target.closest('#btnCargarAsistencia')) {
            cargarLista();
            return;
        }

        // Enlaces "Editar lista de este día" y "Cancelar"
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

            pedir(form.action, {
                method: 'POST',
                body: new FormData(form)
            });
        }
    });

    document.addEventListener('DOMContentLoaded', function () {
        actualizarMotivosLista();
        actualizarContadores();

        if (contenedor()) {
            var fecha = document.getElementById('asistFecha');

            if (fecha) {
                fecha.addEventListener('change', cargarLista);
            }

            cargarLista();
        }
    });
})();
