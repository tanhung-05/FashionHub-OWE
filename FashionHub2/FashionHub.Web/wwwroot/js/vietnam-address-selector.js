(function () {
    const apiBaseUrl = 'https://provinces.open-api.vn/api/v1';

    function normalizeName(value) {
        return (value || '').trim().toLocaleLowerCase('vi-VN');
    }

    function appendOptions(select, items, placeholder) {
        select.innerHTML = '';
        select.add(new Option(placeholder, ''));
        items.forEach(function (item) {
            select.add(new Option(item.name, item.code));
        });
    }

    async function getJson(url) {
        const response = await fetch(url, {
            headers: { Accept: 'application/json' }
        });

        if (!response.ok) {
            throw new Error(`Address API returned ${response.status}`);
        }

        return response.json();
    }

    function initializeAddressSelector(root) {
        if (root.dataset.initialized === 'true') {
            return;
        }

        root.dataset.initialized = 'true';

        const provinceSelect = root.querySelector('[data-vn-province]');
        const districtSelect = root.querySelector('[data-vn-district]');
        const wardSelect = root.querySelector('[data-vn-ward]');
        const provinceName = root.querySelector('[data-vn-province-name]');
        const districtName = root.querySelector('[data-vn-district-name]');
        const wardName = root.querySelector('[data-vn-ward-name]');
        const selectFields = root.querySelector('[data-vn-select-fields]');
        const manualFields = root.querySelector('[data-vn-manual-fields]');
        const manualProvince = root.querySelector('[data-vn-manual-province]');
        const manualDistrict = root.querySelector('[data-vn-manual-district]');
        const manualWard = root.querySelector('[data-vn-manual-ward]');
        const status = root.querySelector('[data-vn-address-status]');

        const currentProvince = root.dataset.currentProvince || '';
        const currentDistrict = root.dataset.currentDistrict || '';
        const currentWard = root.dataset.currentWard || '';

        function setStatus(message, type) {
            status.textContent = message || '';
            status.className = `address-selector-status ${type ? `is-${type}` : ''}`;
        }

        function setManualMode(message) {
            selectFields.classList.add('d-none');
            manualFields.classList.remove('d-none');

            [provinceSelect, districtSelect, wardSelect].forEach(function (select) {
                select.disabled = true;
                select.required = false;
            });
            [manualProvince, manualDistrict, manualWard].forEach(function (input) {
                input.required = true;
            });

            setStatus(message || 'Bạn có thể nhập địa chỉ thủ công.', 'warning');
        }

        function syncManualValues() {
            provinceName.value = manualProvince.value.trim();
            districtName.value = manualDistrict.value.trim();
            wardName.value = manualWard.value.trim();
        }

        [manualProvince, manualDistrict, manualWard].forEach(function (input) {
            input.addEventListener('input', syncManualValues);
        });

        async function loadWards(districtCode, selectedName) {
            appendOptions(wardSelect, [], 'Đang tải phường/xã...');
            wardSelect.disabled = true;
            wardName.value = '';

            const district = await getJson(`${apiBaseUrl}/d/${districtCode}?depth=2`);
            const wards = district.wards || [];
            appendOptions(wardSelect, wards, 'Chọn phường/xã');
            wardSelect.disabled = false;

            const selectedWard = wards.find(function (ward) {
                return normalizeName(ward.name) === normalizeName(selectedName);
            });
            if (selectedWard) {
                wardSelect.value = selectedWard.code;
                wardName.value = selectedWard.name;
            }
        }

        async function loadDistricts(provinceCode, selectedDistrictName, selectedWardName) {
            appendOptions(districtSelect, [], 'Đang tải quận/huyện...');
            appendOptions(wardSelect, [], 'Chọn phường/xã');
            districtSelect.disabled = true;
            wardSelect.disabled = true;
            districtName.value = '';
            wardName.value = '';

            const province = await getJson(`${apiBaseUrl}/p/${provinceCode}?depth=2`);
            const districts = province.districts || [];
            appendOptions(districtSelect, districts, 'Chọn quận/huyện');
            districtSelect.disabled = false;

            const selectedDistrict = districts.find(function (district) {
                return normalizeName(district.name) === normalizeName(selectedDistrictName);
            });
            if (selectedDistrict) {
                districtSelect.value = selectedDistrict.code;
                districtName.value = selectedDistrict.name;
                await loadWards(selectedDistrict.code, selectedWardName);
            }
        }

        async function loadProvinces() {
            try {
                setStatus('Đang tải dữ liệu địa chỉ...', 'loading');
                const provinces = await getJson(`${apiBaseUrl}/p/`);
                appendOptions(provinceSelect, provinces, 'Chọn tỉnh/thành phố');

                const selectedProvince = provinces.find(function (province) {
                    return normalizeName(province.name) === normalizeName(currentProvince);
                });
                if (selectedProvince) {
                    provinceSelect.value = selectedProvince.code;
                    provinceName.value = selectedProvince.name;
                    await loadDistricts(
                        selectedProvince.code,
                        currentDistrict,
                        currentWard);
                }

                setStatus('Dữ liệu hành chính được cung cấp bởi Vietnam Provinces Open API.', 'ready');
            } catch (_error) {
                setManualMode('Không thể tải API địa chỉ. Vui lòng nhập địa chỉ thủ công.');
            }
        }

        provinceSelect.addEventListener('change', async function () {
            const selectedOption = provinceSelect.options[provinceSelect.selectedIndex];
            provinceName.value = provinceSelect.value ? selectedOption.text : '';

            if (!provinceSelect.value) {
                appendOptions(districtSelect, [], 'Chọn quận/huyện');
                appendOptions(wardSelect, [], 'Chọn phường/xã');
                districtSelect.disabled = true;
                wardSelect.disabled = true;
                districtName.value = '';
                wardName.value = '';
                return;
            }

            try {
                await loadDistricts(provinceSelect.value, '', '');
            } catch (_error) {
                setManualMode('Không thể tải quận/huyện. Vui lòng nhập địa chỉ thủ công.');
            }
        });

        districtSelect.addEventListener('change', async function () {
            const selectedOption = districtSelect.options[districtSelect.selectedIndex];
            districtName.value = districtSelect.value ? selectedOption.text : '';

            if (!districtSelect.value) {
                appendOptions(wardSelect, [], 'Chọn phường/xã');
                wardSelect.disabled = true;
                wardName.value = '';
                return;
            }

            try {
                await loadWards(districtSelect.value, '');
            } catch (_error) {
                setManualMode('Không thể tải phường/xã. Vui lòng nhập địa chỉ thủ công.');
            }
        });

        wardSelect.addEventListener('change', function () {
            const selectedOption = wardSelect.options[wardSelect.selectedIndex];
            wardName.value = wardSelect.value ? selectedOption.text : '';
        });

        loadProvinces();
    }

    function initializeAll() {
        document.querySelectorAll('[data-vn-address-selector]')
            .forEach(initializeAddressSelector);
    }

    window.VietnamAddressSelector = {
        initialize: initializeAddressSelector,
        initializeAll: initializeAll
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeAll);
    } else {
        initializeAll();
    }
})();
