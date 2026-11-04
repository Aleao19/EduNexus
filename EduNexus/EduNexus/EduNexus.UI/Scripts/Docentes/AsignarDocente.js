// AsignarDocente.js - Lógica para la asignación de materias y secciones a docentes

document.addEventListener('DOMContentLoaded', function () {
    const selectMateria = document.getElementById('selectMateria');
    const selectSeccion = document.getElementById('selectSeccion');
    const hdAsignar = document.getElementById('hdAsignar');
    const hdInicio = document.getElementById('hdInicio');
    const hdTabla = document.getElementById('hdTabla');
    const hdVacio = document.getElementById('hdVacio');
    const resumenMateria = document.getElementById('resumenMateria');
    const resumenSeccion = document.getElementById('resumenSeccion');
    const alertaConfirmacion = document.getElementById('alertaConfirmacion');

    // Validar que todos los elementos existan
    if (!selectMateria || !selectSeccion) {
        console.warn('Elementos requeridos no encontrados en AsignarDocente.js');
        return;
    }

    /**
     * Actualiza los horarios disponibles basado en la materia y sección seleccionadas
     */
    function actualizarHorarios() {
        const idMateria = selectMateria.value;
        const idSeccion = selectSeccion.value;

        // Mostrar/ocultar elementos
        if (hdTabla) hdTabla.style.display = 'none';
        if (hdVacio) hdVacio.style.display = 'none';
        if (hdInicio) hdInicio.style.display = (idMateria && idSeccion) ? 'none' : 'block';

        if (!idMateria || !idSeccion) {
            if (hdAsignar) hdAsignar.disabled = true;
            if (resumenMateria) {
                resumenMateria.textContent = 'No seleccionada';
                resumenMateria.className = 'mb-0 fw-bold text-muted';
            }
            if (resumenSeccion) {
                resumenSeccion.textContent = 'No seleccionada';
                resumenSeccion.className = 'mb-0 fw-bold text-muted';
            }
            if (alertaConfirmacion) alertaConfirmacion.style.display = 'none';
            return;
        }

        // Actualizar resumen con los valores seleccionados
        const textoMateria = selectMateria.options[selectMateria.selectedIndex].text;
        const textoSeccion = selectSeccion.options[selectSeccion.selectedIndex].text;

        if (resumenMateria) {
            resumenMateria.textContent = textoMateria;
            resumenMateria.className = 'mb-0 fw-bold text-dark';
        }
        if (resumenSeccion) {
            resumenSeccion.textContent = textoSeccion;
            resumenSeccion.className = 'mb-0 fw-bold text-dark';
        }

        // Mostrar horarios disponibles
        const filasHorarios = document.querySelectorAll('.hd-fila');
        let hayHorarios = false;

        filasHorarios.forEach(fila => {
            if (fila.dataset.materia === idMateria && fila.dataset.seccion === idSeccion) {
                fila.style.display = 'table-row';
                hayHorarios = true;
            } else {
                fila.style.display = 'none';
            }
        });

        // Mostrar la tabla si hay horarios, sino mostrar alerta de vacío
        if (hayHorarios) {
            if (hdTabla) hdTabla.style.display = 'block';
            if (hdVacio) hdVacio.style.display = 'none';
            if (alertaConfirmacion) alertaConfirmacion.style.display = 'block';
        } else {
            if (hdTabla) hdTabla.style.display = 'none';
            if (hdVacio) hdVacio.style.display = 'block';
            if (alertaConfirmacion) alertaConfirmacion.style.display = 'none';
        }

        if (hdAsignar) hdAsignar.disabled = false;
    }

    // Agregar event listeners
    if (selectMateria) {
        selectMateria.addEventListener('change', actualizarHorarios);
    }
    if (selectSeccion) {
        selectSeccion.addEventListener('change', actualizarHorarios);
    }

    // Inicializar
    actualizarHorarios();
});