// ====================================================================================
// == PHẠM VI GLOBAL: Các hàm có thể được gọi từ bất kỳ đâu, kể cả từ các View     ==
// ====================================================================================

const AppAlert = (function () {
    function normalizeType(type) {
        if (type === 'danger') {
            return 'error';
        }

        return type || 'success';
    }

    function getIcon(type) {
        switch (normalizeType(type)) {
            case 'success':
                return 'bi-check-circle-fill';
            case 'error':
                return 'bi-exclamation-triangle-fill';
            case 'warning':
                return 'bi-exclamation-circle-fill';
            default:
                return 'bi-info-circle-fill';
        }
    }

    function getTitle(type) {
        switch (normalizeType(type)) {
            case 'success':
                return 'Thành công';
            case 'error':
                return 'Có lỗi xảy ra';
            case 'warning':
                return 'Lưu ý';
            default:
                return 'Thông báo';
        }
    }

    function showToast(message, type) {
        const normalizedType = normalizeType(type);
        const container = document.getElementById('alert-container');

        if (!container) {
            window.alert(message);
            return;
        }

        const toastId = `app-toast-${Date.now()}`;
        const toastHtml = `
            <div id="${toastId}" class="toast app-toast app-toast-${normalizedType}" role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="4500">
                <div class="toast-header">
                    <i class="bi ${getIcon(normalizedType)} me-2"></i>
                    <strong class="me-auto">${getTitle(normalizedType)}</strong>
                    <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Đóng"></button>
                </div>
                <div class="toast-body">
                    ${message}
                </div>
            </div>
        `;

        container.insertAdjacentHTML('beforeend', toastHtml);

        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement);
        toast.show();

        toastElement.addEventListener('hidden.bs.toast', function () {
            toastElement.remove();
        });
    }

    function confirm(message, options) {
        const modalElement = document.getElementById('appConfirmModal');
        const messageElement = document.getElementById('appConfirmModalMessage');
        const okButton = document.getElementById('appConfirmOkBtn');
        const cancelButton = document.getElementById('appConfirmCancelBtn');

        if (!modalElement || !messageElement || !okButton || !cancelButton) {
            return Promise.resolve(window.confirm(message));
        }

        const settings = options || {};
        messageElement.textContent = message;
        okButton.textContent = settings.okText || 'Đồng ý';
        cancelButton.textContent = settings.cancelText || 'Hủy';

        const modal = bootstrap.Modal.getOrCreateInstance(modalElement);

        return new Promise(function (resolve) {
            function cleanup(result) {
                okButton.removeEventListener('click', onOk);
                modalElement.removeEventListener('hidden.bs.modal', onHidden);
                resolve(result);
            }

            function onOk() {
                modal.hide();
                cleanup(true);
            }

            function onHidden() {
                cleanup(false);
            }

            okButton.addEventListener('click', onOk);
            modalElement.addEventListener('hidden.bs.modal', onHidden, { once: true });
            modal.show();
        });
    }

    return {
        ShowSuccess: function (message) {
            showToast(message, 'success');
        },
        ShowError: function (message) {
            showToast(message, 'error');
        },
        ShowWarning: function (message) {
            showToast(message, 'warning');
        },
        ShowInfo: function (message) {
            showToast(message, 'info');
        },
        Confirm: confirm,
        Show: showToast
    };
})();

window.AppAlert = AppAlert;

function renderCartOffcanvasError() {
    $('#cartOffcanvasBody').html(`
        <div class="cart-offcanvas-empty">
            <i class="bi bi-exclamation-circle"></i>
            <h6>Không thể tải giỏ hàng</h6>
            <p>Đường truyền đang có vấn đề. Vui lòng thử lại.</p>
            <button type="button" class="btn btn-outline-dark w-100" id="retry-cart-offcanvas">
                Tải lại
            </button>
        </div>
    `);
}

