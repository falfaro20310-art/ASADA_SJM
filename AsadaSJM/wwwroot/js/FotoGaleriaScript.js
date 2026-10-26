$(document).on('click', '.card-galeria', function () {
    var idGaleria = $(this).data('id');

    $.ajax({
        url: '/FotoGaleria/ObtenerFotosPorGaleria',
        data: { IdGaleria: idGaleria },
        type: 'GET',
        success: function (response) {
            $('#exampleModal .modal-body').html(response);
            var myModal = new bootstrap.Modal(document.getElementById('exampleModal'));
            myModal.show();
        },
        error: function (xhr, status, error) {
            console.error('Error al obtener la vista parcial', error);
        }
    });
});