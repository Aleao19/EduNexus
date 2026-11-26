(function () {
    /** @type {HTMLInputElement | null} */
    var entrada = /** @type {HTMLInputElement | null} */ (document.getElementById('buscarMateria'));
    var aviso = document.getElementById('sinResultados');
    if (!entrada) { return; }

    function filtrarMaterias() {
        var texto = (entrada.value || '').toLowerCase().trim();
        var tarjetas = document.querySelectorAll('.tarjeta-materia');
        var visibles = 0;

        tarjetas.forEach(function (tarjeta) {
            var mostrar = (tarjeta.getAttribute('data-nombre') || '').indexOf(texto) !== -1;
            tarjeta.style.display = mostrar ? '' : 'none';
            if (mostrar) { visibles++; }
        });

        if (aviso) {
            aviso.style.display = (visibles === 0 && tarjetas.length > 0) ? '' : 'none';
        }
    }

    entrada.addEventListener('input', filtrarMaterias);
})();