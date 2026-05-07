 document.addEventListener('DOMContentLoaded', () => {
    const cityButtonsContainer = document.querySelector('.city-buttons');
    const barbershopsContent = document.querySelector('.barbershops-content');

    let cities = [];
    let activeCityId = null;

    // Load cities from API
    async function loadCities() {
        try {
            const response = await fetch('http://localhost:5140/api/Cities');
            if (!response.ok) throw new Error('Failed to load cities');
            cities = await response.json();
            renderCityButtons();
        } catch (error) {
            console.error('Error loading cities:', error);
            barbershopsContent.innerHTML = '<p>Error loading cities. Please try again later.</p>';
        }
    }

    // Render city buttons
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
        // Select first city by default
        if (cities.length > 0) {
            selectCity(cities[0].id);
        }
    }

    // Select a city and load barbershops
    async function selectCity(cityId) {
        // Update active button
        document.querySelectorAll('.city-btn').forEach(btn => {
            btn.classList.toggle('active', btn.dataset.cityId == cityId);
        });

        activeCityId = cityId;

        // Load barbershops
        try {
            barbershopsContent.innerHTML = '<p>Loading barbershops...</p>';
            const response = await fetch(`http://localhost:5140/api/barbershops?cityId=${cityId}`);
            if (!response.ok) throw new Error('Failed to load barbershops');
            const barbershops = await response.json();
            renderBarbershops(barbershops);
        } catch (error) {
            console.error('Error loading barbershops:', error);
            barbershopsContent.innerHTML = '<p>Error loading barbershops. Please try again later.</p>';
        }
    }

    // Render barbershops
    function renderBarbershops(barbershops) {
        if (barbershops.length === 0) {
            barbershopsContent.innerHTML = '<p>No barbershops found in this city.</p>';
            return;
        }

        barbershopsContent.innerHTML = '';
        barbershops.forEach(shop => {
            const shopCard = document.createElement('div');
            shopCard.className = 'shop-card';

            const img = document.createElement('img');
            img.src = '#'; // Use existing image
            img.alt = 'Barber';

            const shopInfo = document.createElement('div');
            shopInfo.className = 'shop-info';

            const h3 = document.createElement('h3');
            h3.textContent = shop.name;

            const p = document.createElement('p');
            p.textContent = shop.address; // Or a description if available

            shopInfo.appendChild(h3);
            shopInfo.appendChild(p);

            shopCard.appendChild(img);
            shopCard.appendChild(shopInfo);

            barbershopsContent.appendChild(shopCard);
        });
    }

    // Initialize
    loadCities();
});
