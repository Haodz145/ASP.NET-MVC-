// Hiển thị ảnh được chọn từ input file lên thẻ img
// (Thẻ input có thuộc tính data-img-preview trỏ đến id của thẻ img dung để hiển thị ảnh)
function previewImage(input) {
    if (!input.files || !input.files[0]) return;

    const previewId = input.dataset.imgPreview; // lấy data-img-preview
    if (!previewId) return;

    const img = document.getElementById(previewId);
    if (!img) return;

    const reader = new FileReader();
    reader.onload = function (e) {
        img.src = e.target.result;
    };
    reader.readAsDataURL(input.files[0]);
}

// Tìm kiếm phân trang bằng AJAX
function paginationSearch(event, form, page) {
    if (event) event.preventDefault();
    if (!form) return;

    const url = form.action;
    const method = (form.method || "GET").toUpperCase();
    const targetId = form.dataset.target;

    const formData = new FormData(form);
    formData.append("page", page);

    let fetchUrl = url;
    if (method === "GET") {
        const params = new URLSearchParams(formData).toString();
        fetchUrl = url + "?" + params;
    }

    let targetEl = null;
    if (targetId) {
        targetEl = document.getElementById(targetId);
        if (targetEl) {
            targetEl.innerHTML = `
                <div class="text-center py-4">
                    <span>Đang tải dữ liệu...</span>
                </div>`;
        }
    }

    fetch(fetchUrl, {
        method: method,
        body: method === "GET" ? null : formData
    })
        .then(res => res.text())
        .then(html => {
            if (targetEl) {
                targetEl.innerHTML = html;
            }
        })
        .catch(() => {
            if (targetEl) {
                targetEl.innerHTML = `
                <div class="text-danger">
                    Không tải được dữ liệu
                </div>`;
            }
        });
}

// Mở modal và load nội dung từ link vào modal
// FIX: Sau khi load HTML vào innerHTML, script tags KHÔNG tự chạy (browser security).
// Dùng execModalScripts() để tái tạo và append script nodes → trình duyệt thực thi được.
(function () {
    //dialogModal là id của modal dùng chung được định nghĩa trong _Layout.cshtml
    const modalEl = document.getElementById("dialogModal");
    if (!modalEl) return;

    const modalContent = modalEl.querySelector(".modal-content");

    // Clear nội dung khi modal đóng
    modalEl.addEventListener('hidden.bs.modal', function () {
        modalContent.innerHTML = '';
    });

    window.openModal = function (event, link) {
        if (!link) return;
        if (event) event.preventDefault();

        const url = link.getAttribute("href");

        // Hiển thị loading
        modalContent.innerHTML = `
            <div class="modal-body text-center py-5">
                <span>Đang tải dữ liệu...</span>
            </div>`;

        // Khởi tạo modal (chỉ tạo 1 lần)
        let modal = bootstrap.Modal.getInstance(modalEl);
        if (!modal) {
            modal = new bootstrap.Modal(modalEl, {
                backdrop: 'static',
                keyboard: false
            });
        }

        modal.show();

        // Load nội dung
        fetch(url)
            .then(res => res.text())
            .then(html => {
                modalContent.innerHTML = html;
                // TODO: FIX "Scripts inserted via innerHTML are not executed"
                // Sau khi gán innerHTML, các <script> tag bên trong KHÔNG được trình duyệt thực thi.
                // Giải pháp: duyệt qua tất cả <script> trong nội dung vừa load,
                // tạo lại script element mới và append vào DOM để trình duyệt chạy.
                execModalScripts(modalContent);
            })
            .catch(() => {
                modalContent.innerHTML = `
                    <div class="modal-body text-danger">
                        Không tải được dữ liệu
                    </div>`;
            });
    };
})();

/**
 * TODO: FIX "Scripts inserted via innerHTML are not executed"
 * Hàm này tìm tất cả <script> tag trong container vừa được gán bằng innerHTML,
 * tạo lại thành script element mới rồi append vào container.
 * Trình duyệt sẽ thực thi các script này như script bình thường.
 * @param {HTMLElement} container - Phần tử chứa HTML vừa được load vào modal
 */
function execModalScripts(container) {
    // Tìm tất cả script tags trong nội dung vừa load
    const scripts = container.querySelectorAll('script');
    scripts.forEach(function (oldScript) {
        const newScript = document.createElement('script');

        // Copy tất cả attributes (src, type, v.v.)
        Array.from(oldScript.attributes).forEach(function (attr) {
            newScript.setAttribute(attr.name, attr.value);
        });

        // Copy nội dung inline script
        newScript.textContent = oldScript.textContent;

        // Xóa script cũ và thêm script mới → trình duyệt thực thi
        oldScript.parentNode.replaceChild(newScript, oldScript);
    });
}