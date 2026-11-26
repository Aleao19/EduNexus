(function () {
    var entrada = document.getElementById('buscarHorario');
    var caja = document.getElementById('sugerenciasHorario');
    if (!entrada || !caja) { return; }

    var urlSugerencias = entrada.getAttribute('data-url');
    var temporizador = null;
    var seleccionado = -1;

    function ocultar() {
        caja.style.display = 'none';
        caja.innerHTML = '';
        seleccionado = -1;
    }

    function elegir(texto) {
        entrada.value = texto;
        ocultar();
        entrada.form.submit();
    }

    function mostrar(lista) {
        caja.innerHTML = '';
        if (!lista || !lista.length) { ocultar(); return; }

        lista.forEach(function (s) {
            var opcion = document.createElement('button');
            opcion.type = 'button';
            opcion.className = 'list-group-item list-group-item-action d-flex justify-content-between align-items-center';

            var texto = document.createElement('span');
            texto.textContent = s.texto;

            var tipo = document.createElement('span');
            tipo.className = 'badge bg-secondary-lt';
            tipo.textContent = s.tipo;

            opcion.appendChild(texto);
            opcion.appendChild(tipo);

            // mousedown evita que el campo pierda el foco antes de elegir
            opcion.addEventListener('mousedown', function (e) {
                e.preventDefault();
                elegir(s.texto);
            });

            caja.appendChild(opcion);
        });

        seleccionado = -1;
        caja.style.display = 'block';
    }

    function consultar() {
        var termino = entrada.value.trim();
        if (termino.length < 2) { ocultar(); return; }

        fetch(urlSugerencias + '?term=' + encodeURIComponent(termino), {
            credentials: 'same-origin',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(function (respuesta) { return respuesta.json(); })
            .then(mostrar)
            .catch(ocultar);
    }

    entrada.addEventListener('input', function () {
        clearTimeout(temporizador);
        temporizador = setTimeout(consultar, 250);
    });

    // Navegacion con teclado: flechas, Enter y Escape
    entrada.addEventListener('keydown', function (e) {
        var opciones = caja.querySelectorAll('.list-group-item');
        if (!opciones.length) { return; }

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            seleccionado = (seleccionado + 1) % opciones.length;
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            seleccionado = (seleccionado - 1 + opciones.length) % opciones.length;
        } else if (e.key === 'Enter' && seleccionado >= 0) {
            e.preventDefault();
            elegir(opciones[seleccionado].firstChild.textContent);
            return;
        } else if (e.key === 'Escape') {
            ocultar();
            return;
        } else {
            return;
        }

        opciones.forEach(function (opcion, i) {
            opcion.classList.toggle('active', i === seleccionado);
        });
    });

    entrada.addEventListener('blur', function () {
        setTimeout(ocultar, 150);
    });
    })();
    (function () {
        var boton = document.getElementById('btnNuevoHorario');
        var combo = document.getElementById('id');
        if (!boton || !combo) { return; }

        function actualizar() {
            var base = boton.getAttribute('data-base');
            boton.href = combo.value ? base + '/' + encodeURIComponent(combo.value) : base;
        }

        combo.addEventListener('change', actualizar);
        actualizar();
})();