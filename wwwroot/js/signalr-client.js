(function () {
    const shell = document.getElementById("app-shell");
    const toastContainer = document.getElementById("toast-container");
    const toastTemplate = document.getElementById("toast-template");
    const notificationBadge = document.getElementById("notification-badge");
    const page = document.querySelector(".ticket-detail-page");

    if (!shell || !window.signalR) {
        return;
    }

    const state = {
        unreadCount: Number.parseInt(shell.dataset.unreadCount || "0", 10) || 0,
        ticketId: Number.parseInt(page?.dataset.ticketId || "0", 10) || null,
        ticketJoined: false
    };

    const setUnreadCount = (value) => {
        state.unreadCount = Math.max(0, value || 0);
        if (!notificationBadge) {
            return;
        }

        notificationBadge.textContent = String(state.unreadCount);
        notificationBadge.classList.toggle("d-none", state.unreadCount === 0);
    };

    const showToast = (title, message, options) => {
        if (!toastContainer || !toastTemplate) {
            return;
        }

        const toast = toastTemplate.content.firstElementChild.cloneNode(true);
        toast.querySelector(".app-toast__title").textContent = title;
        toast.querySelector(".app-toast__message").textContent = message;

        if (options?.variant === "warning") {
            toast.classList.add("app-toast--warning");
        }

        if (options?.url) {
            toast.style.cursor = "pointer";
            toast.addEventListener("click", () => {
                window.location.href = options.url;
            });
        }

        const closeButton = toast.querySelector(".app-toast__close");
        const removeToast = () => toast.remove();
        closeButton?.addEventListener("click", removeToast);

        toastContainer.appendChild(toast);
        window.setTimeout(removeToast, options?.duration ?? 6000);
    };

    const replaceSection = (selector, nextDocument) => {
        const current = document.querySelector(selector);
        const next = nextDocument.querySelector(selector);
        if (!current || !next) {
            return;
        }

        current.replaceWith(next);
    };

    const refreshTicketPage = async () => {
        if (!page || !page.dataset.detailsUrl) {
            return;
        }

        const response = await fetch(page.dataset.detailsUrl, {
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            }
        });

        if (!response.ok) {
            return;
        }

        const html = await response.text();
        const parser = new DOMParser();
        const nextDocument = parser.parseFromString(html, "text/html");

        [
            "#ticket-live-title",
            "#ticket-live-meta",
            "#ticket-status-badge",
            "#ticket-live-description",
            "#ticket-live-timeline",
            "#ticket-live-side-status",
            "#ticket-live-sla",
            "#ticket-live-attachments",
            "#tracked-time-total"
        ].forEach((selector) => replaceSection(selector, nextDocument));

        window.ticketDetailPage?.rebind();
    };

    const notificationConnection = shell.dataset.isAuthenticated === "true"
        ? new signalR.HubConnectionBuilder()
            .withUrl("/hubs/notifications")
            .withAutomaticReconnect()
            .build()
        : null;

    const ticketConnection = state.ticketId
        ? new signalR.HubConnectionBuilder()
            .withUrl("/hubs/tickets")
            .withAutomaticReconnect()
            .build()
        : null;

    if (notificationConnection) {
        notificationConnection.on("NotificationReceived", (payload) => {
            showToast(payload.title, payload.message, { url: payload.url });
        });

        notificationConnection.on("NotificationCountUpdated", (count) => {
            setUnreadCount(count);
        });

        notificationConnection.on("NotificationMarkedAsRead", (payload) => {
            setUnreadCount(payload.unreadCount);
        });

        notificationConnection.on("SlaWarning", (payload) => {
            showToast("SLA Uyarisi", payload.summary, { variant: "warning", url: payload.url });
        });

        notificationConnection.on("MentionNotification", (payload) => {
            showToast("Etiketleme", payload.message, { url: payload.url });
        });

        notificationConnection.start().catch(() => {
        });
    }

    if (ticketConnection) {
        ticketConnection.on("TicketUpdated", async (payload) => {
            await refreshTicketPage();
            showToast(`Talep #${payload.ticketId}`, payload.summary, { url: `/Tickets/Details/${payload.ticketId}` });
        });

        ticketConnection.on("TicketAssigned", async (payload) => {
            await refreshTicketPage();
            showToast(`Talep #${payload.ticketId}`, payload.summary, { url: `/Tickets/Details/${payload.ticketId}` });
        });

        ticketConnection.on("NewReply", async (payload) => {
            await refreshTicketPage();
            showToast(`Yeni Yanit: #${payload.ticketId}`, payload.summary, { url: `/Tickets/Details/${payload.ticketId}` });
        });

        ticketConnection.on("StatusChanged", async (payload) => {
            await refreshTicketPage();
            showToast(`Durum Degisti: #${payload.ticketId}`, `${payload.oldStatus} -> ${payload.newStatus}`, {
                url: `/Tickets/Details/${payload.ticketId}`
            });
        });

        ticketConnection.start()
            .then(() => ticketConnection.invoke("JoinTicketGroup", state.ticketId))
            .then(() => {
                state.ticketJoined = true;
            })
            .catch(() => {
            });

        window.addEventListener("beforeunload", () => {
            if (state.ticketJoined) {
                ticketConnection.invoke("LeaveTicketGroup", state.ticketId).catch(() => {
                });
            }
        });
    }

    setUnreadCount(state.unreadCount);

    window.ticketRealtime = {
        refreshTicketPage,
        setUnreadCount,
        showToast
    };
})();