function loadCartOffcanvas(showAfterLoad) {
    const cartOffcanvasElement = document.getElementById('cartOffcanvas');
    if (!cartOffcanvasElement) {
        return;
    }

    const cartOffcanvas = bootstrap.Offcanvas.getOrCreateInstance(cartOffcanvasElement);
    $('#cartOffcanvasBody').load('/Cart/GetCartOffcanvas', function (_response, status) {
        if (status !== 'success') {
            renderCartOffcanvasError();
        }

        if (showAfterLoad) {
            cartOffcanvas.show();
        }
    });
}

/**
 * Gửi yêu cầu AJAX để thêm một sản phẩm vào giỏ hàng.
 * Sau khi thành công, cập nhật icon giỏ hàng và hiển thị offcanvas.
 * @param {number} variantId ID của biến thể sản phẩm.
 * @param {number} quantity Số lượng sản phẩm.
 */
function addToCartAjax(variantId, quantity, actionType = 'add-to-cart') {
    if (!variantId || quantity < 1) {
        AppAlert.ShowWarning("Vui lòng chọn đầy đủ thông tin sản phẩm và số lượng hợp lệ.");
        return;
    }

    $.ajax({
        url: '/Cart/AddToCart',
        type: 'POST',
        data: { variantId: variantId, quantity: quantity },
        success: function (response) {
            if (response.success) {
                AppAlert.ShowSuccess(response.message);
                $('#cart-count').text(response.cartCount);

                if (actionType === 'buy-now') {
                    window.location.href = '/Order/Checkout';
                } else {
                    const quickViewModalElement = document.getElementById('quickViewModal');
                    const modalInstance = quickViewModalElement ? bootstrap.Modal.getInstance(quickViewModalElement) : null;

                    if (modalInstance) {
                        modalInstance.hide();
                    }

                    loadCartOffcanvas(true);
                }
            } else {
                AppAlert.ShowError(response.message);
            }
        },
        error: function () {
            AppAlert.ShowError('Đã xảy ra lỗi kết nối.');
        }
    });
}

function buyNowAjax(variantId, quantity) {
    if (!variantId || quantity < 1) {
        AppAlert.ShowWarning("Vui lòng chọn đầy đủ thông tin sản phẩm.");
        return;
    }

    $.ajax({
        url: '/Cart/BuyNow',
        type: 'POST',
        data: { variantId: variantId, quantity: quantity },
        success: function (response) {
            if (response.success) {
                window.location.href = response.redirectUrl;
            } else {
                AppAlert.ShowError(response.message);
            }
        },
        error: function () {
            AppAlert.ShowError('Đã xảy ra lỗi. Vui lòng thử lại.');
        }
    });
}

/**
 * Hiển thị một thông báo Bootstrap Toast và tự động xóa sau một khoảng thời gian.
 * @param {string} message Nội dung thông báo.
 * @param {string} type Loại thông báo: success, danger/error, warning, info.
 */
function showAlert(message, type = 'success') {
    AppAlert.Show(message, type);
}

