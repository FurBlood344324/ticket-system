(function () {
    const state = {
        timerHandle: null
    };

    const bindPage = () => {
        const page = document.querySelector(".ticket-detail-page");
        if (!page) {
            return;
        }

        const token = document.querySelector("#ticket-detail-ajax-form input[name='__RequestVerificationToken']")?.value;
        const statusSelect = document.getElementById("ticket-status-select");
        const statusBadge = document.getElementById("ticket-status-badge");
        const sideStatusBadge = document.getElementById("ticket-side-status-badge");
        const startTimerButton = document.getElementById("start-timer-btn");
        const stopTimerButton = document.getElementById("stop-timer-btn");
        const timerState = document.getElementById("timer-state");
        const timerElapsed = document.getElementById("timer-elapsed");
        const timerPanel = document.getElementById("timer-panel");
        let startedAt = timerPanel?.dataset.startedAt ? new Date(timerPanel.dataset.startedAt) : null;

        const postForm = async (url, formData) => {
            const response = await fetch(url, {
                method: "POST",
                headers: {
                    RequestVerificationToken: token
                },
                body: formData
            });

            if (!response.ok) {
                throw new Error("İşlem tamamlanamadı.");
            }

            return response.json();
        };

        const formatElapsed = (date) => {
            if (!date) {
                return "";
            }

            const diffMs = Date.now() - date.getTime();
            const totalMinutes = Math.max(0, Math.floor(diffMs / 60000));
            const hours = Math.floor(totalMinutes / 60);
            const minutes = totalMinutes % 60;
            return hours > 0 ? `${hours} sa ${minutes} dk çalışıyor` : `${minutes} dk çalışıyor`;
        };

        const renderTimer = () => {
            if (!timerState || !timerElapsed || !startTimerButton || !stopTimerButton) {
                return;
            }

            if (startedAt) {
                timerState.textContent = "Sayaç çalışıyor.";
                timerElapsed.textContent = formatElapsed(startedAt);
                startTimerButton.hidden = true;
                stopTimerButton.hidden = false;
            } else {
                timerState.textContent = "Sayaç kapalı.";
                timerElapsed.textContent = "";
                startTimerButton.hidden = false;
                stopTimerButton.hidden = true;
            }
        };

        const startInterval = () => {
            if (state.timerHandle) {
                clearInterval(state.timerHandle);
            }

            state.timerHandle = window.setInterval(renderTimer, 1000);
        };

        if (statusSelect && !statusSelect.dataset.bound) {
            statusSelect.dataset.bound = "true";
            statusSelect.addEventListener("change", async () => {
                const formData = new FormData();
                formData.append("status", statusSelect.value);

                try {
                    const result = await postForm(page.dataset.updateStatusUrl, formData);
                    if (!result.ok) {
                        return;
                    }

                    if (statusBadge) {
                        statusBadge.textContent = result.statusText;
                        statusBadge.className = `badge fs-6 px-3 py-2 ${result.badgeClass}`;
                    }

                    if (sideStatusBadge) {
                        sideStatusBadge.textContent = result.statusText;
                        sideStatusBadge.className = `badge ${result.badgeClass}`;
                    }
                } catch (error) {
                    window.alert(error.message);
                }
            });
        }

        if (startTimerButton && !startTimerButton.dataset.bound) {
            startTimerButton.dataset.bound = "true";
            startTimerButton.addEventListener("click", async () => {
                try {
                    const result = await postForm(page.dataset.startTimerUrl, new FormData());
                    if (!result.ok) {
                        return;
                    }

                    startedAt = new Date(result.startedAt);
                    renderTimer();
                    startInterval();
                } catch (error) {
                    window.alert(error.message);
                }
            });
        }

        if (stopTimerButton && !stopTimerButton.dataset.bound) {
            stopTimerButton.dataset.bound = "true";
            stopTimerButton.addEventListener("click", async () => {
                try {
                    const result = await postForm(page.dataset.stopTimerUrl, new FormData());
                    if (!result.ok) {
                        window.alert(result.message || "Sayaç durdurulamadı.");
                        return;
                    }

                    startedAt = null;
                    renderTimer();
                    if (window.ticketRealtime?.refreshTicketPage) {
                        await window.ticketRealtime.refreshTicketPage();
                    }
                } catch (error) {
                    window.alert(error.message);
                }
            });
        }

        renderTimer();
        if (startedAt) {
            startInterval();
        }
    };

    window.ticketDetailPage = {
        rebind: bindPage
    };

    bindPage();
})();
