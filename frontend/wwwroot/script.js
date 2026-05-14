document.addEventListener('DOMContentLoaded', () => {

    // ===== SCROLL HEADER =====
    window.addEventListener('scroll', () => {
        const header = document.querySelector('header');

        if (window.scrollY > 50) {
            header.style.background = '#0d0d0d';
            header.style.boxShadow = '0 5px 20px rgba(0,0,0,0.3)';
        } else {
            header.style.background = 'rgba(17,17,17,0.95)';
            header.style.boxShadow = 'none';
        }
    });

    // ===== SMOOTH SCROLL =====
    const links = document.querySelectorAll('.nav-links a');

    links.forEach(link => {
        const href = link.getAttribute('href');
        if (!href || !href.startsWith('#')) {
            return;
        }

        link.addEventListener('click', function(e) {
            e.preventDefault();

            const targetId = this.getAttribute('href');
            const targetSection = document.querySelector(targetId);

            if (targetSection) {
                targetSection.scrollIntoView({
                    behavior: 'smooth'
                });
            }
        });
    });

});
