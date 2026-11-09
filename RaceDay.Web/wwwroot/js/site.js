// RaceDay - small bits of behaviour shared by every page.
(function () {
    'use strict';

    // forms with data-confirm ask first (delete / cancel buttons)
    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (form && form.getAttribute && form.getAttribute('data-confirm')) {
            if (!window.confirm(form.getAttribute('data-confirm'))) {
                event.preventDefault();
            }
        }
    });

    // stop double-clicks from sending a form twice (an enrolment, for example)
    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (event.defaultPrevented || !form || form.hasAttribute('data-allow-resubmit')) {
            return;
        }
        var buttons = form.querySelectorAll('button[type="submit"]');
        setTimeout(function () {
            buttons.forEach(function (b) { b.disabled = true; });
        }, 0);
    });

    // coming back with the Back button should not leave buttons stuck in the disabled state
    window.addEventListener('pageshow', function (event) {
        if (event.persisted) {
            window.location.reload();
        }
    });

    // event page: the enrol button only works once a category is chosen
    var enrolForm = document.getElementById('enrol-form');
    var enrolButton = document.getElementById('enrol-button');
    if (enrolForm && enrolButton) {
        enrolForm.addEventListener('change', function (event) {
            if (event.target && event.target.name === 'categoryId') {
                var label = enrolForm.querySelector('label[for="' + event.target.id + '"] .category-name');
                enrolButton.disabled = false;
                enrolButton.textContent = 'Enrol in ' + (label ? label.textContent : 'this category');
            }
        });
    }

    // image inputs show a preview before upload
    var imageInputs = document.querySelectorAll('input[data-image-preview]');
    imageInputs.forEach(function (input) {
        input.addEventListener('change', function () {
            var preview = document.querySelector(input.getAttribute('data-image-preview'));
            if (!preview) { return; }
            var file = input.files && input.files[0];
            if (file && file.type.indexOf('image/') === 0) {
                preview.src = URL.createObjectURL(file);
                preview.classList.remove('d-none');
            } else {
                preview.classList.add('d-none');
            }
        });
    });

    // event page: weather is loaded in the background through this app (never straight from the API)
    var weather = document.getElementById('weather');
    if (weather) {
        var text = weather.querySelector('.weather-text');
        fetch(weather.getAttribute('data-weather-url'), { credentials: 'same-origin', headers: { 'Accept': 'application/json' } })
            .then(function (response) { return response.ok ? response.json() : { available: false }; })
            .then(function (data) {
                if (data && data.available && data.forecast) {
                    text.textContent = 'Right now: ' + data.forecast;
                } else {
                    weather.classList.add('is-unavailable');
                }
            })
            .catch(function () { weather.classList.add('is-unavailable'); });
    }
})();
