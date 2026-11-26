(function () {
    var grupo = document.getElementById('filtrosAsignaciones');
    var sinResultados = document.getElementById('sinResultadosAsignaciones');
    if (!grupo || !sinResultados) { return; }

    grupo.querySelectorAll('button[data-filtro]').forEach(function (boton) {
        boton.addEventListener('click', function () {
            var filtro = boton.getAttribute('data-filtro');

            grupo.querySelectorAll('button').forEach(function (b) {
                b.classList.remove('active');
            });
            boton.classList.add('active');

            var visibles = 0;
            document.querySelectorAll('.tarjeta-asignacion').forEach(function (tarjeta) {
                var estado = tarjeta.getAttribute('data-estado') || '';
                var mostrar = filtro === 'todas' || estado.indexOf(filtro) === 0;
                tarjeta.style.display = mostrar ? '' : 'none';
                if (mostrar) { visibles++; }
            });

            sinResultados.style.display = visibles === 0 ? '' : 'none';
        });
    });
})();