// ====================================================================================
// == PHẠM VI LOCAL: Code chỉ chạy sau khi toàn bộ trang đã được tải xong           ==
// ====================================================================================
$(document).ready(function () {
    console.log("site.js đã được tải và thực thi!");

    const quickViewModalElement = document.getElementById('quickViewModal');
    const quickViewModal = quickViewModalElement ? bootstrap.Modal.getOrCreateInstance(quickViewModalElement) : null;

    let modalSelectedVariant = null;

    $(document).on('click', '#retry-cart-offcanvas', function () {
        loadCartOffcanvas(false);
    });

    $(document).on('click', '.js-offcanvas-remove', function () {
        const button = $(this);
        const variantId = Number.parseInt(button.data('variant-id'), 10);
        button.prop('disabled', true);

        $.post('/Cart/RemoveFromCart', { variantId: variantId })
            .done(function (response) {
                if (!response.success) {
                    AppAlert.ShowError(response.message || 'Không thể xóa sản phẩm.');
                    button.prop('disabled', false);
                    return;
                }

                $('#cart-count').text(response.cartCount);
                loadCartOffcanvas(false);
            })
            .fail(function () {
                AppAlert.ShowError('Không thể cập nhật giỏ hàng. Vui lòng thử lại.');
                button.prop('disabled', false);
            });
    });

    $(document).on('click', '.quick-view-btn', function (e) {
        e.preventDefault();
        e.stopPropagation();

        const actionType = $(this).data('action-type');
        const productId = $(this).data('product-id');

        modalSelectedVariant = null;
        const btn = $('#modal-add-to-cart-btn');
        btn.prop('disabled', true);
        btn.data('action-type', actionType);

        if (actionType === 'buy-now') {
            btn.html('<i class="bi bi-lightning-fill"></i> Mua ngay');
        } else {
            btn.html('<i class="bi bi-cart-plus"></i> Thêm vào giỏ hàng');
        }

        showQuickViewSkeleton();

        if (quickViewModal) {
            quickViewModal.show();
        }

        $.get('/Cart/GetProductDetails', { productId: productId })
            .done(function (response) {
                if (response.success) {
                    populateQuickViewModal(response.data);
                } else {
                    $('#quickViewModalBody').html(`<p class="text-center text-danger">${response.message}</p>`);
                }
            })
            .fail(function () {
                $('#quickViewModalBody').html('<p class="text-center text-danger">Không thể tải thông tin sản phẩm.</p>');
                AppAlert.ShowError('Không thể tải thông tin sản phẩm.');
            });
    });

    function showQuickViewSkeleton() {
        const skeletonHtml = `
        <div class="row">
            <div class="col-md-6">
                <div class="placeholder-glow"><div class="placeholder" style="height: 400px; width: 100%;"></div></div>
            </div>
            <div class="col-md-6">
                <h3 class="placeholder-glow"><span class="placeholder col-8"></span></h3>
                <h4 class="placeholder-glow"><span class="placeholder col-5"></span></h4>
                <div class="my-3 placeholder-glow"><span class="placeholder col-12"></span></div>
                <div class="my-3 placeholder-glow"><span class="placeholder col-12"></span></div>
            </div>
        </div>`;
        $('#quickViewModalBody').html(skeletonHtml);
    }

    function populateQuickViewModal(product) {
        const imagesHtml = product.images.map(img => `
        <div class="me-2">
            <img src="${img.url}" class="img-thumbnail" style="width: 80px; height: 80px; cursor: pointer;"
                 onclick="$('#modal-main-image').attr('src', this.src);"
                 data-image-id="${img.id}" alt="${product.name}">
        </div>`).join('');

        const uniqueColors = [...new Map(product.variants.map(item => [item.colorId, { id: item.colorId, name: item.colorName }])).values()];
        const colorsHtml = uniqueColors.map(color => `<button class="btn btn-outline-dark me-2 modal-color-option" data-color-id="${color.id}">${color.name}</button>`).join('');

        const uniqueSizes = [...new Map(product.variants.map(item => [item.sizeId, { id: item.sizeId, name: item.sizeName }])).values()];
        const sizesHtml = uniqueSizes.map(size => `<button class="btn btn-outline-dark me-2 modal-size-option" data-size-id="${size.id}" disabled>${size.name}</button>`).join('');

        const modalBodyHtml = `
            <div class="row">
                <div class="col-md-6">
                    <img id="modal-main-image" src="${product.images[0]?.url || '/images/placeholder.png'}" class="img-fluid rounded mb-3" alt="${product.name}">
                    <div class="d-flex">${imagesHtml}</div>
                </div>
                <div class="col-md-6">
                    <h3>${product.name}</h3>
                    <h4 id="modal-product-price" class="text-danger fw-bold my-3">Chọn tùy chọn để xem giá</h4>
                    <div class="mb-3">
                        <h5>Màu sắc:</h5>
                        <div id="modal-color-selector">${colorsHtml}</div>
                    </div>
                    <div class="mb-3">
                        <h5>Kích thước:</h5>
                        <div id="modal-size-selector">${sizesHtml}</div>
                    </div>
                    <div class="mb-4">
                        <label for="modal-quantity" class="form-label">Số lượng:</label>
                        <div class="input-group" style="max-width: 150px;">
                            <button class="btn btn-outline-secondary" type="button" id="modal-btn-minus">-</button>
                            <input type="text" id="modal-quantity" class="form-control text-center" value="1" min="1">
                            <button class="btn btn-outline-secondary" type="button" id="modal-btn-plus">+</button>
                        </div>
                        <small id="modal-stock-info" class="text-muted mt-2 d-block"></small>
                    </div>
                </div>
            </div>`;
        $('#quickViewModalBody').html(modalBodyHtml);
        attachVariantSelectionLogic(product.variants);
    }

    function attachVariantSelectionLogic(variants) {
        let selectedColorId = null;
        let selectedSizeId = null;
        const modalButton = $('#modal-add-to-cart-btn');

        function updateModalUI() {
            modalSelectedVariant = variants.find(v => v.colorId === selectedColorId && v.sizeId === selectedSizeId) || null;
            if (modalSelectedVariant) {
                $('#modal-product-price').text(new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(modalSelectedVariant.price));
                $('#modal-stock-info').text(`Còn lại: ${modalSelectedVariant.stock} sản phẩm`);
                $('#modal-quantity').attr('max', modalSelectedVariant.stock);
                modalButton.prop('disabled', false);
            } else {
                $('#modal-product-price').text('Vui lòng chọn kích thước');
                $('#modal-stock-info').text('');
                modalButton.prop('disabled', true);
            }
        }

        $(document).off('click', '.modal-color-option').on('click', '.modal-color-option', function () {
            selectedColorId = parseInt($(this).data('color-id'));
            selectedSizeId = null;
            $('.modal-color-option').removeClass('btn-dark').addClass('btn-outline-dark');
            $(this).removeClass('btn-outline-dark').addClass('btn-dark');

            $('.modal-size-option').prop('disabled', true).removeClass('btn-dark').addClass('btn-outline-dark');
            const availableSizeIds = variants.filter(v => v.colorId === selectedColorId).map(v => v.sizeId);
            availableSizeIds.forEach(id => $(`.modal-size-option[data-size-id=${id}]`).prop('disabled', false));
            updateModalUI();
        });

        $(document).off('click', '.modal-size-option').on('click', '.modal-size-option', function () {
            if ($(this).is(':disabled')) {
                return;
            }

            selectedSizeId = parseInt($(this).data('size-id'));
            $('.modal-size-option').removeClass('btn-dark').addClass('btn-outline-dark');
            $(this).removeClass('btn-outline-dark').addClass('btn-dark');
            updateModalUI();
        });

        $(document).off('click', '#modal-btn-plus').on('click', '#modal-btn-plus', function () {
            const qtyInput = $('#modal-quantity');
            const currentVal = parseInt(qtyInput.val());
            const maxVal = parseInt(qtyInput.attr('max')) || 1;
            if (currentVal < maxVal) {
                qtyInput.val(currentVal + 1);
            }
        });

        $(document).off('click', '#modal-btn-minus').on('click', '#modal-btn-minus', function () {
            const qtyInput = $('#modal-quantity');
            const currentVal = parseInt(qtyInput.val());
            if (currentVal > 1) {
                qtyInput.val(currentVal - 1);
            }
        });
    }

    $(document).on('click', '#modal-add-to-cart-btn', function () {
        if (!modalSelectedVariant) {
            AppAlert.ShowWarning('Vui lòng chọn đầy đủ màu sắc và kích thước.');
            return;
        }

        const quantity = parseInt($('#modal-quantity').val());
        const actionType = $(this).data('action-type');

        if (actionType === 'buy-now') {
            buyNowAjax(modalSelectedVariant.variantId, quantity);
        } else {
            addToCartAjax(modalSelectedVariant.variantId, quantity);
        }
    });
});
