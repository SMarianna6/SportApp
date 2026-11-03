document.addEventListener("DOMContentLoaded", function () {
    const searchForms = document.querySelectorAll("form[asp-action='Index'], form");

    searchForms.forEach(form => {
        const inputs = form.querySelectorAll("input[type='text'], select");

        inputs.forEach(input => {
            input.addEventListener("input", function () {
                clearTimeout(input.timer);
                input.timer = setTimeout(() => {
                    submitFormViaAjax(form);
                }, 400);
            });

            if (input.tagName === "SELECT") {
                input.addEventListener("change", function () {
                    submitFormViaAjax(form);
                });
            }
        });

        form.addEventListener("submit", function (e) {
            e.preventDefault();
            submitFormViaAjax(form);
        });
    });

    function submitFormViaAjax(form) {
        const formData = new FormData(form);
        const url = new URL(form.action || window.location.href);

        const params = new URLSearchParams(formData);

        fetch(`${url.pathname}?${params.toString()}`, {
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            }
        })
            .then(response => response.text())
            .then(html => {
                const parser = new DOMParser();
                const doc = parser.parseFromString(html, "text/html");
                const newTables = doc.querySelectorAll("table");
                const currentTables = document.querySelectorAll("table");

                newTables.forEach((newTable, index) => {
                    if (currentTables[index]) {
                        currentTables[index].replaceWith(newTable);
                    }
                });
            })
            .catch(error => console.error("Filtering error:", error));
    }
});