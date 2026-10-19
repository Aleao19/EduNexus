(function () {
    function el(id) {
        var e = document.getElementById(id);
        return e ? e : document.createElement("div");
    }

    function inp(id) {
        var e = document.getElementById(id);
        return e instanceof HTMLInputElement ? e : document.createElement("input");
    }

    function txt(id, valor) {
        el(id).textContent = valor;
    }

    var grid = el("grid");
    var form = el("formHorario");
    var errorBox = el("errorBox");

    var dias = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes"];
    var inicioMin = 7 * 60;
    var paso = 30;
    var total = 20; // 07:00 a 17:00

    var st = { dia: "", a: -1, b: -1, paso: 0 };

    function fmt(min) {
        var h = Math.floor(min / 60);
        var m = min % 60;
        return (h < 10 ? "0" : "") + h + ":" + (m < 10 ? "0" : "") + m;
    }

    function slotMin(i) {
        return inicioMin + i * paso;
    }

    // Encabezados
    grid.appendChild(document.createElement("div"));
    dias.forEach(function (d) {
        var h = document.createElement("div");
        h.className = "hz-head";
        h.textContent = d;
        grid.appendChild(h);
    });

    // Filas
    for (var i = 0; i < total; i++) {
        var lbl = document.createElement("div");
        lbl.className = "hz-hora";
        lbl.textContent = (i % 2 === 0) ? fmt(slotMin(i)) : "";
        grid.appendChild(lbl);

        for (var j = 0; j < dias.length; j++) {
            var c = document.createElement("div");
            c.className = "hz-cell";
            c.setAttribute("data-dia", dias[j]);
            c.setAttribute("data-i", String(i));
            c.title = dias[j] + " " + fmt(slotMin(i));
            grid.appendChild(c);
        }
    }

    grid.addEventListener("click", function (e) {
        var objetivo = e.target instanceof Element ? e.target.closest(".hz-cell") : null;
        if (!objetivo) { return; }

        var dia = objetivo.getAttribute("data-dia") || "";
        var idx = parseInt(objetivo.getAttribute("data-i") || "0", 10);

        if (st.paso === 1 && st.dia === dia) {
            st.a = Math.min(st.a, idx);
            st.b = Math.max(st.b, idx);
            st.paso = 2;
        } else {
            st.dia = dia;
            st.a = idx;
            st.b = idx;
            st.paso = 1;
        }
        pintar();
    });

    function pintar() {
        var cells = grid.querySelectorAll(".hz-cell");
        for (var k = 0; k < cells.length; k++) {
            var celda = cells[k];
            var idx = parseInt(celda.getAttribute("data-i") || "0", 10);
            var dia = celda.getAttribute("data-dia") || "";
            var seleccionada = st.a >= 0 && st.dia === dia && idx >= st.a && idx <= st.b;
            celda.classList.toggle("hz-sel", seleccionada && st.paso === 2);
            celda.classList.toggle("hz-anchor", seleccionada && st.paso === 1);
        }
        actualizarResumen();
    }

    function materiaSeleccionada() {
        var r = document.querySelector("input[name=materia]:checked");
        if (!(r instanceof HTMLInputElement)) { return ""; }
        var l = document.querySelector("label[for='" + r.id + "']");
        return (l && l.textContent) ? l.textContent : "";
    }

    function actualizarResumen() {
        txt("resMateria", materiaSeleccionada() || "—");
        txt("resDia", st.dia || "—");
        if (st.a >= 0) {
            txt("resHora", fmt(slotMin(st.a)) + " – " + fmt(slotMin(st.b) + paso));
        } else {
            txt("resHora", "—");
        }
    }

    var radios = document.querySelectorAll("input[name=materia]");
    for (var r = 0; r < radios.length; r++) {
        radios[r].addEventListener("change", actualizarResumen);
    }

    form.addEventListener("submit", function (e) {
        var msg = "";
        if (materiaSeleccionada() === "") {
            msg = "Seleccione una materia.";
        } else if (st.a < 0 || st.dia === "") {
            msg = "Marque el bloque de horario en la cuadrícula.";
        } else if (st.paso === 1) {
            msg = "Haga un segundo clic en el mismo día para definir la hora de fin.";
        }

        if (msg !== "") {
            e.preventDefault();
            errorBox.textContent = msg;
            errorBox.style.display = "block";
            return;
        }

        inp("dia_semana").value = st.dia;
        inp("hora_inicio").value = fmt(slotMin(st.a));
        inp("hora_fin").value = fmt(slotMin(st.b) + paso);
    });

    actualizarResumen();
})();