(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var nombreInput = document.getElementById("nombre");
        var profesorSelect = document.getElementById("id_profesor");
        var descInput = document.getElementById("descripcion");
        var contador = document.getElementById("contadorDescripcion");
        var resumen = document.getElementById("resumenCambios");
        var texto = document.getElementById("textoResumen");

        if (!nombreInput || !profesorSelect || !descInput || !resumen || !texto) {
            return;
        }

        var nombreOriginal = nombreInput.value;
        var profesorOriginal = profesorSelect.value;
        var descOriginal = descInput.value;

        function actualizar() {
            if (contador) {
                contador.textContent = descInput.value.length;
            }

            var cambios = [];

            if (nombreInput.value !== nombreOriginal) {
                cambios.push('nombre: "' + nombreOriginal + '" → "' + nombreInput.value + '"');
            }

            if (profesorSelect.value !== profesorOriginal) {
                var opcion = profesorSelect.options[profesorSelect.selectedIndex];
                cambios.push("profesor: " + (opcion ? opcion.text : ""));
            }

            if (descInput.value !== descOriginal) {
                cambios.push("descripción modificada");
            }

            if (cambios.length) {
                texto.textContent = "Cambios pendientes → " + cambios.join(" | ");
                resumen.classList.remove("d-none");
            } else {
                resumen.classList.add("d-none");
            }
        }

        nombreInput.addEventListener("input", actualizar);
        profesorSelect.addEventListener("change", actualizar);
        descInput.addEventListener("input", actualizar);
        actualizar();
    });
})();