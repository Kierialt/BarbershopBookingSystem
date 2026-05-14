document.addEventListener('DOMContentLoaded', () => {
    const cityButtonsContainer = document.querySelector('.city-buttons');
    const shopList = document.getElementById('shopList');
    const reservationPanel = document.getElementById('reservationPanel');
    const reservationCard = document.getElementById('reservationCard');

    let cities = [];
    let activeCityId = null;
    let selectedBarbershopId = null;

    async function loadCities() {
        try {
            const response = await fetch('http://localhost:5140/api/Cities');
            if (!response.ok) throw new Error('Failed to load cities');
            cities = await response.json();
            renderCityButtons();
        } catch (error) {
            console.error('Error loading cities:', error);
            cityButtonsContainer.innerHTML = '<p class="loader">Błąd ładowania miast. Spróbuj ponownie później.</p>';
        }
    }

    function renderCityButtons() {
        cityButtonsContainer.innerHTML = '';
        cities.forEach(city => {
            const button = document.createElement('button');
            button.className = 'city-btn';
            button.textContent = city.name;
            button.dataset.cityId = city.id;
            button.addEventListener('click', () => selectCity(city.id));
            cityButtonsContainer.appendChild(button);
        });
        if (cities.length > 0) {
            selectCity(cities[0].id);
        }
    }

    async function selectCity(cityId) {
        document.querySelectorAll('.city-btn').forEach(btn => {
            btn.classList.toggle('active', btn.dataset.cityId == cityId);
        });
        activeCityId = cityId;
        reservationCard.innerHTML = '<p class="empty-state">Kliknij salon opcji po lewej stronie, aby przejść do rezerwacji.</p>';
        try {
            shopList.innerHTML = '<p class="loader">Ładowanie salonów...</p>';
            const response = await fetch(`http://localhost:5140/api/barbershops?cityId=${cityId}`);
            if (!response.ok) throw new Error('Failed to load barbershops');
            const barbershops = await response.json();
            renderBarbershops(barbershops);
        } catch (error) {
            console.error('Error loading barbershops:', error);
            shopList.innerHTML = '<p class="loader">Błąd ładowania salonów. Spróbuj ponownie później.</p>';
        }
    }

    function renderBarbershops(barbershops) {
        if (barbershops.length === 0) {
            shopList.innerHTML = '<p>No barbershops found in this city.</p>';
            return;
        }

        shopList.innerHTML = '';
        barbershops.forEach(shop => {
            const shopCard = document.createElement('div');
            shopCard.className = 'shop-card';
            shopCard.tabIndex = 0;

            const img = document.createElement('img');
            img.src = 'https://via.placeholder.com/340x240.png?text=Barber';
            img.alt = 'Barber';

            const shopInfo = document.createElement('div');
            shopInfo.className = 'shop-info';

            const h3 = document.createElement('h3');
            h3.textContent = shop.name;

            const p = document.createElement('p');
            p.textContent = shop.address || 'Brak adresu';

            shopInfo.appendChild(h3);
            shopInfo.appendChild(p);
            shopCard.appendChild(img);
            shopCard.appendChild(shopInfo);
            shopCard.addEventListener('click', () => selectBarbershop(shop.id, shopCard));
            shopCard.addEventListener('keydown', event => {
                if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    selectBarbershop(shop.id, shopCard);
                }
            });

            shopList.appendChild(shopCard);
        });
    }

    function selectBarbershop(barbershopId, element) {
        selectedBarbershopId = barbershopId;
        document.querySelectorAll('.shop-card').forEach(card => card.classList.toggle('active', card === element));
        loadBarbershopDetails(barbershopId);
    }

    async function loadBarbershopDetails(barbershopId) {
        if (!reservationCard) return;
        reservationCard.innerHTML = '<p class="loader">Ładowanie danych salonu...</p>';
        try {
            const response = await fetch(`http://localhost:5140/api/Barbershops/${barbershopId}`);
            if (!response.ok) throw new Error('Failed to load barbershop details');
            const detail = await response.json();
            renderReservationForm(detail);
        } catch (error) {
            console.error('Error loading detail:', error);
            reservationCard.innerHTML = '<p class="loader">Nie udało się pobrać szczegółów salonu.</p>';
        }
    }

    function renderReservationForm(detail) {
        if (!reservationCard) return;
        const serviceOptions = detail.services.map(service =>
            `<option value="${service.id}">${service.name} — ${service.price} zł (${service.durationMinutes} min)</option>`
        ).join('');

        const barberOptions = detail.barbers.map(barber =>
            `<option value="${barber.id}">${barber.displayName}</option>`
        ).join('');

        reservationCard.innerHTML = `
            <div class="reservation-info">
                <span><strong>Salon:</strong> ${detail.name}</span>
                <span><strong>Adres:</strong> ${detail.address || 'Brak'}</span>
                <span><strong>Telefon:</strong> ${detail.phone || 'Brak'}</span>
            </div>
            <form class="reservation-form" id="reservationForm">
                <div>
                    <label for="serviceSelect">Usługa</label>
                    <select id="serviceSelect" required>
                        <option value="">Wybierz usługę</option>
                        ${serviceOptions}
                    </select>
                </div>
                <div>
                    <label for="barberSelect">Barber</label>
                    <select id="barberSelect" required>
                        <option value="">Wybierz barbera</option>
                        ${barberOptions}
                    </select>
                </div>
                <div>
                    <label for="datePicker">Data</label>
                    <input id="datePicker" type="date" required />
                </div>
                <div>
                    <label for="timeSelect">Godzina</label>
                    <select id="timeSelect" required>
                        <option value="">Wybierz godzinę</option>
                        <option value="09:00">09:00</option>
                        <option value="10:00">10:00</option>
                        <option value="11:00">11:00</option>
                        <option value="12:00">12:00</option>
                        <option value="13:00">13:00</option>
                        <option value="14:00">14:00</option>
                        <option value="15:00">15:00</option>
                        <option value="16:00">16:00</option>
                        <option value="17:00">17:00</option>
                    </select>
                </div>
                <button type="submit">Zarezerwuj teraz</button>
                <div class="message-box" id="reservationMessage"></div>
            </form>
        `;

        const reservationForm = document.getElementById('reservationForm');
        reservationForm.addEventListener('submit', async (event) => {
            event.preventDefault();
            await submitReservation(detail.id);
        });

        const dateField = document.getElementById('datePicker');
        const today = new Date();
        dateField.min = today.toISOString().split('T')[0];
        dateField.value = dateField.min;

        reservationPanel?.scrollIntoView({ behavior: 'smooth' });
    }

    async function submitReservation(barbershopId) {
        const messageBox = document.getElementById('reservationMessage');
        if (!messageBox) return;

        const serviceSelect = document.getElementById('serviceSelect');
        const barberSelect = document.getElementById('barberSelect');
        const datePicker = document.getElementById('datePicker');
        const timeSelect = document.getElementById('timeSelect');

        messageBox.textContent = '';
        messageBox.style.color = '#d4d4d4';

        if (!serviceSelect?.value || !barberSelect?.value || !datePicker?.value || !timeSelect?.value) {
            messageBox.textContent = 'Wypełnij wszystkie pola formularza.';
            return;
        }

        const token = localStorage.getItem('authToken');
        if (!token) {
            messageBox.textContent = 'Musisz być zalogowany, aby zarezerwować wizytę.';
            return;
        }

        const startUtc = new Date(`${datePicker.value}T${timeSelect.value}:00`).toISOString();

        try {
            const response = await fetch(`http://localhost:5140/api/Bookings`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    Authorization: `Bearer ${token}`
                },
                body: JSON.stringify({
                    barberId: Number(barberSelect.value),
                    serviceId: Number(serviceSelect.value),
                    startUtc
                })
            });

            const result = await response.json();
            if (!response.ok) {
                messageBox.textContent = result.error || 'Błąd rezerwacji.';
                return;
            }

            messageBox.style.color = '#a1ff72';
            messageBox.textContent = 'Rezerwacja została potwierdzona!';
        } catch (error) {
            console.error(error);
            messageBox.textContent = 'Błąd serwera. Spróbuj ponownie.';
        }
    }

    loadCities();
});
