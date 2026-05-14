document.addEventListener('DOMContentLoaded', () => {
    const token = localStorage.getItem('authToken');
    const user = readUser();
    const profileName = document.getElementById('profileName');
    const profileEmail = document.getElementById('profileEmail');
    const reservationsList = document.getElementById('reservationsList');
    const logoutButton = document.getElementById('logoutButton');
    const activeBookingsCount = document.getElementById('activeBookingsCount');
    const base = window.BARBERSHOP_API || window.location.origin;

    if (!token) {
        window.location.href = 'login.html';
        return;
    }

    profileName.textContent = user.fullName || user.email || 'Uzytkownik';
    profileEmail.textContent = user.email || 'Brak adresu e-mail';

    logoutButton?.addEventListener('click', () => {
        localStorage.removeItem('authToken');
        localStorage.removeItem('authUser');
        window.location.href = 'index.html';
    });

    reservationsList.addEventListener('click', handleReservationClick);
    reservationsList.addEventListener('submit', handleRescheduleSubmit);

    loadReservations();

    function readUser() {
        try {
            return JSON.parse(localStorage.getItem('authUser')) || {};
        } catch {
            return {};
        }
    }

    async function loadReservations() {
        reservationsList.innerHTML = '<p class="empty-state">Ladowanie rezerwacji...</p>';

        try {
            const response = await fetch(base + '/api/Bookings/me', {
                headers: {
                    Authorization: `Bearer ${token}`
                }
            });

            if (response.status === 401) {
                localStorage.removeItem('authToken');
                localStorage.removeItem('authUser');
                window.location.href = 'login.html';
                return;
            }

            if (!response.ok) {
                throw new Error('Failed to load bookings');
            }

            const bookings = await response.json();
            renderReservations(bookings);
        } catch (error) {
            console.error(error);
            reservationsList.innerHTML = '<p class="empty-state">Nie udalo sie pobrac rezerwacji.</p>';
        }
    }

    function renderReservations(bookings) {
        const now = new Date();
        const activeBookings = bookings.filter(booking =>
            booking.status !== 'Cancelled' && new Date(booking.startUtc) > now
        );
        activeBookingsCount.textContent = activeBookings.length;

        if (!bookings.length) {
            reservationsList.innerHTML = '<p class="empty-state">Nie masz jeszcze zadnych rezerwacji.</p>';
            return;
        }

        reservationsList.innerHTML = bookings.map(renderBooking).join('');
    }

    function renderBooking(booking) {
        const isCancelled = booking.status === 'Cancelled';
        const start = new Date(booking.startUtc);
        const end = new Date(booking.endUtc);
        const dateValue = toDateInputValue(start);
        const timeValue = toTimeValue(start);
        const statusClass = isCancelled ? ' cancelled' : '';
        const controls = isCancelled ? '' : `
            <div class="reservation-controls">
                <button type="button" class="booking-action secondary" data-action="toggle-reschedule" data-id="${booking.id}">
                    Zmien termin
                </button>
                <button type="button" class="booking-action danger" data-action="cancel" data-id="${booking.id}">
                    Anuluj wizyte
                </button>
            </div>
            <form class="reschedule-form" data-id="${booking.id}">
                <label>
                    Nowa data
                    <input type="date" name="date" min="${todayValue()}" value="${dateValue}" required>
                </label>
                <label>
                    Nowa godzina
                    <select name="time" required>
                        ${renderTimeOptions(timeValue)}
                    </select>
                </label>
                <button type="submit" class="booking-action">Zapisz zmiane</button>
            </form>
        `;

        return `
            <article class="reservation-item" data-booking-id="${booking.id}">
                <div class="reservation-main">
                    <div>
                        <h3>${escapeHtml(booking.barbershopName)}</h3>
                        <div class="reservation-details">
                            <p><strong>Usluga</strong>${escapeHtml(booking.serviceName)}</p>
                            <p><strong>Barber</strong>${escapeHtml(booking.barberName)}</p>
                            <p><strong>Termin</strong>${formatDate(start)} - ${formatTime(end)}</p>
                        </div>
                    </div>
                    <span class="reservation-status${statusClass}">${translateStatus(booking.status)}</span>
                </div>
                ${controls}
                <div class="booking-message" data-message-for="${booking.id}"></div>
            </article>
        `;
    }

    async function handleReservationClick(event) {
        const button = event.target.closest('[data-action]');
        if (!button) {
            return;
        }

        const bookingId = button.dataset.id;
        const action = button.dataset.action;
        const card = button.closest('.reservation-item');

        if (action === 'toggle-reschedule') {
            card.classList.toggle('show-reschedule');
            return;
        }

        if (action === 'cancel') {
            const confirmed = confirm('Czy na pewno chcesz anulowac te wizyte?');
            if (!confirmed) {
                return;
            }

            await cancelBooking(bookingId);
        }
    }

    async function handleRescheduleSubmit(event) {
        const form = event.target.closest('.reschedule-form');
        if (!form) {
            return;
        }

        event.preventDefault();

        const bookingId = form.dataset.id;
        const formData = new FormData(form);
        const date = formData.get('date');
        const time = formData.get('time');

        await rescheduleBooking(bookingId, date, time);
    }

    async function cancelBooking(bookingId) {
        setBookingMessage(bookingId, 'Anulowanie wizyty...');

        try {
            const response = await fetch(`${base}/api/Bookings/${bookingId}/cancel`, {
                method: 'POST',
                headers: {
                    Authorization: `Bearer ${token}`
                }
            });

            if (!response.ok) {
                const result = await readJson(response);
                setBookingMessage(bookingId, result.error || 'Nie udalo sie anulowac wizyty.', true);
                return;
            }

            await loadReservations();
        } catch (error) {
            console.error(error);
            setBookingMessage(bookingId, 'Blad serwera. Sprobuj ponownie.', true);
        }
    }

    async function rescheduleBooking(bookingId, date, time) {
        setBookingMessage(bookingId, 'Zapisywanie nowego terminu...');

        try {
            const newStartUtc = new Date(`${date}T${time}:00`).toISOString();
            const response = await fetch(`${base}/api/Bookings/${bookingId}/reschedule`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`
                },
                body: JSON.stringify({
                    newDay: date,
                    newStartUtc
                })
            });

            if (!response.ok) {
                const result = await readJson(response);
                setBookingMessage(bookingId, result.error || 'Nie udalo sie zmienic terminu.', true);
                return;
            }

            await loadReservations();
        } catch (error) {
            console.error(error);
            setBookingMessage(bookingId, 'Blad serwera. Sprobuj ponownie.', true);
        }
    }

    function setBookingMessage(bookingId, message, isError = false) {
        const messageBox = reservationsList.querySelector(`[data-message-for="${bookingId}"]`);
        if (!messageBox) {
            return;
        }

        messageBox.textContent = message;
        messageBox.style.color = isError ? '#ff8b8b' : '#a1ff72';
    }

    async function readJson(response) {
        try {
            return await response.json();
        } catch {
            return {};
        }
    }

    function renderTimeOptions(selectedValue) {
        const times = ['09:00', '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00', '17:00'];
        return times.map(time => {
            const selected = time === selectedValue ? ' selected' : '';
            return `<option value="${time}"${selected}>${time}</option>`;
        }).join('');
    }

    function todayValue() {
        return toDateInputValue(new Date());
    }

    function toDateInputValue(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    function toTimeValue(date) {
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        return `${hours}:${minutes}`;
    }

    function formatDate(value) {
        return value.toLocaleString('pl-PL', {
            dateStyle: 'medium',
            timeStyle: 'short'
        });
    }

    function formatTime(value) {
        return value.toLocaleTimeString('pl-PL', {
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    function translateStatus(status) {
        if (status === 'Cancelled') {
            return 'Anulowana';
        }

        if (status === 'Confirmed') {
            return 'Potwierdzona';
        }

        return status;
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#039;');
    }
});
