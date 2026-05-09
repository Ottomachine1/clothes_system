(function () {
    'use strict';

    const dropZone = document.getElementById('imageDropZone');
    const fileInput = document.getElementById('imageInput');
    const previewGrid = document.getElementById('imagePreviewGrid');
    const template = document.getElementById('imagePreviewTemplate');

    if (!dropZone || !fileInput || !previewGrid) {
        return;
    }

    let selectedFiles = [];

    dropZone.addEventListener('click', function () {
        fileInput.click();
    });

    fileInput.addEventListener('change', function (e) {
        handleFiles(e.target.files);
    });

    dropZone.addEventListener('dragover', function (e) {
        e.preventDefault();
        dropZone.classList.add('drag-over');
    });

    dropZone.addEventListener('dragleave', function (e) {
        e.preventDefault();
        dropZone.classList.remove('drag-over');
    });

    dropZone.addEventListener('drop', function (e) {
        e.preventDefault();
        dropZone.classList.remove('drag-over');
        handleFiles(e.dataTransfer.files);
    });

    function handleFiles(files) {
        const validTypes = ['image/jpeg', 'image/png', 'image/webp'];
        const maxSize = 5 * 1024 * 1024;

        Array.from(files).forEach(function (file) {
            if (!validTypes.includes(file.type)) {
                alert(file.name + ' 不是支持的图片格式 (仅支持 jpg、png、webp)');
                return;
            }
            if (file.size > maxSize) {
                alert(file.name + ' 超过 5MB 限制');
                return;
            }
            selectedFiles.push(file);
            addPreview(file);
        });

        updateFileInput();
    }

    function addPreview(file) {
        const clone = template.content.cloneNode(true);
        const previewItem = clone.querySelector('.preview-item');
        const img = clone.querySelector('.preview-image');
        const filename = clone.querySelector('.preview-filename');
        const removeBtn = clone.querySelector('.btn-remove-image');

        const reader = new FileReader();
        reader.onload = function (e) {
            img.src = e.target.result;
        };
        reader.readAsDataURL(file);

        filename.textContent = file.name;
        removeBtn.onclick = function () {
            removePreview(file);
        };

        previewItem.dataset.previewId = file.name + '_' + file.size;
        previewGrid.appendChild(clone);
    }

    function removePreview(file) {
        selectedFiles = selectedFiles.filter(function (f) {
            return f !== file;
        });
        renderPreviews();
        updateFileInput();
    }

    function renderPreviews() {
        previewGrid.innerHTML = '';
        selectedFiles.forEach(function (file) {
            addPreview(file);
        });
    }

    function updateFileInput() {
        const dt = new DataTransfer();
        selectedFiles.forEach(function (f) {
            dt.items.add(f);
        });
        fileInput.files = dt.files;
    }
})();
