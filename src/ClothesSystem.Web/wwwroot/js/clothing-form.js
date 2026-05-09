(function () {
    const forms = document.querySelectorAll("[data-clothing-form]");

    function reindexRows(tableBody) {
        const rows = Array.from(tableBody.querySelectorAll("[data-fabric-row]"));

        rows.forEach((row, index) => {
            const fields = row.querySelectorAll("input");
            fields.forEach((field) => {
                if (!field.name) {
                    return;
                }

                field.name = field.name.replace(/FabricEntries\[\d+\]/, `FabricEntries[${index}]`);
                if (field.id) {
                    field.id = field.id.replace(/FabricEntries_\d+__/, `FabricEntries_${index}__`);
                }
            });
        });
    }

    forms.forEach((form) => {
        const editor = form.querySelector("[data-fabric-editor]");
        if (!editor) {
            return;
        }

        const tableBody = editor.querySelector("[data-fabric-table] tbody");
        const template = editor.querySelector("template[data-fabric-row-template]");
        const addButton = editor.querySelector("[data-add-fabric-row]");

        function ensureAtLeastOneRow() {
            if (tableBody.querySelectorAll("[data-fabric-row]").length === 0) {
                addRow();
            }
        }

        function addRow() {
            const nextIndex = tableBody.querySelectorAll("[data-fabric-row]").length;
            const html = template.innerHTML.replaceAll("__index__", String(nextIndex));
            tableBody.insertAdjacentHTML("beforeend", html);
            reindexRows(tableBody);
        }

        addButton?.addEventListener("click", function () {
            addRow();
        });

        tableBody.addEventListener("click", function (event) {
            const trigger = event.target;
            if (!(trigger instanceof HTMLElement) || !trigger.matches("[data-remove-fabric-row]")) {
                return;
            }

            const rows = tableBody.querySelectorAll("[data-fabric-row]");
            const row = trigger.closest("[data-fabric-row]");
            if (!row) {
                return;
            }

            if (rows.length === 1) {
                row.querySelectorAll("input").forEach((input) => {
                    if (input.type !== "hidden") {
                        input.value = "";
                    }
                });
                return;
            }

            row.remove();
            reindexRows(tableBody);
            ensureAtLeastOneRow();
        });

        form.addEventListener("submit", function () {
            reindexRows(tableBody);
        });
    });
})();
