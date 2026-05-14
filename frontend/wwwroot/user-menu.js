document.addEventListener('DOMContentLoaded', () => {
    const userBtn = document.querySelector('.user-btn');
    const userMenu = document.querySelector('.user-menu');
    const dropdown = document.querySelector('.dropdown-menu');

    if (!userBtn || !userMenu || !dropdown) {
        return;
    }

    const isLoggedIn = Boolean(localStorage.getItem('authToken'));

    dropdown.innerHTML = isLoggedIn
        ? `
            <a href="profile.html">Profil</a>
            <button type="button" class="dropdown-logout">Wyloguj się</button>
        `
        : `
            <a href="register.html">Rejestracja</a>
            <a href="login.html">Logowanie</a>
        `;

    const logoutButton = dropdown.querySelector('.dropdown-logout');
    logoutButton?.addEventListener('click', () => {
        localStorage.removeItem('authToken');
        localStorage.removeItem('authUser');
        window.location.href = 'index.html';
    });

    userBtn.addEventListener('click', (event) => {
        event.stopPropagation();
        userMenu.classList.toggle('active');
    });

    dropdown.addEventListener('click', (event) => {
        event.stopPropagation();
    });

    document.addEventListener('click', () => {
        userMenu.classList.remove('active');
    });
});
