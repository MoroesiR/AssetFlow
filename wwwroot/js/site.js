$(document).ready(function () {
   
    var currentUrl = window.location.pathname;
    $('.navbar-nav .nav-link').each(function () {
        var linkUrl = $(this).attr('href');
        if (currentUrl === linkUrl || currentUrl.startsWith(linkUrl + '/')) {
            $(this).addClass('active');
            $(this).append('<span class="visually-hidden">(current)</span>');
        }
    });

    
    $('form').on('submit', function () {
        var submitBtn = $(this).find('button[type="submit"]');
        var originalText = submitBtn.html();
        submitBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-2"></i>Processing...');

        
        setTimeout(function () {
            submitBtn.prop('disabled', false).html(originalText);
        }, 5000);
    });

    
    $('input[data-val-currency]').on('blur', function () {
        var value = $(this).val().replace(/[^0-9.]/g, '');
        if (value) {
            $(this).val('$' + parseFloat(value).toFixed(2));
        }
    });

    
    $('form[data-confirm]').on('submit', function (e) {
        if (!confirm($(this).data('confirm'))) {
            e.preventDefault();
        }
    });

    
    $('textarea[maxlength], input[maxlength]').on('input', function () {
        var maxLength = $(this).attr('maxlength');
        var currentLength = $(this).val().length;
        var counter = $(this).siblings('.char-counter');

        if (counter.length === 0) {
            counter = $('<small class="char-counter text-muted float-end"></small>');
            $(this).after(counter);
        }

        counter.text(currentLength + '/' + maxLength);

        if (currentLength > maxLength * 0.9) {
            counter.addClass('text-warning').removeClass('text-muted');
        } else {
            counter.removeClass('text-warning').addClass('text-muted');
        }
    });

   
    $('[data-bs-toggle="tooltip"]').tooltip();
});


function showToast(message, type = 'success') {
    var toastClass = 'bg-' + (type === 'success' ? 'success' : type === 'error' ? 'danger' : 'info');

    var toast = $('<div class="toast align-items-center text-white ' + toastClass + ' border-0" role="alert" aria-live="assertive" aria-atomic="true">' +
        '<div class="d-flex">' +
        '<div class="toast-body">' + message + '</div>' +
        '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>' +
        '</div>' +
        '</div>');

    $('#toastContainer').append(toast);
    var bsToast = new bootstrap.Toast(toast[0]);
    bsToast.show();

    setTimeout(function () {
        toast.remove();
    }, 3000);
}