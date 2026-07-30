(function (window, document, $) {
    'use strict';

    function escapeHtml(value) {
        return String(value ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#039;');
    }

    function getRequestVerificationToken() {
        return document.querySelector(
            '#global-antiforgery-form input[name="__RequestVerificationToken"], '
            + 'input[name="__RequestVerificationToken"]')?.value ?? '';
    }

    function withAntiforgery(data) {
        return {
            ...(data || {}),
            __RequestVerificationToken: getRequestVerificationToken()
        };
    }

    const AppAlert = (function () {
        function normalizeType(type) {
            return type === 'danger' ? 'error' : (type || 'success');
        }

        function getIcon(type) {
            return {
                success: 'bi-check-circle-fill',
                error: 'bi-exclamation-triangle-fill',
                warning: 'bi-exclamation-circle-fill',
                info: 'bi-info-circle-fill'
            }[normalizeType(type)] || 'bi-info-circle-fill';
        }

        function getTitle(type) {
            return {
                success: 'Thành công',
                error: 'Có lỗi xảy ra',
                warning: 'Lưu ý',
                info: 'Thông báo'
            }[normalizeType(type)] || 'Thông báo';
        }

        function showToast(message, type) {
            const container = document.getElementById('alert-container');
            if (!container || !window.bootstrap) {
                window.alert(message);
                return;
            }

            const normalizedType = normalizeType(type);
            const toast = document.createElement('div');
            toast.className = `toast app-toast app-toast-${normalizedType}`;
            toast.setAttribute('role', 'alert');
            toast.setAttribute('aria-live', 'assertive');
            toast.setAttribute('aria-atomic', 'true');
            toast.dataset.bsDelay = '4500';

            const header = document.createElement('div');
            header.className = 'toast-header';
            header.innerHTML = `<i class="bi ${getIcon(normalizedType)} me-2" aria-hidden="true"></i>`;

            const title = document.createElement('strong');
            title.className = 'me-auto';
            title.textContent = getTitle(normalizedType);
            header.appendChild(title);

            const close = document.createElement('button');
            close.type = 'button';
            close.className = 'btn-close';
            close.dataset.bsDismiss = 'toast';
            close.setAttribute('aria-label', 'Đóng');
            header.appendChild(close);

            const body = document.createElement('div');
            body.className = 'toast-body';
            body.textContent = message;
            toast.append(header, body);
            container.appendChild(toast);

            const instance = window.bootstrap.Toast.getOrCreateInstance(toast);
            toast.addEventListener('hidden.bs.toast', () => toast.remove(), { once: true });
            instance.show();
        }

        function confirmAction(message, options) {
            const modalElement = document.getElementById('appConfirmModal');
            const messageElement = document.getElementById('appConfirmModalMessage');
            const okButton = document.getElementById('appConfirmOkBtn');
            const cancelButton = document.getElementById('appConfirmCancelBtn');

            if (!modalElement || !messageElement || !okButton || !cancelButton || !window.bootstrap) {
                return Promise.resolve(window.confirm(message));
            }

            const settings = options || {};
            messageElement.textContent = message;
            okButton.textContent = settings.okText || 'Đồng ý';
            cancelButton.textContent = settings.cancelText || 'Hủy';
            const modal = window.bootstrap.Modal.getOrCreateInstance(modalElement);

            return new Promise(function (resolve) {
                let resolved = false;

                function finish(result) {
                    if (resolved) {
                        return;
                    }
                    resolved = true;
                    okButton.removeEventListener('click', onOk);
                    modalElement.removeEventListener('hidden.bs.modal', onHidden);
                    resolve(result);
                }

                function onOk() {
                    finish(true);
                    modal.hide();
                }

                function onHidden() {
                    finish(false);
                }

                okButton.addEventListener('click', onOk);
                modalElement.addEventListener('hidden.bs.modal', onHidden, { once: true });
                modal.show();
            });
        }

        return {
            ShowSuccess: message => showToast(message, 'success'),
            ShowError: message => showToast(message, 'error'),
            ShowWarning: message => showToast(message, 'warning'),
            ShowInfo: message => showToast(message, 'info'),
            Show: showToast,
            Confirm: confirmAction
        };
    })();

    window.AppAlert = AppAlert;
    window.AppUtilities = {
        escapeHtml,
        getRequestVerificationToken,
        withAntiforgery
    };
    window.showAlert = (message, type = 'success') => AppAlert.Show(message, type);

    function renderCartOffcanvasError() {
        $('#cartOffcanvasBody').html(`
            <div class="cart-offcanvas-empty">
                <i class="bi bi-exclamation-circle" aria-hidden="true"></i>
                <h6>Không thể tải giỏ hàng</h6>
                <p>Vui lòng kiểm tra kết nối và thử lại.</p>
                <button type="button" class="btn btn-outline-dark w-100" id="retry-cart-offcanvas">
                    Tải lại
                </button>
            </div>`);
    }

    function loadCartOffcanvas(showAfterLoad) {
        const element = document.getElementById('cartOffcanvas');
        if (!element || !window.bootstrap) {
            return;
        }

        const offcanvas = window.bootstrap.Offcanvas.getOrCreateInstance(element);
        $('#cartOffcanvasBody').load('/Cart/GetCartOffcanvas', function (_response, status) {
            if (status !== 'success') {
                renderCartOffcanvasError();
            }
            if (showAfterLoad) {
                offcanvas.show();
            }
        });
    }

    window.loadCartOffcanvas = loadCartOffcanvas;

    function postCart(url, data) {
        return $.ajax({
            url,
            type: 'POST',
            data: withAntiforgery(data)
        });
    }

    window.addToCartAjax = function (variantId, quantity, actionType = 'add-to-cart') {
        if (!variantId || quantity < 1) {
            AppAlert.ShowWarning('Vui lòng chọn đầy đủ biến thể và số lượng.');
            return;
        }

        postCart('/Cart/AddToCart', { variantId, quantity })
            .done(function (response) {
                if (!response.success) {
                    AppAlert.ShowError(response.message || 'Không thể thêm vào giỏ hàng.');
                    return;
                }

                $('#cart-count').text(response.cartCount);
                AppAlert.ShowSuccess(response.message);

                if (actionType === 'buy-now') {
                    window.location.href = '/Order/Checkout';
                    return;
                }

                const quickViewElement = document.getElementById('quickViewModal');
                window.bootstrap?.Modal.getInstance(quickViewElement)?.hide();
                loadCartOffcanvas(true);
            })
            .fail(function (xhr) {
                AppAlert.ShowError(xhr.responseJSON?.message || 'Không thể kết nối để cập nhật giỏ hàng.');
            });
    };

    window.buyNowAjax = function (variantId, quantity) {
        if (!variantId || quantity < 1) {
            AppAlert.ShowWarning('Vui lòng chọn đầy đủ biến thể và số lượng.');
            return;
        }

        postCart('/Cart/BuyNow', { variantId, quantity })
            .done(function (response) {
                if (response.success && response.redirectUrl) {
                    window.location.href = response.redirectUrl;
                    return;
                }
                AppAlert.ShowError(response.message || 'Không thể mua ngay sản phẩm này.');
            })
            .fail(function (xhr) {
                AppAlert.ShowError(xhr.responseJSON?.message || 'Không thể xử lý yêu cầu mua ngay.');
            });
    };

    function updateCartCount() {
        $.getJSON('/Cart/GetCartItemCount')
            .done(response => {
                if (response.success) {
                    $('#cart-count').text(response.count);
                }
            });
    }

    function initializeHeader() {
        const header = document.getElementById('mainHeader');
        if (header) {
            const updateHeader = () => header.classList.toggle('scrolled', window.scrollY > 20);
            updateHeader();
            window.addEventListener('scroll', updateHeader, { passive: true });
        }

        window.addEventListener('pageshow', event => {
            if (event.persisted) {
                updateCartCount();
            }
        });

        $('#cartOffcanvas').on('show.bs.offcanvas', () => loadCartOffcanvas(false));
        $(document).on('click', '#retry-cart-offcanvas', () => loadCartOffcanvas(false));

        $(document).on('click', '#offcanvasNavbar a', function () {
            const menu = document.getElementById('offcanvasNavbar');
            const instance = menu ? window.bootstrap?.Offcanvas.getInstance(menu) : null;
            instance?.hide();
        });
    }

    function initializeCartOffcanvas() {
        $(document).on('click', '.js-offcanvas-remove', async function () {
            const button = $(this);
            const confirmed = await AppAlert.Confirm('Xóa sản phẩm này khỏi giỏ hàng?', {
                okText: 'Xóa sản phẩm'
            });
            if (!confirmed) {
                return;
            }

            button.prop('disabled', true);
            postCart('/Cart/RemoveFromCart', { variantId: Number(button.data('variant-id')) })
                .done(function (response) {
                    if (!response.success) {
                        AppAlert.ShowError(response.message || 'Không thể xóa sản phẩm.');
                        button.prop('disabled', false);
                        return;
                    }
                    $('#cart-count').text(response.cartCount);
                    loadCartOffcanvas(false);
                })
                .fail(function (xhr) {
                    AppAlert.ShowError(xhr.responseJSON?.message || 'Không thể cập nhật giỏ hàng.');
                    button.prop('disabled', false);
                });
        });

        $(document).on('click', '.js-offcanvas-quantity', function () {
            const button = $(this);
            button.prop('disabled', true);
            postCart('/Cart/UpdateCart', {
                variantId: Number(button.data('variant-id')),
                quantity: Number(button.data('quantity'))
            })
                .done(function (response) {
                    if (!response.success) {
                        AppAlert.ShowError(response.message || 'Không thể cập nhật số lượng.');
                        return;
                    }
                    $('#cart-count').text(response.cartCount);
                    loadCartOffcanvas(false);
                })
                .fail(xhr => AppAlert.ShowError(
                    xhr.responseJSON?.message || 'Không thể cập nhật số lượng.'))
                .always(() => button.prop('disabled', false));
        });
    }

    function initializeCartPage() {
        if (!document.querySelector('.cart-page')) {
            return;
        }

        function setBusy(variantId, busy) {
            $(`.cart-line-item[data-variant-id="${variantId}"] button, `
                + `.cart-line-item[data-variant-id="${variantId}"] input`)
                .prop('disabled', busy);
        }

        function updateSummary(response) {
            $('#cart-subtotal, #cart-total').text(`${response.cartTotal} ₫`);
            $('#cart-count, #cart-page-count').text(response.cartCount);
        }

        function updateQuantity(variantId, quantity, input) {
            setBusy(variantId, true);
            postCart('/Cart/UpdateCart', { variantId, quantity })
                .done(function (response) {
                    if (!response.success) {
                        AppAlert.ShowError(response.message || 'Không thể cập nhật số lượng.');
                        window.location.reload();
                        return;
                    }
                    input.val(quantity);
                    $(`#item-total-${variantId}`).text(`${response.itemTotal} ₫`);
                    updateSummary(response);
                    loadCartOffcanvas(false);
                })
                .fail(function (xhr) {
                    AppAlert.ShowError(xhr.responseJSON?.message || 'Không thể cập nhật số lượng.');
                    window.location.reload();
                })
                .always(() => setBusy(variantId, false));
        }

        $(document).on('click', '.js-cart-increase, .js-cart-decrease', function () {
            const variantId = Number($(this).data('id'));
            const input = $(`.quantity-input[data-id="${variantId}"]`);
            const current = Number(input.val()) || 1;
            const maximum = Number(input.attr('max')) || 1;
            const next = $(this).hasClass('js-cart-increase')
                ? Math.min(current + 1, maximum)
                : Math.max(current - 1, 1);
            if (next !== current) {
                updateQuantity(variantId, next, input);
            }
        });

        $(document).on('change', '.quantity-input', function () {
            const input = $(this);
            const maximum = Number(input.attr('max')) || 1;
            const quantity = Math.min(Math.max(Number(input.val()) || 1, 1), maximum);
            updateQuantity(Number(input.data('id')), quantity, input);
        });

        $(document).on('click', '.js-cart-remove', async function () {
            const variantId = Number($(this).data('id'));
            const confirmed = await AppAlert.Confirm('Xóa sản phẩm này khỏi giỏ hàng?', {
                okText: 'Xóa sản phẩm'
            });
            if (!confirmed) {
                return;
            }

            setBusy(variantId, true);
            postCart('/Cart/RemoveFromCart', { variantId })
                .done(function (response) {
                    if (!response.success) {
                        AppAlert.ShowError(response.message || 'Không thể xóa sản phẩm.');
                        return;
                    }

                    updateSummary(response);
                    $(`#row-${variantId}`).fadeOut(180, function () {
                        $(this).remove();
                        if (response.cartCount === 0) {
                            window.location.reload();
                        }
                    });
                    loadCartOffcanvas(false);
                })
                .fail(xhr => AppAlert.ShowError(
                    xhr.responseJSON?.message || 'Không thể xóa sản phẩm.'))
                .always(() => setBusy(variantId, false));
        });
    }

    function initializeQuickView() {
        const modalElement = document.getElementById('quickViewModal');
        if (!modalElement || !window.bootstrap) {
            return;
        }

        const modal = window.bootstrap.Modal.getOrCreateInstance(modalElement);
        let selectedVariant = null;
        let productVariants = [];

        function renderSkeleton() {
            $('#quickViewModalBody').html(`
                <div class="quick-view-loading" aria-live="polite">
                    <span class="spinner-border" aria-hidden="true"></span>
                    <p>Đang tải thông tin sản phẩm...</p>
                </div>`);
        }

        function updateSelection(selectedColorId, selectedSizeId) {
            selectedVariant = productVariants.find(variant =>
                Number(variant.colorId) === selectedColorId
                && Number(variant.sizeId) === selectedSizeId) || null;

            const actionButton = $('#modal-add-to-cart-btn');
            if (selectedVariant && selectedVariant.stock > 0) {
                $('#modal-product-price').text(
                    new Intl.NumberFormat('vi-VN', {
                        style: 'currency',
                        currency: 'VND'
                    }).format(selectedVariant.price));
                $('#modal-stock-info').text(`Còn ${selectedVariant.stock} sản phẩm`);
                $('#modal-quantity').attr('max', selectedVariant.stock).val(1);
                actionButton.prop('disabled', false);
            } else {
                $('#modal-product-price').text('Chọn màu và kích thước');
                $('#modal-stock-info').text('');
                actionButton.prop('disabled', true);
            }
        }

        function populate(product) {
            const images = Array.isArray(product.images) ? product.images : [];
            productVariants = Array.isArray(product.variants) ? product.variants : [];
            const colors = [...new Map(productVariants
                .filter(item => item.colorId !== null)
                .map(item => [item.colorId, { id: item.colorId, name: item.colorName }])).values()];
            const sizes = [...new Map(productVariants
                .filter(item => item.sizeId !== null)
                .map(item => [item.sizeId, { id: item.sizeId, name: item.sizeName }])).values()];
            const fallbackImage = '/images/products/aothun1_den_boxy.jpg';

            const thumbnails = images.map((image, index) => `
                <button type="button" class="modal-thumbnail ${index === 0 ? 'active' : ''}"
                        data-image-src="${escapeHtml(image.url)}"
                        aria-label="Xem ảnh ${index + 1}">
                    <img src="${escapeHtml(image.url)}" alt="${escapeHtml(product.name)}" />
                </button>`).join('');
            const colorButtons = colors.map(color => `
                <button type="button" class="btn btn-outline-dark modal-color-option"
                        data-color-id="${Number(color.id)}">${escapeHtml(color.name)}</button>`).join('');
            const sizeButtons = sizes.map(size => `
                <button type="button" class="btn btn-outline-dark modal-size-option"
                        data-size-id="${Number(size.id)}" disabled>${escapeHtml(size.name)}</button>`).join('');

            $('#quickViewModalBody').html(`
                <div class="quick-view-layout">
                    <div class="quick-view-gallery">
                        <img id="modal-main-image" src="${escapeHtml(images[0]?.url || fallbackImage)}"
                             alt="${escapeHtml(product.name)}" />
                        <div class="quick-view-thumbnails">${thumbnails}</div>
                    </div>
                    <div class="quick-view-details">
                        <p class="section-kicker">QUICK SELECT</p>
                        <h3>${escapeHtml(product.name)}</h3>
                        <strong id="modal-product-price">Chọn màu và kích thước</strong>
                        <fieldset>
                            <legend>Màu sắc</legend>
                            <div class="quick-view-options">${colorButtons}</div>
                        </fieldset>
                        <fieldset>
                            <legend>Kích thước</legend>
                            <div class="quick-view-options">${sizeButtons}</div>
                        </fieldset>
                        <div class="quick-view-quantity">
                            <label for="modal-quantity">Số lượng</label>
                            <input type="number" id="modal-quantity" value="1" min="1" inputmode="numeric" />
                        </div>
                        <small id="modal-stock-info" aria-live="polite"></small>
                    </div>
                </div>`);

            let selectedColorId = null;
            let selectedSizeId = null;

            $('.modal-color-option').on('click', function () {
                selectedColorId = Number($(this).data('color-id'));
                selectedSizeId = null;
                $('.modal-color-option').removeClass('btn-dark').addClass('btn-outline-dark');
                $(this).removeClass('btn-outline-dark').addClass('btn-dark');
                $('.modal-size-option').prop('disabled', true)
                    .removeClass('btn-dark').addClass('btn-outline-dark');
                productVariants
                    .filter(variant => Number(variant.colorId) === selectedColorId && variant.stock > 0)
                    .forEach(variant => {
                        $(`.modal-size-option[data-size-id="${Number(variant.sizeId)}"]`)
                            .prop('disabled', false);
                    });
                updateSelection(selectedColorId, selectedSizeId);
            });

            $('.modal-size-option').on('click', function () {
                if ($(this).prop('disabled')) {
                    return;
                }
                selectedSizeId = Number($(this).data('size-id'));
                $('.modal-size-option').removeClass('btn-dark').addClass('btn-outline-dark');
                $(this).removeClass('btn-outline-dark').addClass('btn-dark');
                updateSelection(selectedColorId, selectedSizeId);
            });
        }

        $(document).on('click', '.modal-thumbnail', function () {
            $('.modal-thumbnail').removeClass('active');
            $(this).addClass('active');
            $('#modal-main-image').attr('src', $(this).data('image-src'));
        });

        $(document).on('click', '.quick-view-btn', function (event) {
            event.preventDefault();
            selectedVariant = null;
            const actionType = $(this).data('action-type') || 'add-to-cart';
            $('#modal-add-to-cart-btn')
                .data('action-type', actionType)
                .prop('disabled', true)
                .html('<i class="bi bi-bag-plus" aria-hidden="true"></i> Thêm vào giỏ');
            renderSkeleton();
            modal.show();

            $.getJSON('/Cart/GetProductDetails', { productId: Number($(this).data('product-id')) })
                .done(response => {
                    if (response.success) {
                        populate(response.data);
                    } else {
                        $('#quickViewModalBody').html(
                            `<div class="catalog-empty compact"><p>${escapeHtml(response.message)}</p></div>`);
                    }
                })
                .fail(function () {
                    $('#quickViewModalBody').html(
                        '<div class="catalog-empty compact"><p>Không thể tải thông tin sản phẩm.</p></div>');
                });
        });

        $(document).on('click', '#modal-add-to-cart-btn', function () {
            if (!selectedVariant) {
                AppAlert.ShowWarning('Vui lòng chọn màu sắc và kích thước.');
                return;
            }
            const maximum = Number(selectedVariant.stock) || 1;
            const quantity = Math.min(
                Math.max(Number($('#modal-quantity').val()) || 1, 1),
                maximum);
            window.addToCartAjax(selectedVariant.variantId, quantity, $(this).data('action-type'));
        });
    }

    function initializeCheckout() {
        const form = document.getElementById('checkout-form');
        if (!form) {
            return;
        }

        const summary = $('#checkout-validation-summary');
        const submitButton = $('#place-order-button');

        function showValidation(message) {
            summary.removeClass('d-none').text(message);
            summary[0]?.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }

        $(form).on('submit', function (event) {
            if (!$('input[name="addressId"]:checked').val()) {
                event.preventDefault();
                showValidation('Vui lòng chọn hoặc thêm địa chỉ giao hàng trước khi đặt hàng.');
                return;
            }
            if (!$('input[name="paymentMethodId"]:checked').val()) {
                event.preventDefault();
                showValidation('Vui lòng chọn phương thức thanh toán.');
                return;
            }
            if (submitButton.prop('disabled')) {
                event.preventDefault();
                return;
            }

            summary.addClass('d-none').empty();
            submitButton.prop('disabled', true).attr('aria-busy', 'true')
                .html('<span class="spinner-border spinner-border-sm" aria-hidden="true"></span>'
                    + '<span>Đang đặt hàng...</span>');
        });

        const addressModalElement = document.getElementById('addAddressModal');
        const addressModal = addressModalElement
            ? window.bootstrap?.Modal.getOrCreateInstance(addressModalElement)
            : null;

        $('#save-address-btn').on('click', function () {
            const addressForm = document.getElementById('add-address-form');
            const button = $(this);
            if (!addressForm?.checkValidity()) {
                addressForm?.reportValidity();
                return;
            }

            button.prop('disabled', true)
                .html('<span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Đang lưu');
            $.ajax({
                url: '/Account/AddAddressAjax',
                type: 'POST',
                data: $(addressForm).serialize()
            })
                .done(function (response) {
                    if (!response.success) {
                        AppAlert.ShowError(response.message || 'Không thể lưu địa chỉ.');
                        return;
                    }

                    addressModal?.hide();
                    addressForm.reset();
                    AppAlert.ShowSuccess(response.message);
                    $(document).trigger('address:added', [response.newAddress]);
                })
                .fail(xhr => AppAlert.ShowError(
                    xhr.responseJSON?.message || 'Không thể lưu địa chỉ. Vui lòng kiểm tra lại.'))
                .always(() => button.prop('disabled', false).text('Lưu địa chỉ'));
        });

        $(document).on('address:added', function (_event, address) {
            const label = document.createElement('label');
            label.className = 'checkout-choice-card';

            const radio = document.createElement('input');
            radio.type = 'radio';
            radio.name = 'addressId';
            radio.value = String(address.iddiaChi);
            radio.required = true;
            radio.checked = true;

            const indicator = document.createElement('span');
            indicator.className = 'checkout-choice-indicator';
            indicator.setAttribute('aria-hidden', 'true');

            const content = document.createElement('span');
            const name = document.createElement('strong');
            name.textContent = address.tenNguoiNhan;
            const phone = document.createElement('small');
            phone.textContent = address.soDienThoai;
            const fullAddress = document.createElement('em');
            fullAddress.textContent = address.fullAddress;
            content.append(name, phone, fullAddress);
            label.append(radio, indicator, content);

            const container = document.getElementById('address-list-container');
            container?.querySelector('.checkout-empty-address')?.remove();
            container?.insertBefore(label, container.querySelector('.checkout-add-address'));
            summary.addClass('d-none').empty();
        });

        $('#apply-coupon-btn').on('click', function () {
            const button = $(this);
            const code = String($('#coupon-code').val() || '').trim();
            if (!code) {
                $('#coupon-message').removeClass('text-success').addClass('text-danger')
                    .text('Vui lòng nhập mã giảm giá.');
                return;
            }

            button.prop('disabled', true).text('Đang kiểm tra');
            $.ajax({
                url: '/Order/ApplyCoupon',
                type: 'POST',
                data: withAntiforgery({
                    couponCode: code,
                    cartType: $('input[name="cartType"]').val()
                })
            })
                .done(function (response) {
                    const message = $('#coupon-message');
                    if (response.success) {
                        message.removeClass('text-danger').addClass('text-success')
                            .text(response.message);
                        $('#discount-amount').text(`- ${response.discount} ₫`);
                        $('#total-amount').text(`${response.newTotal} ₫`);
                    } else {
                        message.removeClass('text-success').addClass('text-danger')
                            .text(response.message);
                        $('#discount-amount').text('- 0 ₫');
                    }
                })
                .fail(xhr => $('#coupon-message')
                    .removeClass('text-success').addClass('text-danger')
                    .text(xhr.responseJSON?.message || 'Không thể kiểm tra mã giảm giá.'))
                .always(() => button.prop('disabled', false).text('Áp dụng'));
        });
    }

    function initializeChat() {
        const box = $('#chat-box');
        const input = $('#chat-input');
        const sendButton = $('#chat-send-btn');
        const content = $('#chat-content');

        if (!box.length) {
            return;
        }

        function setOpen(open) {
            box.toggleClass('d-none', !open);
            $('#chat-toggle-btn').attr('aria-expanded', open.toString());
            if (open) {
                input.trigger('focus');
            }
        }

        function scrollToBottom() {
            const element = content[0];
            if (element) {
                element.scrollTop = element.scrollHeight;
            }
        }

        function appendMessage(message, own) {
            const row = $('<div>').addClass(
                `chat-row ${own ? 'is-user' : 'is-assistant'}`);
            $('<div>').addClass('chat-message').text(message).appendTo(row);
            content.append(row);
            scrollToBottom();
        }

        function sendMessage() {
            const message = String(input.val() || '').trim();
            if (!message || sendButton.prop('disabled')) {
                return;
            }

            appendMessage(message, true);
            input.val('');
            sendButton.prop('disabled', true);
            const loading = $('<div>').addClass('chat-row is-assistant chat-loading')
                .append($('<div>').addClass('chat-message').text('Đang suy nghĩ...'));
            content.append(loading);
            scrollToBottom();

            $.ajax({
                url: '/Chat/GetResponse',
                type: 'POST',
                data: withAntiforgery({ userMessage: message })
            })
                .done(function (response) {
                    loading.remove();
                    if (response.success) {
                        appendMessage(response.response, false);
                    } else {
                        AppAlert.ShowError(response.response || 'Trợ lý chưa thể phản hồi.');
                    }
                })
                .fail(xhr => {
                    loading.remove();
                    AppAlert.ShowError(xhr.responseJSON?.response || 'Không thể kết nối trợ lý OWE.');
                })
                .always(() => sendButton.prop('disabled', false));
        }

        $('#chat-toggle-btn').on('click', () => setOpen(box.hasClass('d-none')));
        $('#chat-close-btn').on('click', () => setOpen(false));
        sendButton.on('click', sendMessage);
        input.on('keydown', function (event) {
            if (event.key === 'Enter' && !event.shiftKey) {
                event.preventDefault();
                sendMessage();
            }
        });
        $(document).on('click', '.quick-reply-btn', function () {
            input.val($(this).text());
            sendMessage();
        });
    }

    function initializeModalFocusSafety() {
        document.addEventListener('hide.bs.modal', function (event) {
            if (event.target.contains(document.activeElement)) {
                document.activeElement.blur();
            }
        });
        document.addEventListener('hide.bs.offcanvas', function (event) {
            if (event.target.contains(document.activeElement)) {
                document.activeElement.blur();
            }
        });
    }

    $(function () {
        initializeHeader();
        initializeCartOffcanvas();
        initializeCartPage();
        initializeQuickView();
        initializeCheckout();
        initializeChat();
        initializeModalFocusSafety();
    });
})(window, document, window.jQuery);
