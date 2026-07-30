(function () {
    "use strict";

    function getToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    function setLoading(button, loading) {
        if (!button) {
            return;
        }

        if (loading) {
            button.dataset.originalHtml = button.innerHTML;
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>Đang xử lý';
            return;
        }

        button.disabled = false;
        if (button.dataset.originalHtml) {
            button.innerHTML = button.dataset.originalHtml;
            delete button.dataset.originalHtml;
        }
    }

    function showToast(type, message) {
        const container = document.querySelector(".admin-toast-container");
        if (!container) {
            window.alert(message);
            return;
        }

        const toast = document.createElement("div");
        toast.className = `toast align-items-center border-0 text-white ${type === "success" ? "bg-success" : "bg-danger"}`;
        toast.setAttribute("role", "status");
        toast.setAttribute("aria-live", "polite");

        const row = document.createElement("div");
        row.className = "d-flex";
        const body = document.createElement("div");
        body.className = "toast-body";
        body.textContent = message;
        const close = document.createElement("button");
        close.type = "button";
        close.className = "btn-close btn-close-white me-2 m-auto";
        close.setAttribute("data-bs-dismiss", "toast");
        close.setAttribute("aria-label", "Đóng");
        row.append(body, close);
        toast.append(row);
        container.append(toast);

        const instance = bootstrap.Toast.getOrCreateInstance(toast, { delay: 3500 });
        toast.addEventListener("hidden.bs.toast", function () {
            toast.remove();
        });
        instance.show();
    }

    async function parseResponse(response) {
        const contentType = response.headers.get("content-type") || "";
        if (!contentType.includes("application/json")) {
            if (!response.ok) {
                throw new Error(`Yêu cầu thất bại (${response.status}).`);
            }
            return {};
        }

        const payload = await response.json();
        if (!response.ok) {
            throw new Error(payload.message || `Yêu cầu thất bại (${response.status}).`);
        }
        return payload;
    }

    async function postForm(url, values, button) {
        const body = values instanceof FormData ? values : new FormData();
        if (!(values instanceof FormData)) {
            Object.entries(values || {}).forEach(([key, value]) => {
                if (value !== undefined && value !== null) {
                    body.append(key, value);
                }
            });
        }

        if (!body.has("__RequestVerificationToken")) {
            body.append("__RequestVerificationToken", getToken());
        }

        setLoading(button, true);
        try {
            const response = await fetch(url, {
                method: "POST",
                body,
                credentials: "same-origin",
                headers: {
                    "RequestVerificationToken": getToken()
                }
            });
            return await parseResponse(response);
        } finally {
            setLoading(button, false);
        }
    }

    const sidebarToggle = document.querySelector("[data-admin-sidebar-toggle]");
    const sidebarBackdrop = document.querySelector("[data-admin-sidebar-close]");
    const closeSidebar = function () {
        document.body.classList.remove("admin-sidebar-open");
        sidebarToggle?.setAttribute("aria-expanded", "false");
    };

    sidebarToggle?.addEventListener("click", function () {
        const isOpen = document.body.classList.toggle("admin-sidebar-open");
        sidebarToggle.setAttribute("aria-expanded", String(isOpen));
    });
    sidebarBackdrop?.addEventListener("click", closeSidebar);
    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            closeSidebar();
        }
    });

    window.AdminUI = {
        getToken,
        parseResponse,
        postForm,
        setLoading,
        showToast
    };
})();
