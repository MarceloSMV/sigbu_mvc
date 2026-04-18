/**
 * Muestra un mensaje del sistema modal.
 * @param {number} id - 1: Éxito (Verde), 2: Error/Alerta (Rojo), 3: Confirmación (Azul), 4: Peligro (Rojo Oscuro)
 * @param {string} mensaje - Texto a mostrar
 * @param {function} callbackAceptar - (Opcional) Función al dar click en Aceptar
 */
function mensajeSistema(id, mensaje, callbackAceptar) {
    var modalEl = document.getElementById('modalSistema');
    if (!modalEl) return; // Protección si el modal no existe
    var modal = new bootstrap.Modal(modalEl);

    // Referencias al DOM
    var icono = document.getElementById('msjIcono');
    var texto = document.getElementById('msjTexto');
    var btnAceptar = document.getElementById('btnAceptar');
    var btnCancelar = document.getElementById('btnCancelar');

    // 1. Resetear clases e icono base
    icono.className = 'bi display-3 mb-3';
    btnAceptar.className = 'btn px-4';
    btnCancelar.classList.remove('d-none'); // Mostrar cancelar por defecto
    texto.innerText = mensaje;

    // 2. Configurar estilo según ID
    switch (id) {
        case 1: // ÉXITO
            icono.classList.add('bi-check-circle-fill', 'text-success');
            btnAceptar.classList.add('btn-success');
            btnAceptar.innerText = "Continuar";
            btnCancelar.classList.add('d-none'); // Ocultar cancelar
            break;

        case 2: // ERROR / ALERTA
            icono.classList.add('bi-exclamation-triangle-fill', 'text-danger');
            btnAceptar.classList.add('btn-danger');
            btnAceptar.innerText = "Entendido";
            btnCancelar.classList.add('d-none'); // Ocultar cancelar
            break;

        case 3: // CONFIRMACIÓN ESTÁNDAR
            icono.classList.add('bi-question-circle-fill', 'text-primary');
            btnAceptar.classList.add('btn-primary');
            btnAceptar.innerText = "Aceptar";
            break;

        case 4: // PELIGRO / CRÍTICO (Eliminar, Rechazar)
            icono.classList.add('bi-x-octagon-fill', 'text-danger');
            btnAceptar.classList.add('btn-danger');
            btnAceptar.innerText = "Confirmar";
            break;

        default: // Fallback
            icono.classList.add('bi-info-circle-fill', 'text-secondary');
            btnAceptar.classList.add('btn-dark');
    }

    // 3. Configurar Acción del Botón Aceptar (Clonación para limpiar eventos previos)
    var nuevoBtn = btnAceptar.cloneNode(true);
    btnAceptar.parentNode.replaceChild(nuevoBtn, btnAceptar);

    nuevoBtn.onclick = function () {
        if (callbackAceptar) callbackAceptar();
        modal.hide();
    };

    modal.show();
}